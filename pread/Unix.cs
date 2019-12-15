using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace pread
{
	/// <summary>
	/// API specific implementations for 'pread' on Unix
	/// </summary>
	public static class Unix
	{
		// figured out thanks to help of members of the c# discord server in lowlevel-advanced,
		// https://discord.gg/csharp

		// TODO: C# 9, nuint instead of UIntPtr
		[DllImport("c", SetLastError = true)]
		public static extern unsafe IntPtr pread(IntPtr fd, void* buf, UIntPtr count, IntPtr offset);

		[StructLayout(LayoutKind.Auto)]
		public struct PreadResult
		{
			public bool DidSucceed;
			public PreadResultData Data;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct PreadResultData
		{
			[FieldOffset(0)]
			public int Errno;

			[FieldOffset(0)]
			public long BytesRead;
		}

		public static unsafe PreadResult Pread(FileStream fileHandle, Span<byte> buffer, ulong fileOffset, uint readAmount)
		{
			var fileDescriptor = fileHandle.SafeFileHandle.DangerousGetHandle();

			fixed (void* bufferPtr = buffer)
			{
				var bytesRead = (long)pread(fileDescriptor, bufferPtr, (UIntPtr)readAmount, (IntPtr)fileOffset);

				if (bytesRead < 0)
				{
					var errno = Marshal.GetLastWin32Error();

					return new PreadResult
					{
						DidSucceed = false,
						Data = new PreadResultData
						{
							Errno = errno
						}
					};
				}
				else
				{
					return new PreadResult
					{
						DidSucceed = true,
						Data = new PreadResultData
						{
							BytesRead = bytesRead
						}
					};
				}
			}
		}
	}
}
