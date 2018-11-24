using System;
using System.Windows.Forms;
using System.IO;
using System.Management;
using SDcard;
using FileOperation;
namespace 磁盘编辑工具
{
	public partial class Form1 : Form
	{
		private SDUtils cipan = new SDUtils();
		private string tergetDisk = "";//目标磁盘
		public Form1()
		{
			InitializeComponent();
			//GetLogicalDrivers();
			Get_info();
			comboBox2.SelectedIndex = 0;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (comboBox2.Text == "读取")
			{
				SaveFileDialog filesave = new SaveFileDialog();
				filesave.Title = "新建文件";
				filesave.Filter = "二进制文件（*.bin,*img）|*.bin;*img|所有文件(*.*)|*.*";
				filesave.FilterIndex = 1;//设置默认文件类型显示顺序 
				filesave.RestoreDirectory = true;//保存对话框是否记忆上次打开的目录 
				filesave.FileName = "读出文件";//设置默认的文件名
				if (filesave.ShowDialog() == DialogResult.OK)
				{
					string[] names = filesave.FileNames;
					textBox4.Text = names[0];
				}

			}
			else if (comboBox1.Text == "写入")
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
					foreach (string file in names)
					{
						MessageBox.Show("已选择文件:" + file, "选择文件提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

					}
				}
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			//写入磁盘
			byte[] WriteByte = new byte[512];
			FileHelper FileBin = new FileHelper();

			for (uint i = 0; i < 512; i++)
			{
				WriteByte[i] = 0xFF;
			}

			//打开磁盘
			if (cipan.OpenDisk((comboBox1.Text[0] >= 'A' && comboBox1.Text[0] <= 'Z') ? comboBox1.Text.Substring(0, 2) : comboBox1.Text))
			{
				button3.Enabled = true;
			}
			else
			{
				MessageBox.Show(this, "打开磁盘失败", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			//打开文件
			if (FileBin.OpenFile(textBox4.Text))
			{
				button3.Enabled = true;
			}
			else
			{
				MessageBox.Show(this, "打开文件失败", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
				cipan.Close();
				return;
			}
			/**********************************************************************************************************/
			//获取文件长度
			long file_size = cipan.Filelen(textBox4.Text);
			long disk_size = cipan.GetSectorCount() * cipan.GetSectorLen();
			if (file_size > disk_size )
			{
				MessageBox.Show("文件数据大于磁盘容量", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if(file_size > 1024 * 1024 *1024)
			{
				MessageBox.Show("文件数据大于1G", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (disk_size > 1024 * 1024 * 1024)
			{
				MessageBox.Show("磁盘数据大于1G", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (MessageBox.Show(this, "确定要执行操作？此操作无法撤销！", "提示信息：", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
			{
				button3.Text = "写入";
				return;
			}
			button3.Text = "写入中";
/**********************************************************************************************************/
			
			if (comboBox2.Text == "写入" || comboBox2.Text == "擦除")
			{
				if (comboBox2.Text == "写入" && file_size <= 0)
				{
					MessageBox.Show(this, "文件无内容或不存在", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Question);
					button3.Text = "执行";
					return;
				}

				for (uint SurplusLen = 0; SurplusLen < file_size; SurplusLen += 512)
				{
					if (comboBox2.Text == "写入")
					{
						//读取数据
						WriteByte = FileBin.BinRead( SurplusLen /512);
					}
					//将数据写入流
					cipan.WriteSector(WriteByte, SurplusLen / 512);

					//将当前流中的数据写入磁盘
					cipan.Refresh();

					//更新进度条
					progressBar1.Step = (int)(SurplusLen * 100 / file_size);
					progressBar1.PerformStep();
				}
				MessageBox.Show(this, "文件写入完成", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Question);
			}
			else if (comboBox2.Text == "读取")
			{
				MessageBox.Show(disk_size.ToString());
				/*读取数据*/
				for (uint SurplusLen = 0; SurplusLen < disk_size; SurplusLen ++)
				{
					Application.DoEvents();
					//读取磁盘数据
					WriteByte = cipan.ReadSector(SurplusLen);

					//写入文件
					FileBin.Write(WriteByte, SurplusLen);

					FileBin.Refresh();
					//更新进度条
					//if (SurplusLen * 100 / disk_size % 1 == 0)
					//{
					//	progressBar1.Step = (int)(SurplusLen * 100 / disk_size);
					//	progressBar1.PerformStep();
					//}
				}
				MessageBox.Show(this, "磁盘读取完成", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Question);

			}
			button3.Text = "写入";
			cipan.Close();
			FileBin.Close();
			Get_info();//关闭磁盘时刷新磁盘信息
		}

		//打印log信息
		private void Log(string data)
		{
			if (data == "")
			{
				textBox1.Text = "";
			}

			textBox1.Text += data;
		}

		//获取磁盘信息
		private void Get_info()
		{
			long lsum = 0,ldr =0;
			string disk_log = "磁盘信息\r\n";
			comboBox1.Items.Clear();
			Log("");
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
						disk_log += "固定磁盘：";
					}
					else if (drive.DriveType == DriveType.Removable)
					{
						disk_log += "移动磁盘：";
					}

					disk_log += drive.Name + ": 总空间=" + lsum.ToString("n") + " MB" + " 剩余空间= " + ldr.ToString("n") + " MB" + "\r\n";
				}
			}

			ManagementObjectSearcher mydisks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
			foreach (ManagementObject mydisk in mydisks.Get())
			{
				//comboBox1.Items.Add(mydisk["Model"].ToString());
				comboBox1.Items.Add(mydisk["DeviceID"].ToString());
			}

			Log(disk_log);
			if (comboBox1.Items.Count > 0)
			{
				comboBox1.SelectedIndex = 0;//设置默认值
			}
		}

		//private void button1_Click(object sender, EventArgs e)
		//{
		//	if (comboBox1.Items.Count <= 0)
		//		return;

		//	tergetDisk = (comboBox1.Text[0] >= 'A' && comboBox1.Text[0] <= 'Z') ? comboBox1.Text.Substring(0,2) : comboBox1.Text;
		//	DiskGeometry geometry = new DiskGeometry { };
		//	cipan.GetDiskinfo(tergetDisk, ref geometry);
		//	if (DiskOpen == false)//磁盘未打开
		//	{
		//		if (cipan.OpenDisk(tergetDisk))
		//		{
		//			button1.Text = "关闭";
		//			button3.Enabled = true;
		//			DiskOpen = true;
		//		}
		//		else
		//		{
		//			MessageBox.Show(this, "打开磁盘失败", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//		}
		//	}
		//	else
		//	{
		//		cipan.Close();
		//		button1.Text = "打开";
		//		button3.Enabled = false;
		//		DiskOpen = false;
		//		Get_info();//关闭磁盘时刷新磁盘信息
		//	}
		//}

		private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

	}

	
}
