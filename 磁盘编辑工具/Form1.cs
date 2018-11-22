using System;
using System.Windows.Forms;
using System.IO;
using System.Management;
using SDcard;
namespace 磁盘编辑工具
{
	public partial class Form1 : Form
	{
		private SDUtils cipan = new SDUtils();
		private string tergetDisk = "";//目标磁盘
		private bool DiskOpen = false;//磁盘状态，标志磁盘是否打开
		public Form1()
		{
			InitializeComponent();
			//GetLogicalDrivers();
			Get_info();
		}

		private void button2_Click(object sender, EventArgs e)
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

		private void button3_Click(object sender, EventArgs e)
		{
			//写入磁盘
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

		private void button1_Click(object sender, EventArgs e)
		{
			if (comboBox1.Items.Count <= 0)
				return;

			tergetDisk = (comboBox1.Text[0] >= 'A' && comboBox1.Text[0] <= 'Z') ? comboBox1.Text.Substring(0,2) : comboBox1.Text;
			DiskGeometry geometry = new DiskGeometry { };
			cipan.GetDiskinfo(tergetDisk, ref geometry);
			if (DiskOpen == false)//磁盘未打开
			{
				if (cipan.OpenDisk(tergetDisk))
				{
					button1.Text = "关闭";
					button3.Enabled = true;
					DiskOpen = true;
				}
				else
				{
					MessageBox.Show(this, "打开磁盘失败", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				cipan.Close();
				button1.Text = "打开";
				button3.Enabled = false;
				DiskOpen = false;
				Get_info();//关闭磁盘时刷新磁盘信息
			}
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

	}

	
}
