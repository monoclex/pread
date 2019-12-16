using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace pread
{
	/// <summary>
	/// Represents a section of a <see cref="FileStream"/>. This class performs atomic seek and read/writes
	/// using the <see cref="P"/> Api. This guarentees thread safety, assuming that only <see cref="FileStreamSection"/>s
	/// access the backing <see cref="FileStream"/>.
	/// </summary>
	public struct FileStreamSection
	{
		internal readonly FileStream _parent;
		private readonly ulong _start;
		private readonly ulong _length;

		/// <summary>
		/// Returns the length of this section.
		/// </summary>
		public ulong Length => _length;

		/// <summary>
		/// Constructs a new <see cref="FileStreamSection"/> whose view encompasses
		/// the entire file.
		/// </summary>
		/// <param name="parent">The <see cref="FileStream"/> to view over.</param>
		public FileStreamSection(FileStream parent)
		{
			_parent = parent;
			_start = 0;
			_length = (ulong)_parent.Length;
		}

		/// <summary>
		/// Constructs a new <see cref="FileStreamSection"/> whose view encompasses
		/// a specific part of a file.
		/// </summary>
		/// <param name="parent">The <see cref="FileStream"/> to view over.</param>
		/// <param name="start">The start offset of the file to begin the view at.</param>
		/// <param name="length">The length of the view.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the start and the length are outside the bounds of the <see cref="FileStream"/>.</exception>
		public FileStreamSection(FileStream parent, ulong start, ulong length)
		{
			var parentLength = (ulong)parent.Length;

			if (start > parentLength)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new ArgumentOutOfRangeException(nameof(start));
			}

			if (start + length > parentLength)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new ArgumentOutOfRangeException(nameof(start));
			}

			_parent = parent;
			_start = start;
			_length = length;
		}

		/// <summary>
		/// Creates a view of a file which is inside of <c>this</c> <see cref="FileStreamSection"/>'s view.
		/// </summary>
		/// <param name="start">The start offset of the file to begin the view at.</param>
		/// <param name="length">The length of the view.</param>
		/// <returns>A new <see cref="FileStreamSection"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FileStreamSection Slice(ulong start, ulong length)
		{
			var newStart = _start + start;
			var newLength = newStart + length;

			if (_length < newLength)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new ArgumentOutOfRangeException(nameof(length));
			}

			return new FileStreamSection(_parent, newStart, newLength);
		}

		/// <summary>
		/// Performs a call to <see cref="P.Read(FileStream, Span{byte}, ulong)"/>, ensuring that the
		/// data that is attempted to be read is within the view.
		/// </summary>
		/// <param name="buffer">The buffer to read data into.</param>
		/// <returns>The amount of bytes read.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the length of the data exceeds the length of the view.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Read(Span<byte> buffer)
		{
			var end = (ulong)buffer.Length;

			if (end > _length)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new InvalidOperationException($"Attempted to write data outside of the {nameof(FileStreamSection)}.");
			}

			return P.Read(_parent, buffer, _start);
		}

		/// <summary>
		/// Performs a call to <see cref="P.Read(FileStream, Span{byte}, ulong)"/>, ensuring that the
		/// data that is attempted to be read is within the view.
		/// </summary>
		/// <param name="buffer">The buffer to read data into.</param>
		/// <param name="offset">The offset to begin reading data into from within this view.</param>
		/// <returns>The amount of bytes read.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the length of the data and the offset exceed the length of the view.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Read(Span<byte> buffer, ulong offset)
		{
			var end = offset + (ulong)buffer.Length;

			if (end > _length)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new InvalidOperationException($"Attempted to write data outside of the {nameof(FileStreamSection)}.");
			}

			return P.Read(_parent, buffer, _start + offset);
		}

		/// <summary>
		/// Performs a call to <see cref="P.Write(FileStream, ReadOnlySpan{byte}, ulong)"/>, ensuring that
		/// the data that is attempted to be written is within the view.
		/// </summary>
		/// <param name="data">The data that is to be read.</param>
		/// <returns>The amount of bytes written.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the length of the data exceeds the length of the view.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Write(ReadOnlySpan<byte> data)
		{
			var end = (ulong)data.Length;

			if (end > _length)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new InvalidOperationException($"Attempted to write data outside of the {nameof(FileStreamSection)}.");
			}

			return P.Write(_parent, data, _start);
		}

		/// <summary>
		/// Performs a call to <see cref="P.Write(FileStream, ReadOnlySpan{byte}, ulong)"/>, ensuring that
		/// the data that is attempted to be written is within the view.
		/// </summary>
		/// <param name="data">The data that is to be read.</param>
		/// <param name="offset">The offset within this own view to write the data at.</param>
		/// <returns>The amount of bytes written.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the length of the data and the offset exceed the length of the view.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Write(ReadOnlySpan<byte> data, ulong offset)
		{
			var end = offset + (ulong)data.Length;

			if (end > _length)
			{
				ThrowHelper();
				static void ThrowHelper() => throw new InvalidOperationException($"Attempted to write data outside of the {nameof(FileStreamSection)}.");
			}

			return P.Write(_parent, data, _start + offset);
		}
	}
}