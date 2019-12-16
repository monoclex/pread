using System;
using System.IO;

namespace pread
{
	/// <summary>
	/// Provides a <see cref="Stream"/> wrapper around a <see cref="FileStreamSection"/>.
	/// </summary>
	public class FileStreamSectionStream : Stream
	{
		private readonly FileStreamSection _fileStreamSection;

		/// <summary>
		/// The underlying <see cref="FileStreamSection"/> in use.
		/// </summary>
		public FileStreamSection FileStreamSection => _fileStreamSection;

		public FileStreamSectionStream(FileStreamSection fileStreamSection)
		{
			if (fileStreamSection._parent == null)
			{
				throw new ArgumentNullException(nameof(fileStreamSection), "Expected fileStreamSection to have a non null FileStream.");
			}

			_fileStreamSection = fileStreamSection;
		}

		public override bool CanRead => _fileStreamSection._parent.CanRead;
		public override bool CanSeek => true;
		public override bool CanWrite => _fileStreamSection._parent.CanWrite;
		public override long Length => (long)_fileStreamSection.Length;

		private ulong _position;

		/// <summary>
		/// Unlike <see cref="FileStream"/>, <see cref="Position"/> has no hidden performance cost to it.
		/// </summary>
		public override long Position
		{
			get => (long)_position;
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				var ul = (ulong)value;

				if (ul > _fileStreamSection.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				_position = ul;
			}
		}

		public override int Read(Span<byte> buffer)
		{
			var result = _fileStreamSection.Read(buffer, _position);
			_position += result;
			return (int)result;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			var bytesWritten = _fileStreamSection.Write(buffer, _position);
			_position += bytesWritten;

			if (bytesWritten != buffer.Length)
			{
				ThrowHelper(buffer, bytesWritten);
				static void ThrowHelper(ReadOnlySpan<byte> buffer, uint bytesWritten) => throw new InvalidOperationException($"Unable to write {buffer.Length} bytes - only wrote {bytesWritten}.");
			}
		}

		/// <summary>
		/// Prefer to use 'Position', as it is faster.
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin: Position = offset; break;
				case SeekOrigin.Current: Position += offset; break;
				case SeekOrigin.End: Position = Length - offset; break;
			}

			return Position;
		}

		public override void SetLength(long value) => throw new InvalidOperationException();

		public override int Read(byte[] buffer, int offset, int count) => Read(new Span<byte>(buffer).Slice(offset, count));

		public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer).Slice(offset, count));

		/// <summary>
		/// Does nothing, as there is no built in buffer.
		/// </summary>
		public override void Flush() { }
	}
}