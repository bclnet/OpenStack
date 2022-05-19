using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace System.IO
{
    public static class Polyfill
    {
        #region Stream

        public static Byte[] ReadAllBytes(this Stream stream)
        {
            using var s = new MemoryStream();
            var oldPosition = stream.Position;
            stream.Position = 0;
            stream.CopyTo(s);
            stream.Position = oldPosition;
            return s.ToArray();
        }

        public static byte[] ReadBytes(this Stream stream, int count) { var data = new byte[count]; stream.Read(data, 0, count); return data; }
        public static void WriteBytes(this Stream stream, byte[] data) => stream.Write(data, 0, data.Length);
        public static void WriteBytes(this Stream stream, BinaryReader r, int count) { var data = r.ReadBytes(count); stream.Write(data, 0, data.Length); }

        #endregion

        #region BinaryWriter

        public static long Position(this BinaryWriter source) => source.BaseStream.Position;
        public static void WriteBytes(this BinaryWriter source, byte[] data) => source.Write(data, 0, data.Length);
        public static void WriteT<T>(this BinaryWriter source, T value, int length) => source.WriteBytes(UnsafeX.MarshalF(value, length));

        #endregion

        #region BinaryReader

        #region Endian

        public static byte[] ReadBytesE(this BinaryReader source, int count, int sizeOf, bool bigEndian = true) { var bytes = source.ReadBytes(count); if (!bigEndian) return bytes; for (var i = 0; i < bytes.Length; i += sizeOf) Array.Reverse(bytes, i, sizeOf); return bytes; }
        public static double ReadDoubleE(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadDouble(); var bytes = source.ReadBytes(sizeof(double)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToDouble(bytes, 0); }
        public static short ReadInt16E(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadInt16(); var bytes = source.ReadBytes(sizeof(short)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt16(bytes, 0); }
        public static int ReadInt32E(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadInt32(); var bytes = source.ReadBytes(sizeof(int)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt32(bytes, 0); }
        public static long ReadInt64E(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadInt64(); var bytes = source.ReadBytes(sizeof(long)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt64(bytes, 0); }
        public static float ReadSingleE(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadSingle(); var bytes = source.ReadBytes(sizeof(float)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToSingle(bytes, 0); }
        public static ushort ReadUInt16E(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadUInt16(); var bytes = source.ReadBytes(sizeof(ushort)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt16(bytes, 0); }
        public static uint ReadUInt32E(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadUInt32(); var bytes = source.ReadBytes(sizeof(uint)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt32(bytes, 0); }
        public static ulong ReadUInt64E(this BinaryReader source, bool bigEndian = true) { if (!bigEndian) return source.ReadUInt64(); var bytes = source.ReadBytes(sizeof(ulong)); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt64(bytes, 0); }

        #endregion

        #region Position

        /// <summary>
        /// Aligns the stream to the next DWORD boundary.
        /// </summary>
        //public static void Align(this BinaryReader source)
        //{
        //    var alignDelta = source.BaseStream.Position % 4;
        //    if (alignDelta != 0) source.BaseStream.Position += (int)(4 - alignDelta);
        //}
        // align to 4-byte boundary
        public static void Align(this BinaryReader source, int align = 4) => source.BaseStream.Position = (source.BaseStream.Position + --align) & ~align;
        public static long Position(this BinaryReader source) => source.BaseStream.Position;
        public static void Position(this BinaryReader source, long position) => source.BaseStream.Position = position;
        public static long Position(this BinaryReader source, long position, int align) { if (position % 4 != 0) position += 4 - (position % 4); source.BaseStream.Position = position; return position; }
        public static void Seek(this BinaryReader source, long offset, SeekOrigin origin = SeekOrigin.Begin) => source.BaseStream.Seek(offset, origin);
        public static void Skip(this BinaryReader source, long count) => source.BaseStream.Position += count; //source.BaseStream.Seek(count, SeekOrigin.Current);

        public static void Peek(this BinaryReader source, Action<BinaryReader> action, int offset = 0)
        {
            var position = source.BaseStream.Position;
            if (offset != 0) source.BaseStream.Position += offset;
            action(source);
            source.BaseStream.Position = position;
        }
        public static T Peek<T>(this BinaryReader source, Func<BinaryReader, T> action, int offset = 0)
        {
            var origPosition = source.BaseStream.Position;
            if (offset != 0) source.BaseStream.Position += offset;
            var value = action(source);
            source.BaseStream.Position = origPosition;
            return value;
        }

        public static void PeekAt(this BinaryReader source, long position, Action<BinaryReader> action)
        {
            var origPosition = source.BaseStream.Position;
            source.BaseStream.Position = position;
            action(source);
            source.BaseStream.Position = origPosition;
        }
        public static T PeekAt<T>(this BinaryReader source, long position, Func<BinaryReader, T> action)
        {
            var origPosition = source.BaseStream.Position;
            source.BaseStream.Position = position;
            var value = action(source);
            source.BaseStream.Position = origPosition;
            return value;
        }

        #endregion

        #region Bytes

        public static void CopyTo(this BinaryReader source, Stream destination, bool resetPosition = true)
        {
            source.BaseStream.CopyTo(destination);
            if (resetPosition) destination.Position = 0;
        }

        public static byte[] ReadBytesAt(this BinaryReader source, long position, int count)
        {
            var last = source.BaseStream.Position;
            source.BaseStream.Position = position;
            var r = source.ReadBytes(count);
            source.BaseStream.Position = last;
            return r;
        }

        public static byte[] ReadToEnd(this BinaryReader source)
        {
            var length = (int)(source.BaseStream.Length - source.BaseStream.Position);
            Debug.Assert(length <= int.MaxValue);
            return source.ReadBytes(length);
        }
        public static void ReadToEnd(this BinaryReader source, byte[] buffer, int startIndex = 0)
        {
            var length = (int)source.BaseStream.Length - source.BaseStream.Position;
            Debug.Assert(startIndex >= 0 && length <= int.MaxValue && startIndex + length <= buffer.Length);
            source.Read(buffer, startIndex, (int)length);
        }

        public static byte[] ReadL16Bytes(this BinaryReader source) => source.ReadBytes((int)source.ReadUInt16());
        public static byte[] ReadL32Bytes(this BinaryReader source) => source.ReadBytes((int)source.ReadUInt32());

        public static byte[] ReadL16EBytes(this BinaryReader source) => source.ReadBytes((int)source.ReadUInt16E());
        public static byte[] ReadL32EBytes(this BinaryReader source) => source.ReadBytes((int)source.ReadUInt32E());

        #endregion

        #region Other

        public static bool ReadBoolean32(this BinaryReader source) => source.ReadUInt32() != 0;

        public static Guid ReadGuid(this BinaryReader source) => new Guid(source.ReadBytes(16));

        public static T ReadT<T>(this BinaryReader source, int sizeOf) where T : struct => UnsafeX.MarshalT<T>(source.ReadBytes(sizeOf));

        /// <summary>
        /// A Compressed UInt32 can be 1, 2, or 4 bytes.<para />
        /// If the first MSB (0x80) is 0, it is one byte.<para />
        /// If the first MSB (0x80) is set and the second MSB (0x40) is 0, it's 2 bytes.<para />
        /// If both (0x80) and (0x40) are set, it's 4 bytes.
        /// </summary>
        public static uint ReadCompressedUInt32(this BinaryReader source)
        {
            var b0 = source.ReadByte();
            if ((b0 & 0x80) == 0) return b0;
            var b1 = source.ReadByte();
            if ((b0 & 0x40) == 0) return (uint)(((b0 & 0x7F) << 8) | b1);
            var s = source.ReadUInt16();
            return (uint)(((((b0 & 0x3F) << 8) | b1) << 16) | s);
        }

        #endregion

        #region String

        public static string ReadStringAsChars(this BinaryReader source, int length) => new string(source.ReadChars(length));
        public static string ReadStringAsBytes(this BinaryReader source, int length, Encoding encoding = null) => (encoding ?? Encoding.Default).GetString(source.ReadBytes(length));
        public static string ReadZString(this BinaryReader source, char endChar = '\0', StringBuilder builder = null)
        {
            var b = builder ?? new StringBuilder();
            char c;
            while ((c = source.ReadChar()) != endChar) b.Append(c);
            var value = b.ToString();
            if (builder != null) builder.Length = 0;
            return value;
        }

        public static string ReadUnicodeString(this BinaryReader source)
        {
            var stringLength = source.ReadCompressedUInt32();
            var thestring = "";
            for (var i = 0; i < stringLength; i++)
            {
                var myChar = source.ReadUInt16();
                thestring += Convert.ToChar(myChar);
            }
            return thestring;
        }

        public static string ReadObfuscatedString(this BinaryReader source)
        {
            var stringlength = source.ReadUInt16();
            if (stringlength == 0) return string.Empty;
            var thestring = source.ReadBytes(stringlength);
            // flip the bytes in the string to undo the obfuscation: i.e. 0xAB => 0xBA
            for (var i = 0; i < stringlength; i++) thestring[i] = (byte)((thestring[i] >> 4) | (thestring[i] << 4));
            return Encoding.GetEncoding(1252).GetString(thestring);
        }

        /// <summary>
        /// Read a Length-prefixed string from the stream
        /// </summary>
        /// <param name="source"></param>
        /// <param name="byteLength">Size of the Length representation</param>
        /// <returns></returns>
        public static string ReadPString(this BinaryReader source, int byteLength = 4)
        {
            var stringLength = byteLength switch
            {
                1 => source.ReadByte(),
                2 => source.ReadInt16(),
                4 => source.ReadInt32(),
                _ => throw new NotSupportedException("Only Int8, Int16, and Int32 string sizes are supported"),
            };
            // If there is actually a string to read
            return stringLength > 0 ? new string(source.ReadChars(stringLength)) : null;
        }

        /// <summary>
        /// Read a NULL-Terminated string from the stream
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ReadCString(this BinaryReader source)
        {
            var stringLength = 0;
            while (source.ReadByte() != 0) stringLength++;
            source.BaseStream.Seek(0 - stringLength - 1, SeekOrigin.Current);
            var chars = source.ReadChars(stringLength + 1);
            // If there is actually a string to read
            return stringLength > 0 ? new string(chars, 0, stringLength) : null;
        }

        /// <summary>
        /// Read a Fixed-Length string from the stream
        /// </summary>
        /// <param name="source"></param>
        /// <param name="stringLength">Size of the String</param>
        /// <returns></returns>
        public static string ReadFString(this BinaryReader source, int stringLength)
        {
            var chars = source.ReadChars(stringLength);
            for (var i = 0; i < stringLength; i++) if (chars[i] == 0) return new string(chars, 0, i);
            return new string(chars);
        }

        public static string ReadL8String(this BinaryReader source, Encoding encoding = null) => (encoding ?? Encoding.ASCII).GetString(source.ReadBytes(source.ReadByte()));
        public static string ReadL16String(this BinaryReader source, Encoding encoding = null) => (encoding ?? Encoding.ASCII).GetString(source.ReadBytes(source.ReadUInt16()));
        public static string ReadL16String(this BinaryReader source, bool nullTerminated, Encoding encoding = null) { var bytes = source.ReadBytes(source.ReadUInt16()); var newLength = bytes.Length - 1; return (encoding ?? Encoding.ASCII).GetString(bytes, 0, nullTerminated && bytes[newLength] == 0 ? newLength : bytes.Length); }
        public static string ReadL32String(this BinaryReader source, Encoding encoding = null) => (encoding ?? Encoding.ASCII).GetString(source.ReadBytes((int)source.ReadUInt32()));
        public static string ReadL32String(this BinaryReader source, bool nullTerminated, Encoding encoding = null) { var bytes = source.ReadBytes((int)source.ReadUInt32()); var newLength = bytes.Length - 1; return (encoding ?? Encoding.ASCII).GetString(bytes, 0, nullTerminated && bytes[newLength] == 0 ? newLength : bytes.Length); }
        public static string ReadC32String(this BinaryReader source, Encoding encoding = null) => (encoding ?? Encoding.ASCII).GetString(source.ReadBytes((int)source.ReadCompressedUInt32()));
        public static string ReadC32String(this BinaryReader source, bool nullTerminated, Encoding encoding = null) { var bytes = source.ReadBytes((int)source.ReadCompressedUInt32()); var newLength = bytes.Length - 1; return (encoding ?? Encoding.ASCII).GetString(bytes, 0, nullTerminated && bytes[newLength] == 0 ? newLength : bytes.Length); }

        public static string ReadZOptionedString(this BinaryReader source, int length, Encoding encoding = null)
        {
            var buf = source.ReadBytes(length);
            int i;
            i = buf[^1] != 0 ? buf.Length : buf.Length - 1;
            return (encoding ?? Encoding.ASCII).GetString(buf, 0, i);
        }
        public static string ReadZPaddedString(this BinaryReader source, int length, Encoding encoding = null)
        {
            var buf = source.ReadBytes(length);
            int i;
            for (i = buf.Length - 1; i >= 0 && buf[i] == 0; i--) { }
            return (encoding ?? Encoding.ASCII).GetString(buf, 0, i + 1);
        }
        public static string ReadZTermatedString(this BinaryReader source, int length, Encoding encoding = null)
        {
            var buf = source.ReadBytes(length);
            int i;
            for (i = 0; i < buf.Length && buf[i] != 0; i++) { }
            return (encoding ?? Encoding.ASCII).GetString(buf, 0, i);
        }

        public static string ReadZEncoding(this BinaryReader source, Encoding encoding)
        {
            var characterSize = encoding.GetByteCount("e");
            using var s = new MemoryStream();
            while (true)
            {
                var data = new byte[characterSize];
                source.Read(data, 0, characterSize);
                if (encoding.GetString(data, 0, characterSize) == "\0") break;
                s.Write(data, 0, data.Length);
            }
            return encoding.GetString(s.ToArray());
        }

        public static string[] ReadZStringArray(this BinaryReader source, int count, StringBuilder buf = null)
        {
            if (buf == null) buf = new StringBuilder();
            var list = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var c = source.ReadChar();
                while (c != 0) { buf.Append(c); c = source.ReadChar(); }
                list.Add(buf.ToString());
                buf.Clear();
            }
            return list.ToArray();
        }

        public static string ReadO32Encoding(this BinaryReader source, Encoding encoding)
        {
            var currentOffset = source.BaseStream.Position;
            var offset = source.ReadUInt32();
            if (offset == 0) return string.Empty;
            source.BaseStream.Position = currentOffset + offset;
            var str = ReadZEncoding(source, encoding);
            source.BaseStream.Position = currentOffset + 4;
            return str;
        }

        public static string ReadO32UTF8(this BinaryReader source)
        {
            var currentOffset = source.BaseStream.Position;
            var offset = source.ReadUInt32();
            if (offset == 0) return string.Empty;
            source.BaseStream.Position = currentOffset + offset;
            var str = ReadZUTF8(source);
            source.BaseStream.Position = currentOffset + 4;
            return str;
        }

        public static string ReadZUTF8(this BinaryReader source, int length = int.MaxValue, MemoryStream buf = null)
        {
            if (buf == null) buf = new MemoryStream();
            buf.SetLength(0);
            byte c;
            while (length-- > 0 && (c = source.ReadByte()) != 0) buf.WriteByte(c);
            return Encoding.UTF8.GetString(buf.ToArray());
        }
        public static string ReadZASCII(this BinaryReader source, int length = int.MaxValue, MemoryStream buf = null)
        {
            if (buf == null) buf = new MemoryStream();
            buf.SetLength(0);
            byte c;
            while (length-- > 0 && (c = source.ReadByte()) != 0) buf.WriteByte(c);
            return Encoding.ASCII.GetString(buf.ToArray());
        }
        public static List<string> ReadZASCIIList(this BinaryReader source, int length = int.MaxValue, MemoryStream buf = null)
        {
            if (buf == null) buf = new MemoryStream();
            var list = new List<string>();
            byte c;
            while (length > 0)
            {
                buf.SetLength(0);
                while (length-- > 0 && (c = source.ReadByte()) != 0) buf.WriteByte(c);
                list.Add(Encoding.ASCII.GetString(buf.ToArray()));
            }
            return list;
        }

        #endregion

        #region Array

        public static T[] ReadL8Array<T>(this BinaryReader source, int sizeOf) where T : struct => ReadTArray<T>(source, sizeOf, source.ReadByte());
        public static T[] ReadL8Array<T>(this BinaryReader source, Func<BinaryReader, T> factory) => ReadTArray(source, factory, source.ReadByte());

        public static T[] ReadL16Array<T>(this BinaryReader source, int sizeOf) where T : struct => ReadTArray<T>(source, sizeOf, source.ReadUInt16());
        public static T[] ReadL16Array<T>(this BinaryReader source, Func<BinaryReader, T> factory) => ReadTArray(source, factory, source.ReadUInt16());
        public static T[] ReadL32Array<T>(this BinaryReader source, int sizeOf) where T : struct => ReadTArray<T>(source, sizeOf, (int)source.ReadUInt32());
        public static T[] ReadL32Array<T>(this BinaryReader source, Func<BinaryReader, T> factory) => ReadTArray(source, factory, (int)source.ReadUInt32());
        public static T[] ReadC32Array<T>(this BinaryReader source, int sizeOf) where T : struct => ReadTArray<T>(source, sizeOf, (int)source.ReadCompressedUInt32());
        public static T[] ReadC32Array<T>(this BinaryReader source, Func<BinaryReader, T> factory) => ReadTArray(source, factory, (int)source.ReadCompressedUInt32());
        public static T[] ReadTArray<T>(this BinaryReader source, int sizeOf, int count) where T : struct => count > 0 ? UnsafeX.MarshalTArray<T>(source.ReadBytes(sizeOf * count), 0, count) : new T[0];
        public static T[] ReadTArray<T>(this BinaryReader source, Func<BinaryReader, T> factory, int count) { var list = new T[count]; for (var i = 0; i < list.Length; i++) list[i] = factory(source); return list; }

        public static T[] ReadL16EArray<T>(this BinaryReader source, int sizeOf, bool bigEndian = true) where T : struct => ReadTEArray<T>(source, sizeOf, source.ReadUInt16E(bigEndian), bigEndian);
        public static T[] ReadL16EArray<T>(this BinaryReader source, Func<BinaryReader, bool, T> factory, bool bigEndian = true) => ReadTEArray(source, factory, source.ReadUInt16E(bigEndian), bigEndian);
        public static T[] ReadL32EArray<T>(this BinaryReader source, int sizeOf, bool bigEndian = true) where T : struct => ReadTEArray<T>(source, sizeOf, (int)source.ReadUInt32E(bigEndian), bigEndian);
        public static T[] ReadL32EArray<T>(this BinaryReader source, Func<BinaryReader, bool, T> factory, bool bigEndian = true) => ReadTEArray(source, factory, (int)source.ReadUInt32E(bigEndian), bigEndian);
        public static T[] ReadTEArray<T>(this BinaryReader source, int sizeOf, int count, bool bigEndian = true) where T : struct => count > 0 ? UnsafeX.MarshalTArray<T>(source.ReadBytesE(sizeOf * count, sizeOf, bigEndian), 0, count) : new T[0];
        public static T[] ReadTEArray<T>(this BinaryReader source, Func<BinaryReader, bool, T> factory, int count, bool bigEndian = true) { var list = new T[count]; if (count > 0) for (var i = 0; i < list.Length; i++) list[i] = factory(source, bigEndian); return list; }

        #endregion

        #region Many

        public static Dictionary<TKey, TValue> ReadL8Many<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTMany<TKey, TValue>(source, keySizeOf, valueFactory, source.ReadByte(), offset);
        public static Dictionary<TKey, TValue> ReadL16Many<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTMany<TKey, TValue>(source, keySizeOf, valueFactory, source.ReadUInt16(), offset);
        public static Dictionary<TKey, TValue> ReadL32Many<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTMany<TKey, TValue>(source, keySizeOf, valueFactory, (int)source.ReadUInt32(), offset);
        public static Dictionary<TKey, TValue> ReadC32Many<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTMany<TKey, TValue>(source, keySizeOf, valueFactory, (int)source.ReadCompressedUInt32(), offset);
        public static Dictionary<TKey, TValue> ReadTMany<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int count, int offset = 0) where TKey : struct
        {
            if (offset != 0) source.Skip(offset);
            var set = new Dictionary<TKey, TValue>();
            for (var i = 0; i < count; i++) set.Add(source.ReadT<TKey>(keySizeOf), valueFactory(source));
            return set;
        }

        public static Dictionary<TKey, TValue> ReadL8Many<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int offset = 0) => ReadTMany<TKey, TValue>(source, keyFactory, valueFactory, source.ReadByte(), offset);
        public static Dictionary<TKey, TValue> ReadL16Many<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int offset = 0) => ReadTMany<TKey, TValue>(source, keyFactory, valueFactory, source.ReadUInt16(), offset);
        public static Dictionary<TKey, TValue> ReadL32Many<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int offset = 0) => ReadTMany<TKey, TValue>(source, keyFactory, valueFactory, (int)source.ReadUInt32(), offset);
        public static Dictionary<TKey, TValue> ReadC32Many<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int offset = 0) => ReadTMany<TKey, TValue>(source, keyFactory, valueFactory, (int)source.ReadCompressedUInt32(), offset);
        public static Dictionary<TKey, TValue> ReadTMany<TKey, TValue>(this BinaryReader source, Func<BinaryReader, TKey> keyFactory, Func<BinaryReader, TValue> valueFactory, int count, int offset = 0)
        {
            if (offset != 0) source.Skip(offset);
            var set = new Dictionary<TKey, TValue>();
            for (var i = 0; i < count; i++) set.Add(keyFactory(source), valueFactory(source));
            return set;
        }

        public static SortedDictionary<TKey, TValue> ReadL16SortedMany<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTSortedMany<TKey, TValue>(source, keySizeOf, valueFactory, source.ReadUInt16(), offset);
        public static SortedDictionary<TKey, TValue> ReadL32SortedMany<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTSortedMany<TKey, TValue>(source, keySizeOf, valueFactory, (int)source.ReadUInt32(), offset);
        public static SortedDictionary<TKey, TValue> ReadC32SortedMany<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int offset = 0) where TKey : struct => ReadTSortedMany<TKey, TValue>(source, keySizeOf, valueFactory, (int)source.ReadCompressedUInt32(), offset);
        public static SortedDictionary<TKey, TValue> ReadTSortedMany<TKey, TValue>(this BinaryReader source, int keySizeOf, Func<BinaryReader, TValue> valueFactory, int count, int offset = 0) where TKey : struct
        {
            if (offset != 0) source.Skip(offset);
            var set = new SortedDictionary<TKey, TValue>();
            for (var i = 0; i < count; i++) set.Add(source.ReadT<TKey>(keySizeOf), valueFactory(source));
            return set;
        }

        #endregion

        #region Numerics

        //:ref https://docs.microsoft.com/en-us/windows/win32/direct3d11/floating-point-rules#16-bit-floating-point-rules
        static float Byte2HexIntFracToFloat2(string hexString)
        {
            string sintPart = hexString[..2], sfracPart = hexString.Substring(2, 2);
            int intPart = Convert.ToSByte(sintPart, 16), num = short.Parse(sfracPart, NumberStyles.AllowHexSpecifier);
            var bytes = BitConverter.GetBytes(num);
            string binary = Convert.ToString(bytes[0], 2).PadLeft(8, '0'), binaryFracPart = binary;
            // convert Fractional Part
            var dec = 0f;
            for (var i = 0; i < binaryFracPart.Length; i++)
            {
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

        public static Matrix3x3 ReadMatrix3x3(this BinaryReader r)
            => new Matrix3x3
            {
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
            => new Matrix3x4
            {
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
        public static Matrix4x4 ReadColumnMajorMatrix3x3(this BinaryReader source)
        {
            var matrix = new Matrix4x4();
            for (var columnIndex = 0; columnIndex < 4; columnIndex++)
                for (var rowIndex = 0; rowIndex < 4; rowIndex++)
                {
                    // If we're in the 3x3 part of the matrix, read values. Otherwise, use the identity matrix.
                    if (rowIndex <= 2 && columnIndex <= 2) matrix.Set(rowIndex, columnIndex, source.ReadSingle());
                    else matrix.Set(rowIndex, columnIndex, rowIndex == columnIndex ? 1f : 0f);
                }
            return matrix;
        }
        /// <summary>
        /// Reads a row-major 3x3 matrix but returns a functionally equivalent 4x4 matrix.
        /// </summary>
        public static Matrix4x4 ReadRowMajorMatrix3x3(this BinaryReader source)
        {
            var matrix = new Matrix4x4();
            for (var rowIndex = 0; rowIndex < 4; rowIndex++)
                for (var columnIndex = 0; columnIndex < 4; columnIndex++)
                {
                    // If we're in the 3x3 part of the matrix, read values. Otherwise, use the identity matrix.
                    if (rowIndex <= 2 && columnIndex <= 2) matrix.Set(rowIndex, columnIndex, source.ReadSingle());
                    else matrix.Set(rowIndex, columnIndex, rowIndex == columnIndex ? 1f : 0f);
                }
            return matrix;
        }
        public static Matrix4x4 ReadColumnMajorMatrix4x4(this BinaryReader source)
        {
            var matrix = new Matrix4x4();
            for (var columnIndex = 0; columnIndex < 4; columnIndex++)
                for (var rowIndex = 0; rowIndex < 4; rowIndex++) matrix.Set(rowIndex, columnIndex, source.ReadSingle());
            return matrix;
        }
        public static Matrix4x4 ReadRowMajorMatrix4x4(this BinaryReader source)
        {
            var matrix = new Matrix4x4();
            for (var rowIndex = 0; rowIndex < 4; rowIndex++)
                for (var columnIndex = 0; columnIndex < 4; columnIndex++) matrix.Set(rowIndex, columnIndex, source.ReadSingle());
            return matrix;
        }
        public static Quaternion ReadQuaternionWFirst(this BinaryReader source)
            => new Quaternion(
                w: source.ReadSingle(),
                x: source.ReadSingle(),
                y: source.ReadSingle(),
                z: source.ReadSingle());
        public static Quaternion ReadQuaternion(this BinaryReader source)
            => new Quaternion(
                x: source.ReadSingle(),
                y: source.ReadSingle(),
                z: source.ReadSingle(),
                w: source.ReadSingle());
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
        public static uint ReadAsDataIDOfKnownType(this BinaryReader source, uint knownType)
        {
            var value = source.ReadUInt16();
            if ((value & 0x8000) != 0)
            {
                var lower = source.ReadUInt16();
                var higher = (value & 0x3FFF) << 16;
                return (uint)(knownType + (higher | lower));
            }
            return knownType + value;
        }

        #endregion

        #endregion
    }
}