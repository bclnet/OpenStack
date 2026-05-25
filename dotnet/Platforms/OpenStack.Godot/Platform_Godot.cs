using Godot;
using OpenStack.Client;
using OpenStack.Gfx;
using OpenStack.Gfx.Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static OpenStack.Gfx.TextureFormat;
using XShader = Godot.Shader;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region Client

public class GodotClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

// GodotObjectBuilder : MISSING
class GodotObjectModelBuilder : ObjectModelBuilderBase<Node, Material, Texture2D> {
    Node _prefabObj;

    public override Node InstanceObject(Node src, Node parent) {
        //var s = UnityEngine.Object.Instantiate(src);
        //if (parent != null) s.transform.parent = parent.transform;
        //return s;
        throw new NotImplementedException();
    }

    public async override Task<Node> CreateObject(ISource source, object path, bool isStatic, MaterialManager<Material, Texture2D> materialManager) {
        var builder = GodotX.BuildersByType[path.GetType()];
        var s = await builder(source, path, isStatic, materialManager);
        s.Reparent(_prefabObj);
        //s.transform.parent = _prefabObj.transform;
        // Add LOD support to the prefab.
        //var lod = s.AddComponent<LODGroup>();
        //lod.SetLODs([new(0.015f, s.GetComponentsInChildren<UnityEngine.Renderer>())]);
        return s;
    }

    public override void EnsurePrefab() {
        if (_prefabObj != null) return;
        _prefabObj = new Node3D { Name = "_Prefabs" };
        //_prefabObj.Visible = false;
    }
}

// GodotShaderBuilder : MISSING

// GodotTextureBuilder
class GodotTextureBuilder : TextureBuilderBase<Texture2D> {
    Texture2D _defaultTexture;
    public override Texture2D DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release() {
        if (_defaultTexture != null) { /*DeleteTexture(_defaultTexture);*/ _defaultTexture = null; }
    }

    Texture2D CreateDefaultTexture() => CreateSolidTexture(4, 4, [
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,

        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
    ]);

    public override Texture2D CreateNormalMapTexture(Texture2D src, float strength) => throw new NotImplementedException();
    public override Texture2D CreateSolidTexture(int width, int height, float[] rgbas) {
        var img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(new Color(rgbas[0], rgbas[1], rgbas[2], rgbas[3]));
        var tex = ImageTexture.CreateFromImage(img);
        return tex;
    }
    // https://docs.godotengine.org/en/stable/classes/class_image.html
    public override Texture2D CreateTexture(Texture2D reuse, ITexture src, System.Range? range = null) => src.Create("GD", x => {
        switch (x) {
            case Texture_Bytes t:
                if (t.Bytes == null) return DefaultTexture;
                else if (t.Format is ValueTuple<TextureFormat, TexturePixel> z) {
                    var (format, pixel) = z;
                    bool s = (pixel & TexturePixel.Signed) != 0, f = (pixel & TexturePixel.Float) != 0;
                    var imageFormat = format switch {
                        DXT1 => Image.Format.Dxt1,
                        //DXT1A => default,
                        DXT3 => Image.Format.Dxt3,
                        DXT5 => Image.Format.Dxt5,
                        BC4 => Image.Format.RgtcR,
                        BC5 => Image.Format.RgtcRg,
                        //BC6H => default,
                        //BC7 => default,
                        //ETC2 => default,
                        //ETC2_EAC => default,
                        //I8 => default,
                        L8 => Image.Format.L8,
                        R8 => Image.Format.R8,
                        R16 => f ? Image.Format.Rh : s ? Image.Format.R16 : Image.Format.R16I,
                        RG16 => f ? Image.Format.Rgf : s ? Image.Format.Rg16 : Image.Format.Rg16I,
                        RGB24 => f ? Image.Format.Rgbf : s ? Image.Format.Rgb8 : Image.Format.Rgb8,
                        RGB565 => Image.Format.Rgb565,
                        RGBA32 => f ? Image.Format.Rgbaf : s ? Image.Format.Rgba8 : Image.Format.Rgba8,
                        //ARGB32 => default,
                        BGRA32 => Image.Format.Rgbaf,
                        //BGRA1555 => default,
                        _ => throw new ArgumentOutOfRangeException("format", $"{format}")
                    };
                    var img = Image.CreateFromData(src.Width, src.Height, src.MipMaps > 1, imageFormat, t.Bytes);
                    Log.Info("Pre Save");
                    var err = img.SavePng("user://Test.png");
                    Log.Info($"Post Save {err}");
                    return null;
                    //var tex = ImageTexture.CreateFromImage(img) ?? throw new Exception($"Unable to create texture: {img}"); tex.ResourceName = "tex";
                    //return tex;
                }
                else throw new ArgumentOutOfRangeException(nameof(t.Format), $"{t.Format}");
            default: throw new ArgumentOutOfRangeException(nameof(x), $"{x}");
        }
    });
    public override void DeleteTexture(Texture2D src) => src.TakeOverPath(null);
}

class GodotMaterialBuilder(TextureManager<Texture2D> textureManager) : MaterialBuilderBase<Material, Texture2D>(textureManager) {
    Material _defaultMaterial;
    public override Material DefaultMaterial => _defaultMaterial ??= new StandardMaterial3D();

    Material _terrainMaterial;
    public override Material TerrainMaterial => _terrainMaterial ??= new StandardMaterial3D();

    public override async Task<Material> CreateMaterial(ISource source, object path) {
        switch (path) {
            case MaterialStdProp p: {
                    var m = new StandardMaterial3D();
                    //m.AlbedoColor = new Color(1, 0, 0);
                    //m.Roughness = 0.5f;

                    //if (p.AlphaBlended) m.SetFloat(Cutoff, 0.5f);
                    //else if (p.AlphaTest) m.EnableKeyword("_ALPHATEST_ON");
                    var mainTex = p.Textures.TryGetValue("Main", out var z) ? z : default;
                    if (mainTex != null) {
                        m.AlbedoTexture = (await TextureManager.CreateTexture(source, mainTex)).tex;
                        var bumpTex = p.Textures.TryGetValue("Bump", out z) ? z : default;
                        if (bumpTex != null)
                            m.NormalTexture = (await TextureManager.CreateTexture(source, bumpTex)).tex;
                    }
                    return m;
                }
            default: throw new ArgumentOutOfRangeException(nameof(path));
        }
    }
}

// GodotGfxApi
public class GodotGfxApi : IOpenGfxApi<Node3D, Material> {
    public void Parent(Node3D source, Node3D parent) => parent.AddChild(source);
    public void Transform(Node3D source, System.Numerics.Vector3 position, System.Numerics.Quaternion rotation, System.Numerics.Vector3 localScale) {
        var transform = new Transform3D { Origin = position.ToGodot() };
        source.Transform = transform;
    }
    public void Transform(Node3D source, System.Numerics.Vector3 position, System.Numerics.Matrix4x4 rotation, System.Numerics.Vector3 localScale) {
        var transform = new Transform3D { Origin = position.ToGodot() };
        source.Transform = transform;
    }
    public void AddMissingMeshCollidersRecursively(Node3D source, bool isStatic) => source.AddMissingMeshCollidersRecursively(isStatic);
    public void SetLayerRecursively(Node3D source, int layer) { }
    public object CreateMesh(object mesh) => throw new NotImplementedException();
    public Node3D CreateObject(string name, string tag = null, Node3D parent = null) => throw new NotImplementedException();
    public void AddMeshRenderer(Node3D source, object mesh, Material material, bool enabled, bool isStatic) {
        //source.AddComponent<MeshFilter>().mesh = (Mesh)mesh;
        //var meshRenderer = source.AddComponent<MeshRenderer>();
        //meshRenderer.material = material;
        //meshRenderer.enabled = enabled;
        //source.isStatic = isStatic;
    }
    public void AddSkinnedMeshRenderer(Node3D source, object mesh, Material material, bool enabled, bool isStatic) {
        //var skin = source.AddComponent<SkinnedMeshRenderer>();
        //skin.sharedMesh = (Mesh)mesh;
        //skin.bones = null;
        //skin.rootBone = null;
        //skin.sharedMaterial = material;
        //skin.enabled = enabled;
        //source.isStatic = isStatic;
    }
    public void AddMeshCollider(Node3D source, object mesh, bool isKinematic, bool isStatic) {
        //if (!isStatic)
        //{
        //    source.AddComponent<BoxCollider>();
        //    source.AddComponent<Rigidbody>().isKinematic = isKinematic;
        //}
        //else source.AddComponent<MeshCollider>().sharedMesh = (Mesh)mesh;
    }
    public void Attach(GfxAttach method, Node3D src, params object[] args) => throw new NotImplementedException();
    public void SetVisible(Node3D src, bool visible) => throw new NotImplementedException();
    public void Destroy(Node3D src) => throw new NotImplementedException();
}

// GodotGfxSprite2D
public class GodotGfxSprite2D : IOpenGfxSprite<Node, Sprite2D> {
    readonly SpriteManager<Sprite2D> _spriteManager;
    public GodotGfxSprite2D() {
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public SpriteManager<Sprite2D> SpriteManager => _spriteManager;
    public void PreloadSprite(ISource source, object path) => _spriteManager.PreloadSprite(source, path);
    public Task<(Sprite2D spr, object tag)> CreateSprite(ISource source, object path, Node parent = default) => _spriteManager.CreateSprite(source, path);
}

// GodotGfxSprite3D
public class GodotGfxSprite3D : IOpenGfxSprite<Node, Sprite3D> {
    readonly SpriteManager<Sprite3D> _spriteManager;
    public GodotGfxSprite3D() {
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public SpriteManager<Sprite3D> SpriteManager => _spriteManager;
    public void PreloadSprite(ISource source, object path) => _spriteManager.PreloadSprite(source, path);
    public Task<(Sprite3D spr, object tag)> CreateSprite(ISource source, object path, Node parent = default) => _spriteManager.CreateSprite(source, path);
}

// GodotGfxModel
public class GodotGfxModel : IOpenGfxModel<Node, Material, Texture2D, XShader> {
    readonly MaterialManager<Material, Texture2D> _materialManager;
    readonly ObjectModelManager<Node, Material, Texture2D> _objectManager;
    readonly ShaderManager<XShader> _shaderManager;
    readonly TextureManager<Texture2D> _textureManager;
    public GodotGfxModel() {
        _textureManager = new TextureManager<Texture2D>(new GodotTextureBuilder());
        _materialManager = new MaterialManager<Material, Texture2D>(_textureManager, new GodotMaterialBuilder(_textureManager));
        _objectManager = new ObjectModelManager<Node, Material, Texture2D>(_materialManager, new GodotObjectModelBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public MaterialManager<Material, Texture2D> MaterialManager => _materialManager;
    public ObjectModelManager<Node, Material, Texture2D> ObjectManager => _objectManager;
    public ShaderManager<XShader> ShaderManager => throw new NotImplementedException(); //_shaderManager;
    public TextureManager<Texture2D> TextureManager => _textureManager;
    public void PreloadObject(ISource source, object path) => _objectManager.PreloadObject(source, path);
    public void PreloadTexture(ISource source, object path) => _textureManager.PreloadTexture(source, path);
    public Task<(Node obj, object tag)> CreateObject(ISource source, object path, bool isStatic, Node parent = default) => throw new NotImplementedException();
    public Task<(XShader sha, object tag)> CreateShader(ISource source, object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(Texture2D tex, object tag)> CreateTexture(ISource source, object path, System.Range? level = null) => _textureManager.CreateTexture(source, path, level);
    public void PostObject(Node src, System.Numerics.Vector3 position, System.Numerics.Vector3 eulerAngles, float? scale, Node parent) => throw new NotImplementedException();
}

// GodotSfx
public class GodotSfx : SystemSfx { }

// GodotPlatform
public class GodotPlatform : Platform {
    public static readonly Platform This = new GodotPlatform();
    GodotPlatform() : base("GD", "Godot") {
        GfxFactory = () => [new GodotGfxApi(), new GodotGfxSprite2D(), new GodotGfxSprite3D(), new GodotGfxModel(), null, null];
        SfxFactory = () => [new GodotSfx()];
        LogFunc = a => GD.Print(a?.Replace("\r", ""));
    }
}

// GodotShellPlatform
public class GodotShellPlatform : Platform {
    public static readonly Platform This = new GodotShellPlatform();
    GodotShellPlatform() : base("GD", "Godot") { }
}

#endregion