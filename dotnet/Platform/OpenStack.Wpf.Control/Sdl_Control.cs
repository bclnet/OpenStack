using OpenStack.Gfx;
using System.Windows;
using System.Windows.Controls;

namespace OpenStack.Wpf.Control;

public abstract class SdlControl : UserControl
{
    #region Binding

    protected object Obj;
    protected Renderer Renderer;
    protected abstract Renderer CreateRenderer();

    public static readonly DependencyProperty GfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(IOpenGfx), typeof(SdlControl), new PropertyMetadata((d, e) => (d as SdlControl).OnSourceChanged()));
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(SdlControl), new PropertyMetadata((d, e) => (d as SdlControl).OnSourceChanged()));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(string), typeof(SdlControl), new PropertyMetadata((d, e) => (d as SdlControl).OnSourceChanged()));

    public IOpenGfx Gfx
    {
        get => GetValue(GfxProperty) as IOpenGfx;
        set => SetValue(GfxProperty, value);
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
        if (Gfx == null || Source == null || Type == null) return;
        Renderer = CreateRenderer();
        Renderer?.Start();
    }

    #endregion
}
