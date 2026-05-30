using OpenStack.Gfx;
using OpenStack.Gfx.Egin;
using OpenStack.Gfx.OpenGL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace OpenStack.Wpf.Control;

public class GLWpfControl : OpenTK.Wpf.GLWpfControl {
    static bool CheckGLCalled;
    public static bool ShowConsole = true;
    public GLCamera Camera;
    public bool ViewportChanged = true;

    public GLWpfControl() {
        if (ShowConsole && !DesignerProperties.GetIsInDesignMode(this)) ConsoleManager.Show();
        base.Render += _Render;
        Start(new GLWpfControlSettings {
            MajorVersion = 4,
            MinorVersion = 6,
            Profile = ContextProfile.Compatability,
            ContextFlags = ContextFlags.Debug,
            RenderContinuously = true,
        });
    }

    void _Render(TimeSpan delta) {
        //if (Visibility != Visibility.Visible) return;
        Tick(delta.Milliseconds);
        //if (ViewportChanged)
        SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight);
        Render(delta.Milliseconds);
    }

    protected override void OnInitialized(EventArgs e) {
        base.OnInitialized(e);
        CheckGL();
        Camera = new GLDebugCamera();
        GL.Enable(EnableCap.DepthTest);
    }

    static void CheckGL() {
        if (CheckGLCalled) return; CheckGLCalled = true;
        Console.WriteLine($"OpenGL version: {GL.GetString(StringName.Version)}");
        Console.WriteLine($"OpenGL vendor: {GL.GetString(StringName.Vendor)}");
        Console.WriteLine($"GLSL version: {GL.GetString(StringName.ShadingLanguageVersion)}");
        var extensions = new HashSet<string>();
        for (var i = 0U; i < GL.GetInteger(GetPName.NumExtensions); i++) extensions.Add(GL.GetStringi(StringName.Extensions, i));
        if (extensions.Contains("GL_EXT_texture_filter_anisotropic")) { GfX.MaxTextureMaxAnisotropy = GL.GetInteger((GetPName)All.MaxTextureMaxAnisotropyExt); Console.WriteLine($"MaxTextureMaxAnisotropyExt: {GfX.MaxTextureMaxAnisotropy}"); }
        else Console.Error.WriteLine("GL_EXT_texture_filter_anisotropic is not supported");
    }

    protected virtual void SetViewport(int x, int y, int width, int height) { Camera.SetViewport(x, y, width, height); ViewportChanged = false; }

    //protected override void OnRender(DrawingContext drawingContext) {
    //    base.OnRender(drawingContext);
    //    if (Visibility != Visibility.Visible) return;
    //    if (ViewportChanged) SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight);
    //    Tick();
    //    //Render(TimeSpan.Zero);
    //}

    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); ViewportChanged = true; }

    public virtual void Tick(float deltaTime) {
        //var mouseState = Mouse.GetState(); var keyboardState = Keyboard.GetState();
        Camera.Tick(deltaTime);
        //Camera.HandleInput(mouseState, keyboardState);
        //HandleInput(mouseState, keyboardState);
    }

    protected new virtual void Render(float deltaTime) {
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1f); // GL.ClearColor(Color4.Blue);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Render(Camera, deltaTime);
    }

    protected new virtual void Render(Camera camera, float deltaTime) { }
}
