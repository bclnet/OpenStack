using System;
using System.Runtime.InteropServices;

namespace OpenStack.Wpf.Control;

public static class ConsoleManager {
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll", SetLastError = true)] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] static extern bool AllocConsole();

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    /// <summary>
    /// Displays the console window.
    /// </summary>
    public static void Show() { var handle = GetConsoleWindow(); if (handle != IntPtr.Zero) ShowWindow(handle, SW_SHOW); else Alloc(); }

    /// <summary>
    /// Hides the console window.
    /// </summary>
    public static void Hide() { var handle = GetConsoleWindow(); if (handle != IntPtr.Zero) ShowWindow(handle, SW_HIDE); }

    /// <summary>
    /// Creates and shows a new console window for the process.
    /// </summary>
    public static void Alloc() { AllocConsole(); }
}
