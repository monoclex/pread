using System;
using System.IO;

using Xunit;

namespace pread.Tests
{
	public class FileStreamSectionTests
	{
		private readonly FileStream _fileStream;
		private readonly FileStreamSection _root;
		private readonly string _name;

		public FileStreamSectionTests()
		{
			_name = Path.GetTempFileName();
			_fileStream = File.Create(_name);
			_fileStream.SetLength(1024);
			_root = new FileStreamSection(_fileStream);
		}

		[Fact]
		public void DoesRead_WhenDataIsSmallEnough()
		{
			// not suppose to test P.Read, just ensure no exceptions are thrown
			Assert.Equal(512u, _root.Read(new byte[512]));
		}

		[Fact]
		public void DoesRead_WhenOffsetAndDataIsWithinFile()
		{
			// not suppose to test P.Read, just ensure no exceptions are thrown
			Assert.Equal(512u, _root.Read(new byte[512], 256));
		}

		[Fact]
		public void DoesWrite_WhenDataIsSmallEnough()
		{
			// not suppose to test P.Read, just ensure no exceptions are thrown
			Assert.Equal(512u, _root.Write(new byte[512]));
		}

		[Fact]
		public void DoesWrite_WhenOffsetAndDataIsWithinFile()
		{
			// not suppose to test P.Read, just ensure no exceptions are thrown
			Assert.Equal(512u, _root.Write(new byte[512], 256));
		}

		[Fact]
		public void NewFileStreamSection_ThrowsOutOfRange_WhenStartIsOutOfRange()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamSection(_fileStream, 2048, 0));
		}

		[Fact]
		public void NewFileStreamSection_ThrowsOutOfRange_WhenGivenOutOfRangeData()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamSection(_fileStream, 512, 1024));
		}

		[Fact]
		public void OutOfBoundsSlice_ThrowsOutOfRangeException_WhenGivenOutOfRangeData()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _root.Slice(512, 1024));
		}

		[Fact]
		public void Read_ThrowsOutOfRangeException_WhenReadingTooMuch()
		{
			Assert.Throws<InvalidOperationException>(() => _root.Read(new byte[2048]));
		}

		[Fact]
		public void Read_ThrowsOutOfRangeException_WhenReadingOutOfBounds()
		{
			Assert.Throws<InvalidOperationException>(() => _root.Read(new byte[1024], 512));
		}

		[Fact]
		public void Write_ThrowsOutOfRangeException_WhenWritingTooMuch()
		{
			Assert.Throws<InvalidOperationException>(() => _root.Write(new byte[2048]));
		}

		[Fact]
		public void Write_ThrowsOutOfRangeException_WhenWritingOutOfBounds()
		{
			Assert.Throws<InvalidOperationException>(() => _root.Write(new byte[1024], 512));
		}
	}
}