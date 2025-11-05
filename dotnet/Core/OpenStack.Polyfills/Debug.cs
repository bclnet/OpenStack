using System;

namespace OpenStack;

/// <summary>
/// Debug
/// </summary>
public class Debug {
    public static Action<bool> AssertFunc;
    public static Action<string> LogFunc;
    public static void Assert(bool condition, string message = null) { } // => AssertFunc(condition);
    public static void Log(string message = null) => LogFunc(message);
    public static void Warn(string message) => LogFunc($"WARN: {message}");
    public static void Error(string message) => LogFunc($"ERROR: {message}");
    public static void Trace(string message) => LogFunc($"TRACE: {message}");
}
