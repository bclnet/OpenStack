using OpenStack.Gfx;
using OpenStack.Gfx.Gl;
using OpenStack.Gfx.Renders;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GameX.App.Explorer.Controls2
{
    public class GL2MapViewer : GLViewerControl
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static readonly DependencyProperty GfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(object), typeof(GL2ParticleViewer),
            new PropertyMetadata((d, e) => (d as GL2MapViewer).OnProperty()));
        public IOpenGfx Gfx
        {
            get => GetValue(GfxProperty) as IOpenGfx;
            set => SetValue(GfxProperty, value);
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(GL2ParticleViewer),
            new PropertyMetadata((d, e) => (d as GL2MapViewer).OnProperty()));
        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        void OnProperty()
        {
            if (Gfx == null || Source == null) return;
            var gfx = Gfx as IOpenGLGfx;
            var source = Source is object z ? z
                : Source is IRedirected<object> y ? y.Value
                : null;
            if (source == null) return;

            Camera.SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight);
            Camera.SetLocation(new Vector3(200));
            Camera.LookAt(new Vector3(0));
        }

        protected override void Render(Camera camera, float frameTime) { }
    }
}
