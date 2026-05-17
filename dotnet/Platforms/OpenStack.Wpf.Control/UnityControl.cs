using OpenStack.Gfx;
using System;
using System.Windows;

namespace OpenStack.Wpf.Control;

public abstract class UnityControl(Func<object, object, object, string, object> shellState) : ShellControl {
    #region Binding

    protected Renderer Renderer;
    protected abstract Renderer CreateRenderer();
    protected override object GetShellState() => shellState(Source, Path, Value, Type);

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ISource), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty Source2Property = DependencyProperty.Register(nameof(Source2), typeof(object), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(object), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(string), typeof(UnityControl), new PropertyMetadata((d, e) => (d as UnityControl).OnSourceChanged()));

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
        Renderer = CreateRenderer();
        Renderer?.Start();
    }

    #endregion
}
