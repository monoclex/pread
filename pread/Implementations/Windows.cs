using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace pread.Implementations
{
	/// <summary>
	/// API specific implementations for 'pread' on Windows
	/// </summary>
	public static class Windows
	{
		// i dislike it but oh well :/
		public static string StringError(int errorCode) => new System.ComponentModel.Win32Exception(errorCode).Message;

#pragma warning disable CA1401 // P/Invokes should not be visible - allow consumers to consume this if they choose to do so

		// extern call signature: https://stackoverflow.com/a/28781279
		// it is said that LPVOID and LPCVOID should be IntPtrs but i like byte* better

		/// <summary>
		/// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile?redirectedfrom=MSDN
		/// </summary>
		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern unsafe bool ReadFile(IntPtr hFile, byte* lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

		/// <summary>
		/// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile
		/// </summary>
		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern unsafe bool WriteFile(IntPtr hFile, byte* lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

#pragma warning restore CA1401 // P/Invokes should not be visible

		/// <summary>
		/// Small utility struct for returning if a given pread was successful.
		/// Note: there is absolute no native interop at play here, besides the
		/// WindowsErrorCode.
		/// </summary>
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
			public int WindowsErrorCode;

			[FieldOffset(0)]
			public uint Bytes;
		}

		public static unsafe PResult Pread(Span<byte> buffer, FileStream fileStream, ulong fileOffset)
		{
			// https://github.com/aleitner/windows_pread/blob/master/src/pread.c#L86
			var handle = fileStream.SafeFileHandle.DangerousGetHandle();

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
					if (!ReadFile(handle, bufferPtr, (uint)buffer.Length, out var bytesRead, (IntPtr)overlapped))
					{
						int errorCode = Marshal.GetLastWin32Error();

						return new PResult
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
			finally
			{
				Marshal.FreeHGlobal((IntPtr)overlapped);
			}
		}

		public static unsafe PResult Pwrite(ReadOnlySpan<byte> data, FileStream fileStream, ulong fileOffset)
		{
			// https://github.com/aleitner/windows_pread/blob/master/src/pread.c#L86
			var handle = fileStream.SafeFileHandle.DangerousGetHandle();

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

				fixed (byte* bufferPtr = data)
				{
					if (!WriteFile(handle, bufferPtr, (uint)data.Length, out var bytesWritten, (IntPtr)overlapped))
					{
						int errorCode = Marshal.GetLastWin32Error();

						return new PResult
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
						return new PResult
						{
							DidSucceed = true,
							Data = new PreadResultData
							{
								Bytes = bytesWritten
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