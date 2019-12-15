using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace pread
{
	/// <summary>
	/// API specific implementations for 'pread' on Windows
	/// </summary>
	public static class Windows
	{
#pragma warning disable CA1401 // P/Invokes should not be visible - allow consumers to consume this if they choose to do so

		// extern call signature: https://stackoverflow.com/a/28781279

		/// <summary>
		/// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile?redirectedfrom=MSDN
		/// </summary>
		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern unsafe bool ReadFile(IntPtr hFile, byte* lpBuffer, UInt32 nNumberOfBytesToRead, out UInt32 lpNumberOfBytesRead, IntPtr lpOverlapped);
#pragma warning restore CA1401 // P/Invokes should not be visible

		/// <summary>
		/// Small utility struct for returning if a given pread was successful.
		/// Note: there is absolute no native interop at play here, besides the
		/// WindowsErrorCode.
		/// </summary>
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
			public int WindowsErrorCode;

			[FieldOffset(0)]
			public uint BytesRead;
		}

		public static unsafe PreadResult Pread(FileStream fileHandle, Span<byte> buffer, ulong fileOffset, uint readAmount)
		{
			// https://github.com/aleitner/windows_pread/blob/master/src/pread.c#L86
			var handle = fileHandle.SafeFileHandle.DangerousGetHandle();

			// https://docs.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-overlapped
			// we can use System.Threading.NativeOverlapped
			NativeOverlapped* overlapped = (NativeOverlapped*)Marshal.AllocHGlobal(sizeof(NativeOverlapped));

			// we're allocating memory, want to make sure we free it when we're done
			try
			{
				// explicitly memset 0 stuff
				overlapped->EventHandle = (IntPtr)0;
				overlapped->InternalHigh = (IntPtr)0;
				overlapped->InternalLow = (IntPtr)0;

				// set the high bits of ulong
				overlapped->OffsetHigh = (int)((fileOffset & 0b11111111_11111111_11111111_11111111_0000000_0000000_0000000_0000000) >> 32);

				// set the low bits
				// NativeOverlapped's 'OffsetLow' is really just 'Offset'
				overlapped->OffsetLow = (int)(fileOffset & 0b0000000_0000000_0000000_0000000_11111111_11111111_11111111_11111111);

				fixed (byte* bufferPtr = buffer)
				{
					if (!ReadFile(handle, bufferPtr, readAmount, out var bytesRead, (IntPtr)overlapped))
					{
						int errorCode = Marshal.GetLastWin32Error();

						return new PreadResult
						{
							DidSucceed = false,
							Data = new PreadResultData
							{
								WindowsErrorCode = errorCode
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
			finally
			{
				Marshal.FreeHGlobal((IntPtr)overlapped);
			}
		}
	}
}
