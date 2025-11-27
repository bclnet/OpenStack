using System.IO;

namespace System;

public static class StreamExtensions {
    public static void CopyTo(this Stream src, Stream dest, long len) {
        const int size = 0x2000;
        var buffer = new byte[size];
        while (len > 0) {
            var n = src.Read(buffer, 0, (int)Math.Min(len, size));
            dest.Write(buffer, 0, n);
            len -= n;
        }
    }
}
