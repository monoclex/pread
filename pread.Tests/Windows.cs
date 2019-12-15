using System;
using System.IO;
using Xunit;

namespace pread.Tests
{
	public class PReadWindows
	{
		[Fact]
		public void Test1()
		{
			// unique name of file per test
			var FileName = $"test_tmp__{nameof(PReadWindows)}__{nameof(Test1)}__.txt";

			if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				// pass
				return;
			}

			if (File.Exists(FileName))
			{
				try
				{
					File.Delete(FileName);
				}
				catch (IOException) { }
			}

			var fileStream = File.Create(FileName);

			// put in some 'garbage data': 1024 bytes
			// we'll want to read 128 bytes after the 512 offset, so let's put some data there

			Span<byte> buffer = stackalloc byte[1024];
			buffer.Slice(512, 128).Fill((byte)'A');

			fileStream.Write(buffer);
			fileStream.Position = 0;

			// check if pread works
			Span<byte> readBuffer = stackalloc byte[128];
			var result = pread.Windows.Pread(readBuffer, fileStream, 512);

			Assert.True(result.DidSucceed);
			Assert.Equal<uint>(128, result.Data.Bytes);

			Assert.Equal((byte)'A', readBuffer[0]);
			Assert.Equal((byte)'A', readBuffer[^1]);
		}

		[Fact]
		public void Test2()
		{
			// unique name of file per test
			var FileName = $"test_tmp__{nameof(PReadWindows)}__{nameof(Test2)}__.txt";

			if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				// pass
				return;
			}

			if (File.Exists(FileName))
			{
				try
				{
					File.Delete(FileName);
				}
				catch (IOException) { }
			}

			var fileStream = File.Create(FileName);
			fileStream.SetLength(1024);

			Span<byte> buffer = stackalloc byte[128];
			buffer.Fill((byte)'A');

			var result = pread.Windows.Pwrite(buffer, fileStream, 512);
			fileStream.Flush(false); // clear internal buffers
			fileStream.Position = 0; // go to beginning

			Assert.True(result.DidSucceed);
			Assert.Equal(128u, result.Data.Bytes);

			buffer = stackalloc byte[512];
			fileStream.Read(buffer);

			Assert.NotEqual((byte)'A', buffer[0]);
			Assert.NotEqual((byte)'A', buffer[512 - 1]);

			fileStream.Read(buffer);

			Assert.Equal((byte)'A', buffer[0]);
			Assert.Equal((byte)'A', buffer[128 - 1]);
			Assert.NotEqual((byte)'A', buffer[512 - 1]);
		}
	}
}
