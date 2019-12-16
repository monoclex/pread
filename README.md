<p align="center"><img src="https://gitcdn.xyz/repo/SirJosh3917/pread/master/logo.png" alt="pread(2)"/></p>

# pread
Perform atomic seek and read/write operations in C#. Named after "pread", a POSIX function for atomically seeking and reading.

| Windows | Linux |
| :--: | :--: |
| [![Build status](https://dev.azure.com/SirJosh3917/pread/_apis/build/status/pipelines-win.yml/windows-tests)](https://dev.azure.com/SirJosh3917/pread/_build/latest?definitionId=4) | [![Build status](https://dev.azure.com/SirJosh3917/pread/_apis/build/status/devops.yml/linux-tests)](https://dev.azure.com/SirJosh3917/pread/_build/latest?definitionId=3) |

## Install pread
[![Nuget](https://img.shields.io/nuget/v/pread?style=flat-square)](https://www.nuget.org/packages/pread)
```
Install-Package pread
```

# About

Streams in C# do not support atomic seek and read operations, meaning you can't read data at an arbitrary position in a file without some kind of locking mechanisms. At the OS level, this is a supported operation - on unix, there's [pread](http://man7.org/linux/man-pages/man2/pwrite.2.html), and on Windows you simply need to pass in the correct data to `Offset` and `OffsetHigh` to an [OVERLAPPED](https://docs.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-overlapped) when calling [ReadFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile). If that sounds like a bunch of mumbo jumbo to you, that's because it is. It's a shame that C# does not support an atomic seek and read/write operation - even [Java does!](https://docs.oracle.com/javase/7/docs/api/java/nio/channels/FileChannel.html#read(java.nio.ByteBuffer,%20long))

Enter `pread`. `pread` exposes a simple api, `P.Read(FileStream, Span<byte>, ulong)` and `P.Read(FileStream, ReadOnlySpan<byte>, ulong)` which wrap around the native unix methods [pread and pwrite](http://man7.org/linux/man-pages/man2/pwrite.2.html) on linux and wraps around [ReadFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile) and [WriteFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile) on windows.

# Performance

Comparison between using `P.Read` versus `Stream.Seek`ing to a position, calling `Stream.Read`, then `Stream.Seek`ing back to the origin.

It should be noted that even if the seek call back to the origin is elided, it only saves about 0.2ns

|            Method |     Mean |     Error |    StdDev |
|------------------ |---------:|----------:|----------:|
|             Pread | 1.984 us | 0.0264 us | 0.0234 us |
| StreamReadAndSeek | 2.253 us | 0.0291 us | 0.0272 us |

|            Method |       Mean |    Error |   StdDev |
|------------------ |-----------:|---------:|---------:|
|             Pread |   896.1 ns | 16.20 ns | 15.15 ns |
| StreamReadAndSeek | 1,974.0 ns | 49.80 ns | 46.58 ns |

# Usage

The main API for this library is `P.Read` and `P.Write`. However, there are various abstractions built on top of these methods that are recommended to be used instead due to the increase safety they provide.

### FileStreamSection

If you only want a caller to be able to view (read and write) to a specific section in a file, `FileStreamSection` is the ideal choice. As it's a struct, it's allocation free and optimal for making big or small views.

```cs
var section = new FileStreamSection(fileStream);

// similar to span
var newSection = section.Slice(256, 256);

var buffer = new byte[128];
// writing is the same
int readBytes = section.Read(buffer);
readBytes = section.Read(buffer, 128);
```

### FileStreamSectionStream

If you have existing APIs that utilize `Stream` behavior, `FileStreamSectionStream` is a wrapper over `FileStreamSection` which provides `Stream` like behavior.

Unlike a FileStream, calling `Length` and `Position` has no performance cost, and you can do so freely. It is also noted that assigning to`Position` is preferred to calling `Seek`.

```cs
var section = new FileStreamSection(fileStream);
var stream = new FileStreamSectionStream(section);
var stream2 = new FileStreamSectionStream(section.Slice(128, 128));

var buffer = new byte[64];

// using `stream` and `stream2` don't affect eachother. they both maintain
// their own position of where the are in a file and make calls to P.Read/Write
// allowing you to have concurrent readers on the same FileStream.

int readBytes = stream.Read(buffer);
stream2.Read(buffer);
stream2.Read(buffer);
readBytes = stream.Read(buffer);

stream.Write(buffer);
```