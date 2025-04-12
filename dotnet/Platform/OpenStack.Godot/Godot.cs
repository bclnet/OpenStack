using Godot;
using OpenStack.Gfx;
using OpenStack.Gfx.Texture;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using XShader = Godot.Shader;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Godot;

// GodotExtensions
public static class GodotExtensions {
    /// <summary>
    /// ToGodot
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Vector3 ToGodot(this System.Numerics.Vector3 source) => new(source.X, source.Z, source.Y);
    /// <summary>
    /// ToGodot
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToGodot(this System.Numerics.Quaternion source) => new(source.X, source.Y, source.Z, source.W);

    /// <summary>
    /// Adds mesh colliders to every descandant object with a mesh filter but no mesh collider, including the object itself.
    /// </summary>
    public static void AddMissingMeshCollidersRecursively(this Node3D source, bool isStatic = true)
    {
        if (!isStatic) return;
    }
}

// GodotObjectBuilder : MISSING

// GodotShaderBuilder : MISSING

// GodotTextureBuilder
public class GodotTextureBuilder : TextureBuilderBase<Texture>
{
    Texture _defaultTexture;
    public override Texture DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release()
    {
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

    public override Texture CreateTexture(Texture reuse, ITexture source, System.Range? level = null)
    {
        throw new NotImplementedException();
    }

    public override Texture CreateSolidTexture(int width, int height, float[] pixels)
    {
        return null;
    }

    public override Texture CreateNormalMap(Texture texture, float strength)
    {
        throw new NotImplementedException();
    }

    public override void DeleteTexture(Texture texture) { }
}

// GodotMaterialBuilder : MISSING


// GodotModelApi
public class GodotModelApi : ModelApi<Node3D, Material>
{
    public Node3D CreateObject(string name) => default;
    public void SetParent(Node3D source, Node3D parent) => parent.AddChild(source);
    public void Transform(Node3D source, System.Numerics.Vector3 position, System.Numerics.Quaternion rotation, System.Numerics.Vector3 localScale)
    {
        var transform = new Transform3D { Origin = position.ToGodot() };
        source.Transform = transform;
    }
    public void Transform(Node3D source, System.Numerics.Vector3 position, System.Numerics.Matrix4x4 rotation, System.Numerics.Vector3 localScale)
    {
        var transform = new Transform3D { Origin = position.ToGodot() };
        source.Transform = transform;
    }
    public void AddMissingMeshCollidersRecursively(Node3D source, bool isStatic) => source.AddMissingMeshCollidersRecursively(isStatic);
    public void SetLayerRecursively(Node3D source, int layer) { }

    public object CreateMesh(object mesh)
    {
        throw new NotImplementedException();
    }

    public void AddMeshRenderer(Node3D source, object mesh, Material material, bool enabled, bool isStatic)
    {
        //source.AddComponent<MeshFilter>().mesh = (Mesh)mesh;
        //var meshRenderer = source.AddComponent<MeshRenderer>();
        //meshRenderer.material = material;
        //meshRenderer.enabled = enabled;
        //source.isStatic = isStatic;
    }

    public void AddSkinnedMeshRenderer(Node3D source, object mesh, Material material, bool enabled, bool isStatic)
    {
        //var skin = source.AddComponent<SkinnedMeshRenderer>();
        //skin.sharedMesh = (Mesh)mesh;
        //skin.bones = null;
        //skin.rootBone = null;
        //skin.sharedMaterial = material;
        //skin.enabled = enabled;
        //source.isStatic = isStatic;
    }

    public void AddMeshCollider(Node3D source, object mesh, bool isKinematic, bool isStatic)
    {
        //if (!isStatic)
        //{
        //    source.AddComponent<BoxCollider>();
        //    source.AddComponent<Rigidbody>().isKinematic = isKinematic;
        //}
        //else source.AddComponent<MeshCollider>().sharedMesh = (Mesh)mesh;
    }
}

//public interface IGodotGfx2dSprite : IOpenGfx2dSpriteAny<Node, Sprite2D> { }
//public interface IGodotGfx3dModel : IOpenGfx3dModelAny<Node, Material, Texture, XShader> { }

// GodotGfx2dSprite
public class GodotGfx2dSprite : IOpenGfx2dSprite<Node, Sprite2D>
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite2D> _spriteManager;
    readonly ObjectSpriteManager<Node, Sprite2D> _objectManager;

    public GodotGfx2dSprite(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new Object2dManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite2D> SpriteManager => _spriteManager;
    public IObjectSpriteManager<Node, Sprite2D> ObjectManager => _objectManager;
    public Sprite2D CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public Node CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// GodotGfx3dSprite
public class GodotGfx3dSprite : IOpenGfx3dSprite<Node, Sprite3D>
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite3D> _spriteManager;
    readonly ObjectSpriteManager<Node, Sprite3D> _objectManager;

    public GodotGfx3dSprite(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new Object2dManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite3D> SpriteManager => _spriteManager;
    public IObjectSpriteManager<Node, Sprite3D> ObjectManager => _objectManager;
    public Sprite3D CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public Node CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// GodotGfx3dModel
public class GodotGfx3dModel : IOpenGfx3dModel<Node, Material, Texture, XShader>
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite3D> _spriteManager;
    readonly ITextureManager<Texture> _textureManager;
    readonly MaterialManager<Material, Texture> _materialManager;
    readonly ObjectModelManager<Node, Material, Texture> _objectManager;
    readonly ShaderManager<XShader> _shaderManager;

    public GodotGfx3dModel(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        _textureManager = new TextureManager<Texture>(source, new GodotTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite3D> SpriteManager => _spriteManager;
    public ITextureManager<Texture> TextureManager => _textureManager;
    public IMaterialManager<Material, Texture> MaterialManager => _materialManager;
    public IObjectModelManager<Node, Material, Texture> ObjectManager => _objectManager;
    public IShaderManager<XShader> ShaderManager => _shaderManager;
    public Texture CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public Node CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public XShader CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// GodotSfx
public class GodotSfx(ISource source) : SystemSfx(source) { }

// GodotPlatform
public class GodotPlatform : Platform
{
    public static readonly Platform This = new GodotPlatform();
    GodotPlatform() : base("GD", "Godot")
    {
        GfxFactory = source => [new GodotGfx2dSprite(source), new GodotGfx3dSprite(source), new GodotGfx3dModel(source)];
        SfxFactory = source => [new GodotSfx(source)];
        LogFunc = a => GD.Print(a?.Replace("\r", ""));
        LogFormatFunc = (a, b) => GD.Print(string.Format(a, b)?.Replace("\r", ""));
    }
}

// GodotShellPlatform
public class GodotShellPlatform : Platform
{
    public static readonly Platform This = new GodotShellPlatform();
    GodotShellPlatform() : base("GD", "Godot") { }
}
