#pragma warning disable CS8500
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.UnsafeX;


namespace System.IO;

public unsafe static partial class Polyfill {
    #region Base

    #endregion

    #region Primitives

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteDoubleBigEndian(Span<byte> source, double value) {
        if (BitConverter.IsLittleEndian) { var tmp = BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToInt64Bits(value)); MemoryMarshal.Write(source, ref tmp); }
        else MemoryMarshal.Write(source, ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteSingleBigEndian(Span<byte> source, float value) {
        if (BitConverter.IsLittleEndian) { var tmp = BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(value)); MemoryMarshal.Write(source, ref tmp); }
        else MemoryMarshal.Write(source, ref value);
    }

    // primatives : endian
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, byte[] bytes, int sizeOf) { for (var i = 0; i < bytes.Length; i += sizeOf) Array.Reverse(bytes, i, sizeOf); source.Write(bytes); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, double value) { Span<byte> b = stackalloc byte[sizeof(double)]; WriteDoubleBigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, short value) { Span<byte> b = stackalloc byte[sizeof(short)]; BinaryPrimitives.WriteInt16BigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, int value) { Span<byte> b = stackalloc byte[sizeof(int)]; BinaryPrimitives.WriteInt32BigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, long value) { Span<byte> b = stackalloc byte[sizeof(long)]; BinaryPrimitives.WriteInt64BigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, float value) { Span<byte> b = stackalloc byte[sizeof(float)]; WriteSingleBigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, ushort value) { Span<byte> b = stackalloc byte[sizeof(ushort)]; BinaryPrimitives.WriteUInt16BigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, uint value) { Span<byte> b = stackalloc byte[sizeof(uint)]; BinaryPrimitives.WriteUInt32BigEndian(b, value); source.Write(b); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteE(this BinaryWriter source, ulong value) { Span<byte> b = stackalloc byte[sizeof(ulong)]; BinaryPrimitives.WriteUInt64BigEndian(b, value); source.Write(b); }

    // primatives : endianX
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, byte[] bytes, int sizeOf, bool endian) { if (endian) source.WriteE(bytes, sizeOf); else source.Write(bytes); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, double value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, short value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, int value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, long value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, float value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, ushort value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, uint value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteX(this BinaryWriter source, ulong value, bool endian) { if (endian) source.WriteE(value); else source.Write(value); }

    // primatives : specialized
    public static void WriteCInt32(this BinaryWriter source, uint value) {
        throw new NotImplementedException();
    }
    public static void WriteCInt32X(this BinaryWriter source, uint value, bool endian = true) {
        if (!endian) source.WriteCInt32(value);
        throw new NotImplementedException();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteBool32(this BinaryWriter source, bool value) => source.Write(value ? 1 : 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteGuid(this BinaryWriter source, Guid value) => source.Write(value.ToByteArray());

    #endregion

    #region Position

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryWriter Align(this BinaryWriter source, int align = 4) { source.BaseStream.Position = (source.BaseStream.Position + --align) & ~align; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long Tell(this BinaryWriter source) => source.BaseStream.Position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryWriter Seek(this BinaryWriter source, long offset) { source.BaseStream.Position = offset; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryWriter SeekAndAlign(this BinaryWriter source, long offset, int align = 4) { source.BaseStream.Position = offset % align != 0 ? offset + align - (offset % align) : offset; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryWriter Skip(this BinaryWriter source, long count) { source.BaseStream.Position += count; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryWriter SkipAndAlign(this BinaryWriter source, long count, int align = 4) { var offset = source.BaseStream.Position + count; source.BaseStream.Position = offset % align != 0 ? offset + align - (offset % align) : offset; return source; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static BinaryWriter End(this BinaryWriter source, long offset) { source.BaseStream.Seek(offset, SeekOrigin.End); return source; }

    #endregion

    #region String

    public static void WriteZASCII(this BinaryWriter source, string value, int length = int.MaxValue) {
        source.Write(Encoding.ASCII.GetBytes(value));
        source.Write((byte)0);
    }

    #endregion

    #region Struct

    // Struct : Single
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteF<T>(this BinaryWriter source, T value, Func<BinaryWriter, T, byte[]> factory) => source.Write(factory(source, value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteS<T>(this BinaryWriter source, T value, int sizeOf = 0) where T : struct => source.Write(MarshalS(value, sizeOf));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void WriteT<T>(this BinaryWriter source, T value, int sizeOf = 0) where T : struct => source.Write(MarshalT(value, sizeOf == 0 ? sizeof(T) : sizeOf));

    // Struct : Array - Factory
    public static void WriteL8FArray<T>(this BinaryWriter source, T[] value, Action<BinaryWriter, T> factory) { source.Write((byte)value.Length); WriteFArray(source, value, factory); }
    public static void WriteL16FArray<T>(this BinaryWriter source, T[] value, Action<BinaryWriter, T> factory, bool endian = false) { source.WriteX((ushort)value.Length, endian); WriteFArray(source, value, factory); }
    public static void WriteL32FArray<T>(this BinaryWriter source, T[] value, Action<BinaryWriter, T> factory, bool endian = false) { source.WriteX((uint)value.Length, endian); WriteFArray(source, value, factory); }
    public static void WriteC32FArray<T>(this BinaryWriter source, T[] value, Action<BinaryWriter, T> factory, bool endian = false) { source.WriteCInt32X((uint)value.Length, endian); WriteFArray(source, value, factory); }
    public static void WriteFArray<T>(this BinaryWriter source, T[] value, Action<BinaryWriter, T> factory) { for (var i = 0; i < value.Length; i++) factory(source, value[i]); }

    // Struct : Array - Struct
    public static void WriteL8SArray<T>(this BinaryWriter source, T[] value) where T : struct { source.Write((byte)value.Length); WriteSArray(source, value); }
    public static void WriteL16SArray<T>(this BinaryWriter source, T[] value, bool endian = false) where T : struct { source.WriteX((byte)value.Length, endian); WriteSArray(source, value); }
    public static void WriteL32SArray<T>(this BinaryWriter source, T[] value, bool endian = false) where T : struct { source.WriteX((byte)value.Length, endian); WriteSArray(source, value); }
    public static void WriteC32SArray<T>(this BinaryWriter source, T[] value, bool endian = false) where T : struct { source.WriteCInt32X((uint)value.Length, endian); WriteSArray(source, value); }
    public static void WriteSArray<T>(this BinaryWriter source, T[] value) where T : struct { if (value.Length == 0) return; source.Write(new byte[0]); }

    // Struct : Array - Type
    public static void WriteL8TArray<T>(this BinaryWriter source, T[] value, int sizeOf) where T : struct { source.Write((byte)value.Length); WriteTArray(source, value, sizeOf); }
    public static void WriteL16TArray<T>(this BinaryWriter source, T[] value, int sizeOf, bool endian = false) where T : struct { source.WriteX((byte)value.Length, endian); WriteTArray(source, value, sizeOf); }
    public static void WriteL32TArray<T>(this BinaryWriter source, T[] value, int sizeOf, bool endian = false) where T : struct { source.WriteX((byte)value.Length, endian); WriteTArray(source, value, sizeOf); }
    public static void WriteC32TArray<T>(this BinaryWriter source, T[] value, int sizeOf, bool endian = false) where T : struct { source.WriteCInt32X((uint)value.Length, endian); WriteTArray(source, value, sizeOf); }
    public static void WriteTArray<T>(this BinaryWriter source, T[] value, int sizeOf) where T : struct { if (value.Length == 0) return; source.Write(MarshalTArray(value, sizeOf)); }

    #endregion
}