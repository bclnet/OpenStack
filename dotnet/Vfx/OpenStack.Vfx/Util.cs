using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenStack.Vfx;

public static unsafe class EndiannessUtils {
    /// <summary>reverses pairs of octets in-place: <c>0xAABBIIJJPPQQYYZZ</c> &lt;=> <c>0xBBAAJJIIQQPPZZYY</c></summary>
    public static void MutatingByteSwap16(Span<byte> a) {
        var l = a.Length;
        Log.Assert(l % 2 is 0, "octets must be in pairs");
        fixed (byte* p = &a[0]) for (var i = 0; i < l; i += 2)
                (p[i + 1], p[i]) = (p[i], p[i + 1]);
    }

    /// <summary>reverses groups of 4 octets in-place: <c>0xAABBCCDDWWXXYYZZ</c> &lt;=> <c>0xDDCCBBAAZZYYXXWW</c></summary>
    public static void MutatingByteSwap32(Span<byte> a) {
        var l = a.Length;
        Log.Assert(l % 4 is 0, "octets must be in groups of 4");
        fixed (byte* p = &a[0]) for (var i = 0; i < l; i += 4) {
            var b = p[i];
            p[i] = p[i + 3];
            p[i + 3] = b;
            b = p[i + 1];
            p[i + 1] = p[i + 2];
            p[i + 2] = b;
        }
    }

    /// <summary>swaps pairs of 16-bit words in-place: <c>0xAABBIIJJPPQQYYZZ</c> &lt;=> <c>0xIIJJAABBYYZZPPQQ</c></summary>
    public static void MutatingShortSwap32(Span<byte> a) {
        // no need to optimise this further, it's only used in the manual byteswap tool and only between the two less common formats
        MutatingByteSwap32(a); // calls can be in either order, though this order ensures the length-mod-4 assert is hit immediately
        MutatingByteSwap16(a);
    }
}

public unsafe static class Util {

    public static byte[] ToSha256(byte[] d, int n, byte* md) {
        throw new NotImplementedException();
        //SHA256_CTX c;
        //static unsigned char m[SHA256_DIGEST_LENGTH];

        //if (md == NULL)
        //    md = m;
        //SHA256_Init(&c);
        //SHA256_Update(&c, d, n);
        //SHA256_Final(md, &c);
        //OPENSSL_cleanse(&c, sizeof(c));
        //return md;
    }

    public static void Resize<T>(this List<T> s, int size, T value) => throw new NotImplementedException();

    public static long Align(long s, long alignment) => (s + alignment - 1) / alignment * alignment;

    const long bufferSize = 0x100000;

    public static void CopyFile(this Stream dst, Stream src, long srcOffset, long size) {
        var buf = new byte[bufferSize];
        src.Seek(srcOffset, SeekOrigin.Begin);
        while (size > 0) {
            var size_ = (int)(size > bufferSize ? bufferSize : size);
            src.Read(buf, 0, size_);
            dst.Write(buf, 0, size_);
            size -= size_;
        }
    }

    public static void Seek2(this Stream src, long size) {
        throw new System.NotImplementedException();
    }

    public static void PadFile(this Stream s, long size, byte padData) {
        var buf = new byte[bufferSize];
        if (padData != 0) fixed (byte* _buf = buf) Unsafe.InitBlock(_buf, padData, (uint)bufferSize);
        while (size > 0) {
            var size_ = (int)(size > bufferSize ? bufferSize : size);
            s.Write(buf, 0, size_);
            size -= size_;
        }
    }

    public static void Read<T>(this Stream s, ref T value, int offset, int count) where T : struct {
        if (offset != 0) throw new Exception();
        var buf = stackalloc byte[count];
        s.Read(new Span<byte>(buf, count));
        value = Marshal.PtrToStructure<T>((IntPtr)buf);
    }

    public static void Write<T>(this Stream s, ref T value, int offset, int count) where T : struct {
        if (offset != 0) throw new Exception();
        var buf = stackalloc byte[count];
        Marshal.StructureToPtr(value, (IntPtr)buf, false);
        s.Write(new Span<byte>(buf, count));
    }

    public static T ToStruct<T>(byte[] s) { fixed (byte* _ = s) return Marshal.PtrToStructure<T>((IntPtr)_); }

    public static byte[] FromHexString(string s) => Enumerable.Range(0, s.Length >> 1).Select(x => byte.Parse(s.Substring(x << 1, 2), NumberStyles.HexNumber)).ToArray();
    public static string ToHexString(byte[] s) { var b = new StringBuilder(); for (var i = 0; i < s.Length; i++) b.Append(s[i].ToString("X2")); return b.ToString(); } //: return new([.. s.SelectMany(x => $"{x:02X}")]);
}
