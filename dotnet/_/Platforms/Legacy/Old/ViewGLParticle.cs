using OpenStack.Gfx;
using OpenStack.Gfx.Gl;
using OpenStack.Gfx.Gl.Renders;
using OpenStack.Gfx.Renders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GameX.App.Explorer.Controls
{
    public class ViewGLParticle : GLViewerControl
    {
        ParticleGridRenderer particleGrid;
       
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static readonly DependencyProperty GfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(object), typeof(ViewGLParticle),
            new PropertyMetadata((d, e) => (d as ViewGLParticle).OnProperty()));

        public IOpenGfx Gfx
        {
            get => GetValue(GfxProperty) as IOpenGfx;
            set => SetValue(GfxProperty, value);
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(ViewGLParticle),
            new PropertyMetadata((d, e) => (d as ViewGLParticle).OnProperty()));

        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        HashSet<ParticleRenderer> Renderers { get; } = [];

        void OnProperty()
        {
            if (Gfx == null || Source == null) return;
            var gfx = Gfx as IOpenGLGfx;
            var source = Source is IParticleSystem z ? z
                : Source is IRedirected<IParticleSystem> y ? y.Value
                : null;
            if (source == null) return;

            particleGrid = new ParticleGridRenderer(gfx, 20, 5);
            Camera.SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight);
            Camera.SetLocation(new Vector3(200));
            Camera.LookAt(new Vector3(0));

            Renderers.Add(new ParticleRenderer(gfx, source));
        }

        protected override void Render(Camera camera, float deltaTime)
        {
            particleGrid?.Render(Camera, RenderPass.Both);
            foreach (var renderer in Renderers)
            {
                renderer.Update(deltaTime);
                renderer.Render(Camera, RenderPass.Both);
            }
        }
    }
}
