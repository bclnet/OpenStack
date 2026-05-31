using Godot;
using System.Collections.Generic;

namespace OpenStack.Gfx.Godot;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly GodotGfxModel GfxModel;

    public TestTriRenderer(Node parent, IOpenGfx[] gfx, ISource source, object obj) {
        GfxModel = (GodotGfxModel)gfx[GfX.XModel];
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly Node Parent;
    readonly GodotGfxModel GfxModel;
    readonly ISource Source;
    readonly object Obj;
    int FrameDelay;

    public TextureRenderer(Node parent, IOpenGfx[] gfx, ISource source, object obj, System.Range level) {
        Parent = parent;
        GfxModel = (GodotGfxModel)gfx[GfX.XModel];
        Source = source;
        Obj = obj;
    }

    public override void Start() {
        var obj = new MeshInstance3D {
            Name = "Texture",
            Mesh = new PlaneMesh { Size = new Vector2(12f, 12f) },
            //RotationDegrees = new Vector3(-90f, -180f, 180f)
        };
        var (material, _) = GfxModel.MaterialManager.CreateMaterial(Source, new MaterialStdProp { Textures = new Dictionary<string, object> { ["Main"] = Obj } }).Result;
        obj.SetSurfaceOverrideMaterial(0, material);
        Parent.AddChild(obj);
    }

    public override void Update(float deltaTime) {
        if (Obj is not ITextureFrames obj || GfxModel == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        //GfxModel.TextureManager.ReloadTexture(Source, obj, Level);
    }
}

#endregion

#region ObjectRenderer

public class ObjectRenderer(object parent, IOpenGfx[] gfx, ISource source, object obj) : Renderer {
    readonly GodotGfxModel GfxModel = (GodotGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        GfxModel.ObjectManager.CreateObject(source, Obj, true, null).Wait();
    }
}

#endregion

#region EngineRenderer

//public class EngineRenderer(IOpenGfx[] gfx, ISource source, object obj) : Renderer {
//    GodotOpenEngine Engine;
//    //GameObject PlayerPrefab = GameObject.Find("Player0");

//    public override void Dispose() { base.Dispose(); Engine?.Dispose(); }

//    public override void Start() {
//        //Log.Info($"PlayerPrefab: {PlayerPrefab}");
//        var db = (ICellDatabase)obj;
//        Engine = new GodotOpenEngine(queue => new CellManager(db.Query, queue, new GodotCellBuilder(db.Archive, db.Query, gfx)), false);
//        Engine.SpawnPlayer(db).Wait();
//    }

//    public override void Update(float deltaTime) => Engine?.Update().Wait();
//}

#endregion



/*
    //var material = Parent
    var surfaceTool = new SurfaceTool();
    surfaceTool.Begin(Mesh.PrimitiveType.TriangleStrip);
    //surfaceTool.SetSmoothGroup(-1);
    //        var st = SurfaceTool.new()
    //st.begin(Mesh.PRIMITIVE_TRIANGLES)

    //# Prepare attributes for add_vertex.
    //st.add_normal(Vector3(0, 0, 1))
    //st.add_uv(Vector2(0, 0))
    //# Call last for each vertex, adds the above attributes.
    //st.add_vertex(Vector3(-1, -1, 0))

    //st.add_normal(Vector3(0, 0, 1))
    //st.add_uv(Vector2(0, 1))
    //st.add_vertex(Vector3(-1, 1, 0))

    //st.add_normal(Vector3(0, 0, 1))
    //st.add_uv(Vector2(1, 1))
    //st.add_vertex(Vector3(1, 1, 0))

    //# Create indices, indices are optional.
    //st.index()

    //# Commit to a mesh.
    //var mesh = st.commit()

    Vector3[] vertices = [
        new Vector3(-1f, -1f, +0f),
        new Vector3(-1f, +1f, +0f),
        new Vector3(+1f, -1f, +0f),
        new Vector3(+1f, +1f, +0f)
    ];
    foreach (var v in vertices) surfaceTool.AddVertex(v);
    //surfaceTool.GenerateNormals();
    surfaceTool.Index();
    //surfaceTool.SetMaterial(material);
    var mesh = surfaceTool.Commit();
    //var mesh = new ArrayMesh();
    //mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles)
    var obj = new MeshInstance3D {
        Name = "Texture",
        Mesh = mesh,
    };
    //obj.Transform.Rotated(new Vector3(-90f, 180f, -180f), 0f);
    //obj.AddChild
    //(meshRenderer.material, _) = Gfx.MaterialManager.CreateMaterial(new FixedMaterialInfo { MainFilePath = path });
    Parent.AddChild(obj);
    Log.Info($"Done {obj}");
*/