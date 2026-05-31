using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenTK.GLControl;

#region DummyGLFWGraphicsContext

/// <summary>
/// At design-time, we don't have a real GLFW graphics context.
/// We use this stub instead, which does nothing but prevent crashes.
/// </summary>
internal class DummyGLFWGraphicsContext : IGLFWGraphicsContext {
    /// <summary>
    /// This can only be constructed internally.
    /// </summary>
    DummyGLFWGraphicsContext() { }

    /// <summary>
    /// The one-and-only instance of this class.
    /// </summary>
    public static DummyGLFWGraphicsContext Instance { get; } = new DummyGLFWGraphicsContext();

    /// <summary>
    /// The mandatory WindowPtr, which is always a null handle.
    /// </summary>
    public IntPtr WindowPtr => IntPtr.Zero;

    /// <summary>
    /// A fake IsCurrent flag, which just stores its last usage.
    /// </summary>
    public bool IsCurrent { get; private set; }

    public int SwapInterval { get; set; }

    /// <summary>
    /// Make this graphics context "current."  This does mostly nothing.
    /// </summary>
    public void MakeCurrent() => IsCurrent = true;

    /// <summary>
    /// Make *no* graphics context "current."  This does mostly nothing.
    /// </summary>
    public void MakeNoneCurrent() => IsCurrent = false;

    /// <summary>
    /// Swap the displayed buffer.  This does *literally* nothing.
    /// </summary>
    public void SwapBuffers() { }
}

#endregion

#region INativeInput

/// <summary>
/// Abstract access to native-input properties, methods, and events.
/// </summary>
public interface INativeInput {
    /// <summary>
    /// Gets or sets the position of the mouse relative to the content area of this window.
    /// </summary>
    Vector2 MousePosition { get; }

    /// <summary>
    /// Gets the current state of the keyboard as of the last time the window processed
    /// events.
    /// </summary>
    KeyboardState KeyboardState { get; }

    /// <summary>
    /// Gets the current state of the joysticks as of the last time the window processed
    /// events.
    /// </summary>
    IReadOnlyList<JoystickState> JoystickStates { get; }

    /// <summary>
    /// Gets the current state of the mouse as of the last time the window processed
    /// events.
    /// </summary>
    MouseState MouseState { get; }

    /// <summary>
    /// Gets a value indicating whether any key is down.
    /// </summary>
    bool IsAnyKeyDown { get; }

    /// <summary>
    /// Gets a value indicating whether any mouse button is pressed.
    /// </summary>
    bool IsAnyMouseButtonDown { get; }

    /// <summary>
    /// Occurs whenever the mouse cursor is moved
    /// </summary>
    event Action<MouseMoveEventArgs> MouseMove;

    /// <summary>
    /// Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is released.
    /// </summary>
    event Action<MouseButtonEventArgs> MouseUp;

    /// <summary>
    /// Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is clicked.
    /// </summary>
    event Action<MouseButtonEventArgs> MouseDown;

    /// <summary>
    /// Occurs whenever the mouse cursor enters the window OpenTK.Windowing.Desktop.NativeWindow.Bounds.
    /// </summary>
    event Action MouseEnter;

    /// <summary>
    /// Occurs whenever the mouse cursor leaves the window OpenTK.Windowing.Desktop.NativeWindow.Bounds.
    /// </summary>
    event Action MouseLeave;

    /// <summary>
    /// Occurs whenever a keyboard key is released.
    /// </summary>
    event Action<KeyboardKeyEventArgs> KeyUp;

    /// <summary>
    /// Occurs whenever a Unicode code point is typed.
    /// </summary>
    event Action<TextInputEventArgs> TextInput;

    /// <summary>
    /// Occurs when a joystick is connected or disconnected.
    /// </summary>
    event Action<JoystickEventArgs> JoystickConnected;

    /// <summary>
    /// Occurs whenever a keyboard key is pressed.
    /// </summary>
    event Action<KeyboardKeyEventArgs> KeyDown;

    /// <summary>
    /// Occurs whenever one or more files are dropped on the window.
    /// </summary>
    event Action<FileDropEventArgs> FileDrop;

    /// <summary>
    /// Occurs whenever a mouse wheel is moved.
    /// </summary>
    event Action<MouseWheelEventArgs> MouseWheel;

    /// <summary>
    /// Gets a <see cref="bool" /> indicating whether this key is currently down.
    /// </summary>
    /// <param name="key">The <see cref="Keys">key</see> to check.</param>
    /// <returns><c>true</c> if <paramref name="key"/> is in the down state; otherwise, <c>false</c>.</returns>
    bool IsKeyDown(Keys key);

    /// <summary>
    /// Gets whether the specified key is pressed in the current frame but released in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="key">The <see cref="Keys">key</see> to check.</param>
    /// <returns>True if the key is pressed in this frame, but not the last frame.</returns>
    bool IsKeyPressed(Keys key);

    /// <summary>
    /// Gets whether the specified key is released in the current frame but pressed in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="key">The <see cref="Keys">key</see> to check.</param>
    /// <returns>True if the key is released in this frame, but pressed the last frame.</returns>
    bool IsKeyReleased(Keys key);

    /// <summary>
    /// Gets a <see cref="bool" /> indicating whether this button is currently down.
    /// </summary>
    /// <param name="button">The <see cref="MouseButton" /> to check.</param>
    /// <returns><c>true</c> if <paramref name="button"/> is in the down state; otherwise, <c>false</c>.</returns>
    bool IsMouseButtonDown(MouseButton button);

    /// <summary>
    /// Gets whether the specified mouse button is pressed in the current frame but released in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed in this frame, but not the last frame.</returns>
    bool IsMouseButtonPressed(MouseButton button);

    /// <summary>
    /// Gets whether the specified mouse button is released in the current frame but pressed in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is released in this frame, but pressed the last frame.</returns>
    bool IsMouseButtonReleased(MouseButton button);
}

#endregion

#region NativeInput

/// <summary>
/// This proxy class provides access to the native input methods and properties
/// exposed by OpenTK, where those methods and properties are safe to invoke.
/// In general, you should prefer to use WinForms's keyboard/mouse input, but
/// if you need access to "raw" device input within a GLControl, this class
/// provides that access.
///
/// Instances of this class are only instantiated if they are required; we
/// don't make one of these if we don't need it.
/// </summary>
internal class NativeInput : INativeInput {
    /// <summary>
    /// Construct a new instance of a NativeInput proxy.
    /// </summary>
    /// <param name="nativeWindow">The NativeWindow that this NativeInput is wrapping.</param>
    internal NativeInput(NativeWindow nativeWindow) => _nativeWindow = nativeWindow;

    /// <summary>
    /// Access to the underlying NativeWindow.
    /// </summary>
    private readonly NativeWindow _nativeWindow;

    /// <summary>
    /// Gets or sets the position of the mouse relative to the content area of this window.
    /// </summary>
    public Vector2 MousePosition {
        get => _nativeWindow.MousePosition;
        set => _nativeWindow.MousePosition = value;
    }

    /// <summary>
    /// Gets the current state of the keyboard as of the last time the window processed
    /// events.
    /// </summary>
    public KeyboardState KeyboardState => _nativeWindow.KeyboardState;

    /// <summary>
    /// Gets the current state of the joysticks as of the last time the window processed
    /// events.
    /// </summary>
    public IReadOnlyList<JoystickState> JoystickStates => _nativeWindow.JoystickStates;

    /// <summary>
    /// Gets the current state of the mouse as of the last time the window processed
    /// events.
    /// </summary>
    public MouseState MouseState => _nativeWindow.MouseState;

    /// <summary>
    /// Gets a value indicating whether any key is down.
    /// </summary>
    public bool IsAnyKeyDown => _nativeWindow.IsAnyKeyDown;

    /// <summary>
    /// Gets a value indicating whether any mouse button is pressed.
    /// </summary>
    public bool IsAnyMouseButtonDown => _nativeWindow.IsAnyMouseButtonDown;


    /// <summary>
    /// Occurs whenever the mouse cursor is moved
    /// </summary>
    public event Action<MouseMoveEventArgs> MouseMove {
        add => _nativeWindow.MouseMove += value;
        remove => _nativeWindow.MouseMove -= value;
    }

    /// <summary>
    /// Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is released.
    /// </summary>
    public event Action<MouseButtonEventArgs> MouseUp {
        add => _nativeWindow.MouseUp += value;
        remove => _nativeWindow.MouseUp -= value;
    }

    /// <summary>
    /// Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is clicked.
    /// </summary>
    public event Action<MouseButtonEventArgs> MouseDown {
        add => _nativeWindow.MouseDown += value;
        remove => _nativeWindow.MouseDown -= value;
    }

    /// <summary>
    /// Occurs whenever the mouse cursor enters the window OpenTK.Windowing.Desktop.NativeWindow.Bounds.
    /// </summary>
    public event Action MouseEnter {
        add => _nativeWindow.MouseEnter += value;
        remove => _nativeWindow.MouseEnter -= value;
    }

    /// <summary>
    /// Occurs whenever the mouse cursor leaves the window OpenTK.Windowing.Desktop.NativeWindow.Bounds.
    /// </summary>
    public event Action MouseLeave {
        add => _nativeWindow.MouseLeave += value;
        remove => _nativeWindow.MouseLeave -= value;
    }

    /// <summary>
    /// Occurs whenever a keyboard key is released.
    /// </summary>
    public event Action<KeyboardKeyEventArgs> KeyUp {
        add => _nativeWindow.KeyUp += value;
        remove => _nativeWindow.KeyUp -= value;
    }

    /// <summary>
    /// Occurs whenever a Unicode code point is typed.
    /// </summary>
    public event Action<TextInputEventArgs> TextInput {
        add => _nativeWindow.TextInput += value;
        remove => _nativeWindow.TextInput -= value;
    }

    /// <summary>
    /// Occurs when a joystick is connected or disconnected.
    /// </summary>
    public event Action<JoystickEventArgs> JoystickConnected {
        add => _nativeWindow.JoystickConnected += value;
        remove => _nativeWindow.JoystickConnected -= value;
    }

    /// <summary>
    /// Occurs whenever a keyboard key is pressed.
    /// </summary>
    public event Action<KeyboardKeyEventArgs> KeyDown {
        add => _nativeWindow.KeyDown += value;
        remove => _nativeWindow.KeyDown -= value;
    }

    /// <summary>
    /// Occurs whenever one or more files are dropped on the window.
    /// </summary>
    public event Action<FileDropEventArgs> FileDrop {
        add => _nativeWindow.FileDrop += value;
        remove => _nativeWindow.FileDrop -= value;
    }

    /// <summary>
    /// Occurs whenever a mouse wheel is moved.
    /// </summary>
    public event Action<MouseWheelEventArgs> MouseWheel {
        add => _nativeWindow.MouseWheel += value;
        remove => _nativeWindow.MouseWheel -= value;
    }

    /// <summary>
    /// Gets a <see cref="bool" /> indicating whether this key is currently down.
    /// </summary>
    /// <param name="key">The <see cref="Keys">key</see> to check.</param>
    /// <returns><c>true</c> if <paramref name="key"/> is in the down state; otherwise, <c>false</c>.</returns>
    public bool IsKeyDown(Keys key) => _nativeWindow.IsKeyDown(key);

    /// <summary>
    /// Gets whether the specified key is pressed in the current frame but released in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="key">The <see cref="Keys">key</see> to check.</param>
    /// <returns>True if the key is pressed in this frame, but not the last frame.</returns>
    public bool IsKeyPressed(Keys key) => _nativeWindow.IsKeyPressed(key);

    /// <summary>
    /// Gets whether the specified key is released in the current frame but pressed in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="key">The <see cref="Keys">key</see> to check.</param>
    /// <returns>True if the key is released in this frame, but pressed the last frame.</returns>
    public bool IsKeyReleased(Keys key) => _nativeWindow.IsKeyReleased(key);

    /// <summary>
    /// Gets a <see cref="bool" /> indicating whether this button is currently down.
    /// </summary>
    /// <param name="button">The <see cref="MouseButton" /> to check.</param>
    /// <returns><c>true</c> if <paramref name="button"/> is in the down state; otherwise, <c>false</c>.</returns>
    public bool IsMouseButtonDown(MouseButton button) => _nativeWindow.IsMouseButtonDown(button);

    /// <summary>
    /// Gets whether the specified mouse button is pressed in the current frame but released in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed in this frame, but not the last frame.</returns>
    public bool IsMouseButtonPressed(MouseButton button) => _nativeWindow.IsMouseButtonPressed(button);

    /// <summary>
    /// Gets whether the specified mouse button is released in the current frame but pressed in the previous frame.
    /// </summary>
    /// <remarks>
    /// "Frame" refers to invocations of <see cref="NativeWindow.ProcessEvents()"/> here.
    /// </remarks>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is released in this frame, but pressed the last frame.</returns>
    public bool IsMouseButtonReleased(MouseButton button) => _nativeWindow.IsMouseButtonReleased(button);
}

#endregion

#region Win32

/// <summary>
/// P/Invoke functions and declarations for Microsoft Windows (32-bit and 64-bit).
/// </summary>
internal static class Win32 {
    public enum WindowLongs : int {
        GWL_EXSTYLE = -20,
        GWLP_HINSTANCE = -6,
        GWLP_HWNDPARENT = -8,
        GWL_ID = -12,
        GWL_STYLE = -16,
        GWL_USERDATA = -21,
        GWL_WNDPROC = -4,
        DWLP_DLGPROC = 4,
        DWLP_MSGRESULT = 0,
        DWLP_USER = 8,
    }

    [Flags]
    public enum WindowStyles : uint {
        WS_BORDER = 0x800000,
        WS_CAPTION = 0xc00000,
        WS_CHILD = 0x40000000,
        WS_CLIPCHILDREN = 0x2000000,
        WS_CLIPSIBLINGS = 0x4000000,
        WS_DISABLED = 0x8000000,
        WS_DLGFRAME = 0x400000,
        WS_GROUP = 0x20000,
        WS_HSCROLL = 0x100000,
        WS_MAXIMIZE = 0x1000000,
        WS_MAXIMIZEBOX = 0x10000,
        WS_MINIMIZE = 0x20000000,
        WS_MINIMIZEBOX = 0x20000,
        WS_OVERLAPPED = 0x0,
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_POPUP = 0x80000000u,
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        WS_SIZEFRAME = 0x40000,
        WS_SYSMENU = 0x80000,
        WS_TABSTOP = 0x10000,
        WS_VISIBLE = 0x10000000,
        WS_VSCROLL = 0x200000,
    }

    [Flags]
    public enum WindowStylesEx : uint {
        WS_EX_ACCEPTFILES = 0x00000010,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_COMPOSITED = 0x02000000,
        WS_EX_CONTEXTHELP = 0x00000400,
        WS_EX_CONTROLPARENT = 0x00010000,
        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_LAYERED = 0x00080000,
        WS_EX_LAYOUTRTL = 0x00400000,
        WS_EX_LEFT = 0x00000000,
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        WS_EX_LTRREADING = 0x00000000,
        WS_EX_MDICHILD = 0x00000040,
        WS_EX_NOACTIVATE = 0x08000000,
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        WS_EX_RIGHT = 0x00001000,
        WS_EX_RIGHTSCROLLBAR = 0x00000000,
        WS_EX_RTLREADING = 0x00002000,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_TRANSPARENT = 0x00000020,
        WS_EX_WINDOWEDGE = 0x00000100,
    }

    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public static int GetLastError() => Marshal.GetLastWin32Error();     // This alias isn't strictly needed, but it reads better.

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, WindowLongs nIndex) => GetWindowLongPtr(hWnd, (int)nIndex);

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)] private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)] private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongs nIndex, IntPtr dwNewLong) => SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)] private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)] private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}

#endregion