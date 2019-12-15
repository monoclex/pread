using System;
using System.IO;
using Xunit;

namespace pread.Tests
{
	public class PReadUnix
	{
		[Fact]
		public void Test1()
		{
			// unique name of file per test
			var FileName = $"test_tmp__{nameof(PReadUnix)}__{nameof(Test1)}__.txt";

			if (System.Environment.OSVersion.Platform != PlatformID.Unix)
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
			var result = pread.Unix.Pread(readBuffer, fileStream, 512);

			Assert.True(result.DidSucceed);
			Assert.Equal(128, result.Data.BytesRead);

			Assert.Equal((byte)'A', readBuffer[0]);
			Assert.Equal((byte)'A', readBuffer[^1]);
		}
	}
}
