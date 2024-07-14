using OpenStack.Graphics.OpenGL;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static OpenStack.Graphics.OpenGL.GLCamera;

namespace OpenStack.Graphics.Controls
{
    public class GLViewerControl : GLControl
    {
        public static bool ShowConsole;
        public GLCamera Camera;
        readonly Stopwatch Watch = new();
        readonly DispatcherTimer Timer;
        public bool ViewportChanged = true;
        public float DeltaTime;

        public GLViewerControl(TimeSpan? interval = default)
        {
            interval = new TimeSpan(1);
            if (ShowConsole && !IsInDesignMode) ConsoleManager.Show();
            IsVisibleChanged += OnIsVisibleChanged;
            Watch.Start();
            if (interval != null)
            {
                Timer = new() { Interval = interval.Value };
                Timer.Tick += OnTimerTick;
                Timer.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Timer != null) { Timer.Tick -= OnTimerTick; Timer.Stop(); }
            Watch.Stop();
            IsVisibleChanged -= OnIsVisibleChanged;
            base.Dispose(disposing);
        }

        #region Events

        protected void OnTimerTick(object sender, EventArgs e) => InvalidateVisual();
        protected void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) { if (!HasValidContext || (bool)e.NewValue != true) return; Focus(); ViewportChanged = true; }
        protected override void OnMouseEnter(MouseEventArgs e) { Camera.Event(EventType.MouseEnter, e, null); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(MouseEventArgs e) { Camera.Event(EventType.MouseLeave, e, null); base.OnMouseLeave(e); }
        protected override void OnMouseMove(MouseEventArgs e) { Camera.Event(EventType.MouseMove, e, null); base.OnMouseMove(e); }
        protected override void OnMouseWheel(MouseWheelEventArgs e) { Camera.Event(EventType.MouseWheel, e, null); base.OnMouseWheel(e); }
        protected override void OnMouseDown(MouseButtonEventArgs e) { Camera.Event(EventType.MouseDown, e, null); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseButtonEventArgs e) { Camera.Event(EventType.MouseUp, e, null); base.OnMouseUp(e); }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) { base.OnRenderSizeChanged(sizeInfo); ViewportChanged = true; }
        protected override void OnGotFocus(RoutedEventArgs e) => MakeCurrent(); // ViewportChanged = true; Render(0);

        #endregion

        #region Tick

        public virtual void Tick(float? deltaTime = null)
        {
            DeltaTime = deltaTime ?? Watch.ElapsedMilliseconds / 1000f; Watch.Restart();
            var mouseState = OpenTK.Input.Mouse.GetState(); var keyboardState = OpenTK.Input.Keyboard.GetState();
            Camera.Tick(DeltaTime);
            Camera.HandleInput(mouseState, keyboardState);
            HandleInput(mouseState, keyboardState);
        }

        protected virtual void HandleInput(OpenTK.Input.MouseState mouseState, OpenTK.Input.KeyboardState keyboardState) { }

        #endregion

        #region Render

        protected virtual void SetViewportSize(int x, int y, int width, int height)
        {
            Camera.SetViewportSize(x, y, width, height);
            ViewportChanged = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!HasValidContext || Visibility != Visibility.Visible) { base.OnRender(drawingContext); return; }
            if (ViewportChanged) SetViewportSize(0, 0, (int)ActualWidth, (int)ActualHeight);
            Tick();
            Render();
            SwapBuffers();
        }

        protected virtual void Render()
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Render(Camera, DeltaTime);
        }

        protected virtual void Render(Camera camera, float deltaTime) { }

        #endregion

        #region InitializeGL

        protected override void AttachHandle(HandleRef hwnd) { base.AttachHandle(hwnd); if (!HasValidContext) return; InitializeGL(); }
        protected override void DestroyHandle(HandleRef hwnd) { base.DestroyHandle(hwnd); }

        void InitializeGL()
        {
            MakeCurrent();
            CheckGL();
            InitGL();
            Camera = new GLDebugCamera();
            GL.Enable(EnableCap.DepthTest);
        }

        static bool _checkGLCalled;
        void CheckGL()
        {
            if (_checkGLCalled) return;
            _checkGLCalled = true;
            Console.WriteLine($"OpenGL version: {GL.GetString(StringName.Version)}");
            Console.WriteLine($"OpenGL vendor: {GL.GetString(StringName.Vendor)}");
            Console.WriteLine($"GLSL version: {GL.GetString(StringName.ShadingLanguageVersion)}");
            var extensions = new HashSet<string>();
            for (var i = 0; i < GL.GetInteger(GetPName.NumExtensions); i++)
            {
                var extension = GL.GetString(StringNameIndexed.Extensions, i);
                if (!extensions.Contains(extension)) extensions.Add(extension);
            }
            if (extensions.Contains("GL_EXT_texture_filter_anisotropic"))
            {
                var maxTextureMaxAnisotropy = GL.GetInteger((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt);
                PlatformStats.MaxTextureMaxAnisotropy = maxTextureMaxAnisotropy;
                Console.WriteLine($"MaxTextureMaxAnisotropyExt: {maxTextureMaxAnisotropy}");
            }
            else Console.Error.WriteLine("GL_EXT_texture_filter_anisotropic is not supported");
        }

        protected virtual void InitGL() { }

        #endregion
    }
}
