using System;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SDcard
{
	class SDUtils
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
		private const uint FSCTL_LOCK_VOLUME = 0x090018;
		private const uint FSCTL_UNLOCK_VOLUME = 0x090019;
		private const uint FSCTL_DISMOUNT_VOLUME = 0x09001A;
		private const uint OPEN_EXISTING = 3;
        private FileStream _DriverStream;
        private uint _SectorNum = 0;
		private uint _SectorLen = 0;
		private SafeFileHandle _DriverHandle;

		/// <summary>
		/// 扇区数
		/// </summary>
		public uint GetSectorNum()
		{ return _SectorNum; }

		/// <summary>
		/// 扇区大小
		/// </summary>
		public uint GetSectorLen()
		{ return _SectorLen; }

		/// <summary>
		/// 获取扇区信息
		/// </summary>
		/// <param name="DriverName">G:</param>

		public SDUtils()
		{
		}

		/// <summary>
		/// 打开磁盘
		/// </summary>
		/// <param name="OpenDisk">磁盘盘符</param>
		/// <returns>成功返回1</returns>
		public bool OpenDisk(string DriverName)
		{
			try
			{
				if (DriverName == null && DriverName.Trim().Length == 0) return false;
				_DriverHandle = NativeMethods.CreateFile("\\\\.\\" + DriverName.Trim(), 
					GENERIC_READ | GENERIC_WRITE, 
					FILE_SHARE_READ | FILE_SHARE_WRITE, 
					IntPtr.Zero, 
					OPEN_EXISTING, 
					0, 
					IntPtr.Zero);
				_DriverStream = new FileStream(_DriverHandle, FileAccess.ReadWrite);
				GetSectorCount();
				//GetDeviceID(_DriverHandle);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return false;
			}
		}

		int number1;
        /// <summary>
        /// 扇区显示转换
        /// </summary>
        /// <param name="SectorBytes">扇区长度512</param>
        /// <returns>EB 52 90 ......55 AA</returns>
        public string Byte2String(byte[] SectorBytes)
        {
            if (SectorBytes == null || SectorBytes.Length != 512) return "Content is empty";
            StringBuilder ReturnText = new StringBuilder();
             
            for (number1 = 0; number1 < 16; number1++)
                ReturnText.Append(number1.ToString("X02") + " ");
            ReturnText.Append("\r\n");
            byte number=0;
            int RowCount = 0;
            ReturnText.Append(number.ToString("X02") + ":  ");
            number++;
            for (int i = 0; i != 512; i++)
            {
                ReturnText.Append(SectorBytes[i].ToString("X02") + " ");
                if (RowCount == 15 && number<=31)
                {
                    ReturnText.Append("\r\n");
                    ReturnText.Append(number.ToString("X02") + ":  ");
                    number++;
                    RowCount = -1;
                }
                RowCount++;
            }
            return ReturnText.ToString();
        }
        /// <summary>
        /// 获取分区扇区数量
        /// </summary>
        public uint GetSectorCount()
        {
            if (_DriverStream == null) return 0;
            _DriverStream.Position = 0;
            byte[] ReturnByte = new byte[512];
            _DriverStream.Read(ReturnByte, 0, 512); //获取第1扇区
			if (ReturnByte[0x36] == 0x46 && ReturnByte[0x37] == 0x41 && ReturnByte[0x38] == 0x54 && ReturnByte[0x39] == 0x31 && ReturnByte[0x3A] == 0x36
				&& ReturnByte[0x3B] == 0x20 && ReturnByte[0x3C] == 0x20 && ReturnByte[0x3D] == 0x20)          //FAT16
			{
				_SectorLen = (uint)BitConverter.ToInt16(new byte[] { ReturnByte[0x0B], ReturnByte[0x0C] }, 0);
				if (ReturnByte[13] == 0x00 && ReturnByte[14] == 0x00)//小扇区数(Small Sector) 该分区上的扇区数，表示为16位(<65536)。对大于65536个扇区的分区来说，本字段的值为0，而使用大扇区数来取代它。
				{
					_SectorNum = (uint)BitConverter.ToInt16(new byte[] { ReturnByte[0x13], ReturnByte[0x14] }, 0);
				}
				else
				{
					_SectorNum = (uint)BitConverter.ToInt32(new byte[] { ReturnByte[0x20], ReturnByte[0x21], ReturnByte[0x22], ReturnByte[0x23] }, 0);
				}
			}
			else if (ReturnByte[0x52] == 0x46 && ReturnByte[0x53] == 0x41 && ReturnByte[0x54] == 0x54 && ReturnByte[0x55] == 0x33 && ReturnByte[0x56] == 0x32
					&& ReturnByte[0x57] == 0x20 && ReturnByte[0x58] == 0x20 && ReturnByte[0x59] == 0x20)//FAT32
			{
				_SectorNum = (uint)BitConverter.ToInt32(new byte[] { ReturnByte[0x20], ReturnByte[0x21], ReturnByte[0x22], ReturnByte[0x23] }, 0);
			}
			else
			{
				MessageBox.Show("未知分区");
			}
			return _SectorNum;
		}

		/// <summary>
		/// 获取磁盘信息
		/// </summary>
		public bool GetDiskinfo(string DriverName, ref DiskGeometry geometry)
		{
			SafeFileHandle diskHandle =
			NativeMethods.CreateFile(
				//@"\\.\PhysicalDrive0",
				"\\\\.\\" + DriverName,
				NativeMethods.FileAccessGenericRead,
				NativeMethods.FileShareWrite | NativeMethods.FileShareRead,
				IntPtr.Zero,
				NativeMethods.CreationDispositionOpenExisting,
				0,
				IntPtr.Zero
			);

			if (diskHandle.IsInvalid)
			{
				//ShowMessage("CreateFile failed with error: " + Marshal.GetLastWin32Error().ToString());
				return false;
			}

			int geometrySize = Marshal.SizeOf(typeof(DiskGeometry));
			//ShowMessage("geometry size " + geometrySize.ToString());

			IntPtr geometryBlob = Marshal.AllocHGlobal(geometrySize);
			uint numBytesRead = 0;


			if (0 == NativeMethods.DeviceIoControl(
					diskHandle,
					NativeMethods.IoCtlDiskGetDriveGeometry,
					IntPtr.Zero,
					0,
					geometryBlob,
					(uint)geometrySize,
					ref numBytesRead,
					IntPtr.Zero
					))
			{
				//ShowMessage("DeviceIoControl failed with error: " +Marshal.GetLastWin32Error().ToString());

				return false;
			}

			//ShowMessage("Bytes read = " + numBytesRead.ToString());

			geometry = (DiskGeometry)Marshal.PtrToStructure(geometryBlob, typeof(DiskGeometry));
			Marshal.FreeHGlobal(geometryBlob);

			long bytesPerCylinder = (long)geometry.TracksPerCylinder * geometry.SectorsPerTrack * geometry.BytesPerSector;
			long totalSize = geometry.Cylinders * bytesPerCylinder;

			if (diskHandle.IsInvalid == false)
			{
				NativeMethods.CloseHandle(diskHandle);
			}

			return true;
		}

		/// <summary>
		/// 通过盘符获取磁盘id
		/// </summary>
		/// <param name="hVolume"></param>
		/// <returns></returns>
		private uint GetDeviceID(SafeFileHandle hVolume)
		{
			VOLUME_DISK_EXTENTS sd = new VOLUME_DISK_EXTENTS();
			uint bytesreturned = 0;
			int ret = 0;
			int size = Marshal.SizeOf(sd);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			ret = NativeMethods.DeviceIoControl(hVolume, NativeMethods.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, ptr, (uint)size, ref bytesreturned, IntPtr.Zero);
			if (0 == ret)
			{
				int err = Marshal.GetLastWin32Error();
				string msg = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
			}
			else
			{
				Marshal.PtrToStructure(ptr, sd);
			}
			Marshal.FreeHGlobal(ptr);

			return sd.Extents[0].DiskNumber;
		}

		// 将卷锁定
		public int LockVolume()
		{
			uint dwOutBytes = 0;
			int bResult;

			// 用FSCTL_LOCK_VOLUME锁卷
			bResult = NativeMethods.DeviceIoControl(_DriverHandle,  // 设备句柄
			 FSCTL_LOCK_VOLUME,   // 锁卷
			 IntPtr.Zero, 0,    // 不需要输入数据
			 IntPtr.Zero, 0,    // 不需要输出数据
			 ref dwOutBytes,    // 输出数据长度
			 IntPtr.Zero);   // 用同步I/O

			return bResult;
		}

		// 将卷解锁
		public int UnlockVolume()
		{
			uint dwOutBytes = 0;
			int bResult;

			// 用FSCTL_UNLOCK_VOLUME开卷锁
			bResult = NativeMethods.DeviceIoControl(_DriverHandle,  // 设备句柄
			 FSCTL_UNLOCK_VOLUME,   // 开卷锁
			 IntPtr.Zero, 0,    // 不需要输入数据
			 IntPtr.Zero, 0,    // 不需要输出数据
			 ref dwOutBytes,    // 输出数据长度
			 IntPtr.Zero);   // 用同步I/O

			return bResult;
		}

		// 将卷卸下
		// 该操作使系统重新辨识磁盘，等效于重新插盘
		public int DismountVolume()
		{
			uint dwOutBytes = 0;
			int bResult;

			// 用FSCTL_DISMOUNT_VOLUME卸卷
			bResult = NativeMethods.DeviceIoControl(_DriverHandle,  // 设备句柄
			 FSCTL_DISMOUNT_VOLUME,   // 卸卷
			 IntPtr.Zero, 0,    // 不需要输入数据
			 IntPtr.Zero, 0,    // 不需要输出数据
			 ref dwOutBytes,    // 输出数据长度
			 IntPtr.Zero);   // 用同步I/O

			return bResult;
		}

		/// <summary>
		/// 读取扇区
		/// </summary>
		/// <param name="SectorIndex">扇区号</param>
		/// <returns>如果扇区数大于分区信息的扇区数，则返回NULL</returns>
		public bool ReadSector(long SectorIndex, int size, ref byte[] data)
        {
            //_DriverStream.Position = SectorIndex;
			_DriverStream.Seek(SectorIndex, SeekOrigin.Begin);
			_DriverStream.Read(data, 0, size); //获取扇区
            return true;
        }
        /// <summary>
        /// 向磁盘扇区写入数据
        /// </summary>
        /// <param name="SectorBytes">扇区长度512</param>
        /// <param name="SectorIndex">扇区位置</param>
        public void WriteSector( ref byte[] SectorBytes, long SectorIndex, int size)
        {
            if (SectorBytes.Length < size) return;
            if (SectorIndex > _SectorNum * _SectorLen) return;
            //_DriverStream.Position = SectorIndex ;
			_DriverStream.Seek(SectorIndex, SeekOrigin.Begin);
			_DriverStream.Write(SectorBytes, 0, size); //写入扇区 
        }

		/// <summary>
		/// 写入磁盘，将缓冲区的数据写入磁盘
		/// </summary>
		public void Refresh()
		{
			_DriverStream.Flush();
		}

		//获取文件长度
		public uint Filelen(string filename)
		{
			if (filename == "" || File.Exists(filename) == false)
				return 0;
			FileInfo fi = new FileInfo(filename);
			return (uint)fi.Length;
		}

		/// <summary>
		/// 关闭
		/// </summary>
		public void Close()
        {
            _DriverStream.Close();
			//if (_DriverHandle.IsInvalid == false)
			//{
			//	NativeMethods.CloseHandle(_DriverHandle);
			//}
		}
    }

	[StructLayout(LayoutKind.Sequential)]
	internal struct DiskGeometry
	{
		public long Cylinders;
		public int MediaType;
		public int TracksPerCylinder;
		public int SectorsPerTrack;
		public int BytesPerSector;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct DISK_EXTENT
	{

		//
		// Specifies the storage device number of
		// the disk on which this extent resides.
		//
		public uint DiskNumber;

		//
		// Specifies the offset and length of this
		// extent relative to the beginning of the
		// disk.
		//

		public long StartingOffset;
		public long ExtentLength;

	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct VOLUME_DISK_EXTENTS
	{
		public uint NumberOfDiskExtents;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
		public DISK_EXTENT[] Extents;
	}

	internal static class NativeMethods
	{
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern SafeFileHandle CreateFile(
			string fileName,
			uint fileAccess,
			uint fileShare,
			IntPtr securityAttributes,
			uint creationDisposition,
			uint flags,
			IntPtr template
			);

		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool CloseHandle(SafeFileHandle hObject);

		[DllImport("Kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
		public static extern int DeviceIoControl(
			SafeFileHandle device,
			uint controlCode,
			IntPtr inBuffer,
			uint inBufferSize,
			IntPtr outBuffer,
			uint outBufferSize,
			ref uint bytesReturned,
			IntPtr overlapped
			);

		internal const uint FileAccessGenericRead = 0x80000000;
		internal const uint FileShareWrite = 0x2;
		internal const uint FileShareRead = 0x1;
		internal const uint CreationDispositionOpenExisting = 0x3;
		internal const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x56000000;
		internal const uint IoCtlDiskGetDriveGeometry = 0x70000;
		internal const short INVALID_HANDLE_VALUE = -1;
	}

}