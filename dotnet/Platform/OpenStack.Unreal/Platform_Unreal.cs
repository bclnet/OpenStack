using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region Client

public class UnrealClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

// UnrealGfxApi
public class UnrealGfxApi(ISource source) : IOpenGfxApi<object, object> {
    public ISource Source => source;
    public Task<T> GetAsset<T>(object path) => throw new NotImplementedException();
    public void AddMeshCollider(object src, object mesh, bool isKinematic, bool isStatic) => throw new NotImplementedException();
    public void AddMeshRenderer(object src, object mesh, object material, bool enabled, bool isStatic) => throw new NotImplementedException();
    public void AddMissingMeshCollidersRecursively(object src, bool isStatic) => throw new NotImplementedException();
    public void Attach(GfxAttach method, object source, params object[] args) => throw new NotImplementedException();
    public object CreateMesh(object mesh) => throw new NotImplementedException();
    public object CreateObject(string name, string tag = null, object parent = null) => throw new NotImplementedException();
    public void SetLayerRecursively(object src, int layer) => throw new NotImplementedException();
    public void Parent(object src, object parent) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Quaternion rotation, Vector3 localScale) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Matrix4x4 rotation, Vector3 localScale) => throw new NotImplementedException();
    public void SetVisible(object src, bool visible) => throw new NotImplementedException();
    public object CreateLight(float radius, Color color, bool indoors) => throw new NotImplementedException();
    public void PostObject(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

// UnrealGfxSprite3D
public class UnrealGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public UnrealGfxSprite3D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new ObjectSpriteManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public ObjectSpriteManager<object, object> ObjectManager => _objectManager;
    public Task<T> GetAsset<T>(object path) => _source.GetAsset<T>(path);
    public object CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public object CreateObject(object path, object parent = default) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public void AttachObject(GfxAttach method, object source, params object[] args) => throw new NotImplementedException();
}

// UnrealGfxModel
public class UnrealGfxModel : IOpenGfxModel<object, object, object, object> {
    readonly ISource _source;
    readonly TextureManager<object> _textureManager;
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;

    public UnrealGfxModel(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new UnrealTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public TextureManager<object> TextureManager => _textureManager;
    public MaterialManager<object, object> MaterialManager => _materialManager;
    public ObjectModelManager<object, object, object> ObjectManager => _objectManager;
    public ShaderManager<object> ShaderManager => _shaderManager;
    public Task<T> GetAsset<T>(object path) => _source.GetAsset<T>(path);
    public object CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public object CreateObject(object path, object parent = null) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public object CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public void AttachObject(GfxAttach method, object source, params object[] args) => throw new NotImplementedException();
}

// UnrealSfx
public class UnrealSfx(ISource source) : SystemSfx(source) { }

// UnrealPlatform
public class UnrealPlatform : Platform {
    public static readonly Platform This = new UnrealPlatform();
    UnrealPlatform() : base("UR", "Unreal") {
        GfxFactory = source => [new UnrealGfxApi(source), null, new UnrealGfxSprite3D(source), new UnrealGfxModel(source), null];
        SfxFactory = source => [new UnrealSfx(source)];
    }
}

// UnrealShellPlatform
public class UnrealShellPlatform : Platform {
    public static readonly Platform This = new UnrealShellPlatform();
    UnrealShellPlatform() : base("UR", "Unreal") { }
}

#endregion