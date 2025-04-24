using Godot;
using static OpenStack.Debug;
using XTexture = Godot.Texture;

namespace OpenStack.Gfx.Godot;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly GodotGfxModel Gfx;

    public TestTriRenderer(Node parent, GodotGfxModel gfx, object obj) {
        Gfx = gfx;
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly Node Parent;
    readonly GodotGfxModel Gfx;
    readonly object Obj;
    readonly System.Range Level;
    readonly XTexture Texture;
    int FrameDelay;

    public TextureRenderer(Node parent, GodotGfxModel gfx, object obj, System.Range level) {
        Parent = parent;
        Gfx = gfx;
        Obj = obj;
        Level = level;
        Gfx.TextureManager.DeleteTexture(obj);
        Texture = Gfx.TextureManager.CreateTexture(obj, level).tex;
    }

    public override void Start() {
        var path = Obj is string z ? z : null;
        if (string.IsNullOrEmpty(path)) return;

        Log($"MakeTexture {path}");

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
        Log($"Done {obj}");
    }

    public override void Update(float deltaTime) {
        if (Obj is not ITextureFrames obj || Gfx == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        Gfx.TextureManager.ReloadTexture(obj, Level);
    }
}

#endregion