using OpenStack.Gfx;
using OpenStack.Sfx;
using System.Collections.Generic;
using System.Windows;

namespace OpenStack.Wpf.Control;

public abstract class UnityControl : ShellControl {
    #region Binding

    protected Renderer Renderer;
    protected abstract Renderer CreateRenderer();

    public static readonly DependencyProperty GfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(IList<IOpenGfx>), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty SfxProperty = DependencyProperty.Register(nameof(Sfx), typeof(IList<IOpenSfx>), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(object), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(string), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));

    public IList<IOpenGfx> Gfx {
        get => GetValue(GfxProperty) as IList<IOpenGfx>;
        set => SetValue(GfxProperty, value);
    }

    public IList<IOpenSfx> Sfx {
        get => GetValue(SfxProperty) as IList<IOpenSfx>;
        set => SetValue(SfxProperty, value);
    }

    public object Path {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public object Source {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string Type {
        get => GetValue(TypeProperty) as string;
        set => SetValue(TypeProperty, value);
    }

    void OnSourceChanged() {
        if (Gfx == null || Path == null || Source == null || Type == null) return;
        Renderer = CreateRenderer();
        Renderer?.Start();
    }

    #endregion
}
