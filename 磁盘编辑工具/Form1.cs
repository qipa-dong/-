using System;
using System.Windows.Forms;
using System.IO;
using System.Management;
using System.Security.Principal;
using SDcard;
using FileOperation;
namespace 磁盘编辑工具
{
	public partial class Form1 : Form
	{
		private SDUtils cipan = new SDUtils();
		private uint file_size = 0;
		private bool Identity = false;//获取当前是否为管理员权限
		public Form1()
		{
			InitializeComponent();
			//GetLogicalDrivers();
			Get_info();
			comboBox2.SelectedIndex = 0;
			Identity = IsAdministrator();
		}

		//获取当前是否为管理员权限打开
		public bool IsAdministrator()
		{
			WindowsIdentity current = WindowsIdentity.GetCurrent();
			WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
			return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		private void Button2_Click(object sender, EventArgs e)
		{
			file_size = 0;
			if (comboBox2.Text == "读取")
			{
				SaveFileDialog filesave = new SaveFileDialog
				{
					Title = "新建文件",
					Filter = "二进制文件（*.bin,*img）|*.bin;*img|所有文件(*.*)|*.*",
					FilterIndex = 1,//设置默认文件类型显示顺序 
					RestoreDirectory = true,//保存对话框是否记忆上次打开的目录 
					FileName = "读出文件"//设置默认的文件名
				};
				if (filesave.ShowDialog() == DialogResult.OK)
				{
					string[] names = filesave.FileNames;
					textBox4.Text = names[0];
				}

			}
			else if (comboBox2.Text == "写入")
			{
				//打开文件
				OpenFileDialog fileDialog = new OpenFileDialog
				{
					Multiselect = true,
					Title = "请选择文件",
					Filter = "二进制文件（*.bin,*img）|*.bin;*img|所有文件(*.*)|*.*"
				};
				if (fileDialog.ShowDialog() == DialogResult.OK)
				{
					string[] names = fileDialog.FileNames;
					textBox4.Text = names[0];//显示文件名
					file_size = cipan.Filelen(textBox4.Text);
				}
			}
			Log("文件大小为：" + file_size.ToString("N0") + "字节");
		}

		private void Button3_Click(object sender, EventArgs e)
		{
			//写入磁盘
			byte[] WriteByte = new byte[1024 * 1024];
			FileHelper FileBin = new FileHelper();
			

			for (uint i = 0; i < WriteByte.Length; i++)
			{
				WriteByte[i] = 0xFF;
			}
			/* Check ---------------------------------------------------------------------------------------*/
			//打开磁盘
			if (cipan.OpenDisk((comboBox1.Text[0] >= 'A' && comboBox1.Text[0] <= 'Z') ? comboBox1.Text.Substring(0, 2) : comboBox1.Text))
			{
				button3.Enabled = true;
				Log("打开磁盘：" + comboBox1.Text.ToString());
			}
			else
			{
				Log("打开磁盘失败! " + comboBox1.Text);
				return;
			}
			uint disk_Sector_size = cipan.GetSectorLen();
			uint disk_sector_num = cipan.GetSectorNum();
			uint disk_size = disk_Sector_size * disk_sector_num;
			
			if (disk_size > 1024 * 1024 * 1024 || disk_size == 0)
			{
				MessageBox.Show("磁盘数据大小错误", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				cipan.Close();
				return;
			}

			//打开文件
			if (comboBox2.Text != "擦除")
			{
				if (FileBin.OpenFile(textBox4.Text))
				{
					button3.Enabled = true;
					Log("打开文件：" + textBox4.Text.ToString());
				}
				else
				{
					Log("打开文件失败! " + textBox4.Text);
					cipan.Close();
					return;
				}

				if (file_size <= 0)
				{
					if (comboBox2.Text == "写入")
					{
						MessageBox.Show(this, "文件无内容或不存在", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Question);
						FileBin.Close();
						cipan.Close();
						return;
					}
				}
				else if (file_size > 1024 * 1024 * 1024)
				{
					MessageBox.Show("文件数据大于1G", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					FileBin.Close();
					cipan.Close();
					return;
				}

				if (file_size > disk_size)
				{
					MessageBox.Show("文件数据大于磁盘容量", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					FileBin.Close();
					cipan.Close();
					return;
				}

			}
			else
			{
				file_size = disk_size;
			}
			/* Confirm -----------------------------------------------------------------------------------------------*/
			if (MessageBox.Show(this, "确定要执行操作？此操作无法撤销！", "提示信息：", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
			{
				button3.Text = "写入";
				cipan.Close();
				FileBin.Close();
				return;
			}

			/**********************************************************************************************************/
			button3.Text = "写入中";
			button3.Enabled = false;
			Log(comboBox2.Text.ToString() + "中........");

			if (comboBox2.Text == "写入" || comboBox2.Text == "擦除")
			{
				uint complete_num = file_size / (uint)WriteByte.Length;
				uint surplus = file_size % (uint)WriteByte.Length;

				cipan.LockVolume();
				for (uint SurplusLen = 0; SurplusLen < complete_num; SurplusLen ++)
				{
					if (comboBox2.Text == "写入")
					{
						//读取数据
						FileBin.BinRead( SurplusLen * (uint)WriteByte.Length, WriteByte.Length, ref WriteByte);
					}
					//将数据写入流
					cipan.WriteSector(ref WriteByte, SurplusLen * (uint)WriteByte.Length, WriteByte.Length);

					//将当前流中的数据写入磁盘
					cipan.Refresh();

					//更新进度条
					progressBar1.Value = (int)(SurplusLen * 100 / complete_num);
				}
				if (surplus > 0)//存在不足1M的数据
				{
					FileBin.BinRead(complete_num * (uint)WriteByte.Length, (int)surplus, ref WriteByte);
					cipan.WriteSector(ref WriteByte, complete_num * (uint)WriteByte.Length, (int)surplus);
				}
				cipan.DismountVolume();
				cipan.UnlockVolume();
				cipan.Close();
				progressBar1.Value = 100;
				button3.Text = "执行";
				button3.Enabled = true;
				Log("文件" + comboBox2.Text.ToString() + "完成!");
			}
			else if (comboBox2.Text == "读取")
			{
				uint complete_num = disk_size / (uint)WriteByte.Length;
				uint surplus = disk_size % (uint)WriteByte.Length;

				cipan.LockVolume();
				/*读取数据*/
				for (uint SurplusLen = 0; SurplusLen < complete_num; SurplusLen ++)
				{
					Application.DoEvents();
					//读取磁盘数据
					cipan.ReadSector(SurplusLen * (uint)WriteByte.Length, WriteByte.Length, ref WriteByte);

					//写入文件
					FileBin.Write(ref WriteByte, SurplusLen * (uint)WriteByte.Length, WriteByte.Length);

					FileBin.Refresh();
					//更新进度条
					progressBar1.Value = (int)(SurplusLen * 100 / complete_num);
				}
				if (surplus > 0)//存在不足1M的数据
				{
					cipan.ReadSector(complete_num * (uint)WriteByte.Length, (int)surplus, ref WriteByte);
					FileBin.Write(ref WriteByte, complete_num * (uint)WriteByte.Length, (int)surplus);
				}
				//更新进度条
				progressBar1.Value = 100;

				cipan.UnlockVolume();
				cipan.Close();
				FileBin.Close();
				button3.Text = "执行";
				button3.Enabled = true;
				Log("磁盘读取完成!");
			}
			//Get_info();//关闭磁盘时刷新磁盘信息
		}

		//打印log信息
		private void Log(string data)
		{
			if (data == "")
			{
				textBox1.Text = "";
			}
			else
			{
				textBox1.Text += data + "\r\n";
			}
		}

		//获取磁盘信息
		private void Get_info()
		{
			long lsum = 0,ldr =0;
			Log("");
			Log("磁盘信息");
			comboBox1.Items.Clear();
			
			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				lsum = drive.TotalSize / 1024 / 1024;//MB,磁盘总大小
				ldr = drive.TotalFreeSpace / 1024 / 1024;//剩余大小

				if(lsum <= 0x2000)
				{
					if (drive.IsReady)//磁盘已就绪
						comboBox1.Items.Add(drive.Name.Substring(0, drive.Name.Length - 1) + drive.VolumeLabel);
					else
						comboBox1.Items.Add(drive.Name.Substring(0, drive.Name.Length - 1) + "未知");

					//判断是否是固定磁盘  
					if (drive.DriveType == DriveType.Fixed)
					{
						Log("固定磁盘：" + drive.Name + ": 总空间=" + lsum.ToString("n") + " MB" + " 剩余空间= " + ldr.ToString("n") + " MB");
					}
					else if (drive.DriveType == DriveType.Removable)
					{
						Log("移动磁盘：" + drive.Name + ": 总空间=" + lsum.ToString("n") + " MB" + " 剩余空间= " + ldr.ToString("n") + " MB");
					}
				}
			}

			ManagementObjectSearcher mydisks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
			foreach (ManagementObject mydisk in mydisks.Get())
			{
				comboBox1.Items.Add(mydisk["DeviceID"].ToString());
			}

			if (comboBox1.Items.Count > 0)
			{
				comboBox1.SelectedIndex = 0;//设置默认值
			}
		}

		private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			long disk_size = 0;
			//打开磁盘
			if (cipan.OpenDisk((comboBox1.Text[0] >= 'A' && comboBox1.Text[0] <= 'Z') ? comboBox1.Text.Substring(0, 2) : comboBox1.Text))
			{
				button3.Enabled = true;
			}
			else
			{
				Log("打开磁盘失败! " + comboBox1.Text);
				return;
			}

			cipan.Close();
			
			disk_size = cipan.GetSectorLen() * (long)cipan.GetSectorNum();
			if (disk_size / 1024 / 1024 > 0)
			{
				label2.Text = (disk_size / 1024).ToString("N0") + " KB";
			}
			else if (disk_size / 1024 > 0)
			{
				label2.Text = disk_size.ToString("N0") + " B";
			}
			else if (disk_size > 0)
			{
				label2.Text = disk_size.ToString() + " B";
			}
			else
			{
				label2.Text = "未知";
			}
		}

	}

	
}
