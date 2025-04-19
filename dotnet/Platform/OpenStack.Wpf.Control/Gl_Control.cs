using OpenStack.Gfx;
using OpenStack.Gfx.Render;
using OpenStack.Gfx.Texture;
using OpenStack.Sfx;
using OpenTK.Input;
using System.Collections.Generic;
using System.Windows;
using Key = OpenTK.Input.Key;

namespace OpenStack.Wpf.Control;

#region OpenGLControl

public abstract class OpenGLControl : GLControl
{
    int Id = 0;

    #region Binding

    protected object Obj;
    protected EginRenderer Renderer;
    protected abstract Renderer CreateRenderer();

    public static readonly DependencyProperty GfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(IList<IOpenGfx>), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty SfxProperty = DependencyProperty.Register(nameof(Sfx), typeof(IList<IOpenSfx>), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(object), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(string), typeof(OpenGLControl), new PropertyMetadata((d, e) => (d as OpenGLControl).OnSourceChanged()));

    public IList<IOpenGfx> Gfx
    {
        get => GetValue(GfxProperty) as IList<IOpenGfx>;
        set => SetValue(GfxProperty, value);
    }

    public IList<IOpenSfx> Sfx
    {
        get => GetValue(SfxProperty) as IList<IOpenSfx>;
        set => SetValue(SfxProperty, value);
    }

    public object Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public object Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string Type
    {
        get => GetValue(TypeProperty) as string;
        set => SetValue(TypeProperty, value);
    }

    void OnSourceChanged()
    {
        if (Gfx == null || Path == null || Source == null || Type == null) return;
        Renderer = (EginRenderer)CreateRenderer();
        Renderer?.Start();
        if (Source is ITextureSelect z2) z2.Select(Id);
        //Camera.SetLocation(new Vector3(200));
        //Camera.LookAt(new Vector3(0));
    }

    #endregion

    #region Render

    protected override void SetViewport(int x, int y, int width, int height)
    {
        (int width, int height) p = Renderer?.GetViewport((width, height)) ?? (width, height);
        base.SetViewport(x, y, p.width, p.height);
    }

    protected override void Render(Camera camera, float frameTime)
        => Renderer?.Render(camera, default);

    public override void Tick(int? deltaTime = null)
    {
        base.Tick(deltaTime);
        Renderer?.Update(DeltaTime);
        Render(Camera, 0f);
    }

    #endregion

    #region HandleInput

    static readonly Key[] Keys = [Key.Q, Key.W, Key.A, Key.Z, Key.Escape, Key.Space, Key.Tilde];
    readonly HashSet<Key> KeyDowns = [];

    protected override void HandleInput(MouseState mouseState, KeyboardState keyboardState)
    {
        if (Renderer == null) return;
        foreach (var key in Keys)
            if (!KeyDowns.Contains(key) && keyboardState.IsKeyDown(key)) KeyDowns.Add(key);
        foreach (var key in KeyDowns)
            if (keyboardState.IsKeyUp(key))
            {
                KeyDowns.Remove(key);
                switch (key)
                {
                    case Key.W: Select(++Id); break;
                    case Key.Q: Select(--Id); break;
                    //case Key.A: MovePrev(); break;
                    //case Key.Z: MoveNext(); ; break;
                    //case Key.Escape: Reset(); break;
                    //case Key.Space: MoveReset(); break;
                    //case Key.Tilde: Toggle(); break;
                }
            }
    }

    void Select(int id)
    {
        if (Obj is ITextureSelect z2) z2.Select(id);
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
