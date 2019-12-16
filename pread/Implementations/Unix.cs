using System;
using System.IO;
using System.Runtime.InteropServices;

namespace pread.Implementations
{
	/// <summary>
	/// API specific implementations for 'pread' on Unix
	/// </summary>
	public static class Unix
	{
		// figured out thanks to help of members of the c# discord server in lowlevel-advanced,
		// https://discord.gg/csharp

		// to get the string of an error, we call strerr (simple) and then free the char* afterwords
		[DllImport("c")]
		public static extern unsafe IntPtr strerror(IntPtr errnum);

		// [DllImport("c")]
		// public static extern unsafe void free(IntPtr ptr);

		public static string StringError(int errorCode)
		{
			var result = strerror((IntPtr)errorCode);

			try
			{
				return Marshal.PtrToStringAnsi(result);
			}
			finally
			{
				// calling 'free' causes the runtime to crash
				// free(result);
			}
		}

		// TODO: C# 9, nuint instead of UIntPtr
		[DllImport("c", SetLastError = true)]
		public static extern unsafe IntPtr pread(IntPtr fd, void* buf, UIntPtr count, IntPtr offset);

		[DllImport("c", SetLastError = true)]
		public static extern unsafe IntPtr pwrite(IntPtr fd, void* buf, UIntPtr count, IntPtr offset);

		[StructLayout(LayoutKind.Auto)]
		public struct PResult
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
			public long Bytes;
		}

		public static unsafe PResult Pread(Span<byte> buffer, FileStream fileStream, ulong fileOffset)
		{
			var fileDescriptor = fileStream.SafeFileHandle.DangerousGetHandle();

			fixed (void* bufferPtr = buffer)
			{
				var bytesRead = (long)pread(fileDescriptor, bufferPtr, (UIntPtr)buffer.Length, (IntPtr)fileOffset);

				if (bytesRead < 0)
				{
					var errno = Marshal.GetLastWin32Error();

					return new PResult
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
					return new PResult
					{
						DidSucceed = true,
						Data = new PreadResultData
						{
							Bytes = bytesRead
						}
					};
				}
			}
		}

		public static unsafe PResult Pwrite(ReadOnlySpan<byte> buffer, FileStream fileStream, ulong fileOffset)
		{
			var fileDescriptor = fileStream.SafeFileHandle.DangerousGetHandle();

			fixed (void* bufferPtr = buffer)
			{
				var bytesRead = (long)pwrite(fileDescriptor, bufferPtr, (UIntPtr)buffer.Length, (IntPtr)fileOffset);

				if (bytesRead < 0)
				{
					var errno = Marshal.GetLastWin32Error();

					return new PResult
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
					return new PResult
					{
						DidSucceed = true,
						Data = new PreadResultData
						{
							Bytes = bytesRead
						}
					};
				}
			}
		}
	}
}