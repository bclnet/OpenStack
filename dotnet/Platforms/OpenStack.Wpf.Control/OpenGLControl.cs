using OpenStack.Gfx;
using OpenStack.Gfx.Egin;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Windows;
#pragma warning disable CS9113

namespace OpenStack.Wpf.Control;

#region OpenGLControl

public abstract class OpenGLControl(Func<object, object, object, string, object> shellState) : GLWpfControl {
    int Id = 0;

    #region Binding

    protected EginRenderer Renderer;
    protected abstract Renderer CreateRenderer();

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ISource), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty Source2Property = DependencyProperty.Register(nameof(Source2), typeof(object), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(object), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(string), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));

    public ISource Source {
        get => GetValue(SourceProperty) as ISource;
        set => SetValue(SourceProperty, value);
    }

    public object Source2 {
        get => GetValue(Source2Property);
        set => SetValue(Source2Property, value);
    }

    public object Path {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public object Value {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Type {
        get => GetValue(TypeProperty) as string;
        set => SetValue(TypeProperty, value);
    }

    void OnSourceChanged() {
        if (Source == null || Path == null || Value == null || Type == null) return;
        Renderer = (EginRenderer)CreateRenderer();
        Renderer?.Start();
        if (Value is ITextureSelect z2) z2.Select(Id);
        //Camera.SetLocation(new Vector3(200));
        //Camera.LookAt(new Vector3(0));
    }

    #endregion

    #region Render

    protected override void SetViewport(int x, int y, int width, int height) {
        (int width, int height) p = Renderer?.GetViewport((width, height)) ?? (width, height);
        base.SetViewport(x, y, p.width, p.height);
    }

    protected override void Render(Camera camera, float frameTime)
        => Renderer?.Render(camera, default);

    public override void Tick(float deltaTime) {
        base.Tick(deltaTime);
        Renderer?.Update(deltaTime);
        Render(Camera, 0f);
    }

    #endregion

    #region HandleInput

    static readonly Keys[] AllKeys = [Keys.Q, Keys.W, Keys.A, Keys.Z, Keys.Escape, Keys.Space, Keys.GraveAccent];
    readonly HashSet<Keys> KeyDowns = [];

    //protected override void HandleInput(MouseState mouseState, KeyboardState keyboardState) {
    //    if (Renderer == null) return;
    //    foreach (var key in Keys)
    //        if (!KeyDowns.Contains(key) && keyboardState.IsKeyDown(key)) KeyDowns.Add(key);
    //    foreach (var key in KeyDowns)
    //        if (keyboardState.IsKeyReleased(key)) {
    //            KeyDowns.Remove(key);
    //            switch (key) {
    //                case Key.W: Select(++Id); break;
    //                case Key.Q: Select(--Id); break;
    //                    //case Key.A: MovePrev(); break;
    //                    //case Key.Z: MoveNext(); ; break;
    //                    //case Key.Escape: Reset(); break;
    //                    //case Key.Space: MoveReset(); break;
    //                    //case Key.Tilde: Toggle(); break;
    //            }
    //        }
    //}

    void Select(int id) {
        if (Source is ITextureSelect z2) z2.Select(id);
        OnSourceChanged();
        //Views.FileExplorer.Current.OnInfoUpdated();
    }
    //void MoveReset() { Id = 0; View.Level = 0..; OnSourceChanged(); }
    //void MoveNext() { if (View.Level.Start.Value < 10) View.Level = new(View.Level.Start.Value + 1, View.Level.End); OnSourceChanged(); }
    //void MovePrev() { if (View.Level.Start.Value > 0) View.Level = new(View.Level.Start.Value - 1, View.Level.End); OnSourceChanged(); }
    //void Reset() { Id = 0; View.Level = 0..; OnSourceChanged(); }
    //void Toggle() { View.ToggleValue = !View.ToggleValue; OnSourceChanged(); }

    #endregion
}

#endregion
