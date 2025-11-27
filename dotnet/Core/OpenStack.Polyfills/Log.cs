using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OpenStack;

/// <summary>
/// Log
/// </summary>
public class Log {
    public static Action<bool> AssertFunc;
    public static Action<string> Func;
    public static void Assert(bool condition, string message = null) { } // => AssertFunc(condition);
    public static void Info(string message = null) => Func(message);
    public static void Warn(string message) => Func($"WARN: {message}");
    public static void Error(string message) => Func($"ERROR: {message}");
    public static void Trace(string message) => Func($"TRACE: {message}");
}

/// <summary>
/// LogFile
/// </summary>
/// <param name="directory"></param>
/// <param name="file"></param>
public class LogFile(string directory, string file) : IDisposable {
    readonly FileStream logStream = new        (
            $"{directory}/{DateTime.Now:yyyy-MM-dd_hh-mm-ss}_{file}",
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite,
            4096,
            true
        );

    public void Dispose() => logStream.Close();

    public void Write(string message) {
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(message.Length);
        try {
            Encoding.UTF8.GetBytes
            (
                message,
                0,
                message.Length,
                buffer,
                0
            );
            logStream.Write(buffer, 0, message.Length);
            logStream.WriteByte((byte)'\n');
            logStream.Flush();
        }
        finally {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task WriteAsync(string message) {
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(message.Length);
        try {
            Encoding.UTF8.GetBytes
            (
                message,
                0,
                message.Length,
                buffer,
                0
            );
            await logStream.WriteAsync(buffer, 0, message.Length);
            logStream.WriteByte((byte)'\n');
            await logStream.FlushAsync();
        }
        finally {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    public override string ToString() => logStream.Name;
}