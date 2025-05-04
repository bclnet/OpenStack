using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenStack.Rom.Nintendo._3ds;

public unsafe static class Util {
    public static long Align(long s, long alignment) => (s + alignment - 1) / alignment * alignment;

    public static void CopyFile(this Stream src, Stream dst, long srcOffset, long size) {
        const long bufferSize = 0x100000;
        var buf = new byte[bufferSize];
        src.Seek(srcOffset, SeekOrigin.Begin);
        while (size > 0) {
            var size2 = (int)(size > bufferSize ? bufferSize : size);
            src.Read(buf, 0, size2);
            dst.Write(buf, 0, size2);
            size -= size2;
        }
    }

    public static void Seek2(this Stream src, long size) {
        throw new System.NotImplementedException();
    }

    public static void PadFile(this Stream s, long padSize, byte padData) {
        const long bufferSize = 0x100000;
        var buf = new byte[bufferSize];
        if (padData != 0) fixed (byte* _buf = buf) Unsafe.InitBlock(_buf, padData, (uint)bufferSize);
        while (padSize > 0) {
            var size = (int)(padSize > bufferSize ? bufferSize : padSize);
            s.Write(buf, 0, size);
            padSize -= size;
        }
    }

    public static void Read(this Stream s, ref byte[] value, nint size, nint count) {
        value = default;
    }
    public static void Read<T>(this Stream s, ref T value, nint size, nint count) where T : struct {
        value = default;
    }

    public static void Write(this Stream s, ref byte[] value, nint size, nint count) {
        value = default;
    }
    public static void Write<T>(this Stream s, ref T value, nint size, nint count) where T : struct {
        value = default;
    }

    public static T ToStruct<T>(byte[] s) => default;
    public static byte[] ToBytes(string s) => Enumerable.Range(0, s.Length >> 1).Select(x => byte.Parse(s.Substring(x << 1, 2), NumberStyles.HexNumber)).ToArray();
    public static string FromBytes(byte[] s) => new([.. s.SelectMany(x => $"{x:02X}")]);
}
