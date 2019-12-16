using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace pread.Tests
{
	public class FileStreamSectionStreamTests
	{
		private readonly FileStream _fileStream;
		private readonly FileStreamSection _root;
		private readonly FileStreamSectionStream _stream;
		private readonly string _name;

		public FileStreamSectionStreamTests()
		{
			_name = Path.GetTempFileName();
			_fileStream = File.Create(_name);
			_fileStream.SetLength(1024);
			_root = new FileStreamSection(_fileStream);
			_stream = new FileStreamSectionStream(_root);
		}

		[Fact]
		public void FileStreamSections_Equivalent()
		{
			Assert.Equal(_root, _stream.FileStreamSection);
		}

		[Fact]
		public void ReadWrite_Integration()
		{
			Span<byte> data = stackalloc byte[512];
			data.Fill((byte)'A');
			_stream.Write(data);

			data.Slice(256).Fill((byte)'B');
			_stream.Write(data.Slice(256));

			_stream.Position = 0;

			var readBytes = _stream.Read(data);
			Assert.Equal(512, readBytes);

			for (var i = 0; i < 512; i++)
			{
				Assert.Equal((byte)'A', data[i]);
			}

			readBytes = _stream.Read(data);
			Assert.Equal(512, readBytes);

			for (var i = 0; i < 256; i++)
			{
				Assert.Equal((byte)'B', data[i]);
			}

			for (var i = 0; i < 256; i++)
			{
				Assert.Equal(0, data[256 + i]);
			}
		}

		[Fact]
		public void Writes_IncreasePosition()
		{
			Assert.Equal(0, _stream.Position);

			_stream.Write(new byte[128]);
			Assert.Equal(128, _stream.Position);

			_stream.Write(new byte[256]);
			Assert.Equal(128 + 256, _stream.Position);
		}

		[Fact]
		public void Reads_IncreasePosition()
		{
			Assert.Equal(0, _stream.Position);

			_stream.Read(new byte[128]);
			Assert.Equal(128, _stream.Position);

			_stream.Read(new byte[256]);
			Assert.Equal(128 + 256, _stream.Position);
		}

		[Fact]
		public void Seek_Begin()
		{
			_stream.Seek(0, SeekOrigin.Begin);
			Assert.Equal(0, _stream.Position);

			_stream.Seek(128, SeekOrigin.Begin);
			Assert.Equal(128, _stream.Position);

			_stream.Seek(256, SeekOrigin.Begin);
			Assert.Equal(256, _stream.Position);
		}

		[Fact]
		public void Seek_Current()
		{
			_stream.Seek(0, SeekOrigin.Current);
			Assert.Equal(0, _stream.Position);

			_stream.Seek(128, SeekOrigin.Current);
			Assert.Equal(128, _stream.Position);

			_stream.Seek(256, SeekOrigin.Current);
			Assert.Equal(128 + 256, _stream.Position);
		}

		[Fact]
		public void Seek_End()
		{
			_stream.Seek(0, SeekOrigin.End);
			Assert.Equal(1024, _stream.Position);

			_stream.Seek(128, SeekOrigin.End);
			Assert.Equal(1024 - 128, _stream.Position);

			_stream.Seek(256, SeekOrigin.End);
			Assert.Equal(1024 - 256, _stream.Position);
		}

		[Fact]
		public void Read_ThrowsOnBigBuffer()
		{
			Assert.Throws<InvalidOperationException>(() => _stream.Read(new byte[2048]));
		}

		[Fact]
		public void Write_ThrowsOnBigBuffer()
		{
			Assert.Throws<InvalidOperationException>(() => _stream.Write(new byte[2048]));
		}

		[Fact]
		public void ThrowsArgumentNullException_WhenFileStreamSectionStream_IsConstructed_WithoutAnInitializedFileStreamSection()
		{
			Assert.Throws<ArgumentNullException>(() => new FileStreamSectionStream(default));
		}

		[Fact]
		public void ThrowsArgumentOutOfRangeException_WhenStreamPosition_IsSetNegative()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _stream.Position = -1);
		}

		[Fact]
		public void ThrowsArgumentOutOfRangeException_WhenStreamPosition_IsOutOfBounds()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _stream.Position = (long)_root.Length + 1);
		}

		[Fact]
		public void StreamPosition_ActsLikeAProperty()
		{
			_stream.Position = 512;
			Assert.Equal(512, _stream.Position);
		}

		[Fact]
		public void StreamProperites_AreWhatTheyShouldBe()
		{
			Assert.Equal(_fileStream.CanRead, _stream.CanRead);
			Assert.True(_stream.CanSeek);
			Assert.Equal(_fileStream.CanWrite, _stream.CanWrite);
			Assert.Equal(_root.Length, (ulong)_stream.Length);
		}
	}
}
