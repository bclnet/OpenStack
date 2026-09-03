using OpenStack.Gfx;
using OpenStack.Gfx.Gl;
using OpenStack.Gfx.Gl.Renders;
using OpenStack.Gfx.Gl.Scenes;
using OpenStack.Gfx.Renders;
using OpenStack.Gfx.Scenes;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GameX.App.Explorer.Controls
{
    public abstract class ViewGLScene : GLViewerControl
    {
        public Scene Scene { get; private set; }
        public Scene SkyboxScene { get; protected set; }

        public bool ShowBaseGrid { get; set; } = true;
        public bool ShowSkybox { get; set; } = true;

        protected float SkyboxScale { get; set; } = 1.0f;
        protected Vector3 SkyboxOrigin { get; set; } = Vector3.Zero;

        bool ShowStaticOctree = false;
        bool ShowDynamicOctree = false;
        Frustum CullFrustum;

        //ComboBox _renderModeComboBox;
        ParticleGridRenderer BaseGrid;
        Camera SkyboxCamera = new GLDebugCamera();
        OctreeDebugRenderer<SceneNode> StaticOctreeRenderer;
        OctreeDebugRenderer<SceneNode> DynamicOctreeRenderer;

        protected ViewGLScene(Frustum cullFrustum = null)
        {
            CullFrustum = cullFrustum;
            InitializeControl();
            //AddCheckBox("Show Grid", ShowBaseGrid, (v) => ShowBaseGrid = v);
            //AddCheckBox("Show Static Octree", _showStaticOctree, (v) => _showStaticOctree = v);
            //AddCheckBox("Show Dynamic Octree", _showDynamicOctree, (v) => _showDynamicOctree = v);
            //AddCheckBox("Lock Cull Frustum", false, (v) => { _lockedCullFrustum = v ? Scene.MainCamera.ViewFrustum.Clone() : null; });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static readonly DependencyProperty GraphicProperty = DependencyProperty.Register(nameof(Gfx), typeof(object), typeof(ViewGLScene),
            new PropertyMetadata((d, e) => (d as ViewGLScene).OnProperty()));

        public IOpenGfx Gfx
        {
            get => GetValue(GraphicProperty) as IOpenGfx;
            set => SetValue(GraphicProperty, value);
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(ViewGLScene),
            new PropertyMetadata((d, e) => (d as ViewGLScene).OnProperty()));

        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        void OnProperty()
        {
            if (Gfx == null || Source == null) return;

            var gfx = Gfx as IOpenGLGfx;

            Scene = new Scene(gfx, MeshBatchRenderer.Render);
            BaseGrid = new ParticleGridRenderer(gfx, 20, 5);

            Camera.SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight); //: HandleResize()
            Camera.SetLocation(new Vector3(256));
            Camera.LookAt(new Vector3(0));

            LoadScene(Source);

            if (Scene.AllNodes.Any())
            {
                var bbox = Scene.AllNodes.First().BoundingBox;
                var location = new Vector3(bbox.Max.Z, 0, bbox.Max.Z) * 1.5f;

                Camera.SetLocation(location);
                Camera.LookAt(bbox.Center);
            }

            StaticOctreeRenderer = new OctreeDebugRenderer<SceneNode>(Scene.StaticOctree, Gfx as IOpenGLGfx, false);
            DynamicOctreeRenderer = new OctreeDebugRenderer<SceneNode>(Scene.DynamicOctree, Gfx as IOpenGLGfx, true);

            //if (_renderModeComboBox != null)
            //{
            //    var supportedRenderModes = Scene.AllNodes
            //        .SelectMany(r => r.GetSupportedRenderModes())
            //        .Distinct();
            //    SetAvailableRenderModes(supportedRenderModes);
            //}
        }

        protected abstract void InitializeControl();

        protected abstract void LoadScene(object source);

        protected override void Render(Camera camera, float frameTime)
        {
            Scene.MainCamera = camera;
            Scene.Update(frameTime);

            if (ShowBaseGrid) BaseGrid.Render(camera, RenderPass.Both);

            if (ShowSkybox && SkyboxScene != null)
            {
                SkyboxCamera.CopyFrom(camera);
                SkyboxCamera.SetLocation(camera.Location - SkyboxOrigin);
                SkyboxCamera.SetScale(SkyboxScale);

                SkyboxScene.MainCamera = SkyboxCamera;
                SkyboxScene.Update(frameTime);
                SkyboxScene.RenderWithCamera(SkyboxCamera);

                GL.Clear(ClearBufferMask.DepthBufferBit);
            }

            Scene.RenderWithCamera(camera, CullFrustum);

            if (ShowStaticOctree) StaticOctreeRenderer.Render(camera, RenderPass.Both);
            if (ShowDynamicOctree) DynamicOctreeRenderer.Render(camera, RenderPass.Both);
        }

        protected void SetEnabledLayers(HashSet<string> layers)
        {
            Scene.SetEnabledLayers(layers);
            StaticOctreeRenderer = new OctreeDebugRenderer<SceneNode>(Scene.StaticOctree, Gfx as IOpenGLGfx, false);
        }

        //protected void AddRenderModeSelectionControl()
        //{
        //    if (_renderModeComboBox == null)
        //        _renderModeComboBox = AddSelection("Render Mode", (renderMode, _) =>
        //        {
        //            foreach (var node in Scene.AllNodes)
        //                node.SetRenderMode(renderMode);

        //            if (SkyboxScene != null)
        //                foreach (var node in SkyboxScene.AllNodes)
        //                    node.SetRenderMode(renderMode);
        //        });
        //}

        //void SetAvailableRenderModes(IEnumerable<string> renderModes)
        //{
        //    _renderModeComboBox.Items.Clear();
        //    if (renderModes.Any())
        //    {
        //        _renderModeComboBox.Enabled = true;
        //        _renderModeComboBox.Items.Add("Default Render Mode");
        //        _renderModeComboBox.Items.AddRange(renderModes.ToArray());
        //        _renderModeComboBox.SelectedIndex = 0;
        //    }
        //    else
        //    {
        //        _renderModeComboBox.Items.Add("(no render modes available)");
        //        _renderModeComboBox.SelectedIndex = 0;
        //        _renderModeComboBox.Enabled = false;
        //    }
        //}
    }
}
