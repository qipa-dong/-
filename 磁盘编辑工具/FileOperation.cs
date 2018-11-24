//根据查阅的资料对代码进行修改并完善备注后的结果。希望能对新手有所帮助。
using System;  
using System.IO;
using System.Text;
using System.Windows.Forms;
namespace FileOperation
{
    public class FileHelper
    {
		private FileStream sr;
		public FileHelper()
        {
            // TODO: Complete member initialization
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="filePath">文件全路径</param>
        /// <returns></returns>
        public static bool Exists(string filePath)
        {
            if (filePath == null || filePath.Trim() == "")
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="dirPath">文件夹路径</param>
        /// <returns></returns>
        public static bool CreateDir(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            return true;
        }


        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static bool CreateFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                FileStream fs = File.Create(filePath);
                fs.Close();
                fs.Dispose();
            }
            return true;
        }

		/// <summary>
		/// 打开文件
		/// </summary>
		/// <param name="OpenDisk">磁盘盘符</param>
		/// <returns>成功返回1</returns>
		public bool OpenFile(string filePath)
		{
			try
			{
				if (filePath == null && filePath.Trim().Length == 0) return false;
				sr = File.Create(filePath);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return false;
			}
		}

		/// <summary>
		/// 二进制读取文件
		/// </summary>
		/// <param name="filePath">文件路径</param>
		/// <param name="offset">开始读取的位置</param>
		/// <param name="lenght">读取的长度</param>
		/// <returns name="byte[]">读取到的数据</returns>
		public bool BinRead(uint offset,uint size, ref byte[] data)
        {
			//将文件信息读入流中
			sr.Position = offset;
			sr.Read(data, 0, 512); //获取扇区
			return true;
		}


        /// <summary>
        /// 写文件(512字节)
        /// </summary>
        /// <param name="filePath">文件路径</param>
		/// <param name="seek">文件偏移</param>
        /// <param name="content">文件内容</param>
        /// <returns></returns>
        public bool Write(ref byte[] content, uint seek,int size)
        {
			try
			{
				if (content.Length < size)
					return false;

				sr.Position = seek;

				//设置偏移
				sr.Write(content, 0, size);

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return false;
			}
			return true;

		}

		/// <summary>
		/// 写入文件，将缓冲区的数据写入文件
		/// </summary>
		public void Refresh()
		{
			sr.Flush();
		}

		/// <summary>
		/// 关闭
		/// </summary>
		/// <param name="filePath">文件路径</param>
		/// <param name="seek">文件偏移</param>
		/// <param name="content">文件内容</param>
		/// <returns></returns>
		public void Close()
		{
			//同步并关闭文件
			sr.Close();
			sr.Dispose();

		}


		/// <summary>
		/// 删除文件
		/// </summary>
		/// <param name="filePath">文件的完整路径</param>
		/// <returns></returns>
		public static bool DeleteFile(string filePath)
        {
            if (Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }


        /// <summary>
        /// 在指定的目录中查找文件
        /// </summary>
        /// <param name="dir">目录路径</param>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static bool FindFile(string dir, string fileName)
        {
            if (dir == null || dir.Trim() == "" || fileName == null || fileName.Trim() == "" || !Directory.Exists(dir))
            {
                return false;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            return FindFile(dirInfo, fileName);

        }


        public static bool FindFile(DirectoryInfo dir, string fileName)
        {
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                if (File.Exists(d.FullName + "\\" + fileName))
                {
                    return true;
                }
                FindFile(d, fileName);
            }

            return false;
        }

        //获取文件长度
        public long Filelen(string filename)
        {
            if (filename == "")
                return 0;
            FileInfo fi = new FileInfo(filename);
            return fi.Length;
        }
        
    }
}