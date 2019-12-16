using System;

using Xunit;

namespace pread.Tests
{
	public partial class Errors
	{
		public class Unix
		{
			[Fact]
			public void When_StringErrorPassed2_Returns_ENOENT()
			{
				if (Environment.OSVersion.Platform != PlatformID.Unix)
				{
					return;
				}

				// http://man7.org/linux/man-pages/man3/errno.3.html
				var error = pread.Implementations.Unix.StringError(2);
				Assert.Equal("No such file or directory", error);
			}
		}
	}
}