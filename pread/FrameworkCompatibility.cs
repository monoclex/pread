// This file serves to provide functionality in newer versions to older versions for compatibility.

#if NET45
namespace System.Runtime.InteropServices
{
	internal struct OSPlatform
	{
		public static OSPlatform Windows => new OSPlatform { IsWindows = true };
		public static OSPlatform Linux => new OSPlatform { IsWindows = false };
		public static OSPlatform OSX => new OSPlatform { IsWindows = false };

		public bool IsWindows;
	}

	internal static class RuntimeInformation
	{
		public static bool IsOSPlatform(OSPlatform osPlatform) => osPlatform.IsWindows;
	}
}
#endif

#if NETSTANDARD1_3
// https://source.dot.net/#System.Private.CoreLib/NativeOverlapped.cs,e91e40b99f8bc395
using System.Runtime.InteropServices;

namespace System.Threading
{
	[StructLayout(LayoutKind.Sequential)]
	public struct NativeOverlapped
	{
		public IntPtr InternalLow;
		public IntPtr InternalHigh;
		public int OffsetLow;
		public int OffsetHigh;
		public IntPtr EventHandle;
	}
}
#endif