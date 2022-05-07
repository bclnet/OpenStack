using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public interface INativeReader
    {
        unsafe void Read(IntPtr ptr, IntPtr buffer, int length);
    }

    public static class NativeReader
    {
        public static Func<bool> IsUnix => () => false; //: PlatformStats.Unix
        static readonly INativeReader _nativeReader = IsUnix() ? new NativeReaderUnix() : (INativeReader)new NativeReaderWin32();
        public static unsafe void Read(IntPtr ptr, IntPtr buffer, int length) => _nativeReader.Read(ptr, buffer, length);
    }

    class NativeReaderWin32 : INativeReader
    {
        //[DllImport("kernel32")] unsafe static extern int _lread(IntPtr hFile, void* lpBuffer, int wBytes);

        [DllImport("kernel32")] unsafe static extern bool ReadFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, NativeOverlapped* lpOverlapped);

        public unsafe void Read(IntPtr ptr, IntPtr buffer, int length)
        {
            //_lread(ptr, buffer, length);
            var lpNumberOfBytesRead = 0U;
            ReadFile(ptr, buffer, (uint)length, ref lpNumberOfBytesRead, null);
        }
    }

    class NativeReaderUnix : INativeReader
    {
        [DllImport("libc")] unsafe static extern int read(IntPtr ptr, IntPtr buffer, int length);

        public unsafe void Read(IntPtr ptr, IntPtr buffer, int length) => read(ptr, buffer, length);
    }
}