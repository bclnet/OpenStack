using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe static class UnsafeX
    {
        //static UnsafeX()
        //{
        //    var dynamicMethod = new DynamicMethod("Memset", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, null, new[] { typeof(IntPtr), typeof(byte), typeof(int) }, typeof(UnsafeX), true);
        //    var b = dynamicMethod.GetILGenerator();
        //    b.Emit(OpCodes.Ldarg_0);
        //    b.Emit(OpCodes.Ldarg_1);
        //    b.Emit(OpCodes.Ldarg_2);
        //    b.Emit(OpCodes.Initblk);
        //    b.Emit(OpCodes.Ret);
        //    MemsetDelegate = (Action<IntPtr, byte, int>)dynamicMethod.CreateDelegate(typeof(Action<IntPtr, byte, int>));
        //}

        //static Action<IntPtr, byte, int> MemsetDelegate;
        //public static void Memset(byte[] array, byte what, int length)
        //{
        //    var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
        //    MemsetDelegate(gcHandle.AddrOfPinnedObject(), what, length);
        //    gcHandle.Free();
        //}

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)] extern unsafe static void msvcrt_memcpy(void* dest, void* src, uint count);
        [DllImport("libc.so", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)] extern unsafe static void libc_memcpy(void* dest, void* src, uint count);
        public delegate void MemcpyDelgate(void* dest, void* src, uint count);
        public static MemcpyDelgate Memcpy = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => msvcrt_memcpy,
            PlatformID.Unix => libc_memcpy,
            _ => Unsafe.CopyBlock,
        };

        public static byte[] MarshalSApply(byte[] source, string p, int count = 1)
        {
            if (p[0] == '<') return source;
            var s = p.ToCharArray();
            char c; int _ = 0, cnt = 0, size;
            for (var k = 0; k < count; k++)
                for (var i = 1; i < s.Length; i++)
                {
                    c = s[i];
                    if (char.IsDigit(c)) { cnt = cnt * 10 + c - '0'; continue; }
                    else if (cnt == 0) cnt = 1;
                    size = MarshalPSymbol(c);
                    if (size <= 0) _ += cnt;
                    else for (var j = 0; j < cnt; j++) { Array.Reverse(source, _, size); _ += size; }
                    cnt = 0;
                }
            return source;
        }

        public static class Shape<T>
        {
            public static readonly object Value = GetValue();
            public static readonly (string p, int s) StructT = Value is (string, int) ? ((string, int))Value : default;
            public static readonly Dictionary<int, string> StructM = Value is (Dictionary<int, string>) ? (Dictionary<int, string>)Value : default;
            static object GetValue()
                => (typeof(T).GetField("Struct", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception($"{typeof(T).Name} needs a Struct field"))
                .GetValue(null);
        }

        #region MarshalP

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static int MarshalPSymbolSize(char c) => (int)Math.Pow(2, "cxbhiq".IndexOf(char.ToLower(c)) - 2);
        public static int MarshalPSymbol(char c) => char.ToLower(c) switch
        {
            'c' or 'x' or 'b' or 's' => 1,
            'h' => 2,
            'i' or 'f' => 4,
            'q' or 'd' => 8,
            _ => throw new Exception($"Unknown PSymbol: {c}")
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MarshalPSize(string p)
        {
            var s = p.ToCharArray(); var sLen = s.Length;
            char c = s[0];
            if (sLen == 1) return MarshalPSymbol(c);
            int _ = 0, cnt = 0, size;
            for (var i = c == '<' || c == '>' ? 1 : 0; i < sLen; i++)
            {
                c = s[i];
                if (char.IsDigit(c)) { cnt = cnt * 10 + c - 0x30; continue; }
                else if (cnt == 0) cnt = 1;
                size = MarshalPSymbol(c);
                _ += size <= 0 ? cnt : size * cnt;
                cnt = 0;
            }
            return _;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MarshalP<T>(string pat, Func<int, byte[]> bytesFunc) where T : struct
        {
            var s = MarshalPSize(pat);
            var bytes = bytesFunc(s);
            if (pat[0] == '>') bytes = MarshalSApply(bytes, pat);
            fixed (byte* _ = bytes) return Marshal.PtrToStructure<T>((IntPtr)_);
            //return MemoryMarshal.Cast<byte, T>(bytes2)[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] MarshalPArray<T>(string pat, Func<int, byte[]> bytesFunc, int count) where T : struct
        {
            var s = MarshalPSize(pat);
            var bytes = bytesFunc(s);
            if (pat[0] == '>') bytes = MarshalSApply(bytes, pat);
            var typeOfT = typeof(T);
            var isEnum = typeOfT.IsEnum;
            var result = isEnum ? Array.CreateInstance(typeOfT.GetEnumUnderlyingType(), count) : new T[count];
            var hresult = GCHandle.Alloc(result, GCHandleType.Pinned);
            fixed (byte* _ = bytes) Memcpy((void*)hresult.AddrOfPinnedObject(), _, (uint)bytes.Length);
            hresult.Free();
            return isEnum ? result.Cast<T>().ToArray() : (T[])result;
        }

        #endregion

        #region MarshalS

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // READ
        public static T MarshalS<T>(Func<int, byte[]> bytesFunc, int sizeOf) where T : struct
        {
            var (p, s) = Shape<T>.StructM != null ? (Shape<T>.StructM.TryGetValue(sizeOf, out var pat) ? pat : throw new ArgumentOutOfRangeException(nameof(sizeOf), $"{sizeOf}"), sizeOf) : Shape<T>.StructT;
            if (sizeOf > 0 && sizeOf != s) throw new Exception($"Sizes are different: {sizeOf}|{s}");
            var bytes = bytesFunc(s);
            if (p[0] == '>') bytes = MarshalSApply(bytes, p);
            fixed (byte* _ = bytes) return Marshal.PtrToStructure<T>((IntPtr)_);
            //return MemoryMarshal.Cast<byte, T>(bytes2)[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // WRITE
        public static byte[] MarshalS<T>(T value, int sizeOf) where T : struct
        {
            var (p, s) = Shape<T>.StructM != null ? (Shape<T>.StructM.TryGetValue(sizeOf, out var pat) ? pat : throw new ArgumentOutOfRangeException(nameof(sizeOf), $"{sizeOf}"), sizeOf) : Shape<T>.StructT;
            if (sizeOf > 0 && sizeOf != s) throw new Exception($"Sizes are different: {sizeOf}|{s}");
            var bytes = new byte[s];
            fixed (byte* _ = bytes) Marshal.StructureToPtr(value, (IntPtr)_, false);
            if (p[0] == '>') bytes = MarshalSApply(bytes, p);
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // READ
        public static T[] MarshalSArray<T>(Func<int, byte[]> bytesFunc, int count, int sizeOf) where T : struct
        {
            var (p, s) = Shape<T>.StructM != null ? (Shape<T>.StructM.TryGetValue(sizeOf, out var pat) ? pat : throw new ArgumentOutOfRangeException(nameof(sizeOf), $"{sizeOf}"), sizeOf) : Shape<T>.StructT;
            if (sizeOf > 0 && sizeOf != s) throw new Exception($"Sizes are different: {sizeOf}|{s}");
            var bytes = bytesFunc(s * count);
            if (p[0] == '>') bytes = MarshalSApply(bytes, p);
            var typeOfT = typeof(T);
            var isEnum = typeOfT.IsEnum;
            var result = isEnum ? Array.CreateInstance(typeOfT.GetEnumUnderlyingType(), count) : new T[count];
            var hresult = GCHandle.Alloc(result, GCHandleType.Pinned);
            fixed (byte* _ = bytes) Memcpy((void*)hresult.AddrOfPinnedObject(), _, (uint)bytes.Length);
            hresult.Free();
            return isEnum ? result.Cast<T>().ToArray() : (T[])result;
        }

        #endregion

        #region MarshalT

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MarshalT<T>(byte[] bytes) where T : struct
        {
            fixed (byte* _ = bytes) return Marshal.PtrToStructure<T>((IntPtr)_);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] MarshalT<T>(T value, int sizeOf) where T : struct
        {
            var bytes = new byte[sizeOf];
            fixed (byte* _ = bytes) Marshal.StructureToPtr(value, (IntPtr)_, false);
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] MarshalTArray<T>(byte[] bytes, int count) where T : struct
        {
            //return MemoryMarshal.Cast<byte, T>(bytes).ToArray();
            var typeOfT = typeof(T);
            var isEnum = typeOfT.IsEnum;
            var result = isEnum ? Array.CreateInstance(typeOfT.GetEnumUnderlyingType(), count) : new T[count];
            var hresult = GCHandle.Alloc(result, GCHandleType.Pinned);
            fixed (byte* _ = bytes) Memcpy((void*)hresult.AddrOfPinnedObject(), _, (uint)bytes.Length);
            hresult.Free();
            return isEnum ? result.Cast<T>().ToArray() : (T[])result;
        }

        public static byte[] MarshalTArray<T>(T[] values, int count) where T : struct
        {
            throw new NotImplementedException();
        }

        #endregion

        public static string FixedAString(byte* data, int length) => Encoding.ASCII.GetString(data, length).TrimEnd('\0');
        public static string FixedAStringScan(byte* data, int length)
        {
            var i = 0;
            while (data[i] != 0 && length-- > 0) i++;
            return i > 0 ? Encoding.ASCII.GetString(data, length) : null;
        }

        public static T[] FixedTArray<T>(T* data, int length)
        {
            var value = new T[length];
            fixed (T* p = value) for (var i = 0; i < length; i++) p[i] = data[i];
            return value;
        }

        public static int Atoi(string data)
        {
            int n = 0; bool neg = false;
            fixed (char* _ = data)
            {
                var s = _;
                var send = s + data.Length;
                while (s != send && char.IsWhiteSpace(*s)) s++;
                switch (*s)
                {
                    case '-': neg = true; s++; break;
                    case '+': s++; break;
                }
                while (s != send && char.IsDigit(*s)) n = 10 * n - (*s++ - '0');
                return neg ? n : -n;
            }
        }

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int QuickSortComparDelegate(void* a, void* b);
        //[DllImport("msvcrt.dll", EntryPoint = "qsort", SetLastError = false)] public static unsafe extern void QuickSort(void* base0, nint n, nint size, QuickSortComparDelegate compar);
        //[DllImport("msvcrt.dll", EntryPoint = "memmove", SetLastError = false)] public static unsafe extern void MoveBlock(void* destination, void* source, uint byteCount);
        //[DllImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)] public static unsafe extern void CopyBlock(void* destination, void* source, uint byteCount);
        //[DllImport("msvcrt.dll", EntryPoint = "memset", SetLastError = false)] public static unsafe extern void InitBlock(void* destination, int c, uint byteCount);
        //[DllImport("msvcrt.dll", EntryPoint = "memcmp", SetLastError = false)] public static unsafe extern int CompareBlock(void* b1, void* b2, int byteCount);

        //[DllImport("Kernel32")] extern static int _lread(SafeFileHandle hFile, void* lpBuffer, int wBytes);
        //public static void ReadBuffer(this FileStream stream, byte[] buf, int length)
        //{
        //    fixed (byte* pbuf = buf) _lread(stream.SafeFileHandle, pbuf, length);
        //}

        //public static T MarshalT<T>(byte[] bytes, int length = -1)
        //{
        //    var size = Marshal.SizeOf(typeof(T));
        //    if (length > 0 && size > length) Array.Resize(ref bytes, size);
        //    fixed (byte* src = bytes) return Marshal.PtrToStructure<T>(new IntPtr(src));
        //    //return (T)Marshal.PtrToStructure(new IntPtr(src), typeof(T));
        //}

        //public static T MarshalTCopy<T>(byte[] bytes, int offset = 0, int length = -1)
        //{
        //    var r = default(T);
        //    var hr = GCHandle.Alloc(r, GCHandleType.Pinned);
        //    fixed (byte* _ = bytes) Memcpy((void*)hr.AddrOfPinnedObject(), _ + offset, (uint)bytes.Length);
        //    hr.Free();
        //    return r;
        //}

        //public static byte[] MarshalF<T>(T value, int length = -1)
        //{
        //    var size = Marshal.SizeOf(typeof(T));
        //    var bytes = new byte[size];
        //    fixed (byte* _ = bytes) Marshal.StructureToPtr(value, new IntPtr(_), false);
        //    return bytes;
        //}

        //        public static T[] MarshalTArray<T>(FileStream stream, int offset, int length)
        //        {
        //            var dest = new T[length];
        //            var h = GCHandle.Alloc(dest, GCHandleType.Pinned);
        //#if !MONO
        //            NativeFile.Read(stream.SafeFileHandle.DangerousGetHandle() + offset, h.AddrOfPinnedObject(), length);
        //#else
        //            NativeFile.Read(stream.Handle + offset, h.AddrOfPinnedObject(), length);
        //#endif
        //            h.Free();
        //            return dest;
        //        }

        //public static T[] MarshalTArray<T>(byte[] bytes, int offset, int count)
        //{
        //    var typeOfT = typeof(T);
        //    var isEnum = typeOfT.IsEnum;
        //    var result = isEnum ? Array.CreateInstance(typeOfT.GetEnumUnderlyingType(), count) : new T[count];
        //    var hresult = GCHandle.Alloc(result, GCHandleType.Pinned);
        //    fixed (byte* _ = bytes) Memcpy((void*)hresult.AddrOfPinnedObject(), _ + offset, (uint)bytes.Length);
        //    hresult.Free();
        //    return isEnum ? result.Cast<T>().ToArray() : (T[])result;
        //}

        //public static byte[] MarshalTArray<T>(T[] values, int count)
        //{
        //    throw new NotImplementedException();
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void WriteGenericToPtr<T>(IntPtr dest, T value, int sizeOfT) where T : struct
        //{
        //    var bytePtr = (byte*)dest;

        //    var valueref = __makeref(value);
        //    var valuePtr = (byte*)*((IntPtr*)&valueref);
        //    for (var i = 0; i < sizeOfT; ++i) bytePtr[i] = valuePtr[i];
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static T ReadGenericFromPtr<T>(IntPtr source, int sizeOfT) where T : struct
        //{
        //    var bytePtr = (byte*)source;

        //    T result = default;
        //    var resultRef = __makeref(result);
        //    var resultPtr = (byte*)*((IntPtr*)&resultRef);

        //    for (var i = 0; i < sizeOfT; ++i) resultPtr[i] = bytePtr[i];

        //    return result;
        //}
    }
}