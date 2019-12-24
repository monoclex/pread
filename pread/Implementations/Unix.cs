using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace pread.Implementations
{
	/// <summary>
	/// API specific implementations for 'pread' on Unix
	/// </summary>
	public static class Unix
	{
		/// <summary>
		/// Determines if the current machine is a unix machine. This is
		/// implemented with <see cref="RuntimeInformation.IsOSPlatform(OSPlatform)"/>.
		/// </summary>
		public static bool MachineIsUnix { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)

			// 90% sure OSX supports pread, will need CI/CD for this
			|| RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

		/// <summary>
		/// This gets an error message associated with an error code, for the
		/// <see cref="Unix"/> platform.
		/// </summary>
		/// <param name="errorCode">The error code.</param>
		/// <returns>The error message of the given <paramref name="errorCode"/>.</returns>
		public static string StringError(int errorCode)

		// this returns the appropriate error messages on unix as well, despite being named win32
			=> new System.ComponentModel.Win32Exception(errorCode).Message;

		// figured out thanks to help of members of the c# discord server in lowlevel-advanced,
		// https://discord.gg/csharp

		public static class Native
		{
			// TODO: C# 9, nuint instead of UIntPtr
			/// <summary>
			/// http://man7.org/linux/man-pages/man2/pwrite.2.html
			/// </summary>
			[DllImport("c", SetLastError = true)]
			public static extern unsafe IntPtr pread(IntPtr fd, void* buf, UIntPtr count, IntPtr offset);

			/// <summary>
			/// http://man7.org/linux/man-pages/man2/pwrite.2.html
			/// </summary>
			[DllImport("c", SetLastError = true)]
			public static extern unsafe IntPtr pwrite(IntPtr fd, void* buf, UIntPtr count, IntPtr offset);
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
			/// The error code of the operation, if the operation had failed.
			/// Ensure <see cref="DidSucceed"/> is <c>false</c> before accessing <see cref="Errno"/>.
			/// If <see cref="DidSucceed"/> is <c>true</c>, accessing <see cref="Errno"/> is equivalent to performing
			/// an unchecked cast on <see cref="Bytes"/> to int.
			/// </summary>
			[FieldOffset(0)]
			public int Errno;

			/// <summary>
			/// The amount of bytes read from the operation, if the operation had succeeded.
			/// Ensure <see cref="DidSucceed"/> is <c>true</c> before accessing <see cref="Bytes"/>.
			/// If <see cref="DidSucceed"/> is <c>false</c>, accessing <see cref="Bytes"/> is equivalent to performing
			/// an unchecked cast on left 32 bits of <see cref="Bytes"/>'s value to an int.
			/// </summary>
			[FieldOffset(0)]
			public long Bytes;

			/// <summary>
			/// Tells whether the operation had succeeded (<c>true</c>) or failed (<c>false</c>).
			/// If <see cref="DidSucceed"/> is <c>true</c>, access <see cref="Bytes"/>.
			/// If <see cref="DidSucceed"/> is <c>false</c>, access <see cref="Errno"/>.
			/// </summary>
			[FieldOffset(sizeof(long))]
			public bool DidSucceed;
		}

		// API compatibility
		[Obsolete("Use " + nameof(PRead) + ".")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PResult Pread(Span<byte> buffer, FileStream fileStream, ulong fileOffset)
			=> PRead(fileStream, buffer, fileOffset);

		/// <summary>
		/// Performs a <c>pread</c> on a filestream at an offset to a buffer.
		/// </summary>
		/// <param name="fileStream">The file to read from.</param>
		/// <param name="buffer">The buffer to write data to.</param>
		/// <param name="fileOffset">The offset in the file to read data from.</param>
		/// <returns>A <see cref="PResult"/>.</returns>
		// Pread -> PRead: inline with P.Read better, aligns with C# naming conventions better
		// Span<byte>, FileStream, ulong -> FileStream, Span<byte>, ulong: aligns with P.Read better.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PResult PRead(FileStream fileStream, Span<byte> buffer, ulong fileOffset)
		{
			var fileDescriptor = fileStream.SafeFileHandle.DangerousGetHandle();

			fixed (void* bufferPtr = buffer)
			{
				var bytesRead = (long)Native.pread(fileDescriptor, bufferPtr, (UIntPtr)buffer.Length, (IntPtr)fileOffset);
				return PResultFromBytes(bytesRead);
			}
		}

		[Obsolete("Use " + nameof(PWrite) + ".")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PResult Pwrite(ReadOnlySpan<byte> buffer, FileStream fileStream, ulong fileOffset)
			=> PWrite(fileStream, buffer, fileOffset);

		/// <summary>
		/// Performs a <c>pwrite</c> on a filestream at an offset to a buffer.
		/// </summary>
		/// <param name="fileStream">The file to write to.</param>
		/// <param name="data">The data to write to the file.</param>
		/// <param name="fileOffset">The offset in the file to write data at.</param>
		/// <returns>A <see cref="PResult"/>.</returns>
		// Pwrite -> PWrite: inline with P.Write better, aligns with C# naming conventions better
		// ReadOnlySpan<byte>, FileStream, ulong -> FileStream, ReadOnlySpan<byte>, ulong: aligns with P.Write better.
		// buffer -> data: better name, aligns with Windows.PWrite, was typo.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PResult PWrite(FileStream fileStream, ReadOnlySpan<byte> data, ulong fileOffset)
		{
			var fileDescriptor = fileStream.SafeFileHandle.DangerousGetHandle();

			fixed (void* bufferPtr = data)
			{
				var bytesWritten = (long)Native.pwrite(fileDescriptor, bufferPtr, (UIntPtr)data.Length, (IntPtr)fileOffset);
				return PResultFromBytes(bytesWritten);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static PResult PResultFromBytes(long bytes)
		{
			if (bytes < 0)
			{
				var errno = Marshal.GetLastWin32Error();

				return new PResult
				{
					DidSucceed = false,
					Errno = errno,
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