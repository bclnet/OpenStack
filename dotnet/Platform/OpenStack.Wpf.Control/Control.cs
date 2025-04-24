using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using static OpenStack.Wpf.Control.WindowsNative;

namespace OpenStack.Wpf.Control;

public static class WindowsNative {
    [DllImport("User32.dll", EntryPoint = "SetParent")] public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    [DllImport("User32.dll", EntryPoint = "ShowWindow")] public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("User32.dll")] public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);
}

public class ShellControl : UserControl {
    public ShellControl() {
        AddChild(Host = new WindowsFormsHost());
        Host.Child = new System.Windows.Forms.MaskedTextBox("00/00/0000");
        Host.Loaded += OnLoaded;
        Host.Unloaded += OnUnloaded;
        Host.SizeChanged += OnSizeChanged;
    }

    #region Attach

    static readonly string ShellFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Game2.exe");
    readonly WindowsFormsHost Host;
    Process Process;

    void OnLoaded(object sender, RoutedEventArgs e) {
        Process = null;
        var handle = Host.Handle;
        var processFile = new FileInfo(ShellFile);
        var processName = processFile.Name.Replace(".exe", ""); // Clean up extra processes beforehand
        foreach (var p in Process.GetProcesses().Where(p => p.ProcessName == processName)) {
            Console.WriteLine("Clean up extra processes, Process number: {0}", p.Id);
            p.Kill();
        }
        if (!processFile.Exists) return;
        Process = new Process();
        Process.StartInfo.FileName = ShellFile;
        Process.StartInfo.UseShellExecute = true;
        Process.StartInfo.CreateNoWindow = true;
        Process.Start();
        Process.WaitForInputIdle();
        Thread.Sleep(100); // Wait a minute for the handle
        SetParent(Process.MainWindowHandle, handle);
        _ = ShowWindow(Process.MainWindowHandle, (int)ProcessWindowStyle.Maximized);
    }

    void OnUnloaded(object sender, RoutedEventArgs e) {
        try {
            if (Process != null) {
                Process.CloseMainWindow();
                Thread.Sleep(1000);
                while (!Process.HasExited) Process.Kill();
            }
        }
        catch (Exception) { }
    }

    void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        if (Process == null || Process.MainWindowHandle == IntPtr.Zero) return;
        var size = e.NewSize;
        MoveWindow(Process.MainWindowHandle, 0, 0, (int)size.Width, (int)size.Height, true);
    }

    #endregion
}
