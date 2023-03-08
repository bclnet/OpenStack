using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe static class UnsafeX
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)] extern static IntPtr memcpy(IntPtr dest, IntPtr src, uint count);
        public static Func<IntPtr, IntPtr, uint, IntPtr> Memcpy = memcpy;

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int QuickSortComparDelegate(void* a, void* b);
        //[DllImport("msvcrt.dll", EntryPoint = "qsort", SetLastError = false)] public static unsafe extern void QuickSort(void* base0, nint n, nint size, QuickSortComparDelegate compar);
        [DllImport("msvcrt.dll", EntryPoint = "memmove", SetLastError = false)] public static unsafe extern void MoveBlock(void* destination, void* source, uint byteCount);
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)] public static unsafe extern void CopyBlock(void* destination, void* source, uint byteCount);
        [DllImport("msvcrt.dll", EntryPoint = "memset", SetLastError = false)] public static unsafe extern void InitBlock(void* destination, int c, uint byteCount);
        [DllImport("msvcrt.dll", EntryPoint = "memcmp", SetLastError = false)] public static unsafe extern int CompareBlock(void* b1, void* b2, int byteCount);

        public static string ReadZASCII(byte* data, int length)
        {
            var i = 0;
            while (data[i] != 0 && length-- > 0) i++;
            if (i == 0) return null;
            var value = new byte[i];
            fixed (byte* p = value) while (--i >= 0) p[i] = data[i];
            return Encoding.ASCII.GetString(value);
        }

        public static byte[] ReadBytes(byte* data, int length)
        {
            var value = new byte[length];
            fixed (byte* p = value) for (var i = 0; i < length; i++) p[i] = data[i];
            return value;
        }

        [DllImport("Kernel32")] extern static int _lread(SafeFileHandle hFile, void* lpBuffer, int wBytes);
        public static void ReadBuffer(this FileStream stream, byte[] buf, int length)
        {
            fixed (byte* pbuf = buf) _lread(stream.SafeFileHandle, pbuf, length);
        }

        public static T MarshalT<T>(byte[] bytes, int length = -1)
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

        public static byte[] MarshalF<T>(T value, int length = -1)
        {
            var size = Marshal.SizeOf(typeof(T));
            var bytes = new byte[size];
            fixed (byte* src = bytes) Marshal.StructureToPtr(value, new IntPtr(src), false);
            return bytes;
        }

        public static T[] MarshalTArray<T>(FileStream stream, int offset, int length)
        {
            var dest = new T[length];
            var h = GCHandle.Alloc(dest, GCHandleType.Pinned);
#if !MONO
            NativeFile.Read(stream.SafeFileHandle.DangerousGetHandle() + offset, h.AddrOfPinnedObject(), length);
#else
            NativeFile.Read(stream.Handle + offset, h.AddrOfPinnedObject(), length);
#endif
            h.Free();
            return dest;
        }

        public static T[] MarshalTArray<T>(byte[] bytes, int offset, int count)
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

        public static byte[] MarshalTArray<T>(T[] values, int count)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteGenericToPtr<T>(IntPtr dest, T value, int sizeOfT) where T : struct
        {
            var bytePtr = (byte*)dest;

            var valueref = __makeref(value);
            var valuePtr = (byte*)*((IntPtr*)&valueref);
            for (var i = 0; i < sizeOfT; ++i) bytePtr[i] = valuePtr[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadGenericFromPtr<T>(IntPtr source, int sizeOfT) where T : struct
        {
            var bytePtr = (byte*)source;

            T result = default;
            var resultRef = __makeref(result);
            var resultPtr = (byte*)*((IntPtr*)&resultRef);

            for (var i = 0; i < sizeOfT; ++i) resultPtr[i] = bytePtr[i];

            return result;
        }
    }
}