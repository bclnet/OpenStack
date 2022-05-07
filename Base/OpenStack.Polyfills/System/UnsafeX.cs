using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static class UnsafeX
    {
        //static UnsafeX() => Estate.Bootstrap();

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)] extern static unsafe IntPtr memcpy(IntPtr dest, IntPtr src, uint count);
        public static Func<IntPtr, IntPtr, uint, IntPtr> Memcpy = memcpy;

        public static unsafe string ReadZASCII(byte* data, int length)
        {
            var i = 0;
            while (data[i] != 0 && length-- > 0) i++;
            if (i == 0) return null;
            var value = new byte[i];
            fixed (byte* p = value) while (--i >= 0) p[i] = data[i];
            return Encoding.ASCII.GetString(value);
        }

        public static unsafe byte[] ReadBytes(byte* data, int length)
        {
            var value = new byte[length];
            fixed (byte* p = value) for (var i = 0; i < length; i++) p[i] = data[i];
            return value;
        }

        [DllImport("Kernel32")] extern static unsafe int _lread(SafeFileHandle hFile, void* lpBuffer, int wBytes);
        public static unsafe void ReadBuffer(this FileStream stream, byte[] buf, int length)
        {
            fixed (byte* pbuf = buf) _lread(stream.SafeFileHandle, pbuf, length);
        }

        public static unsafe T MarshalT<T>(byte[] bytes, int length = -1)
        {
            var size = Marshal.SizeOf(typeof(T));
            if (length > 0 && size > length) Array.Resize(ref bytes, size);
            fixed (byte* src = bytes) return Marshal.PtrToStructure<T>(new IntPtr(src));
            //return (T)Marshal.PtrToStructure(new IntPtr(src), typeof(T));
            //fixed (byte* src = bytes)
            //{
            //    var r = default(T);
            //    var hr = GCHandle.Alloc(r, GCHandleType.Pinned);
            //    Memcpy(hr.AddrOfPinnedObject(), new IntPtr(src + offset), (uint)bytes.Length);
            //    hr.Free();
            //    return r;
            //}
        }

        public static unsafe byte[] MarshalF<T>(T value, int length = -1)
        {
            var size = Marshal.SizeOf(typeof(T));
            var bytes = new byte[size];
            fixed (byte* src = bytes) Marshal.StructureToPtr(value, new IntPtr(src), false);
            return bytes;
        }

        public static void MarshalTArray<T>(FileStream stream, T[] dest, int offset, int length)
        {
            var h = GCHandle.Alloc(dest, GCHandleType.Pinned);
#if !MONO
            NativeReader.Read(stream.SafeFileHandle.DangerousGetHandle() + offset, h.AddrOfPinnedObject(), length);
#else
            NativeReader.Read(stream.Handle + offset, h.AddrOfPinnedObject(), length);
#endif
            h.Free();
        }

        public static unsafe T[] MarshalTArray<T>(byte[] bytes, int offset, int count)
        {
            var typeOfT = typeof(T);
            var isEnum = typeOfT.IsEnum;
            var result = isEnum ? Array.CreateInstance(typeOfT.GetEnumUnderlyingType(), count) : new T[count];
            fixed (byte* src = bytes)
            {
                var hresult = GCHandle.Alloc(result, GCHandleType.Pinned);
                Memcpy(hresult.AddrOfPinnedObject(), new IntPtr(src + offset), (uint)bytes.Length);
                hresult.Free();
                return isEnum ? result.Cast<T>().ToArray() : (T[])result;
            }
        }

        /// <summary>
        /// Flips a portion of a 2D array vertically.
        /// </summary>
        /// <param name="source">A 2D array represented as a 1D row-major array.</param>
        /// <param name="startIndex">The 1D index of the top left element in the portion of the 2D array we want to flip.</param>
        /// <param name="rows">The number of rows in the sub-array.</param>
        /// <param name="bytesPerRow">The number of columns in the sub-array.</param>
        public static void Flip2DArrayVertically<T>(T[] source, int rowCount, int columnCount) => Flip2DSubArrayVertically(source, 0, rowCount, columnCount);
        /// <summary>
        /// Flips a portion of a 2D array vertically.
        /// </summary>
        /// <param name="source">A 2D array represented as a 1D row-major array.</param>
        /// <param name="startIndex">The 1D index of the top left element in the portion of the 2D array we want to flip.</param>
        /// <param name="rows">The number of rows in the sub-array.</param>
        /// <param name="bytesPerRow">The number of columns in the sub-array.</param>
        public static void Flip2DSubArrayVertically<T>(T[] source, int startIndex, int rows, int bytesPerRow)
        {
            Debug.Assert(startIndex >= 0 && rows >= 0 && bytesPerRow >= 0 && (startIndex + (rows * bytesPerRow)) <= source.Length);
            var tmpRow = new T[bytesPerRow];
            var lastRowIndex = rows - 1;
            for (var rowIndex = 0; rowIndex < (rows / 2); rowIndex++)
            {
                var otherRowIndex = lastRowIndex - rowIndex;
                var rowStartIndex = startIndex + (rowIndex * bytesPerRow);
                var otherRowStartIndex = startIndex + (otherRowIndex * bytesPerRow);
                Array.Copy(source, otherRowStartIndex, tmpRow, 0, bytesPerRow); // other -> tmp
                Array.Copy(source, rowStartIndex, source, otherRowStartIndex, bytesPerRow); // row -> other
                Array.Copy(tmpRow, 0, source, rowStartIndex, bytesPerRow); // tmp -> row
            }
        }
    }
}