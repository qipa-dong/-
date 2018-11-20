using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
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

		private void Get_info()
		{
			long lsum = 0,ldr;
			StringBuilder mStringBuilder = new StringBuilder();
			comboBox1.Items.Clear();
			label6.Text = "";
			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				//判断是否是固定磁盘  
				if (drive.DriveType == DriveType.Fixed)
				{
					lsum = drive.TotalSize / 1024 /1024;//MB,磁盘总大小
					ldr = drive.TotalFreeSpace / 1024 /1024;//剩余大小
					label6.Text += "固定磁盘：";
					label6.Text += drive.Name + ": 总空间=" + lsum.ToString("n") + " MB" + " 剩余空间= " + ldr.ToString("n") + " MB" + "\r\n";
				}
				else if (drive.DriveType == DriveType.Removable)
				{
					lsum = drive.TotalSize / 1024 / 1024;//MB,磁盘总大小
					ldr = drive.TotalFreeSpace / 1024 / 1024;//剩余大小
					label6.Text += "移动磁盘：";
					label6.Text += drive.Name + ": 总空间=" + lsum.ToString("n") + " 剩余空间= " + ldr.ToString("n") + " MB" + "\r\n";
				}

				if(lsum <= 0x2000)
				{
					if (drive.IsReady)//磁盘已就绪
						comboBox1.Items.Add(drive.Name + drive.VolumeLabel);
					else
						comboBox1.Items.Add(drive.Name + "未知");
				}
			}
			if(comboBox1.Items.Count > 0)
			{
				comboBox1.SelectedIndex = 0;//设置默认值
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (comboBox1.Items.Count <= 0)
				return;

			tergetDisk = comboBox1.Text.Substring(0, 2);
			if (DiskOpen == false)//磁盘未打开
			{
				if (cipan.OpenDisk(tergetDisk))
				{
					button1.Text = "磁盘" + comboBox1.Text.Substring(0, 2);
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
				button1.Text = "打开磁盘";
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
