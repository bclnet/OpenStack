#nullable enable
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using NativeWindow = OpenTK.Windowing.Desktop.NativeWindow;

namespace OpenTK.GLControl;

#region GLControl

/// <summary>
/// OpenGL-capable WinForms control that is a specialized wrapper around
/// OpenTK's NativeWindow.
/// </summary>
public class GLControl : Control {
    /// <summary>
    /// The OpenGL configuration of this control.
    /// </summary>
    private GLControlSettings _glControlSettings;

    /// <summary>
    /// The underlying native window.  This will be reparented to be a child of
    /// this control.
    /// </summary>
    private NativeWindow? _nativeWindow = null;

    // Indicates that OnResize was called before OnHandleCreated.
    // To avoid issues with missing OpenGL contexts, we suppress
    // the premature Resize event and raise it as soon as the handle
    // is ready.
    private bool _resizeEventSuppressed;

    /// <summary>
    /// This is used to render the control at design-time, since we cannot
    /// use a real GLFW instance in the WinForms Designer.
    /// </summary>
    private GLControlDesignTimeRenderer? _designTimeRenderer;

    /// <summary>
    /// Gets or sets a value representing the current graphics API.
    /// This value cannot be changed after the control has been initialized (before <see cref="OnHandleCreated(EventArgs)"/> is triggered).
    /// </summary>
    [Category("OpenGL"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ContextAPI API {
        get => _nativeWindow?.API ?? _glControlSettings.API;
        set {
            if (_nativeWindow == null) _glControlSettings.API = value;
            else throw new InvalidOperationException("Can't set OpenGL settings when the control is initialized.");
        }
    }

    /// <summary>
    /// Gets or sets a value representing the current graphics API profile.
    /// This value cannot be changed after the control has been initialized (before <see cref="OnHandleCreated(EventArgs)"/> is triggered).
    /// </summary>
    [Category("OpenGL"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ContextProfile Profile {
        get => _nativeWindow?.Profile ?? _glControlSettings.Profile;
        set {
            if (_nativeWindow == null) _glControlSettings.Profile = value;
            else throw new InvalidOperationException("Can't set OpenGL settings when the control is initialized.");
        }
    }

    /// <summary>
    /// Gets or sets a value representing the current graphics profile flags.
    /// This value cannot be changed after the control has been initialized (before <see cref="OnHandleCreated(EventArgs)"/> is triggered).
    /// </summary>
    [Category("OpenGL"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ContextFlags Flags {
        get => _nativeWindow?.Flags ?? _glControlSettings.Flags;
        set {
            if (_nativeWindow == null) _glControlSettings.Flags = value;
            else throw new InvalidOperationException("Can't set OpenGL settings when the control is initialized.");
        }
    }

    /// <summary>
    /// Gets or sets a value representing the current version of the graphics API.
    /// This value cannot be changed after the control has been initialized (before <see cref="OnHandleCreated(EventArgs)"/> is triggered).
    /// </summary>
    [Category("OpenGL"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Version APIVersion {
        get => _nativeWindow?.APIVersion ?? _glControlSettings.APIVersion;
        set {
            if (_nativeWindow == null) _glControlSettings.APIVersion = value;
            else throw new InvalidOperationException("Can't set OpenGL settings when the control is initialized.");
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="GLControl"/> used to share OpenGL resources.
    /// This value cannot be changed after the control has been initialized (before <see cref="OnHandleCreated(EventArgs)"/> is triggered).
    /// </summary>
    [Category("OpenGL"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public GLControl? SharedContext {
        get => _sharedContext;
        set {
            if (_nativeWindow == null) _sharedContext = value;
            else throw new InvalidOperationException("Can't set OpenGL settings when the control is initialized.");
        }
    }
    private GLControl? _sharedContext;

    /// <summary>
    /// Gets the <see cref="IGraphicsContext"/> instance that is associated with the <see cref="GLControl"/>.
    /// </summary>
    [Browsable(false)]
    public IGLFWGraphicsContext? Context => _nativeWindow?.Context;

    /// <summary>
    /// Gets or sets a value indicating whether or not this window is event-driven.
    /// An event-driven window will wait for events before updating/rendering. It is useful for non-game applications,
    /// where the program only needs to do any processing after the user inputs something.
    /// </summary>
    [Category("Behavior"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool IsEventDriven {
        get => _nativeWindow?.IsEventDriven ?? _glControlSettings.IsEventDriven;
        set {
            if (value != IsEventDriven) {
                _glControlSettings.IsEventDriven = value;
                if (IsHandleCreated && _nativeWindow != null) {
                    _nativeWindow.IsEventDriven = value;
                }
            }
        }
    }

    /// <summary>
    /// The standard DesignMode property is horribly broken; it doesn't work correctly
    /// inside the constructor, and it doesn't work correctly under inheritance or when
    /// a control is contained by another control.  For compatibility reasons, Microsoft
    /// is also unlikely to fix it.  So this properly has *more* correct design-time
    /// behavior, everywhere except the constructor.  It tries several techniques to
    /// figure out if this is design-time or not, and then it caches the result.
    /// </summary>
    /// <remarks>
    /// In future versions of this control we can use
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.isancestorsiteindesignmode">IsAncestorSiteInDesignMode</see>
    /// instead of this check.
    /// </remarks>
    [Browsable(false)]
    public bool IsDesignMode
        => _isDesignMode ??= DetermineIfThisIsInDesignMode();
    private bool? _isDesignMode;

    /// <summary>
    /// Gets a value indicating whether the underlying native window was
    /// successfully created.
    /// </summary>
    [Browsable(false)]
    public bool HasValidContext => _nativeWindow != null;

    /// <summary>
    /// Gets the aspect ratio of this GLControl.
    /// </summary>
    [Description("The aspect ratio of the client area of this GLControl.")]
    [Category("Layout")]
    public float AspectRatio
        => Width / (float)Height;

    // Remove the Text property from the WinForms editor.
    [Browsable(false)]
    public override string Text { get => base.Text; set => base.Text = value; }

    /// <summary>
    /// Access to native-input properties and methods, for more direct control
    /// of the keyboard/mouse/joystick than WinForms natively provides.
    /// We don't instantiate this unless someone asks for it.  In general, if you
    /// *can* do input using WinForms, you *should* do input using WinForms.  But
    /// if you need more direct input control, you can use this property instead.
    ///
    /// This property is null by default.  If you need NativeInput, you
    /// *must* use EnableNativeInput to access it.
    /// </summary>
    private NativeInput? _nativeInput;

    /// <summary>
    /// Constructs a new instance with default GLControlSettings.  Various things
    /// that like to use reflection want to have an empty constructor available,
    /// so we offer this constructor rather than just adding `= null` to the
    /// constructor that does the actual construction work.
    /// </summary>
    public GLControl()
        : this(null) {
    }

    /// <summary>
    /// Constructs a new instance with the specified GLControlSettings.
    /// </summary>
    /// <param name="glControlSettings">The preferred configuration for the OpenGL
    /// renderer.  If null, 'GLControlSettings.Default' will be used instead.</param>
    public GLControl(GLControlSettings? glControlSettings) {
        SetStyle(ControlStyles.Opaque, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = false;

        _glControlSettings = glControlSettings != null
            ? glControlSettings.Clone() : new GLControlSettings();
    }

    /// <summary>
    /// This event handler will be invoked by WinForms when the HWND of this
    /// control itself has been created and assigned in the Handle property.
    /// We capture the event to construct the NativeWindow that will be responsible
    /// for all of the actual OpenGL rendering and native device input.
    /// </summary>
    /// <param name="e">An EventArgs instance (ignored).</param>
    protected override void OnHandleCreated(EventArgs e) {
        // We don't convert the GLControlSettings to NativeWindowSettings here as that would call GLFW.
        // And this function will be created in design mode.
        CreateNativeWindow(_glControlSettings);

        base.OnHandleCreated(e);

        if (_resizeEventSuppressed) {
            OnResize(EventArgs.Empty);
            _resizeEventSuppressed = false;
        }

        if (IsDesignMode) {
            _designTimeRenderer = new GLControlDesignTimeRenderer(this);
        }

        if (Focused || (_nativeWindow?.IsFocused ?? false)) {
            ForceFocusToCorrectWindow();
        }

        IComponentChangeService changeService = (IComponentChangeService)GetService(typeof(IComponentChangeService))!;
        if (changeService != null) {
            changeService.ComponentChanged -= ChangeService_ComponentChanged!; // to avoid multiple subscriptions
            changeService.ComponentChanged += ChangeService_ComponentChanged!;
        }
    }

    /// <summary>
    /// This is used to invalidate the control when properties on it change.
    /// This is needed as we display the <see cref="Control.Name"/> in the preview of the control.
    /// If the name changes we need to invalidate the control for it to update accordingly.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A System.ComponentModel.Design.ComponentChangedEventArgs that contains the event data.</param>
    private void ChangeService_ComponentChanged(object sender, ComponentChangedEventArgs e) {
        if (e.Component == this && DesignMode) {
            Invalidate();
        }
    }

    /// <summary>
    /// Construct the child NativeWindow that will wrap the underlying GLFW instance.
    /// </summary>
    /// <param name="glControlSettings">The settings to use for
    /// the new GLFW window.</param>
    private void CreateNativeWindow(GLControlSettings glControlSettings) {
        if (IsDesignMode)
            return;

        if (SharedContext != null) {
            if (SharedContext._nativeWindow == null) {
                throw new InvalidOperationException("The GLControl set as the shared context to this GLControl is not yet initialized. Initialization order when sharing contexts is important.");
            }

            _glControlSettings.SharedContext = SharedContext.Context;
        }

        NativeWindowSettings nativeWindowSettings = glControlSettings.ToNativeWindowSettings();

        _nativeWindow = new NativeWindow(nativeWindowSettings);
        _nativeWindow.FocusedChanged += OnNativeWindowFocused;

        NonportableReparent(_nativeWindow);

        // Force the newly child-ified GLFW window to be resized to fit this control.
        ResizeNativeWindow();

        // And now show the child window, since it hasn't been made visible yet.
        _nativeWindow.IsVisible = true;
    }

    /// <summary>
    /// Gets the CreateParams instance for this GLControl.
    /// This is overridden to force correct child behavior.
    /// </summary>
    protected override CreateParams CreateParams {
        get {
            const int CS_VREDRAW = 0x1;
            const int CS_HREDRAW = 0x2;
            const int CS_OWNDC = 0x20;

            CreateParams cp = base.CreateParams;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
            }
            return cp;
        }
    }

    /// <summary>
    /// Ensure that the required underlying GLFW window has been created.
    /// </summary>
    // FIXME: In .net5.0+ we could add this attribute.
    // This is not strictly true in DesignMode, but maybe it's better than nothing?
    //[MemberNotNull("_nativeWindow")]
    private void EnsureCreated() {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);

        if (!IsHandleCreated) {
            CreateControl();

            if (_nativeWindow == null)
                throw new InvalidOperationException("Failed to create GLControl."
                    + " This is usually caused by trying to perform operations on the GLControl"
                    + " before its containing form has been fully created.  Make sure you are not"
                    + " invoking methods on it before the Form's constructor has completed.");
        }

        if (_nativeWindow == null && !IsDesignMode) {
            RecreateHandle();

            if (_nativeWindow == null)
                throw new InvalidOperationException("Failed to recreate GLControl :-(");
        }
    }

    /// <summary>
    /// Because we're really two windows in one, keyboard-focus is a complex
    /// topic.  To ensure correct behavior, we have to capture the various attempts
    /// to assign focus to one or the other window, and if focus is sent to the
    /// wrong window, we have to redirect it to the correct one.  So every attempt
    /// to set focus to *either* window will trigger this method, which will force
    /// the focus to whichever of the two windows it's supposed to be on.
    /// </summary>
    private void ForceFocusToCorrectWindow() {
        if (IsDesignMode || _nativeWindow == null)
            return;

        unsafe {
            if (IsNativeInputEnabled(_nativeWindow)) {
                // Focus should be on the NativeWindow inside the GLControl.
                _nativeWindow.Focus();
            }
            else {
                // Focus should be on the GLControl itself.
                Focus();
            }
        }
    }

    /// <summary>
    /// Reparent the given NativeWindow to be a child of this GLControl.  This is a
    /// non-portable operation, as its name implies:  It works wildly differently
    /// between OSes.  The current implementation only supports Microsoft Windows.
    /// </summary>
    /// <param name="nativeWindow">The NativeWindow that must become a child of this control.</param>
    private unsafe void NonportableReparent(NativeWindow nativeWindow) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            IntPtr hWnd = GLFW.GetWin32Window(nativeWindow.WindowPtr);

            // Change the real HWND's window styles to be "WS_CHILD | WS_DISABLED" (i.e.,
            // a child of some container, with no input support), and turn off *all* the
            // other style bits (most of the rest of them could cause trouble).  In
            // particular, this turns off stuff like WS_BORDER and WS_CAPTION and WS_POPUP
            // and so on, any of which GLFW might have turned on for us.
            IntPtr style = (IntPtr)(long)(Win32.WindowStyles.WS_CHILD
                | Win32.WindowStyles.WS_DISABLED);
            Win32.SetWindowLongPtr(hWnd, Win32.WindowLongs.GWL_STYLE, style);

            // Change the real HWND's extended window styles to be "WS_EX_NOACTIVATE", and
            // turn off *all* the other extended style bits (most of the rest of them
            // could cause trouble).  We want WS_EX_NOACTIVATE because we don't want
            // Windows mistakenly giving the GLFW window the focus as soon as it's created,
            // regardless of whether it's a hidden window.
            style = (IntPtr)(long)Win32.WindowStylesEx.WS_EX_NOACTIVATE;
            Win32.SetWindowLongPtr(hWnd, Win32.WindowLongs.GWL_EXSTYLE, style);

            // Reparent the real HWND under this control.
            Win32.SetParent(hWnd, Handle);
        }
        else throw new NotSupportedException("The current operating system is not supported by this control.");
    }

    /// <summary>
    /// Enable/disable NativeInput for the given NativeWindow.
    /// </summary>
    /// <param name="isEnabled">Whether NativeInput support should be enabled or disabled.</param>
    private unsafe void EnableNativeInput(NativeWindow nativeWindow, bool isEnabled) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            IntPtr hWnd = GLFW.GetWin32Window(nativeWindow.WindowPtr);

            // Tweak the WS_DISABLED style bit for the native window.  When enabled,
            // it will eat all input events directed to it.  When disabled, events will
            // "pass through" to the parent window (i.e., our WinForms control).
            IntPtr style = Win32.GetWindowLongPtr(hWnd, Win32.WindowLongs.GWL_STYLE);
            if (isEnabled) {
                style = (IntPtr)((Win32.WindowStyles)(long)style & ~Win32.WindowStyles.WS_DISABLED);
            }
            else {
                style = (IntPtr)((Win32.WindowStyles)(long)style | Win32.WindowStyles.WS_DISABLED);
            }
            Win32.SetWindowLongPtr(hWnd, Win32.WindowLongs.GWL_STYLE, style);
        }
        else throw new NotSupportedException("The current operating system is not supported by this control.");
    }

    /// <summary>
    /// Determine if native input is enabled for the given NativeWindow.
    /// </summary>
    /// <param name="nativeWindow">The NativeWindow to query.</param>
    /// <returns>True if native input is enabled; false if it is not.</returns>
    private unsafe bool IsNativeInputEnabled(NativeWindow nativeWindow) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            IntPtr hWnd = GLFW.GetWin32Window(nativeWindow.WindowPtr);
            IntPtr style = Win32.GetWindowLongPtr(hWnd, Win32.WindowLongs.GWL_STYLE);
            return ((Win32.WindowStyles)(long)style & Win32.WindowStyles.WS_DISABLED) == 0;
        }
        else throw new NotSupportedException("The current operating system is not supported by this control.");
    }

    /// <summary>
    /// A fix for the badly-broken DesignMode property, this answers (somewhat more
    /// reliably) whether this is DesignMode or not.  This does *not* work when invoked
    /// from the GLControl's constructor.
    /// </summary>
    /// <returns>True if this is in design mode, false if it is not.</returns>
    private bool DetermineIfThisIsInDesignMode() {
        // The obvious test.
        if (DesignMode)
            return true;

        // This works on .NET Framework but no longer seems to work reliably on .NET Core.
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return true;

        // Try walking the control tree to see if any ancestors are in DesignMode.
        for (Control control = this; control != null; control = control.Parent) {
            if (control.Site != null && control.Site.DesignMode)
                return true;
        }

        // Try checking for `IDesignerHost` in the service collection.
        if (GetService(typeof(System.ComponentModel.Design.IDesignerHost)) != null)
            return true;

        // Last-ditch attempt:  Is the process named `devenv` or `VisualStudio`?
        // These are bad, hacky tests, but they *can* work sometimes.
        if (System.Reflection.Assembly.GetExecutingAssembly().Location.Contains("VisualStudio", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(System.Diagnostics.Process.GetCurrentProcess().ProcessName, "devenv", StringComparison.OrdinalIgnoreCase))
            return true;

        // Nope.  Not design mode.  Probably.  Maybe.
        return false;
    }

    /// <summary>
    /// This is triggered when the underlying Handle/HWND instance is *about to be*
    /// destroyed (this is called *before* the Handle/HWND is destroyed).  We use it
    /// to cleanly destroy the NativeWindow before its parent disappears.
    /// </summary>
    /// <param name="e">An EventArgs instance (ignored).</param>
    protected override void OnHandleDestroyed(EventArgs e) {
        base.OnHandleDestroyed(e);

        DestroyNativeWindow();
    }

    /// <summary>
    /// Destroy the child NativeWindow that wraps the underlying GLFW instance.
    /// </summary>
    private void DestroyNativeWindow() {
        if (_nativeWindow != null) {
            _nativeWindow.Dispose();
            _nativeWindow = null!;
        }
    }

    /// <summary>
    /// This private object is used as the reference for the 'Load' handler in
    /// the Events collection, and is only needed if you use the 'Load' event.
    /// </summary>
    private static readonly object EVENT_LOAD = new object();

    /// <summary>
    /// An event hook, triggered when the control is created for the first time.
    /// </summary>
    [Category("Behavior")]
    [Description("Occurs when the GLControl is first created.")]
    public event EventHandler Load {
        add => Events.AddHandler(EVENT_LOAD, value);
        remove => Events.RemoveHandler(EVENT_LOAD, value);
    }

    /// <summary>
    /// Raises the CreateControl event.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    protected override void OnCreateControl() {
        base.OnCreateControl();

        OnLoad(EventArgs.Empty);
    }

    /// <summary>
    /// The Load event is fired before the control becomes visible for the first time.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    protected virtual void OnLoad(EventArgs e) {
        // There is no good way to explain this event except to say
        // that it's just another name for OnControlCreated.
        ((EventHandler)Events[EVENT_LOAD])?.Invoke(this, e);
    }

    /// <summary>
    /// This is raised by WinForms to paint this instance.
    /// </summary>
    /// <param name="e">A PaintEventArgs object that describes which areas
    /// of the control need to be painted.</param>
    protected override void OnPaint(PaintEventArgs e) {
        EnsureCreated();

        if (IsDesignMode) {
            _designTimeRenderer?.Paint(e.Graphics);
        }

        base.OnPaint(e);
    }

    /// <summary>
    /// This is invoked when the Resize event is triggered, and is used to position
    /// the internal GLFW window accordingly.
    ///
    /// Note: This method may be called before the OpenGL context is ready or the
    /// NativeWindow even exists, so everything inside it requires safety checks.
    /// </summary>
    /// <param name="e">An EventArgs instance (ignored).</param>
    protected override void OnResize(EventArgs e) {
        // Do not raise OnResize event before the handle and context are created.
        if (!IsHandleCreated) {
            _resizeEventSuppressed = true;
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            BeginInvoke(new Action(ResizeNativeWindow)); // Need the native window to resize first otherwise our control will be in the wrong place.
        }
        else {
            ResizeNativeWindow();
        }

        base.OnResize(e);
    }

    /// <summary>
    /// Resize the native window to fit this control.
    /// </summary>
    private void ResizeNativeWindow() {
        if (IsDesignMode)
            return;

        if (_nativeWindow != null) {
            _nativeWindow.ClientRectangle = new Box2i(0, 0, Width, Height);
        }
    }

    /// <summary>
    /// This event is raised when this control's parent control is changed,
    /// which may result in this control becoming a different size or shape, so
    /// we capture it to ensure that the underlying GLFW window gets correctly
    /// resized and repositioned as well.
    /// </summary>
    /// <param name="e">An EventArgs instance (ignored).</param>
    protected override void OnParentChanged(EventArgs e) {
        ResizeNativeWindow();

        base.OnParentChanged(e);
    }

    /// <summary>
    /// This event is raised when something sets the focus to the GLControl.
    /// It is overridden to potentially force the focus to the NativeWindow, if
    /// necessary.
    /// </summary>
    /// <param name="e">An EventArgs instance (ignored).</param>
    protected override void OnGotFocus(EventArgs e) {
        base.OnGotFocus(e);

        if (!ReferenceEquals(e, _noRecursionSafetyArgs)) {
            ForceFocusToCorrectWindow();
        }
    }

    /// <summary>
    /// These EventArgs are used as a safety check to prevent unexpected recursion
    /// in OnGotFocus.
    /// </summary>
    private static readonly EventArgs _noRecursionSafetyArgs = new EventArgs();

    /// <summary>
    /// This event is raised when something sets the focus to the NativeWindow.
    /// It is overridden to potentially force the focus to the GLControl, if
    /// necessary.
    /// </summary>
    /// <param name="e">A FocusChangedEventArgs instance, used to detect if the
    /// NativeWindow is gaining the focus.</param>
    private void OnNativeWindowFocused(FocusedChangedEventArgs e) {
        if (e.IsFocused) {
            ForceFocusToCorrectWindow();
            OnGotFocus(_noRecursionSafetyArgs);
        }
        else {
            OnLostFocus(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Swaps the front and back buffers, presenting the rendered scene to the user.
    /// </summary>
    public void SwapBuffers() {
        if (IsDesignMode)
            return;

        EnsureCreated();
        // FIXME: See [MemberNotNull] comment on EnsureCreated().
        if (_nativeWindow == null)
            throw new Exception("EnsureCreated() failed to create _nativeWindow. This is a bug.");
        _nativeWindow.Context.SwapBuffers();
    }

    /// <summary>
    /// Makes this control's OpenGL context current in the calling thread.
    /// All OpenGL commands issued are hereafter interpreted by this context.
    /// When using multiple GLControls, calling MakeCurrent on one control
    /// will make all other controls non-current in the calling thread.
    /// A GLControl can only be current in one thread at a time.
    /// </summary>
    public void MakeCurrent() {
        if (IsDesignMode)
            return;

        EnsureCreated();
        // FIXME: See [MemberNotNull] comment on EnsureCreated().
        if (_nativeWindow == null)
            throw new Exception("EnsureCreated() failed to create _nativeWindow. This is a bug.");
        _nativeWindow.MakeCurrent();
    }

    /// <summary>
    /// Access to native-input properties and methods, for more direct control
    /// of the keyboard/mouse/joystick than WinForms natively provides.
    /// We don't enable this unless someone asks for it.  In general, if you
    /// *can* do input using WinForms, you *should* do input using WinForms.  But
    /// if you need more direct input control, you can use this property instead.
    ///
    /// Note that enabling native input causes *normal* WinForms input methods to
    /// stop working for this GLControl -- all input for will be sent through the
    /// NativeInput interface instead.
    /// </summary>
    public INativeInput EnableNativeInput() {
        EnsureCreated();
        // FIXME: See [MemberNotNull] comment on EnsureCreated().
        if (_nativeWindow == null)
            throw new Exception("EnsureCreated() failed to create _nativeWindow. This is a bug.");

        _nativeInput ??= new NativeInput(_nativeWindow);

        if (!IsNativeInputEnabled(_nativeWindow)) {
            EnableNativeInput(_nativeWindow, true);
        }

        if (Focused || _nativeWindow.IsFocused) {
            ForceFocusToCorrectWindow();
        }

        return _nativeInput;
    }

    /// <summary>
    /// Disable native input support, and return to using WinForms for all
    /// keyboard/mouse input.  Any INativeInput interface you may have access
    /// to will no longer work propertly until you call EnableNativeInput() again.
    /// </summary>
    public void DisableNativeInput() {
        EnsureCreated();
        // FIXME: See [MemberNotNull] comment on EnsureCreated().
        if (_nativeWindow == null)
            throw new Exception("EnsureCreated() failed to create _nativeWindow. This is a bug.");

        if (IsNativeInputEnabled(_nativeWindow)) {
            EnableNativeInput(_nativeWindow, false);
        }

        if (Focused || _nativeWindow.IsFocused) {
            ForceFocusToCorrectWindow();
        }
    }

}

#endregion

#region GLControlDesignTimeRenderer

/// <summary>
/// At design-time, we really can't load OpenGL and GLFW and render with it
/// for real; the WinForms designer is too limited to support such advanced
/// things without exploding.  So instead, we simply use GDI+ to draw a cube
/// at design-time.
/// </summary>
internal class GLControlDesignTimeRenderer {
    /// <summary>
    /// The GLControl that needs to be rendered at design-time.
    /// </summary>
    private readonly GLControl _owner;

    /// <summary>
    /// The angle (yaw) of the design-time cube.
    /// </summary>
    private float _designTimeCubeYaw;

    /// <summary>
    /// The angle (pitch) of the design-time cube.
    /// </summary>
    private float _designTimeCubeRoll;

    /// <summary>
    /// Endpoints that can make a cube.  We only use this in design mode.
    /// </summary>
    private static (Vector3, Vector3)[] CubeLines { get; } = [
        (new Vector3(-1, -1, -1), new Vector3(+1, -1, -1)),
        (new Vector3(-1, -1, -1), new Vector3(-1, +1, -1)),
        (new Vector3(-1, -1, -1), new Vector3(-1, -1, +1)),

        (new Vector3(+1, -1, -1), new Vector3(+1, +1, -1)),
        (new Vector3(+1, -1, -1), new Vector3(+1, -1, +1)),

        (new Vector3(-1, +1, -1), new Vector3(+1, +1, -1)),
        (new Vector3(-1, +1, -1), new Vector3(-1, +1, +1)),

        (new Vector3(-1, -1, +1), new Vector3(+1, -1, +1)),
        (new Vector3(-1, -1, +1), new Vector3(-1, +1, +1)),

        (new Vector3(+1, +1, +1), new Vector3(+1, +1, -1)),
        (new Vector3(+1, +1, +1), new Vector3(-1, +1, +1)),
        (new Vector3(+1, +1, +1), new Vector3(+1, -1, +1)),
    ];

    /// <summary>
    /// Instantiate a new design-timer renderer for the given GLControl.
    /// </summary>
    /// <param name="owner">The GLControl that needs to be rendered at
    /// design-time.</param>
    public GLControlDesignTimeRenderer(GLControl owner) {
        _owner = owner;
        _designTimeCubeYaw += -10 * (float)(Math.PI / 97);
    }

    /// <summary>
    /// Draw a simple cube, in an ortho projection, using GDI+.
    /// </summary>
    /// <param name="graphics">The GDI+ Graphics object to draw on.</param>
    /// <param name="color">The color for the cube.</param>
    /// <param name="cx">The X coordinate of the center point of the cube,
    /// in Graphics coordinates.</param>
    /// <param name="cy">The Y coordinate of the center point of the cube,
    /// in Graphics coordinates.</param>
    /// <param name="radius">The radius to the cube's corners from the center point.</param>
    /// <param name="yaw">The yaw (rotation around the Y axis) of the cube.</param>
    /// <param name="pitch">The pitch (rotation around the X axis) of the cube.</param>
    /// <param name="roll">The roll (rotation around the Z axis) of the cube.</param>
    private void DrawCube(System.Drawing.Graphics graphics,
        System.Drawing.Color color,
        float cx, float cy, float radius,
        float yaw = 0, float pitch = 0, float roll = 0) {
        // We use matrices to rotate and scale the cube, but we just use a simple
        // center offset to position it.  That saves a lot of extra multiplies all
        // over this code, since we can use Matrix3 and Vector3 instead of Matrix4
        // and Vector4.  And no, quaternions aren't worth the effort here either.
        Matrix3 matrix = Matrix3.CreateRotationZ(roll)
            * Matrix3.CreateRotationY(yaw)
            * Matrix3.CreateRotationX(pitch)
            * Matrix3.CreateScale(radius);

        // Draw the edges of the cube in the given color.  Since it's just a single-
        // color wireframe, the order of the edges doesn't matter at all.
        using System.Drawing.Brush brush = new System.Drawing.SolidBrush(color);
        using System.Drawing.Pen pen = new System.Drawing.Pen(brush);

        foreach ((Vector3 start, Vector3 end) in CubeLines) {
            Vector3 projStart = start * matrix;
            Vector3 projEnd = end * matrix;
            graphics.DrawLine(pen, cx + projStart.X, cy - projStart.Y, cx + projEnd.X, cy - projEnd.Y);
        }
    }

    /// <summary>
    /// In design mode, we have nothing to show, so we paint the
    /// background black and put a cube on it so that it's
    /// obvious that it's a 3D renderer.
    /// </summary>
    public void Paint(System.Drawing.Graphics graphics) {
        // Since we're always DoubleBuffered = false, we have to do
        // simple double-buffering ourselves, using a bitmap.
        using System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_owner.Width, _owner.Height, graphics);
        using System.Drawing.Graphics bitmapGraphics = System.Drawing.Graphics.FromImage(bitmap);

        // Other resources we'll need.
        using System.Drawing.Font bigFont = new System.Drawing.Font("Arial", 12);
        using System.Drawing.Font smallFont = new System.Drawing.Font("Arial", 9);
        using System.Drawing.Brush titleBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        using System.Drawing.Brush subtitleBrush = new System.Drawing.SolidBrush(System.Drawing.Color.PaleGoldenrod);

        // Configuration.
        const float cubeRadius = 16;
        const string title = "GLControl";
        int cx = _owner.Width / 2, cy = _owner.Height / 2;
        string subtitle = $"( {_owner.Name} )";

        // These sizes will hold font metrics.
        System.Drawing.SizeF titleSize;
        System.Drawing.SizeF subtitleSize;
        System.Drawing.SizeF totalTextSize;

        // Start with a black background.
        bitmapGraphics.Clear(System.Drawing.Color.Black);
        bitmapGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Measure and the title (fixed) and the subtitle (the control's Name).
        titleSize = bitmapGraphics.MeasureString(title, bigFont);
        subtitleSize = bitmapGraphics.MeasureString(subtitle, smallFont);
        totalTextSize = new System.Drawing.SizeF(
            Math.Max(titleSize.Width, subtitleSize.Width),
            titleSize.Height + subtitleSize.Height
        );

        // Draw both of the title and subtitle centered, now that we know how big they are.
        bitmapGraphics.DrawString(title, bigFont, titleBrush,
            new System.Drawing.PointF(cx - totalTextSize.Width / 2 + cubeRadius + 2, cy - totalTextSize.Height / 2));
        bitmapGraphics.DrawString(subtitle, smallFont, subtitleBrush,
            new System.Drawing.PointF(cx - totalTextSize.Width / 2 + cubeRadius + 2, cy - totalTextSize.Height / 2 + titleSize.Height));

        // Draw a cube beside the title and subtitle.
        DrawCube(bitmapGraphics, System.Drawing.Color.Red,
            cx - totalTextSize.Width / 2 - cubeRadius - 2, cy, cubeRadius,
            _designTimeCubeYaw, (float)(Math.PI / 8), _designTimeCubeRoll);

        // Draw the resulting bitmap on the real window canvas.
        graphics.DrawImage(bitmap, 0, 0);
    }
}

#endregion

#region GLControlSettings

/// <summary>
/// Configuration settings for a GLControl.  The properties here are a subset
/// of the NativeWindowSettings properties, restricted to those that make
/// sense in a WinForms environment.
/// </summary>
public class GLControlSettings {
    /// <summary>
    /// Gets the default settings for a <see cref="GLControl"/>.
    /// </summary>
    public static readonly GLControlSettings Default = new GLControlSettings();

    /// <summary>
    /// Gets or sets a value representing the current version of the graphics API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OpenGL 3.3 is selected by default, and runs on almost any hardware made within the last ten years.
    /// This will run on Windows, Mac OS, and Linux.
    /// </para>
    /// <para>
    /// OpenGL 4.1 is suggested for modern apps meant to run on more modern hardware.
    /// This will run on Windows, Mac OS, and Linux.
    /// </para>
    /// <para>
    /// OpenGL 4.6 is suggested for modern apps that only intend to run on Windows and Linux;
    /// Mac OS doesn't support it.
    /// </para>
    /// <para>
    /// Note that if you choose an API other than base OpenGL, this will need to be updated accordingly,
    /// as the versioning of OpenGL and OpenGL ES do not match.
    /// </para>
    /// </remarks>
    public Version APIVersion { get; set; } = new Version(3, 3, 0, 0);

    /// <summary>
    /// Gets or sets a value indicating whether or not OpenGL bindings should be automatically loaded
    /// when the window is created.
    /// </summary>
    public bool AutoLoadBindings { get; set; } = true;

    /// <summary>
    /// Gets or sets a value representing the current graphics profile flags.
    /// </summary>
    public ContextFlags Flags { get; set; } = ContextFlags.Default;

    /// <summary>
    /// Gets or sets a value representing the current graphics API profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This only has an effect on OpenGL 3.2 and higher. On older versions, this setting does nothing.
    /// </para>
    /// </remarks>
    public ContextProfile Profile { get; set; } = ContextProfile.Core;

    /// <summary>
    /// Gets or sets a value representing the current graphics API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this is changed, you'll have to modify the API version as well, as the versioning of OpenGL and OpenGL ES
    /// do not match.
    /// </para>
    /// </remarks>
    public ContextAPI API { get; set; } = ContextAPI.OpenGL;

    /// <summary>
    /// Gets or sets a value indicating whether or not this window is event-driven.
    /// An event-driven window will wait for events before updating/rendering. It is useful for non-game applications,
    /// where the program only needs to do any processing after the user inputs something.
    /// </summary>
    public bool IsEventDriven { get; set; } = true;

    /// <summary>
    /// Gets or sets the context to share.
    /// </summary>
    public IGLFWGraphicsContext? SharedContext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of samples that should be used.
    /// </summary>
    /// <remarks>
    /// <c>0</c> indicates that no multisampling should be used;
    /// otherwise multisampling is used if available. The actual number of samples is the closest matching the given number that is supported.
    /// </remarks>
    public int NumberOfSamples { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of stencil bits used for OpenGL context creation.
    /// </summary>
    /// <remarks>
    /// Default value is 8.
    /// </remarks>
    public int? StencilBits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of depth bits used for OpenGL context creation.
    /// </summary>
    /// <remarks>
    /// Default value is 24.
    /// </remarks>
    public int? DepthBits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of red bits used for OpenGL context creation.
    /// </summary>
    /// <remarks>
    /// Default value is 8.
    /// </remarks>
    public int? RedBits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of green bits used for OpenGL context creation.
    /// </summary>
    /// <remarks>
    /// Default value is 8.
    /// </remarks>
    public int? GreenBits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of blue bits used for OpenGL context creation.
    /// </summary>
    /// <remarks>
    /// Default value is 8.
    /// </remarks>
    public int? BlueBits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the number of alpha bits used for OpenGL context creation.
    /// </summary>
    /// <remarks>
    /// Default value is 8.
    /// </remarks>
    public int? AlphaBits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the backbuffer should be sRGB capable.
    /// </summary>
    public bool SrgbCapable { get; set; }

    /// <summary>
    /// Make a perfect shallow copy of this object.
    /// </summary>
    /// <returns>A perfect shallow copy of this GLControlSettings object.</returns>
    public GLControlSettings Clone() => new() {
        APIVersion = APIVersion,
        AutoLoadBindings = AutoLoadBindings,
        Flags = Flags,
        Profile = Profile,
        API = API,
        IsEventDriven = IsEventDriven,
        SharedContext = SharedContext,
        NumberOfSamples = NumberOfSamples,
        StencilBits = StencilBits,
        DepthBits = DepthBits,
        RedBits = RedBits,
        GreenBits = GreenBits,
        BlueBits = BlueBits,
        AlphaBits = AlphaBits,
        SrgbCapable = SrgbCapable,
    };

    /// <summary>
    /// Derive a NativeWindowSettings object from this GLControlSettings object.
    /// The NativeWindowSettings has all of our properties and more, but many of
    /// its properties cannot be reasonably configured by the user when a
    /// NativeWindow is being used as a child window.
    /// </summary>
    /// <returns>The NativeWindowSettings to use when constructing a new
    /// NativeWindow.</returns>
    public NativeWindowSettings ToNativeWindowSettings() => new() {
        APIVersion = FixupVersion(APIVersion),
        AutoLoadBindings = AutoLoadBindings,
        Flags = Flags,
        Profile = Profile,
        API = API,
        IsEventDriven = IsEventDriven,
        SharedContext = SharedContext,
        NumberOfSamples = NumberOfSamples,
        StencilBits = StencilBits,
        DepthBits = DepthBits,
        RedBits = RedBits,
        GreenBits = GreenBits,
        BlueBits = BlueBits,
        AlphaBits = AlphaBits,
        SrgbCapable = SrgbCapable,
        StartFocused = false,
        StartVisible = false,
        WindowBorder = WindowBorder.Hidden,
        WindowState = WindowState.Normal,
    };

    /// <summary>
    /// The WinForms Designer has bugs when it comes to editing Version objects:
    /// Many times when a component is left out, it is treated not as 0, but as -1!
    /// So this little method corrects for bad data from the WinForms designer.
    /// </summary>
    /// <param name="version">A version number.</param>
    /// <returns>The same version number, but with all negative values clipped to 0.</returns>
    private static Version FixupVersion(Version version) => new(
        Math.Max(version.Major, 0),
        Math.Max(version.Minor, 0),
        Math.Max(version.Build, 0),
        Math.Max(version.Revision, 0)
    );
}

#endregion