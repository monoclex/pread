using System;
using System.IO;

using Xunit;

namespace pread.Tests
{
	/// <summary>
	/// Platform agnostic testing of the API, through the <see cref="P"/> entrypoint.
	/// </summary>
	public class PlatformAgnostic : IDisposable
	{
		private const int FileSize = 1024;
		private const int Offset = 512;
		private const int DataLength = 128;

		private readonly FileStream _fileStream;
		private readonly string _name;

		public PlatformAgnostic()
		{
			_name = Path.GetTempFileName();
			_fileStream = File.Create(_name);
		}

#pragma warning disable CA1063 // Implement IDisposable Correctly

		public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
		{
			_fileStream.Dispose();
			File.Delete(_name);
		}

		private Span<byte> GenerateData()
		{
			Span<byte> data = new byte[FileSize];
			data.Slice(Offset, DataLength).Fill((byte)'A');
			return data;
		}

		private Span<byte> ReadFile()
		{
			_fileStream.Position = 0;
			Span<byte> data = new byte[_fileStream.Length];
			_fileStream.Read(data);
			return data;
		}

		private void AssertSpansEqual(Span<byte> a, Span<byte> b)
		{
			// TODO: more helpful information
			Assert.Equal(a.Length, b.Length);

			for (int i = 0; i < a.Length; i++)
			{
				Assert.Equal(a[i], b[i]);
			}
		}

		[Fact]
		public void Read()
		{
			var data = GenerateData();
			_fileStream.Write(data);

			Span<byte> readBuffer = stackalloc byte[DataLength];
			var bytesRead = P.Read(_fileStream, readBuffer, Offset);

			// do not test where the filestream's position is, as per the warning in P.Read

			Assert.Equal((uint)DataLength, bytesRead);

			AssertSpansEqual(data.Slice(Offset, DataLength), readBuffer);
		}

		[Fact]
		public void Write()
		{
			var data = GenerateData();
			_fileStream.SetLength(FileSize);

			var bytesWritten = P.Write(_fileStream, data.Slice(Offset, DataLength), Offset);
			_fileStream.Flush(false);
			_fileStream.Position = 0;

			// clear internal FileStream buffer so it will read data fine
			var fileData = ReadFile();

			// do not test where the filestream's position is, as per the warning in P.Write

			Assert.Equal(bytesWritten, (uint)DataLength);
			AssertSpansEqual(data, fileData);
		}
	}
}