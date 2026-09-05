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

public static partial class Poly {
    #region Base

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double ReadDoubleBigEndian(ReadOnlySpan<byte> self) {
        return BitConverter.IsLittleEndian ?
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(self))) :
            MemoryMarshal.Read<double>(self);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float ReadSingleBigEndian(ReadOnlySpan<byte> self) {
        return BitConverter.IsLittleEndian ?
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(self))) :
            MemoryMarshal.Read<float>(self);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //static decimal ReadDecimalBigEndian(ReadOnlySpan<byte> self) {
    //    return BitConverter.IsLittleEndian ?
    //        throw new NotImplementedException() :
    //        MemoryMarshal.Read<decimal>(self);
    //}

    static ReadOnlySpan<byte> InternalRead(this BinaryReader self, Span<byte> buffer) {
        Debug.Assert(buffer.Length != 1, "length of 1 should use ReadByte.");
        self.Read(buffer);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadBytes(this BinaryReader self, uint count) => self.ReadBytes((int)count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this BinaryReader self, Stream destination, bool resetAfter = true) {
        self.BaseStream.CopyTo(destination);
        if (resetAfter) destination.Position = 0;
    }

    public static StreamReader ToStream(this BinaryReader self) => new(self.BaseStream);

    #endregion

    #region Bytes

    // primatives : bytes
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader ReadBytesToReader(this BinaryReader self, int count) => new(new MemoryStream(self.ReadBytes(count)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL8Bytes(this BinaryReader self, int maxLength = 0) { var length = self.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? self.ReadBytes(length) : []; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL16Bytes(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadUInt16X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? self.ReadBytes(length) : []; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL32Bytes(this BinaryReader self) => self.ReadBytes((int)self.ReadUInt32());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadL32Bytes(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadUInt32X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? self.ReadBytes(length) : []; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader ReadL32BytesToReader(this BinaryReader self) => self.ReadBytesToReader((int)self.ReadUInt32());
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte[] ReadLV7Bytes(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadVInt7X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("byte length exceeds maximum length"); return length > 0 ? self.ReadBytes(length) : []; }
    public static byte[] ReadToEnd(this BinaryReader self) {
        var bs = self.BaseStream;
        if (bs.CanSeek) return self.ReadBytes((int)(bs.Length - bs.Position));
        var ms = new MemoryStream();
        var buf = new byte[4096]; // 4KB chunk
        int read;
        while ((read = bs.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, read);
        return ms.ToArray();
    }
    //public static void ReadToEnd(this BinaryReader self, byte[] buffer, int startIndex = 0)
    //{
    //    var length = (int)self.BaseStream.Length - self.BaseStream.Position;
    //    Debug.Assert(startIndex >= 0 && length <= int.MaxValue && startIndex + length <= buffer.Length);
    //    self.Read(buffer, startIndex, (int)length);
    //}
    public static byte[] ReadToValue(this BinaryReader self, byte value = 0, int length = int.MaxValue, MemoryStream ms = null) {
        if (ms == null) ms = new MemoryStream();
        else ms.SetLength(0);
        byte c; length = Math.Min(length, (int)(self.BaseStream.Length - self.BaseStream.Position));
        while (length-- > 0 && (c = self.ReadByte()) != value) ms.WriteByte(c);
        return ms.ToArray();
    }

    #endregion

    #region Primitives

    // primatives : big
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static double ReadDoubleE(this BinaryReader self) => ReadDoubleBigEndian(InternalRead(self, stackalloc byte[sizeof(double)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static short ReadInt16E(this BinaryReader self) => BinaryPrimitives.ReadInt16BigEndian(InternalRead(self, stackalloc byte[sizeof(short)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ReadInt32E(this BinaryReader self) => BinaryPrimitives.ReadInt32BigEndian(InternalRead(self, stackalloc byte[sizeof(int)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long ReadInt64E(this BinaryReader self) => BinaryPrimitives.ReadInt64BigEndian(InternalRead(self, stackalloc byte[sizeof(long)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float ReadSingleE(this BinaryReader self) => ReadSingleBigEndian(InternalRead(self, stackalloc byte[sizeof(float)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort ReadUInt16E(this BinaryReader self) => BinaryPrimitives.ReadUInt16BigEndian(InternalRead(self, stackalloc byte[sizeof(ushort)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint ReadUInt32E(this BinaryReader self) => BinaryPrimitives.ReadUInt32BigEndian(InternalRead(self, stackalloc byte[sizeof(uint)]));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong ReadUInt64E(this BinaryReader self) => BinaryPrimitives.ReadUInt64BigEndian(InternalRead(self, stackalloc byte[sizeof(ulong)]));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static decimal ReadDecimalE(this BinaryReader self) => ReadDecimalBigEndian(InternalRead(self, stackalloc byte[sizeof(decimal)]));

    // primatives : endianX
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static double ReadDoubleX(this BinaryReader self, bool big) => big ? self.ReadDoubleE() : self.ReadDouble();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static short ReadInt16X(this BinaryReader self, bool big) => big ? self.ReadInt16E() : self.ReadInt16();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ReadInt32X(this BinaryReader self, bool big) => big ? self.ReadInt32E() : self.ReadInt32();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long ReadInt64X(this BinaryReader self, bool big) => big ? self.ReadInt64E() : self.ReadInt64();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float ReadSingleX(this BinaryReader self, bool big) => big ? self.ReadSingleE() : self.ReadSingle();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort ReadUInt16X(this BinaryReader self, bool big) => big ? self.ReadUInt16E() : self.ReadUInt16();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint ReadUInt32X(this BinaryReader self, bool big) => big ? self.ReadUInt32E() : self.ReadUInt32();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong ReadUInt64X(this BinaryReader self, bool big) => big ? self.ReadUInt64E() : self.ReadUInt64();
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static decimal ReadDecimalX(this BinaryReader self, bool big) => big ? self.ReadDecimalE() : self.ReadDecimal();

    // primatives : specialized
    public static int ReadIntV7(this BinaryReader self) { //# Read7BitEncodedInt - LEB128-style varint
        int r = 0, b = 0; byte v;
        while (true) {
            v = self.ReadByte(); r |= (v & 0x7f) << b; b += 7;
            if ((v & 0x80) == 0) return r;
            else if (b > 31) throw new Exception("7-bit encoding too long");
        }
    }
    public static int ReadIntV7X(this BinaryReader self, bool big) => big ? throw new NotImplementedException() : self.ReadIntV7();
    /// <summary>
    /// A Compressed UInt32 can be 1, 2, or 4 bytes.<para />
    /// If the first MSB (0x80) is 0, it is one byte.<para />
    /// If the first MSB (0x80) is set and the second MSB (0x40) is 0, it's 2 bytes.<para />
    /// If both (0x80) and (0x40) are set, it's 4 bytes.
    /// </summary>
    public static uint ReadUIntV8(this BinaryReader self) { // 1/2/4-byte length prefix selected by the top bits
        var b0 = self.ReadByte(); if ((b0 & 0x80) == 0) return b0;
        var b1 = self.ReadByte(); if ((b0 & 0x40) == 0) return (uint)(((b0 & 0x7F) << 8) | b1);
        return (uint)(((((b0 & 0x3F) << 8) | b1) << 16) | self.ReadUInt16());
    }
    public static uint ReadUIntV8X(this BinaryReader self, bool big) => big ? throw new NotImplementedException() : self.ReadUIntV8();
    public static uint ReadUIntV8a(this BinaryReader self) { var z = self.ReadByte(); return z < 0xFE ? z : z != 0xFE ? self.ReadUInt32() : throw new FormatException(); }
    public static uint ReadUIntV8aX(this BinaryReader self, bool big) { var z = self.ReadByte(); return z < 0xFE ? z : z != 0xFE ? self.ReadUInt32X(big) : throw new FormatException(); }
    public static (uint, bool) ReadUIntV8a2(this BinaryReader self) { var z = self.ReadByte(); return z < 0xFE ? (z, false) : (self.ReadUInt32(), z != 0xFF); }
    public static (uint, bool) ReadUIntV8a2X(this BinaryReader self, bool big) { var z = self.ReadByte(); return z < 0xFE ? (z, false) : (self.ReadUInt32X(big), z != 0xFF); }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool ReadBoolean(this BinaryReader self) => self.ReadByte() != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool ReadBoolean32(this BinaryReader self) => self.ReadUInt32() != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Guid ReadGuid(this BinaryReader self) => new(self.ReadBytes(16));

    #endregion

    #region Position

    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Align(this BinaryReader self) { var alignDelta = self.BaseStream.Position % 4; if (alignDelta != 0) self.BaseStream.Position += (int)(4 - alignDelta); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader Align(this BinaryReader self, int align = 4) { self.BaseStream.Position = (self.BaseStream.Position + --align) & ~align; return self; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long Tell(this BinaryReader self) => self.BaseStream.Position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader Seek(this BinaryReader self, long offset) { self.BaseStream.Position = offset; return self; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T SeekAfter<T>(this BinaryReader self, T value, long offset) { self.BaseStream.Position = offset; return value; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader SeekAndAlign(this BinaryReader self, long offset, int align = 4) { self.BaseStream.Position = offset % align != 0 ? offset + align - (offset % align) : offset; return self; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader Skip(this BinaryReader self, long count) { self.BaseStream.Position += count; return self; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T SkipAfter<T>(this BinaryReader self, T value, long count) { self.BaseStream.Position += count; return value; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader SkipAndAlign(this BinaryReader self, long count, int align = 4) { var offset = self.BaseStream.Position + count; self.BaseStream.Position = offset % align != 0 ? offset + align - (offset % align) : offset; return self; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryReader End(this BinaryReader self, long offset) { self.BaseStream.Seek(offset, SeekOrigin.End); return self; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Peek(this BinaryReader self, Action<BinaryReader> action, long offset = 0L, SeekOrigin origin = SeekOrigin.Current) {
        var pos = self.BaseStream.Position;
        self.BaseStream.Seek(offset, origin);
        action(self);
        self.BaseStream.Position = pos;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Peek<T>(this BinaryReader self, Func<BinaryReader, T> action, long offset = 0L, SeekOrigin origin = SeekOrigin.Current) {
        var pos = self.BaseStream.Position;
        self.BaseStream.Seek(offset, origin);
        var value = action(self);
        self.BaseStream.Position = pos;
        return value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool AtEnd(this BinaryReader self, long? end = null) => self.BaseStream.Position >= (end ?? self.BaseStream.Length);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void EnsureAtEnd(this BinaryReader self, long? end = -1, string message = "Not at end") { if (self.BaseStream.Position != (end ?? self.BaseStream.Length)) throw new Exception(message); }

    #endregion

    #region String

    // String : Special

    public static string ReadL16OString(this BinaryReader self, int codepage = 1252) //: ReadObfuscatedString
    {
        var length = self.ReadUInt16();
        if (length == 0) return string.Empty;
        var bytes = self.ReadBytes(length);
        // flip the bytes in the string to undo the obfuscation: i.e. 0xAB => 0xBA
        for (var i = 0; i < length; i++) bytes[i] = (byte)((bytes[i] >> 4) | (bytes[i] << 4));
        return Encoding.GetEncoding(codepage).GetString(bytes);
    }

    // String : Wide

    /// <summary>
    /// Read a Fixed-Length string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadFWString(this BinaryReader self, int length) => length != 0 ? new string(self.ReadChars(length), 0, length).TrimEnd('\0') : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadVWString(this BinaryReader self, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) => Encoding.Unicode.GetString(self.ReadToValue(stopValue, length, ms));
    /// <summary>
    /// Read a Length-prefixed wide string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="byteLength">Size of the Length representation</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8WString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(self.ReadChars(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16WString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadUInt16X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(self.ReadChars(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32WString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadUInt32X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(self.ReadChars(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadLV8WString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadUIntV8X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? new string(self.ReadChars(length), 0, length).TrimEnd('\0') : null; }
    public static string ReadLV8W2String(this BinaryReader self, int maxLength = 0, bool big = false) {
        var length = self.ReadUIntV8X(big);
        if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length");
        if (length == 0) return null;
        var b = new StringBuilder();
        for (var i = 0; i < length; i++) b.Append(Convert.ToChar(self.ReadUInt16()));
        return b.ToString();
    }

    // String : Utf8

    /// <summary>
    /// Read a Fixed-Length utf8 string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadFUString(this BinaryReader self, int length) => length != 0 ? Encoding.UTF8.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadVUString(this BinaryReader self, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) => Encoding.UTF8.GetString(self.ReadToValue(stopValue, length, ms));
    /// <summary>
    /// Read a Length-prefixed utf-8 string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="byteLength">Size of the Length representation</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8UString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.UTF8.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16UString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadUInt16X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.UTF8.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32UString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadUInt32X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.UTF8.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadLV8UString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadVInt8X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.UTF8.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }

    // String : Ascii

    /// <summary>
    /// Read a Fixed-Length ascii string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadFAString(this BinaryReader self, int length) => length != 0 ? Encoding.ASCII.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null;

    //var nameAsSpan = r.ReadBytes(0x108).AsSpan();
    //var path = Encoding.ASCII.GetString(nameAsSpan[..nameAsSpan.IndexOf(byte.MinValue)]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadVAString(this BinaryReader self, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) => Encoding.ASCII.GetString(self.ReadToValue(stopValue, length, ms));
    /// <summary>
    /// Read a Length-prefixed ascii string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="byteLength">Size of the Length representation</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8AString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16AString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = self.ReadUInt16X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32AString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadUInt32X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadLV8AString(this BinaryReader self, int maxLength = 0, bool big = false) { var length = (int)self.ReadVInt8X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? Encoding.ASCII.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }

    // String : X

    /// <summary>
    /// Read a Fixed-Length ascii string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="length">Size of the String</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadFXString(this BinaryReader self, Encoding encoding, int length) => length != 0 ? encoding.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadVXString(this BinaryReader self, Encoding encoding, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) => encoding.GetString(self.ReadToValue(stopValue, length, ms));
    /// <summary>
    /// Read a Length-prefixed x string from the stream
    /// </summary>
    /// <param name="self"></param>
    /// <param name="byteLength">Size of the Length representation</param>
    /// <param name="zstring">Remove last character</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL8XString(this BinaryReader self, Encoding encoding, int maxLength = 0, bool big = false) { var length = self.ReadByte(); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? encoding.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL16XString(this BinaryReader self, Encoding encoding, int maxLength = 0, bool big = false) { var length = self.ReadUInt16X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? encoding.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadL32XString(this BinaryReader self, Encoding encoding, int maxLength = 0, bool big = false) { var length = (int)self.ReadUInt32X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? encoding.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static string ReadLV8XString(this BinaryReader self, Encoding encoding, int maxLength = 0, bool big = false) { var length = (int)self.ReadVInt8X(big); if (maxLength > 0 && length > maxLength) throw new FormatException("string length exceeds maximum length"); return length > 0 ? encoding.GetString(self.ReadBytes(length), 0, length).TrimEnd('\0') : null; }

    public static List<string> ReadVAStringList(this BinaryReader self, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) {
        ms ??= new MemoryStream();
        var r = new List<string>();
        byte c;
        while (length > 0) {
            ms.SetLength(0);
            while (length-- > 0 && (c = self.ReadByte()) != stopValue) ms.WriteByte(c);
            r.Add(Encoding.ASCII.GetString(ms.ToArray()));
        }
        return r;
    }

    public static List<string> ReadVUStringList(this BinaryReader self, int length = int.MaxValue, byte stopValue = 0, MemoryStream ms = null) {
        ms ??= new MemoryStream();
        var r = new List<string>();
        byte c;
        while (length > 0) {
            ms.SetLength(0);
            while (length-- > 0 && (c = self.ReadByte()) != stopValue) ms.WriteByte(c);
            r.Add(Encoding.UTF8.GetString(ms.ToArray()));
        }
        return r;
    }

    #region not used

    //public static string ReadLString(this BinaryReader self, int byteLength = 4, bool zstring = false) //:was ReadPString
    //{
    //    var length = byteLength switch
    //    {
    //        1 => self.ReadByte(),
    //        2 => self.ReadInt16(),
    //        4 => self.ReadInt32(),
    //        _ => throw new NotSupportedException("Only Int8, Int16, and Int32 string sizes are supported"),
    //    };
    //    return length > 0 ? new string(self.ReadChars(length), 0, zstring ? length - 1 : length) : null;
    //}
    //public static string ReadLAString(this BinaryReader self, int byteLength = 4, bool zstring = false)
    //{
    //    var length = byteLength switch
    //    {
    //        1 => self.ReadByte(),
    //        2 => self.ReadInt16(),
    //        4 => self.ReadInt32(),
    //        _ => throw new NotSupportedException("Only Int8, Int16, and Int32 string sizes are supported"),
    //    };
    //    return length != 0 ? Encoding.ASCII.GetString(self.ReadBytes(length), 0, zstring ? length - 1 : length) : null;
    //}
    //public static string ReadFCString(this BinaryReader self, int length)
    //{
    //    if (length == 0) return null;
    //    var chars = self.ReadChars(length);
    //    for (var i = 0; i < length; i++) if (chars[i] == 0) return new string(chars, 0, i);
    //    return new string(chars);
    //}
    //public static string ReadZString(this BinaryReader self, int length, Encoding encoding = null)
    //{
    //    var buf = self.ReadBytes(length);
    //    int i;
    //    for (i = buf.Length - 1; i >= 0 && buf[i] == 0; i--) { }
    //    return (encoding ?? Encoding.ASCII).GetString(buf, 0, i + 1);
    //}
    //public static string ReadCString(this BinaryReader self, int length, Encoding encoding = null)
    //{
    //    var buf = self.ReadBytes(length);
    //    int i;
    //    for (i = 0; i < buf.Length && buf[i] != 0; i++) { }
    //    return (encoding ?? Encoding.ASCII).GetString(buf, 0, i);
    //}
    //public static string ReadZString2(this BinaryReader self) //:was ReadCString (Dolkens)
    //{
    //    var length = 0;
    //    var maxPosition = self.BaseStream.Length;
    //    while (self.BaseStream.Position < maxPosition && self.ReadChar() != 0) length++;
    //    var nul = self.BaseStream.Position;
    //    self.BaseStream.Seek(0 - length - 1, SeekOrigin.Current);
    //    var chars = self.ReadChars(length + 1);
    //    self.BaseStream.Seek(nul, SeekOrigin.Begin);
    //    return length > 0 ? new string(chars, 0, length).Replace("\u0000", "") : null;
    //}

    #endregion

    public static string ReadZEncoding(this BinaryReader self, Encoding encoding) {
        var characterSize = encoding.GetByteCount("e");
        using var s = new MemoryStream();
        while (true) {
            var data = new byte[characterSize];
            self.Read(data, 0, characterSize);
            if (encoding.GetString(data, 0, characterSize) == "\0") break;
            s.Write(data, 0, data.Length);
        }
        return encoding.GetString(s.ToArray());
    }

    public static string[] ReadCStringArray(this BinaryReader self, int count, StringBuilder buf = null) {
        if (buf == null) buf = new StringBuilder();
        var list = new List<string>();
        for (var i = 0; i < count; i++) {
            var c = self.ReadChar();
            while (c != 0) { buf.Append(c); c = self.ReadChar(); }
            list.Add(buf.ToString());
            buf.Clear();
        }
        return list.ToArray();
    }



    public static string ReadO32Encoding(this BinaryReader self, Encoding encoding) {
        var currentOffset = self.BaseStream.Position;
        var offset = self.ReadUInt32();
        if (offset == 0) return string.Empty;
        self.BaseStream.Position = currentOffset + offset;
        var str = ReadZEncoding(self, encoding);
        self.BaseStream.Position = currentOffset + 4;
        return str;
    }

    //: TODO Use Encoding Method
    public static string ReadO32UTF8(this BinaryReader self) {
        var currentOffset = self.BaseStream.Position;
        var offset = self.ReadUInt32();
        if (offset == 0) return string.Empty;
        self.BaseStream.Position = currentOffset + offset;
        var str = ReadVWString(self);
        self.BaseStream.Position = currentOffset + 4;
        return str;
    }

    #endregion

    #region Struct

    //var abc = MemoryMarshal.Cast<byte, ushort>(data);

    // Struct : Single
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadF<T>(this BinaryReader self, Func<BinaryReader, T> factory) => factory(self);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadP<T>(this BinaryReader self, string pat) where T : struct => MarshalP<T>(pat, self.ReadBytes);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadS<T>(this BinaryReader self, int sizeOf = 0) where T : struct => MarshalS<T>(self.ReadBytes, sizeOf);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T ReadT<T>(this BinaryReader self, int sizeOf) where T : struct => MarshalT<T>(self.ReadBytes(sizeOf));

    // Struct : Array - Factory
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8FArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, T[] obj = default) => ReadFArray(self, factory, self.ReadByte(), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16FArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, T[] obj = default) => ReadFArray(self, factory, self.ReadUInt16X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32FArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, T[] obj = default) => ReadFArray(self, factory, (int)self.ReadUInt32X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadLV7FArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, T[] obj = default) => ReadFArray(self, factory, (int)self.ReadIntV7X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadLV8FArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, T[] obj = default) => ReadFArray(self, factory, (int)self.ReadUIntV8X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadFArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, uint count, T[] obj = default) => ReadFArray(self, factory, (int)count, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadFArray<T>(this BinaryReader self, Func<BinaryReader, T> factory, int count, T[] obj = default) {
        var s = obj ?? new T[count]; if (count > 0) for (var i = 0; i < s.Length; i++) s[i] = factory(self);
        return s;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadFIArray<T>(this BinaryReader self, Func<BinaryReader, int, T> factory, int count, T[] obj = default) {
        var s = obj ?? new T[count]; if (count > 0) for (var i = 0; i < s.Length; i++) s[i] = factory(self, i);
        return s;
    }

    // Struct : Array - Pattern
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8PArray<T>(this BinaryReader self, string pat, T[] obj = default) where T : struct => ReadPArray<T>(self, pat, self.ReadByte(), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16PArray<T>(this BinaryReader self, string pat, bool big = false, T[] obj = default) where T : struct => ReadPArray<T>(self, pat, self.ReadUInt16X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32PArray<T>(this BinaryReader self, string pat, bool big = false, T[] obj = default) where T : struct => ReadPArray<T>(self, pat, (int)self.ReadUInt32X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadLV7PArray<T>(this BinaryReader self, string pat, bool big = false, T[] obj = default) where T : struct => ReadPArray<T>(self, pat, (int)self.ReadIntV7X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadLV8PArray<T>(this BinaryReader self, string pat, bool big = false, T[] obj = default) where T : struct => ReadPArray<T>(self, pat, (int)self.ReadUIntV8X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadPArray<T>(this BinaryReader self, string pat, uint count, T[] obj = default) where T : struct => ReadPArray<T>(self, pat, (int)count, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadPArray<T>(this BinaryReader self, string pat, int count, T[] obj = default) where T : struct {
        if (obj == null) return count > 0 ? MarshalPArray<T>(pat, sizeOf => self.ReadBytes(sizeOf * count), count, obj) : [];
        if (count > 0) { }
        return obj;
    }

    // Struct : Array - Struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8SArray<T>(this BinaryReader self, int sizeOf = 0, T[] obj = default) where T : struct => ReadSArray<T>(self, self.ReadByte(), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16SArray<T>(this BinaryReader self, int sizeOf = 0, bool big = false, T[] obj = default) where T : struct => ReadSArray<T>(self, self.ReadUInt16X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32SArray<T>(this BinaryReader self, int sizeOf = 0, bool big = false, T[] obj = default) where T : struct => ReadSArray<T>(self, (int)self.ReadUInt32X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadLV7SArray<T>(this BinaryReader self, int sizeOf = 0, bool big = false, T[] obj = default) where T : struct => ReadSArray<T>(self, (int)self.ReadIntV7X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadLV8SArray<T>(this BinaryReader self, int sizeOf = 0, bool big = false, T[] obj = default) where T : struct => ReadSArray<T>(self, (int)self.ReadUIntV8X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadSArray<T>(this BinaryReader self, uint count, int sizeOf = 0, T[] obj = default) where T : struct => ReadSArray<T>(self, (int)count, sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadSArray<T>(this BinaryReader self, int count, int sizeOf = 0, T[] obj = default) where T : struct {
        if (obj == null) return count > 0 ? MarshalSArray<T>(self.ReadBytes, count, sizeOf, obj) : [];
        return obj;
    }

    // Struct : Array - Each
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadSEach<T>(this BinaryReader self, int count, T[] obj = default) where T : struct { var s = obj ?? new T[count]; if (count > 0) for (var i = 0; i < s.Length; i++) s[i] = MarshalS<T>(self.ReadBytes, -1); return s; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadTEach<T>(this BinaryReader self, int sizeOf, int count, T[] obj = default) where T : struct { var s = obj ?? new T[count]; if (count > 0) for (var i = 0; i < s.Length; i++) s[i] = MarshalT<T>(self.ReadBytes(sizeOf)); return s; }

    // Struct : Array - Type
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL8TArray<T>(this BinaryReader self, int sizeOf, T[] obj = default) where T : struct => ReadTArray<T>(self, sizeOf, self.ReadByte());
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL16TArray<T>(this BinaryReader self, int sizeOf, bool big = false, T[] obj = default) where T : struct => ReadTArray<T>(self, sizeOf, self.ReadUInt16X(big));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadL32TArray<T>(this BinaryReader self, int sizeOf, bool big = false, T[] obj = default) where T : struct => ReadTArray<T>(self, sizeOf, (int)self.ReadUInt32X(big));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadC32TArray<T>(this BinaryReader self, int sizeOf, bool big = false, T[] obj = default) where T : struct => ReadTArray<T>(self, sizeOf, (int)self.ReadCInt32X(big));
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static T[] ReadTArray<T>(this BinaryReader self, int sizeOf, int count, T[] obj = default) where T : struct => count > 0 ? MarshalTArray<T>(self.ReadBytes(sizeOf * count), count) : [];

    // Struct : List - Factory
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL8FList<T>(this BinaryReader self, Func<BinaryReader, T> factory, List<T> obj = default) => ReadFList(self, factory, self.ReadByte(), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL16FList<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, List<T> obj = default) => ReadFList(self, factory, self.ReadUInt16X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL32FList<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, List<T> obj = default) => ReadFList(self, factory, (int)self.ReadUInt32X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadLV7FList<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, List<T> obj = default) => ReadFList(self, factory, (int)self.ReadIntV7X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadLV8FList<T>(this BinaryReader self, Func<BinaryReader, T> factory, bool big = false, List<T> obj = default) => ReadFList(self, factory, (int)self.ReadUIntV8X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadFList<T>(this BinaryReader self, Func<BinaryReader, T> factory, uint count, List<T> obj = default) => ReadFList(self, factory, (int)count, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> ReadFList<T>(this BinaryReader self, Func<BinaryReader, T> factory, int count, List<T> obj = default) {
        var s = obj ?? new List<T>(count); if (count > 0) for (var i = 0; i < count; i++) s.Add(factory(self));
        return s;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> ReadFIList<T>(this BinaryReader self, Func<BinaryReader, int, T> factory, int count, List<T> obj = default) {
        var s = obj ?? new List<T>(count); if (count > 0) for (var i = 0; i < count; i++) s.Add(factory(self, i));
        return s;
    }

    // Struct : List - Pattern
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL8PList<T>(this BinaryReader self, string pat, List<T> obj = default) where T : struct => ReadPList<T>(self, pat, self.ReadByte(), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL16PList<T>(this BinaryReader self, string pat, bool big = false, List<T> obj = default) where T : struct => ReadPList<T>(self, pat, self.ReadUInt16X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL32PList<T>(this BinaryReader self, string pat, bool big = false, List<T> obj = default) where T : struct => ReadPList<T>(self, pat, (int)self.ReadUInt32X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadLV7PList<T>(this BinaryReader self, string pat, bool big = false, List<T> obj = default) where T : struct => ReadPList<T>(self, pat, (int)self.ReadIntV7X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadLV8PList<T>(this BinaryReader self, string pat, bool big = false, List<T> obj = default) where T : struct => ReadPList<T>(self, pat, (int)self.ReadUIntV8X(big), obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadPList<T>(this BinaryReader self, string pat, uint count, List<T> obj = default) where T : struct => ReadPList<T>(self, pat, (int)count, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> ReadPList<T>(this BinaryReader self, string pat, int count, List<T> obj = default) where T : struct {
        if (obj == null) return count > 0 ? [.. MarshalPArray<T>(pat, sizeOf => self.ReadBytes(sizeOf * count), count)] : [];
        return obj;
    }

    // Struct : List - Struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL8SList<T>(this BinaryReader self, int sizeOf = 0, List<T> obj = default) where T : struct => ReadSList<T>(self, self.ReadByte(), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL16SList<T>(this BinaryReader self, int sizeOf = 0, bool big = false, List<T> obj = default) where T : struct => ReadSList<T>(self, self.ReadUInt16X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadL32SList<T>(this BinaryReader self, int sizeOf = 0, bool big = false, List<T> obj = default) where T : struct => ReadSList<T>(self, (int)self.ReadUInt32X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadLV7SList<T>(this BinaryReader self, int sizeOf = 0, bool big = false, List<T> obj = default) where T : struct => ReadSList<T>(self, (int)self.ReadIntV7X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadLV8SList<T>(this BinaryReader self, int sizeOf = 0, bool big = false, List<T> obj = default) where T : struct => ReadSList<T>(self, (int)self.ReadUIntV8X(big), sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static List<T> ReadSList<T>(this BinaryReader self, uint count, int sizeOf = 0, List<T> obj = default) where T : struct => ReadSList<T>(self, (int)count, sizeOf, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> ReadSList<T>(this BinaryReader self, int count, int sizeOf = 0, List<T> obj = default) where T : struct {
        if (obj == null) return count > 0 ? [.. MarshalSArray<T>(self.ReadBytes, count, sizeOf)] : [];
        return obj;
    }

    // Struct : Many - Factory
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8FMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) => ReadFMany(self, keyFactory, valueFactory, self.ReadByte(), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16FMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) => ReadFMany(self, keyFactory, valueFactory, self.ReadUInt16X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32FMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) => ReadFMany(self, keyFactory, valueFactory, (int)self.ReadUInt32X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadLV8FMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) => ReadFMany(self, keyFactory, valueFactory, (int)self.ReadUIntV8X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadFMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, uint count, bool sorted = false, IDictionary<TKey, TValue> obj = default) => ReadFMany(self, keyFactory, valueFactory, (int)count, sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDictionary<TKey, TValue> ReadFMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false, IDictionary<TKey, TValue> obj = default) {
        var set = obj ?? (sorted ? new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>()); for (var i = 0; i < count; i++) set.Add(keyFactory(self), valueFactory(self));
        return set;
    }

    // Struct : Many - Pattern
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8PMany<TKey, TValue>(this BinaryReader self, string pat, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadPMany<TKey, TValue>(self, pat, valueFactory, self.ReadByte(), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16PMany<TKey, TValue>(this BinaryReader self, string pat, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadPMany<TKey, TValue>(self, pat, valueFactory, self.ReadUInt16X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32PMany<TKey, TValue>(this BinaryReader self, string pat, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadPMany<TKey, TValue>(self, pat, valueFactory, (int)self.ReadUInt32X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadLV8PMany<TKey, TValue>(this BinaryReader self, string pat, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadPMany<TKey, TValue>(self, pat, valueFactory, (int)self.ReadUIntV8X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadPMany<TKey, TValue>(this BinaryReader self, string pat, Func<BinaryReader, TValue> valueFactory, uint count, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadPMany<TKey, TValue>(self, pat, valueFactory, (int)count, sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDictionary<TKey, TValue> ReadPMany<TKey, TValue>(this BinaryReader self, string pat, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct {
        var set = obj ?? (sorted ? new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>()); for (var i = 0; i < count; i++) set.Add(self.ReadP<TKey>(pat), valueFactory(self));
        return set;
    }

    // Struct : Many - Struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8SMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadSMany<TKey, TValue>(self, valueFactory, self.ReadByte(), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16SMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadSMany<TKey, TValue>(self, valueFactory, self.ReadUInt16X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32SMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadSMany<TKey, TValue>(self, valueFactory, (int)self.ReadUInt32X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadLV8SMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadSMany<TKey, TValue>(self, valueFactory, (int)self.ReadUIntV8X(big), sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadSMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TValue> valueFactory, uint count, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => ReadSMany<TKey, TValue>(self, valueFactory, (int)count, sorted, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDictionary<TKey, TValue> ReadSMany<TKey, TValue>(this BinaryReader self, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct {
        var set = obj ?? (sorted ? new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>()); for (var i = 0; i < count; i++) set.Add(self.ReadS<TKey>(), valueFactory(self));
        return set;
    }

    // Struct : Many - Type
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL8TMany<TKey, TValue>(this BinaryReader self, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => self.ReadTMany<TKey, TValue>(sizeOf, valueFactory, self.ReadByte(), sorted, obj);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL16TMany<TKey, TValue>(this BinaryReader self, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => self.ReadTMany<TKey, TValue>(sizeOf, valueFactory, self.ReadUInt16X(big), sorted, obj);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadL32TMany<TKey, TValue>(this BinaryReader self, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => self.ReadTMany<TKey, TValue>(sizeOf, valueFactory, (int)self.ReadUInt32X(big), sorted, obj);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IDictionary<TKey, TValue> ReadLV8TMany<TKey, TValue>(this BinaryReader self, int sizeOf, Func<BinaryReader, TValue> valueFactory, bool big = false, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct => self.ReadTMany<TKey, TValue>(sizeOf, valueFactory, (int)self.ReadUIntV8X(big), sorted, obj);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static IDictionary<TKey, TValue> ReadTMany<TKey, TValue>(this BinaryReader self, int sizeOf, Func<BinaryReader, TValue> valueFactory, int count, bool sorted = false, IDictionary<TKey, TValue> obj = default) where TKey : struct {
    //    var set = obj ?? (sorted ? new SortedDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>()); for (var i = 0; i < count; i++) set.Add(self.ReadT<TKey>(sizeOf), valueFactory(self));
    //    return set;
    //}

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

    public static Vector2 ReadVector2(this BinaryReader self)
        => new(
            x: self.ReadSingle(),
            y: self.ReadSingle());
    public static Vector2 ReadHalfVector2(this BinaryReader self)
        => new(
            x: (float)self.ReadHalf(),
            y: (float)self.ReadHalf());
    public static Vector3 ReadVector3(this BinaryReader self)
        => new(
            x: self.ReadSingle(),
            y: self.ReadSingle(),
            z: self.ReadSingle());
    public static Vector3 ReadHalfVector3(this BinaryReader self)
        => new(
            x: (float)self.ReadHalf(),
            y: (float)self.ReadHalf(),
            z: (float)self.ReadHalf());
    public static Vector3 ReadHalf16Vector3(this BinaryReader self)
        => new(
            x: self.ReadHalf16(),
            y: self.ReadHalf16(),
            z: self.ReadHalf16());
    public static Vector4 ReadVector4(this BinaryReader self)
        => new(
            x: self.ReadSingle(),
            y: self.ReadSingle(),
            z: self.ReadSingle(),
            w: self.ReadSingle());
    public static Vector4 ReadHalfVector4(this BinaryReader self)
        => new(
            x: (float)self.ReadHalf(),
            y: (float)self.ReadHalf(),
            z: (float)self.ReadHalf(),
            w: (float)self.ReadHalf());

    public static Matrix2x2 ReadMatrix2x2(this BinaryReader r)
        => new() {
            M11 = r.ReadSingle(),
            M12 = r.ReadSingle(),
            M21 = r.ReadSingle(),
            M22 = r.ReadSingle(),
        };
    public static Matrix3x3 ReadMatrix3x3(this BinaryReader r)
        => new() {
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
        => new() {
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
    // /// <summary>
    // /// Reads a column-major 3x3 matrix but returns a functionally equivalent 4x4 matrix.
    // /// </summary>
    // public static Matrix4x4 ReadMatrixColumn3x3As4x4(this BinaryReader r)
    //     => new() {
    //         M11 = r.ReadSingle(),
    //         M21 = r.ReadSingle(),
    //         M31 = r.ReadSingle(),
    //         M41 = 1f,
    //         M12 = r.ReadSingle(),
    //         M22 = r.ReadSingle(),
    //         M32 = r.ReadSingle(),
    //         M42 = 1f,
    //         M13 = r.ReadSingle(),
    //         M23 = r.ReadSingle(),
    //         M33 = r.ReadSingle(),
    //         M43 = 1f,
    //         M14 = 0f,
    //         M24 = 0f,
    //         M34 = 0f,
    //         M44 = 1f
    //     };
    /// <summary>
    /// Reads a row-major 3x3 matrix but returns a functionally equivalent 4x4 matrix.
    /// </summary>
    public static Matrix4x4 ReadMatrix3x3As4x4(this BinaryReader r)
        => new() {
            M11 = r.ReadSingle(),
            M12 = r.ReadSingle(),
            M13 = r.ReadSingle(),
            M14 = 1f,
            M21 = r.ReadSingle(),
            M22 = r.ReadSingle(),
            M23 = r.ReadSingle(),
            M24 = 1f,
            M31 = r.ReadSingle(),
            M32 = r.ReadSingle(),
            M33 = r.ReadSingle(),
            M34 = 1f,
            M41 = 0f,
            M42 = 0f,
            M43 = 0f,
            M44 = 1f
        };
    // public static Matrix4x4 ReadMatrixColumn4x4(this BinaryReader r)
    //     => new() {
    //         M11 = r.ReadSingle(),
    //         M21 = r.ReadSingle(),
    //         M31 = r.ReadSingle(),
    //         M41 = r.ReadSingle(),
    //         M12 = r.ReadSingle(),
    //         M22 = r.ReadSingle(),
    //         M32 = r.ReadSingle(),
    //         M42 = r.ReadSingle(),
    //         M13 = r.ReadSingle(),
    //         M23 = r.ReadSingle(),
    //         M33 = r.ReadSingle(),
    //         M43 = r.ReadSingle(),
    //         M14 = r.ReadSingle(),
    //         M24 = r.ReadSingle(),
    //         M34 = r.ReadSingle(),
    //         M44 = r.ReadSingle()
    //     };
    public static Matrix4x4 ReadMatrix4x4(this BinaryReader r)
        => new() {
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
            M34 = r.ReadSingle(),
            M41 = r.ReadSingle(),
            M42 = r.ReadSingle(),
            M43 = r.ReadSingle(),
            M44 = r.ReadSingle()
        };

    public static Quaternion ReadQuaternion(this BinaryReader self)
        => new(
            x: self.ReadSingle(),
            y: self.ReadSingle(),
            z: self.ReadSingle(),
            w: self.ReadSingle());
    public static Quaternion ReadQuaternionWFirst(this BinaryReader self)
        => new(
            w: self.ReadSingle(),
            x: self.ReadSingle(),
            y: self.ReadSingle(),
            z: self.ReadSingle());
    public static Quaternion ReadHalfQuaternion(this BinaryReader self)
        => new(
            x: (float)self.ReadHalf(),
            y: (float)self.ReadHalf(),
            z: (float)self.ReadHalf(),
            w: (float)self.ReadHalf());

    #endregion

    #region Unknown

    /// <summary>
    /// First reads a UInt16. If the MSB is set, it will be masked with 0x3FFF, shifted left 2 bytes, and then OR'd with the next UInt16. The sum is then added to knownType.
    /// </summary>
    public static uint ReadAsDataIDOfKnownType(this BinaryReader self, uint knownType) {
        var value = self.ReadUInt16();
        if ((value & 0x8000) != 0) {
            var lower = self.ReadUInt16();
            var higher = (value & 0x3FFF) << 16;
            return (uint)(knownType + (higher | lower));
        }
        return knownType + value;
    }

    #endregion
}

#region Old

// USE THIS?
//public static IEnumerable<long> SeekNeedles(this BinaryReader self, byte[] needle)
//{
//    var buffer = new byte[0x100000];
//    int read, i, j = 0;
//    var position = self.BaseStream.Position;
//    while ((read = self.BaseStream.Read(buffer, 0, buffer.Length)) != 0)
//    {
//        for (i = 0; i < read; i++)
//            if (needle[j] == buffer[i])
//            {
//                j++;
//                if (j == needle.Length)
//                {
//                    yield return self.BaseStream.Position = position + i + 1 - needle.Length;
//                    j = 0;
//                }
//            }
//            else j = 0;
//        self.BaseStream.Position = position += read;
//    }
//}


#endregion