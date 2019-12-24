using System;

using Xunit;

namespace pread.Tests
{
	partial class Errors
	{
		public class Windows
		{
			[Fact]
			public void When_StringErrorPassed2_Returns_ERROR_FILE_NOT_FOUND()
			{
				if (Implementations.Windows.MachineIsWindows)
				{
					// https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
					var error = pread.Implementations.Windows.StringError(2);
					Assert.Equal("The system cannot find the file specified.", error);
				}
			}
		}
	}
}