using System;
using Stride.Core.Presentation.Controls;
using Stride.Editor.Engine;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering.ProceduralModels;
using System.Windows.Controls;
using System.Windows;
using OpenStack.Gfx;
using System.Threading;
using Stride.CommunityToolkit.Engine;
using OpenStack.Sfx;
using System.Collections.Generic;

namespace GameX.App.Explorer.Controls;

public abstract class StrideControl : UserControl {
    #region Embedding

    readonly TaskCompletionSource<bool> GameStartedTaskSource = new();
    Thread GameThread;
    IntPtr WindowHandle;

    public StrideControl() {
        GameThread = new Thread(SafeAction.Wrap(GameRunThread)) {
            IsBackground = true,
            Name = "Game Thread"
        };
        GameThread.SetApartmentState(ApartmentState.STA);
        Loaded += async (sender, args) => await StartGame();
    }

    async Task StartGame() {
        GameThread.Start();
        await GameStartedTaskSource.Task;
        Content = new GameEngineHost(WindowHandle);
    }

    void GameRunThread() {
        // Create the form from this thread
        var form = new EmbeddedGameForm() { TopLevel = false, Visible = false };
        WindowHandle = form.Handle;
        var context = new GameContextWinforms(form);
        GameStartedTaskSource.SetResult(true);
        var game = new Game();
        game.Run(context, (Scene scene) => {
            game.Window.IsBorderLess = true;
            game.SetupBase();
            var entity1 = new Entity("Name", new Vector3(1f, 0.5f, 3f))
                {
                    new ModelComponent(new CubeProceduralModel().Generate(game.Services))
                };
            scene.Entities.Add(entity1);
        });
    }

    #endregion

    #region Binding

    protected Renderer Renderer;
    protected abstract Renderer CreateRenderer();

    public static readonly DependencyProperty GfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(IList<IOpenGfx>), typeof(StrideControl), new PropertyMetadata((d, e) => (d as StrideControl).OnSourceChanged()));
    public static readonly DependencyProperty SfxProperty = DependencyProperty.Register(nameof(Gfx), typeof(IList<IOpenSfx>), typeof(StrideControl), new PropertyMetadata((d, e) => (d as StrideControl).OnSourceChanged()));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Gfx), typeof(object), typeof(StrideControl), new PropertyMetadata((d, e) => (d as StrideControl).OnSourceChanged()));
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(StrideControl), new PropertyMetadata((d, e) => (d as StrideControl).OnSourceChanged()));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(string), typeof(StrideControl), new PropertyMetadata((d, e) => (d as StrideControl).OnSourceChanged()));

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
        Renderer = CreateRenderer(); //this, Gfx3d as IStrideGfx3d, Source, Type);
        Renderer?.Start();
    }

    #endregion

    #region BS

    //    void Start(Scene rootScene)
    //    {
    //        game.SetupBase3DScene();
    //        game.AddSkybox();

    //        AddMesh(game.GraphicsDevice, rootScene, Vector3.Zero, GiveMeATriangle);
    //        AddMesh(game.GraphicsDevice, rootScene, Vector3.UnitX * 2, GiveMeAPlane);
    //    }

    //    void Update(Scene rootScene, GameTime gameTime)
    //    {
    //        var segments = (int)((Math.Cos(gameTime.Total.TotalMilliseconds / 500) + 1) / 2 * 47) + 3;
    //        circleEntity?.Remove();
    //        circleEntity = AddMesh(game.GraphicsDevice, rootScene, Vector3.UnitX * -2, b => GiveMeACircle(b, segments));
    //    }

    //    void GiveMeATriangle(MeshBuilder meshBuilder)
    //    {
    //        meshBuilder.WithIndexType(IndexingType.Int16);
    //        meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    //        var position = meshBuilder.WithPosition<Vector3>();
    //        var color = meshBuilder.WithColor<Color>();

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(0, 0, 0));
    //        meshBuilder.SetElement(color, Color.Red);

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(1, 0, 0));
    //        meshBuilder.SetElement(color, Color.Green);

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(.5f, 1, 0));
    //        meshBuilder.SetElement(color, Color.Blue);

    //        meshBuilder.AddIndex(0);
    //        meshBuilder.AddIndex(2);
    //        meshBuilder.AddIndex(1);
    //    }

    //    void GiveMeAPlane(MeshBuilder meshBuilder)
    //    {
    //        meshBuilder.WithIndexType(IndexingType.Int16);
    //        meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    //        var position = meshBuilder.WithPosition<Vector3>();
    //        var color = meshBuilder.WithColor<Color>();

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(0, 0, 0));
    //        meshBuilder.SetElement(color, Color.Red);

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(0, 1, 0));
    //        meshBuilder.SetElement(color, Color.Green);

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(1, 1, 0));
    //        meshBuilder.SetElement(color, Color.Blue);

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(1, 0, 0));
    //        meshBuilder.SetElement(color, Color.Yellow);

    //        meshBuilder.AddIndex(0);
    //        meshBuilder.AddIndex(1);
    //        meshBuilder.AddIndex(2);

    //        meshBuilder.AddIndex(0);
    //        meshBuilder.AddIndex(2);
    //        meshBuilder.AddIndex(3);
    //    }

    //    void GiveMeACircle(MeshBuilder meshBuilder, int segments)
    //    {
    //        meshBuilder.WithIndexType(IndexingType.Int16);
    //        meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    //        var position = meshBuilder.WithPosition<Vector3>();
    //        var color = meshBuilder.WithColor<Color4>();

    //        for (var i = 0; i < segments; i++)
    //        {
    //            var x = (float)Math.Sin(Math.Tau / segments * i) / 2;
    //            var y = (float)Math.Cos(Math.Tau / segments * i) / 2;
    //            var hsl = new ColorHSV(360f / segments * i, 1, 1, 1).ToColor();

    //            meshBuilder.AddVertex();
    //            meshBuilder.SetElement(position, new Vector3(x + .5f, y + .5f, 0));
    //            meshBuilder.SetElement(color, hsl);
    //        }

    //        meshBuilder.AddVertex();
    //        meshBuilder.SetElement(position, new Vector3(.5f, .5f, 0));
    //        meshBuilder.SetElement(color, Color.Black.ToColor4());

    //        for (var i = 0; i < segments; i++)
    //        {
    //            meshBuilder.AddIndex(segments);
    //            meshBuilder.AddIndex(i);
    //            meshBuilder.AddIndex((i + 1) % segments);
    //        }
    //    }

    //    Entity AddMesh(GraphicsDevice graphicsDevice, Scene rootScene, Vector3 position, Action<MeshBuilder> build)
    //    {
    //        using var meshBuilder = new MeshBuilder();
    //        build(meshBuilder);

    //        var entity = new Entity { Scene = rootScene, Transform = { Position = position } };
    //        var model = new Model
    //{
    //    new MaterialInstance {
    //        Material = Material.New(graphicsDevice, new MaterialDescriptor {
    //            Attributes = new MaterialAttributes {
    //                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
    //                Diffuse = new MaterialDiffuseMapFeature {
    //                    DiffuseMap = new ComputeVertexStreamColor()
    //                },
    //            }
    //        })
    //    },
    //    new Mesh {
    //        Draw = meshBuilder.ToMeshDraw(graphicsDevice),
    //        MaterialIndex = 0
    //    }
    //};
    //        entity.Add(new ModelComponent { Model = model });
    //        return entity;
    //    }

    #endregion
}

//{
//    // Create an entity and add it to the scene.
//    var entity = new Entity();
//    scene.Entities.Add(entity);
//    // Create a model and assign it to the model component.
//    var model = new Model();
//    entity.GetOrCreate<ModelComponent>().Model = model;
//    // Add one or more meshes using geometric primitives (eg spheres or cubes).
//    var meshDraw = GeometricPrimitive.Sphere.New(game.GraphicsDevice).ToMeshDraw();
//    var mesh = new Mesh { Draw = meshDraw };
//    model.Meshes.Add(mesh);
//}

//{
//    var entity = new Entity();
//    scene.Entities.Add(entity);
//    var model = new Model();
//    entity.GetOrCreate<ModelComponent>().Model = model;
//    var vertices = new VertexPositionTexture[3];
//    vertices[0].Position = new Vector3(0f, 0f, 1f);
//    vertices[1].Position = new Vector3(0f, 1f, 0f);
//    vertices[2].Position = new Vector3(0f, 1f, 1f);
//    var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(game.GraphicsDevice, vertices, GraphicsResourceUsage.Dynamic);
//    int[] indices = { 0, 2, 1 };
//    var indexBuffer = Stride.Graphics.Buffer.Index.New(game.GraphicsDevice, indices);

//    var customMesh = new Mesh
//    {
//        Draw = new MeshDraw
//        {
//            /* Vertex buffer and index buffer setup */
//            PrimitiveType = PrimitiveType.TriangleList,
//            DrawCount = indices.Length,
//            IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
//            VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexPositionTexture.Layout, vertexBuffer.ElementCount)],
//        }
//    };
//    // add the mesh to the model
//    model.Meshes.Add(customMesh);
//}