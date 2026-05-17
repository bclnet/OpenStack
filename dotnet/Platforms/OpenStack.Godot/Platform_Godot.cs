using Godot;
using OpenStack.Client;
using OpenStack.Gfx;
using OpenStack.Gfx.Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

// GodotShaderBuilder : MISSING

// GodotTextureBuilder
class GodotTextureBuilder : TextureBuilderBase<Texture> {
    Texture _defaultTexture;
    public override Texture DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release() {
        if (_defaultTexture != null) { /*DeleteTexture(_defaultTexture);*/ _defaultTexture = null; }
    }

    Texture CreateDefaultTexture() => CreateSolidTexture(4, 4, [
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

    public override Texture CreateNormalMapTexture(Texture src, float strength) => throw new NotImplementedException();
    public override Texture CreateSolidTexture(int width, int height, float[] pixels) => null;
    public override Texture CreateTexture(Texture reuse, ITexture src, System.Range? level = null) => throw new NotImplementedException();
    public override void DeleteTexture(Texture src) { }
}

// GodotMaterialBuilder : MISSING

// GodotModelApi
public class GodotModelApi : IOpenGfxApi<Node3D, Material> {
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
public class GodotGfxModel : IOpenGfxModel<Node, Material, Texture, XShader> {
    readonly MaterialManager<Material, Texture> _materialManager;
    readonly ObjectModelManager<Node, Material, Texture> _objectManager;
    readonly ShaderManager<XShader> _shaderManager;
    readonly TextureManager<Texture> _textureManager;
    public GodotGfxModel() {
        _textureManager = new TextureManager<Texture>(new GodotTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public MaterialManager<Material, Texture> MaterialManager => _materialManager;
    public ObjectModelManager<Node, Material, Texture> ObjectManager => _objectManager;
    public ShaderManager<XShader> ShaderManager => _shaderManager;
    public TextureManager<Texture> TextureManager => _textureManager;
    public void PreloadObject(ISource source, object path) => _objectManager.PreloadObject(source, path);
    public void PreloadTexture(ISource source, object path) => _textureManager.PreloadTexture(source, path);
    public Task<(Node obj, object tag)> CreateObject(ISource source, object path, bool isStatic, Node parent = default) => throw new NotImplementedException();
    public Task<(XShader sha, object tag)> CreateShader(ISource source, object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(Texture tex, object tag)> CreateTexture(ISource source, object path, System.Range? level = null) => _textureManager.CreateTexture(source, path, level);
    public void PostObject(Node src, System.Numerics.Vector3 position, System.Numerics.Vector3 eulerAngles, float? scale, Node parent) => throw new NotImplementedException();
}

// GodotSfx
public class GodotSfx : SystemSfx { }

// GodotPlatform
public class GodotPlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new GodotPlatform();
    GodotPlatform() : base("GD", "Godot") {
        GfxFactory = () => [null, new GodotGfxSprite2D(), new GodotGfxSprite3D(), new GodotGfxModel(), null, null];
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