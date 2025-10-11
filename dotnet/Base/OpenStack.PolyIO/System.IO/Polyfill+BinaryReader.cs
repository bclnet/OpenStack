using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.UnsafeX;

namespace System.IO;

public static partial class Polyfill {
    #region Base

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadBytes(this BinaryReader source, uint count) => source.ReadBytes((int)count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this BinaryReader source, Stream destination, bool resetAfter = true) {
        source.BaseStream.CopyTo(destination);
        if (resetAfter) destination.Position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadToEnd(this BinaryReader source) {
        var length = (int)(source.BaseStream.Length - source.BaseStream.Position);
        return source.ReadBytes(length);
    }
    //public static void ReadToEnd(this BinaryReader source, byte[] buffer, int startIndex = 0)
    //{
    //    var length = (int)source.BaseStream.Length - source.BaseStream.Position;
    //    Debug.Assert(startIndex >= 0 && length <= int.MaxValue && startIndex + length <= buffer.Length);
    //    source.Read(buffer, startIndex, (int)length);
    //}
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadToValue(this BinaryReader source, byte value = 0, int length = int.MaxValue, MemoryStream ms = null) {
        if (ms == null) ms = new MemoryStream();
        else ms.SetLength(0);
        byte c; length = Math.Min(length, (int)(source.BaseStream.Length - source.BaseStream.Position));
        while (length-- > 0 && (c = source.ReadByte()) != value) ms.WriteByte(c);
        return ms.ToArray();
    }

    public static StreamReader ToStream(this BinaryReader source) => new StreamReader(source.BaseStream);

    #endregion

    #region Primitives

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double ReadDoubleBigEndian(ReadOnlySpan<byte> source) {
        return BitConverter.IsLittleEndian ?
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(source))) :
            MemoryMarshal.Read<double>(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float ReadSingleBigEndian(ReadOnlySpan<byte> source) {
        return BitConverter.IsLittleEndian ?
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(source))) :
            MemoryMarshal.Read<float>(source);
    }

    static ReadOnlySpan<byte> InternalRead(this BinaryReader source, Span<byte> buffer) {
        Debug.Assert(buffer.Length != 1, "length of 1 should use ReadByte.");
        source.Read(buffer);
        return buffer;
    }

    // primatives : bytes
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL8Bytes(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = source.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? source.ReadBytes(length) : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL16Bytes(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = source.ReadUInt16X(endian); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? source.ReadBytes(length) : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL32Bytes(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = (int)source.ReadUInt32X(endian); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? source.ReadBytes(length) : null; }

    // primatives : endian
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static double ReadDoubleE(this BinaryReader source) => ReadDoubleBigEndian(InternalRead(source, stackalloc byte[sizeof(double)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static short ReadInt16E(this BinaryReader source) => BinaryPrimitives.ReadInt16BigEndian(InternalRead(source, stackalloc byte[sizeof(short)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ReadInt32E(this BinaryReader source) => BinaryPrimitives.ReadInt32BigEndian(InternalRead(source, stackalloc byte[sizeof(int)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long ReadInt64E(this BinaryReader source) => BinaryPrimitives.ReadInt64BigEndian(InternalRead(source, stackalloc byte[sizeof(long)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float ReadSingleE(this BinaryReader source) => ReadSingleBigEndian(InternalRead(source, stackalloc byte[sizeof(float)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort ReadUInt16E(this BinaryReader source) => BinaryPrimitives.ReadUInt16BigEndian(InternalRead(source, stackalloc byte[sizeof(ushort)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint ReadUInt32E(this BinaryReader source) => BinaryPrimitives.ReadUInt32BigEndian(InternalRead(source, stackalloc byte[sizeof(uint)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong ReadUInt64E(this BinaryReader source) => BinaryPrimitives.ReadUInt64BigEndian(InternalRead(source, stackalloc byte[sizeof(ulong)]));

    // primatives : endianX
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static double ReadDoubleX(this BinaryReader source, bool endian) => endian ? source.ReadDoubleE() : source.ReadDouble();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static short ReadInt16X(this BinaryReader source, bool endian) => endian ? source.ReadInt16E() : source.ReadInt16();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ReadInt32X(this BinaryReader source, bool endian) => endian ? source.ReadInt32E() : source.ReadInt32();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long ReadInt64X(this BinaryReader source, bool endian) => endian ? source.ReadInt64E() : source.ReadInt64();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float ReadSingleX(this BinaryReader source, bool endian) => endian ? source.ReadSingleE() : source.ReadSingle();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort ReadUInt16X(this BinaryReader source, bool endian) => endian ? source.ReadUInt16E() : source.ReadUInt16();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint ReadUInt32X(this BinaryReader source, bool endian) => endian ? source.ReadUInt32E() : source.ReadUInt32();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong ReadUInt64X(this BinaryReader source, bool endian) => endian ? source.ReadUInt64E() : source.ReadUInt64();

    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double ReadDoubleX(this BinaryReader source, bool endian) { if (!endian) return source.ReadDouble(); var bytes = source.ReadBytes(sizeof(double)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToDouble(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static short ReadInt16X(this BinaryReader source, bool endian) { if (!endian) return source.ReadInt16(); var bytes = source.ReadBytes(sizeof(short)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt16(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ReadInt32X(this BinaryReader source, bool endian) { if (!endian) return source.ReadInt32(); var bytes = source.ReadBytes(sizeof(int)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt32(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static long ReadInt64X(this BinaryReader source, bool endian) { if (!endian) return source.ReadInt64(); var bytes = source.ReadBytes(sizeof(long)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt64(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float ReadSingleX(this BinaryReader source, bool endian) { if (!endian) return source.ReadSingle(); var bytes = source.ReadBytes(sizeof(float)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToSingle(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort ReadUInt16X(this BinaryReader source, bool endian) { if (!endian) return source.ReadUInt16(); var bytes = source.ReadBytes(sizeof(ushort)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt16(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint ReadUInt32X(this BinaryReader source, bool endian) { if (!endian) return source.ReadUInt32(); var bytes = source.ReadBytes(sizeof(uint)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt32(bytes, 0); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong ReadUInt64X(this BinaryReader source, bool endian) { if (!endian) return source.ReadUInt64(); var bytes = source.ReadBytes(sizeof(ulong)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt64(bytes, 0); }

    // primatives : specialized
    /// <summary>
    /// A Compressed UInt32 can be 1, 2, or 4 bytes.<para />
    /// If the first MSB (0x80) is 0, it is one byte.<para />
    /// If the first MSB (0x80) is set and the second MSB (0x40) is 0, it's 2 bytes.<para />
    /// If both (0x80) and (0x40) are set, it's 4 bytes.
    /// </summary>
    public static uint ReadCInt32(this BinaryReader source) {
        var b0 = source.ReadByte(); if ((b0 & 0x80) == 0) return b0;
        var b1 = source.ReadByte(); if ((b0 & 0x40) == 0) return (uint)(((b0 & 0x7F) << 8) | b1);
        var s = source.ReadUInt16(); return (uint)(((((b0 & 0x3F) << 8) | b1) << 16) | s);
    }
    public static uint ReadCInt32X(this BinaryReader source, bool endian = true) {
        if (!endian) return source.ReadCInt32();
        throw new NotImplementedException();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool ReadBool32(this BinaryReader source) => source.ReadUInt32() != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Guid ReadGuid(this BinaryReader source) => new Guid(source.ReadBytes(16));

    #endregion

    #region Position

    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Align(this BinaryReader source) { var alignDelta = source.BaseStream.Position % 4; if (alignDelta != 0) source.BaseStream.Position += (int)(4 - alignDelta); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader Align(this BinaryReader source, int align = 4) { source.BaseStream.Position = (source.BaseStream.Position + --align) & ~align; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long Tell(this BinaryReader source) => source.BaseStream.Position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader Seek(this BinaryReader source, long offset) { source.BaseStream.Position = offset; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader SeekAndAlign(this BinaryReader source, long offset, int align = 4) { source.BaseStream.Position = offset % align != 0 ? offset + align - (offset % align) : offset; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader Skip(this BinaryReader source, long count) { source.BaseStream.Position += count; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader SkipAndAlign(this BinaryReader source, long count, int align = 4) { var offset = source.BaseStream.Position + count; source.BaseStream.Position = offset % align != 0 ? offset + align - (offset % align) : offset; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader End(this BinaryReader source, long offset) { source.BaseStream.Seek(offset, SeekOrigin.End); return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Peek(this BinaryReader source, Action<BinaryReader> action, long offset = 0L, SeekOrigin origin = SeekOrigin.Current) {
        var pos = source.BaseStream.Position;
        source.BaseStream.Seek(offset, origin);
        action(source);
        source.BaseStream.Position = pos;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Peek<T>(this BinaryReader source, Func<BinaryReader, T> action, long offset = 0L, SeekOrigin origin = SeekOrigin.Current) {
        var pos = source.BaseStream.Position;
        source.BaseStream.Seek(offset, origin);
        var value = action(source);
        source.BaseStream.Position = pos;
        return value;
    }

    #endregion

    #region String

    // String : Special

    public static string ReadL16OString(this BinaryReader source, int codepage = 1252) //: ReadObfuscatedString
    {
        var length = source.ReadUInt16();
        if (length == 0) return string.Empty;
        var bytes = source.ReadBytes(length);
        // flip the bytes in the string to undo the obfuscation: i.e. 0xAB => 0xBA
        for (var i = 0; i < length; i++) bytes[i] = (byte)((bytes[i] >> 4) | (bytes[i] << 4));
        return Encoding.GetEncoding(codepage).GetString(bytes);
    }

    // String : Length

    /// <summary>
    /// Read a Length-prefixed string from the stream
    /// </summary>
    /// <param name="source"></param>
    /// <param name="byteLength">Size of the Length representation</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8UString(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = source.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(source.ReadChars(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16UString(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = source.ReadUInt16X(endian); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(source.ReadChars(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32UString(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = (int)source.ReadUInt32X(endian); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(source.ReadChars(length), 0, length).TrimEnd('\0') : null; }

    /// <summary>
    /// Read a Length-prefixed ascii string from the stream
    /// </summary>
    /// <param name="source"></param>
    /// <param name="byteLength">Size of the Length representation</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8AString(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = source.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(source.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16AString(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = source.ReadUInt16X(endian); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(source.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32AString(this BinaryReader source, int maxLength = 0, bool endian = false) { var length = (int)source.ReadUInt32X(endian); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(source.ReadBytes(length), 0, length).TrimEnd('\0') : null; }

    public static string ReadC32WString(this BinaryReader source) {
        var length = source.ReadCInt32();
        if (length == 0) return null;
        var b = new StringBuilder();
        for (var i = 0; i < length; i++) b.Append(Convert.ToChar(source.ReadUInt16()));
        return b.ToString();
    }

    // String : Fixed

    /// <summary>
    /// Read a Fixed-Length string from the stream
    /// </summary>
    /// <param name="source"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadFUString(this BinaryReader source, int length) => length != 0 ? new string(source.ReadChars(length), 0, length).TrimEnd('\0') : null;

    /// <summary>
    /// Read a Fixed-Length utf8 string from the stream
    /// </summary>
    /// <param name="source"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadF8String(this BinaryReader source, int length) => length != 0 ? Encoding.UTF8.GetString(source.ReadBytes(length), 0, length).TrimEnd('\0') : null;

    /// <summary>
    /// Read a Fixed-Length ascii string from the stream
    /// </summary>
    /// <param name="source"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadFAString(this BinaryReader source, int length) => length != 0 ? Encoding.ASCII.GetString(source.ReadBytes(length), 0, length).TrimEnd('\0') : null;

    #region not used

    //public static string ReadLString(this BinaryReader source, int byteLength = 4, bool zstring = false) //:was ReadPString
    //{
    //    var length = byteLength switch
    //    {
    //        1 => source.ReadByte(),
    //        2 => source.ReadInt16(),
    //        4 => source.ReadInt32(),
    //        _ => throw new NotSupportedException("Only Int8, Int16, and Int32 string sizes are supported"),
    //    };
    //    return length > 0 ? new string(source.ReadChars(length), 0, zstring ? length - 1 : length) : null;
    //}
    //public static string ReadLAString(this BinaryReader source, int byteLength = 4, bool zstring = false)
    //{
    //    var length = byteLength switch
    //    {
    //        1 => source.ReadByte(),
    //        2 => source.ReadInt16(),
    //        4 => source.ReadInt32(),
    //        _ => throw new NotSupportedException("Only Int8, Int16, and Int32 string sizes are supported"),
    //    };
    //    return length != 0 ? Encoding.ASCII.GetString(source.ReadBytes(length), 0, zstring ? length - 1 : length) : null;
    //}
    //public static string ReadFCString(this BinaryReader source, int length)
    //{
    //    if (length == 0) return null;
    //    var chars = source.ReadChars(length);
    //    for (var i = 0; i < length; i++) if (chars[i] == 0) return new string(chars, 0, i);
    //    return new string(chars);
    //}
    //public static string ReadZString(this BinaryReader source, int length, Encoding encoding = null)
    //{
    //    var buf = source.ReadBytes(length);
    //    int i;
    //    for (i = buf.Length - 1; i >= 0 && buf[i] == 0; i--) { }
    //    return (encoding ?? Encoding.ASCII).GetString(buf, 0, i + 1);
    //}
    //public static string ReadCString(this BinaryReader source, int length, Encoding encoding = null)
    //{
    //    var buf = source.ReadBytes(length);
    //    int i;
    //    for (i = 0; i < buf.Length && buf[i] != 0; i++) { }
    //    return (encoding ?? Encoding.ASCII).GetString(buf, 0, i);
    //}
    //public static string ReadZString2(this BinaryReader source) //:was ReadCString (Dolkens)
    //{
    //    var length = 0;
    //    var maxPosition = source.BaseStream.Length;
    //    while (source.BaseStream.Position < maxPosition && source.ReadChar() != 0) length++;
    //    var nul = source.BaseStream.Position;
    //    source.BaseStream.Seek(0 - length - 1, SeekOrigin.Current);
    //    var chars = source.ReadChars(length + 1);
    //    source.BaseStream.Seek(nul, SeekOrigin.Begin);
    //    return length > 0 ? new string(chars, 0, length).Replace("\u0000", "") : null;
    //}

    #endregion

    // String : Variable

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadVUString(this BinaryReader source, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) => Encoding.UTF8.GetString(source.ReadToValue(stopValue, length, ms));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadVAString(this BinaryReader source, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) => Encoding.ASCII.GetString(source.ReadToValue(stopValue, length, ms));

    // String : Encoding

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadEncoding(this BinaryReader source, int length, Encoding encoding = null) => length > 0 ? (encoding ?? Encoding.Default).GetString(source.ReadBytes(length)) : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8Encoding(this BinaryReader source, Encoding encoding = null) { var length = source.ReadByte(); return length > 0 ? (encoding ?? Encoding.ASCII).GetString(source.ReadBytes(length)) : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16Encoding(this BinaryReader source, Encoding encoding = null) { var length = source.ReadUInt16(); return length > 0 ? (encoding ?? Encoding.ASCII).GetString(source.ReadBytes(length)) : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32Encoding(this BinaryReader source, Encoding encoding = null) { var length = (int)source.ReadUInt32(); return length > 0 ? (encoding ?? Encoding.ASCII).GetString(source.ReadBytes(length)) : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadC32Encoding(this BinaryReader source, Encoding encoding = null) { var length = (int)source.ReadCInt32(); return length > 0 ? (encoding ?? Encoding.ASCII).GetString(source.ReadBytes(length)) : null; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadYEncoding(this BinaryReader source, int length, Encoding encoding = null) { var bytes = source.ReadBytes(length); return (encoding ?? Encoding.ASCII).GetString(bytes, 0, bytes[^1] != 0 ? bytes.Length : bytes.Length - 1); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16YEncoding(this BinaryReader source, Encoding encoding = null) { var length = source.ReadUInt16(); if (length == 0) return null; var bytes = source.ReadBytes(length); return (encoding ?? Encoding.ASCII).GetString(bytes, 0, bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32YEncoding(this BinaryReader source, Encoding encoding = null) { var length = (int)source.ReadUInt32(); if (length == 0) return null; var bytes = source.ReadBytes(length); return (encoding ?? Encoding.ASCII).GetString(bytes, 0, bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadC32YEncoding(this BinaryReader source, Encoding encoding = null) { var length = (int)source.ReadCInt32(); if (length == 0) return null; var bytes = source.ReadBytes(length); return (encoding ?? Encoding.ASCII).GetString(bytes, 0, bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length); }


    public static string ReadZEncoding(this BinaryReader source, Encoding encoding) {
        var characterSize = encoding.GetByteCount("e");
        using var s = new MemoryStream();
        while (true) {
            var data = new byte[characterSize];
            source.Read(data, 0, characterSize);
            if (encoding.GetString(data, 0, characterSize) == "\0") break;
            s.Write(data, 0, data.Length);
        }
        return encoding.GetString(s.ToArray());
    }

    public static string[] ReadCStringArray(this BinaryReader source, int count, StringBuilder buf = null) {
        if (buf == null) buf = new StringBuilder();
        var list = new List<string>();
        for (var i = 0; i < count; i++) {
            var c = source.ReadChar();
            while (c != 0) { buf.Append(c); c = source.ReadChar(); }
            list.Add(buf.ToString());
            buf.Clear();
        }
        return list.ToArray();
    }


    public static List<string> ReadZAStringList(this BinaryReader source, int length = int.MaxValue) {
        var buf = new MemoryStream();
        var list = new List<string>();
        byte c;
        while (length > 0) {
            buf.SetLength(0);
            while (length-- > 0 && (c = source.ReadByte()) != 0) buf.WriteByte(c);
            list.Add(Encoding.ASCII.GetString(buf.ToArray()));
        }
        return list;
    }

    public static string ReadO32Encoding(this BinaryReader source, Encoding encoding) {
        var currentOffset = source.BaseStream.Position;
        var offset = source.ReadUInt32();
        if (offset == 0) return string.Empty;
        source.BaseStream.Position = currentOffset + offset;
        var str = ReadZEncoding(source, encoding);
        source.BaseStream.Position = currentOffset + 4;
        return str;
    }

    //: TODO Use Encoding Method
    public static string ReadO32UTF8(this BinaryReader source) {
        var currentOffset = source.BaseStream.Position;
        var offset = source.ReadUInt32();
        if (offset == 0) return string.Empty;
        source.BaseStream.Position = currentOffset + offset;
        var str = ReadVUString(source);
        source.BaseStream.Position = currentOffset + 4;
        return str;
    }

    #endregion

    #region Struct

    //var abc = MemoryMarshal.Cast<byte, ushort>(data);

    // Struct : Single
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadF<T>(this BinaryReader source, Func<BinaryReader, T> factory) => factory(source);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadP<T>(this BinaryReader source, string pat) where T : struct => MarshalP<T>(pat, source.ReadBytes);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadS<T>(this BinaryReader source, int sizeOf = 0) where T : struct => MarshalS<T>(source.ReadBytes, sizeOf);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadT<T>(this BinaryReader source, int sizeOf) where T : struct => MarshalT<T>(source.ReadBytes(sizeOf));

    // Struct : Array - Factory
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8FArray<T>(this BinaryReader source, Func<BinaryReader, T> factory) => ReadFArray(source, factory, source.ReadByte());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16FArray<T>(this BinaryReader source, Func<BinaryReader, T> factory, bool endian = false) => ReadFArray(source, factory, source.ReadUInt16X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32FArray<T>(this BinaryReader source, Func<BinaryReader, T> factory, bool endian = false) => ReadFArray(source, factory, (int)source.ReadUInt32X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadC32FArray<T>(this BinaryReader source, Func<BinaryReader, T> factory, bool endian = false) => ReadFArray(source, factory, (int)source.ReadCInt32X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadFArray<T>(this BinaryReader source, Func<BinaryReader, T> factory, uint count) => ReadFArray(source, factory, (int)count);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadFArray<T>(this BinaryReader source, Func<BinaryReader, T> factory, int count) { var list = new T[count]; if (count > 0) for (var i = 0; i < list.Length; i++) list[i] = factory(source); return list; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadFArray<T>(this BinaryReader source, Func<BinaryReader, int, T> factory, int count) { var list = new T[count]; if (count > 0) for (var i = 0; i < list.Length; i++) list[i] = factory(source, i); return list; }

    // Struct : Array - Pattern
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8PArray<T>(this BinaryReader source, string pat) where T : struct => ReadPArray<T>(source, pat, source.ReadByte());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16PArray<T>(this BinaryReader source, string pat, bool endian = false) where T : struct => ReadPArray<T>(source, pat, source.ReadUInt16X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32PArray<T>(this BinaryReader source, string pat, bool endian = false) where T : struct => ReadPArray<T>(source, pat, (int)source.ReadUInt32X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadC32PArray<T>(this BinaryReader source, string pat, bool endian = false) where T : struct => ReadPArray<T>(source, pat, (int)source.ReadCInt32X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadPArray<T>(this BinaryReader source, string pat, uint count) where T : struct => ReadPArray<T>(source, pat, (int)count);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadPArray<T>(this BinaryReader source, string pat, int count) where T : struct => count > 0 ? MarshalPArray<T>(pat, sizeOf => source.ReadBytes(sizeOf * count), count) : [];

    // Struct : Array - Struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8SArray<T>(this BinaryReader source, int sizeOf = 0) where T : struct => ReadSArray<T>(source, source.ReadByte(), sizeOf);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16SArray<T>(this BinaryReader source, int sizeOf = 0, bool endian = false) where T : struct => ReadSArray<T>(source, source.ReadUInt16X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32SArray<T>(this BinaryReader source, int sizeOf = 0, bool endian = false) where T : struct => ReadSArray<T>(source, (int)source.ReadUInt32X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadC32SArray<T>(this BinaryReader source, int sizeOf = 0, bool endian = false) where T : struct => ReadSArray<T>(source, (int)source.ReadCInt32X(endian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadSArray<T>(this BinaryReader source, uint count, int sizeOf = 0) where T : struct => ReadSArray<T>(source, (int)count, sizeOf);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadSArray<T>(this BinaryReader source, int count, int sizeOf = 0) where T : struct => count > 0 ? MarshalSArray<T>(source.ReadBytes, count, sizeOf) : [];

    // Struct : Array - Type
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8TArray<T>(this BinaryReader source, int sizeOf) where T : struct => ReadTArray<T>(source, sizeOf, source.ReadByte());
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16TArray<T>(this BinaryReader source, int sizeOf, bool endian = false) where T : struct => ReadTArray<T>(source, sizeOf, source.ReadUInt16X(endian));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32TArray<T>(this BinaryReader source, int sizeOf, bool endian = false) where T : struct => ReadTArray<T>(source, sizeOf, (int)source.ReadUInt32X(endian));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadC32TArray<T>(this BinaryReader source, int sizeOf, bool endian = false) where T : struct => ReadTArray<T>(source, sizeOf, (int)source.ReadCInt32X(endian));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadTArray<T>(this BinaryReader source, int sizeOf, int count) where T : struct => count > 0 ? MarshalTArray<T>(source.ReadBytes(sizeOf * count), count) : [];

    // Struct : Each
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadSEach<T>(this BinaryReader source, int count) where T : struct { var list = new T[count]; if (count > 0) for (var i = 0; i < list.Length; i++) list[i] = MarshalS<T>(source.ReadBytes, -1); return list; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadTEach<T>(this BinaryReader source, int sizeOf, int count) where T : struct { var list = new T[count]; if (count > 0) for (var i = 0; i < list.Length; i++) list[i] = MarshalT<T>(source.ReadBytes(sizeOf)); return list; }

    // Struct : Many - Factory
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8FMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) => ReadFMany(source, keyFactory, valueFactory, source.ReadByte(), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16FMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) => ReadFMany(source, keyFactory, valueFactory, source.ReadUInt16X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32FMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) => ReadFMany(source, keyFactory, valueFactory, (int)source.ReadUInt32X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadC32FMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) => ReadFMany(source, keyFactory, valueFactory, (int)source.ReadCInt32X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadFMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, uint count, bool sorted = false) => ReadFMany(source, keyFactory, valueFactory, (int)count, sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadFMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false) { var set = sorted ? (IDictionary<TKey, TValue>)new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(); for (var i = 0; i < count; i++) set.Add(keyFactory(source), valueFactory(source)); return set; }

    // Struct : Many - Pattern
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8PMany<TKey, TValue>(this BinaryReader source, string pat, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadPMany<TKey, TValue>(source, pat, valueFactory, source.ReadByte(), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16PMany<TKey, TValue>(this BinaryReader source, string pat, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadPMany<TKey, TValue>(source, pat, valueFactory, source.ReadUInt16X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32PMany<TKey, TValue>(this BinaryReader source, string pat, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadPMany<TKey, TValue>(source, pat, valueFactory, (int)source.ReadUInt32X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadC32PMany<TKey, TValue>(this BinaryReader source, string pat, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadPMany<TKey, TValue>(source, pat, valueFactory, (int)source.ReadCInt32X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadPMany<TKey, TValue>(this BinaryReader source, string pat, Func<BinaryReader, TValue> valueFactory, uint count, bool sorted = false) where TKey : struct => ReadPMany<TKey, TValue>(source, pat, valueFactory, (int)count, sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadPMany<TKey, TValue>(this BinaryReader source, string pat, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false) where TKey : struct { var set = sorted ? (IDictionary<TKey, TValue>)new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(); for (var i = 0; i < count; i++) set.Add(source.ReadP<TKey>(pat), valueFactory(source)); return set; }

    // Struct : Many - Struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8SMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadSMany<TKey, TValue>(source, valueFactory, source.ReadByte(), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16SMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadSMany<TKey, TValue>(source, valueFactory, source.ReadUInt16X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32SMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadSMany<TKey, TValue>(source, valueFactory, (int)source.ReadUInt32X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadC32SMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => ReadSMany<TKey, TValue>(source, valueFactory, (int)source.ReadCInt32X(endian), sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadSMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TValue> valueFactory, uint count, bool sorted = false) where TKey : struct => ReadSMany<TKey, TValue>(source, valueFactory, (int)count, sorted);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadSMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false) where TKey : struct { var set = sorted ? (IDictionary<TKey, TValue>)new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(); for (var i = 0; i < count; i++) set.Add(source.ReadS<TKey>(), valueFactory(source)); return set; }

    // Struct : Many - Type
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8TMany<TKey, TValue>(this BinaryReader source, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => source.ReadTMany<TKey, TValue>(sizeOf, valueFactory, source.ReadByte(), sorted);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16TMany<TKey, TValue>(this BinaryReader source, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => source.ReadTMany<TKey, TValue>(sizeOf, valueFactory, source.ReadUInt16X(endian), sorted);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32TMany<TKey, TValue>(this BinaryReader source, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => source.ReadTMany<TKey, TValue>(sizeOf, valueFactory, (int)source.ReadUInt32X(endian), sorted);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadC32TMany<TKey, TValue>(this BinaryReader source, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool endian = false, bool sorted = false) where TKey : struct => source.ReadTMany<TKey, TValue>(sizeOf, valueFactory, (int)source.ReadCInt32X(endian), sorted);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadTMany<TKey, TValue>(this BinaryReader source, int sizeOf, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false) where TKey : struct { var set = sorted ? (IDictionary<TKey, TValue>)new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(); for (var i = 0; i < count; i++) set.Add(source.ReadT<TKey>(sizeOf), valueFactory(source)); return set; }

    #endregion

    #region Numerics

    //:ref https://docs.microsoft.com/en-us/windows/win32/direct3d11/floating-point-rules#16-bit-floating-point-rules
    static float Byte2HexIntFracToFloat2(string hexString) {
        string sintPart = hexString[..2], sfracPart = hexString.Substring(2, 2);
        int intPart = Convert.ToSByte(sintPart, 16), num = short.Parse(sfracPart, NumberStyles.AllowHexSpecifier);
        var bytes = BitConverter.GetBytes(num);
        string binary = Convert.ToString(bytes[0], 2).PadLeft(8, '0'), binaryFracPart = binary;
        // convert Fractional Part
        var dec = 0f;
        for (var i = 0; i < binaryFracPart.Length; i++) {
            if (binaryFracPart[i] == '0') continue;
            dec += (float)Math.Pow(2, (i + 1) * (-1));
        }
        return intPart + dec;
    }

    public static float ReadHalf(this BinaryReader r)
        => new HalfFloat { bits = r.ReadUInt16() }.ToSingle();

    public static float ReadHalf16(this BinaryReader r)
        => Byte2HexIntFracToFloat2(r.ReadUInt16().ToString("X4")) / 127f;

    public static Vector2 ReadVector2(this BinaryReader source)
        => new Vector2(
            x: source.ReadSingle(),
            y: source.ReadSingle());
    public static Vector2 ReadHalfVector2(this BinaryReader source)
        => new Vector2(
            x: source.ReadHalf(),
            y: source.ReadHalf());
    public static Vector3 ReadVector3(this BinaryReader source)
        => new Vector3(
            x: source.ReadSingle(),
            y: source.ReadSingle(),
            z: source.ReadSingle());
    public static Vector3 ReadHalfVector3(this BinaryReader source)
        => new Vector3(
            x: source.ReadHalf(),
            y: source.ReadHalf(),
            z: source.ReadHalf());
    public static Vector3 ReadHalf16Vector3(this BinaryReader source)
        => new Vector3(
            x: source.ReadHalf16(),
            y: source.ReadHalf16(),
            z: source.ReadHalf16());
    public static Vector4 ReadVector4(this BinaryReader source)
        => new Vector4(
            x: source.ReadSingle(),
            y: source.ReadSingle(),
            z: source.ReadSingle(),
            w: source.ReadSingle());
    public static Vector4 ReadHalfVector4(this BinaryReader source)
        => new Vector4(
            x: source.ReadHalf(),
            y: source.ReadHalf(),
            z: source.ReadHalf(),
            w: source.ReadHalf());

    public static Matrix2x2 ReadMatrix2x2(this BinaryReader r)
        => new Matrix2x2 {
            M11 = r.ReadSingle(),
            M12 = r.ReadSingle(),
            M21 = r.ReadSingle(),
            M22 = r.ReadSingle(),
        };

    public static Matrix3x3 ReadMatrix3x3(this BinaryReader r)
        => new Matrix3x3 {
            M11 = r.ReadSingle(),
            M12 = r.ReadSingle(),
            M13 = r.ReadSingle(),
            M21 = r.ReadSingle(),
            M22 = r.ReadSingle(),
            M23 = r.ReadSingle(),
            M31 = r.ReadSingle(),
            M32 = r.ReadSingle(),
            M33 = r.ReadSingle(),
        };

    public static Matrix3x4 ReadMatrix3x4(this BinaryReader r)
        => new Matrix3x4 {
            M11 = r.ReadSingle(),
            M12 = r.ReadSingle(),
            M13 = r.ReadSingle(),
            M14 = r.ReadSingle(),
            M21 = r.ReadSingle(),
            M22 = r.ReadSingle(),
            M23 = r.ReadSingle(),
            M24 = r.ReadSingle(),
            M31 = r.ReadSingle(),
            M32 = r.ReadSingle(),
            M33 = r.ReadSingle(),
            M34 = r.ReadSingle()
        };

    /// <summary>
    /// Reads a column-major 3x3 matrix but returns a functionally equivalent 4x4 matrix.
    /// </summary>
    public static Matrix4x4 ReadMatrixColumn3x3As4x4(this BinaryReader source) {
        // If in the 3x3 part of the matrix, read values. Otherwise, use the identity matrix.
        var matrix = new Matrix4x4();
        for (var columnIndex = 0; columnIndex < 4; columnIndex++) for (var rowIndex = 0; rowIndex < 4; rowIndex++) matrix.Set(rowIndex, columnIndex, rowIndex <= 2 && columnIndex <= 2 ? source.ReadSingle() : rowIndex == columnIndex ? 1f : 0f);
        return matrix;
    }
    /// <summary>
    /// Reads a row-major 3x3 matrix but returns a functionally equivalent 4x4 matrix.
    /// </summary>
    public static Matrix4x4 ReadMatrix3x3As4x4(this BinaryReader source) {
        // If in the 3x3 part of the matrix, read values. Otherwise, use the identity matrix.
        var matrix = new Matrix4x4();
        for (var rowIndex = 0; rowIndex < 4; rowIndex++) for (var columnIndex = 0; columnIndex < 4; columnIndex++) matrix.Set(rowIndex, columnIndex, rowIndex <= 2 && columnIndex <= 2 ? source.ReadSingle() : rowIndex == columnIndex ? 1f : 0f);
        return matrix;
    }
    public static Matrix4x4 ReadMatrixColumn4x4(this BinaryReader source) {
        var matrix = new Matrix4x4();
        for (var columnIndex = 0; columnIndex < 4; columnIndex++) for (var rowIndex = 0; rowIndex < 4; rowIndex++) matrix.Set(rowIndex, columnIndex, source.ReadSingle());
        return matrix;
    }
    public static Matrix4x4 ReadMatrix4x4(this BinaryReader source) {
        var matrix = new Matrix4x4();
        for (var rowIndex = 0; rowIndex < 4; rowIndex++) for (var columnIndex = 0; columnIndex < 4; columnIndex++) matrix.Set(rowIndex, columnIndex, source.ReadSingle());
        return matrix;
    }
    public static Quaternion ReadQuaternion(this BinaryReader source)
        => new Quaternion(
            x: source.ReadSingle(),
            y: source.ReadSingle(),
            z: source.ReadSingle(),
            w: source.ReadSingle());
    public static Quaternion ReadQuaternionWFirst(this BinaryReader source)
        => new Quaternion(
            w: source.ReadSingle(),
            x: source.ReadSingle(),
            y: source.ReadSingle(),
            z: source.ReadSingle());
    public static Quaternion ReadHalfQuaternion(this BinaryReader source)
        => new Quaternion(
            x: source.ReadHalf(),
            y: source.ReadHalf(),
            z: source.ReadHalf(),
            w: source.ReadHalf());

    #endregion

    #region Unknown

    /// <summary>
    /// First reads a UInt16. If the MSB is set, it will be masked with 0x3FFF, shifted left 2 bytes, and then OR'd with the next UInt16. The sum is then added to knownType.
    /// </summary>
    public static uint ReadAsDataIDOfKnownType(this BinaryReader source, uint knownType) {
        var value = source.ReadUInt16();
        if ((value & 0x8000) != 0) {
            var lower = source.ReadUInt16();
            var higher = (value & 0x3FFF) << 16;
            return (uint)(knownType + (higher | lower));
        }
        return knownType + value;
    }

    /// <summary>
    /// Ensures stream is complete
    /// </summary>
    public static void EnsureComplete(this BinaryReader source) {
        if (source.BaseStream.Length != source.BaseStream.Position) throw new Exception("Not Complete");
    }

    #endregion
}

#region Old

// USE THIS?
//public static IEnumerable<long> SeekNeedles(this BinaryReader source, byte[] needle)
//{
//    var buffer = new byte[0x100000];
//    int read, i, j = 0;
//    var position = source.BaseStream.Position;
//    while ((read = source.BaseStream.Read(buffer, 0, buffer.Length)) != 0)
//    {
//        for (i = 0; i < read; i++)
//            if (needle[j] == buffer[i])
//            {
//                j++;
//                if (j == needle.Length)
//                {
//                    yield return source.BaseStream.Position = position + i + 1 - needle.Length;
//                    j = 0;
//                }
//            }
//            else j = 0;
//        source.BaseStream.Position = position += read;
//    }
//}


#endregion