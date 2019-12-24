using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace pread.Implementations
{
	/// <summary>
	/// API specific implementations for 'pread' on Windows
	/// </summary>
	public static class Windows
	{
		/// <summary>
		/// Determines if the current machine is a windows machine. This is
		/// implemented with <see cref="RuntimeInformation.IsOSPlatform(OSPlatform)"/>.
		/// </summary>
		public static bool MachineIsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		/// <summary>
		/// This gets an error message associated with an error code, for the
		/// <see cref="Windows"/> platform.
		/// </summary>
		/// <param name="errorCode">The error code.</param>
		/// <returns>The error message of the given <paramref name="errorCode"/>.</returns>
		public static string StringError(int errorCode)

			// i dislike having to new up an exception to get the message, but it's eh
			=> new System.ComponentModel.Win32Exception(errorCode).Message;

		/// <summary>
		/// Provides <c>DllImport</c>s to native functions used to emulate the
		/// functionality of <c>pread</c> and <c>pwrite</c>.
		/// </summary>
		public static class Native
		{
#pragma warning disable CA1401 // P/Invokes should not be visible - allow consumers to consume this if they choose to do so
			// extern call signature: https://stackoverflow.com/a/28781279
			// it is said that LPVOID and LPCVOID should be IntPtrs but i like byte* better

			/// <summary>
			/// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile?redirectedfrom=MSDN
			/// </summary>
			[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern unsafe bool ReadFile(IntPtr hFile, byte* lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, NativeOverlapped* lpOverlapped);

			/// <summary>
			/// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile
			/// </summary>
			[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern unsafe bool WriteFile(IntPtr hFile, byte* lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, NativeOverlapped* lpOverlapped);
#pragma warning restore CA1401 // P/Invokes should not be visible
		}

		/// <summary>
		/// Represents the result of a pread/pwrite operation. Provides
		/// information about a possible failure or error codes in a memory
		/// efficient manner.
		/// </summary>
		[StructLayout(LayoutKind.Explicit)]
		public struct PResult
		{
			/// <summary>
			/// The error code of the operation, if it had failed.
			/// Ensure <see cref="DidSucceed"/> is <c>false</c> before accessing <see cref="WindowsErrorCode"/>.
			/// If <see cref="DidSucceed"/> is <c>true</c>, accessing <see cref="WindowsErrorCode"/> is equivalent to performing
			/// an unchecked cast on <see cref="Bytes"/> to int.
			/// </summary>
			[FieldOffset(0)]
			public int WindowsErrorCode;

			/// <summary>
			/// The amount of bytes read from the operation, if it had succeeded.
			/// Ensure <see cref="DidSucceed"/> is <c>true</c> before accessing <see cref="Bytes"/>.
			/// If <see cref="DidSucceed"/> is <c>false</c>, accessing <see cref="Bytes"/> is equivalent to performing
			/// an unchecked cast on <see cref="WindowsErrorCode"/> to uint.
			/// </summary>
			[FieldOffset(0)]
			public uint Bytes;

			/// <summary>
			/// Tells whether the operation had succeeded (<c>true</c>) or failed (<c>false</c>).
			/// If <see cref="DidSucceed"/> is <c>true</c>, access <see cref="Bytes"/>.
			/// If <see cref="DidSucceed"/> is <c>false</c>, access <see cref="WindowsErrorCode"/>.
			/// </summary>
			[FieldOffset(sizeof(uint))]
			public bool DidSucceed;
		}

		// API compatibility
		[Obsolete("Use " + nameof(PRead) + ".")]
		public static PResult Pread(Span<byte> buffer, FileStream fileStream, ulong fileOffset)
			=> PRead(fileStream, buffer, fileOffset);

		/// <summary>
		/// Performs the windows equivalent of a <c>pread</c> on a filestream at an offset to a buffer.
		/// </summary>
		/// <param name="fileStream">The file to read from.</param>
		/// <param name="buffer">The buffer to write data to.</param>
		/// <param name="fileOffset">The offset in the file to read data from.</param>
		/// <returns>A <see cref="PResult"/>.</returns>
		// Pread -> PRead: inline with P.Read better, aligns with C# naming conventions better
		// Span<byte>, FileStream, ulong -> FileStream, Span<byte>, ulong: aligns with P.Read better.
		public static unsafe PResult PRead(FileStream fileStream, Span<byte> buffer, ulong fileOffset)
		{
			// https://github.com/aleitner/windows_pread/blob/master/src/pread.c#L86
			var handle = fileStream.SafeFileHandle.DangerousGetHandle();

			// https://docs.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-overlapped
			// we can use System.Threading.NativeOverlapped
			using var overlapped = MarshalAlloc<NativeOverlapped>.New();

			// we're allocating memory, want to make sure we free it when we're done
			SetOverlapped(overlapped, fileOffset);

			fixed (byte* bufferPtr = buffer)
			{
				var success = Native.ReadFile(handle, bufferPtr, (uint)buffer.Length, out var bytesRead, overlapped);
				return PResultFromSuccess(success, bytesRead);
			}
		}

		[Obsolete("Use " + nameof(PWrite) + ".")]
		public static PResult Pwrite(ReadOnlySpan<byte> data, FileStream fileStream, ulong fileOffset)
			=> PWrite(fileStream, data, fileOffset);

		/// <summary>
		/// Performs the windows equivalent of a <c>pwrite</c> on a filestream at an offset to a buffer.
		/// </summary>
		/// <param name="fileStream">The file to write to.</param>
		/// <param name="data">The data to write to the file.</param>
		/// <param name="fileOffset">The offset in the file to write data at.</param>
		/// <returns>A <see cref="PResult"/>.</returns>
		// Pwrite -> PWrite: inline with P.Write better, aligns with C# naming conventions better
		// ReadOnlySpan<byte>, FileStream, ulong -> FileStream, ReadOnlySpan<byte>, ulong: aligns with P.Write better.
		public static unsafe PResult PWrite(FileStream fileStream, ReadOnlySpan<byte> data, ulong fileOffset)
		{
			// https://github.com/aleitner/windows_pread/blob/master/src/pread.c#L86
			var handle = fileStream.SafeFileHandle.DangerousGetHandle();

			// https://docs.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-overlapped
			using var overlapped = MarshalAlloc<NativeOverlapped>.New();

			// we're allocating memory, want to make sure we free it when we're done
			SetOverlapped(overlapped, fileOffset);

			fixed (byte* bufferPtr = data)
			{
				var success = Native.WriteFile(handle, bufferPtr, (uint)data.Length, out var bytesWritten, overlapped);
				return PResultFromSuccess(success, bytesWritten);
			}
		}

		// used to cleanly alloc/free marshal data
		private unsafe ref struct MarshalAlloc<T> where T : unmanaged
		{
			public T* Data;

			public static MarshalAlloc<T> New() => new MarshalAlloc<T>
			{
				Data = (T*)Marshal.AllocHGlobal(sizeof(T))
			};

			public void Dispose() => Marshal.FreeHGlobal((IntPtr)Data);

			public static implicit operator T*(MarshalAlloc<T> marshalAlloc) => marshalAlloc.Data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void SetOverlapped(NativeOverlapped* overlapped, ulong fileOffset)
		{
			// explicitly memset 0 stuff
			overlapped->EventHandle = IntPtr.Zero;
			overlapped->InternalHigh = IntPtr.Zero;
			overlapped->InternalLow = IntPtr.Zero;

			// set the high bits of ulong
			overlapped->OffsetHigh = (int)((fileOffset & 0b11111111_11111111_11111111_11111111_0000000_0000000_0000000_0000000) >> 32);

			// set the low bits
			// NativeOverlapped's 'OffsetLow' is really just 'Offset'
			overlapped->OffsetLow = (int)(fileOffset & 0b0000000_0000000_0000000_0000000_11111111_11111111_11111111_11111111);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static PResult PResultFromSuccess(bool success, uint bytes)
		{
			if (!success)
			{
				var errorCode = Marshal.GetLastWin32Error();

				return new PResult
				{
					DidSucceed = false,
					WindowsErrorCode = errorCode,
				};
			}
			else
			{
				return new PResult
				{
					DidSucceed = true,
					Bytes = bytes,
				};
			}
		}
	}
}