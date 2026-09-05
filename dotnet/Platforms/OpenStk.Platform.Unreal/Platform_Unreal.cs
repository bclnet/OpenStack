using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
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
public class UnrealGfxApi : IOpenGfxApi<object, object> {
    public void AddMeshCollider(object src, object mesh, bool isKinematic, bool isStatic) => throw new NotImplementedException();
    public void AddMeshRenderer(object src, object mesh, object material, bool enabled, bool isStatic) => throw new NotImplementedException();
    public void AddMissingMeshCollidersRecursively(object src, bool isStatic) => throw new NotImplementedException();
    public void Attach(GfxAttach method, object src, params object[] args) => throw new NotImplementedException();
    public object CreateMesh(object mesh) => throw new NotImplementedException();
    public object CreateObject(string name, string tag = null, object parent = null) => throw new NotImplementedException();
    public void SetLayerRecursively(object src, int layer) => throw new NotImplementedException();
    public void Parent(object src, object parent) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Quaternion rotation, Vector3 localScale) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Matrix4x4 rotation, Vector3 localScale) => throw new NotImplementedException();
    public void SetVisible(object src, bool visible) => throw new NotImplementedException();
    public void Destroy(object src) => throw new NotImplementedException();
}

// UnrealGfxSprite3D
public class UnrealGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly SpriteManager<object> _spriteManager;
    public UnrealGfxSprite3D() {
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(ISource source, object path) => _spriteManager.PreloadSprite(source, path);
    public Task<(object spr, object tag)> CreateSprite(ISource source, object path, object parent = default) => _spriteManager.CreateSprite(source, path);
}

// UnrealGfxModel
public class UnrealGfxModel : IOpenGfxModel<object, object, object, object> {
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;
    readonly TextureManager<object> _textureManager;
    public UnrealGfxModel() {
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new UnrealTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public MaterialManager<object, object> MaterialManager => _materialManager;
    public ObjectModelManager<object, object, object> ObjectManager => _objectManager;
    public ShaderManager<object> ShaderManager => _shaderManager;
    public TextureManager<object> TextureManager => _textureManager;
    public void PreloadObject(ISource source, object path) => throw new NotImplementedException();
    public void PreloadTexture(ISource source, object path) => throw new NotImplementedException();
    public Task<(object obj, object tag)> CreateObject(ISource source, object path, bool isStatic, object parent = default) => throw new NotImplementedException();
    public Task<(object sha, object tag)> CreateShader(ISource source, object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(object tex, object tag)> CreateTexture(ISource source, object path, System.Range? level = null) => _textureManager.CreateTexture(source, path, level);
    public void PostObject(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

// UnrealSfx
public class UnrealSfx : SystemSfx { }

// UnrealPlatform
public class UnrealPlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new UnrealPlatform();
    UnrealPlatform() : base("UR", "Unreal") {
        Caps = PlatformX.Caps.Drawing;
        GfxFactory = () => [new UnrealGfxApi(), null, new UnrealGfxSprite3D(), new UnrealGfxModel(), null, null];
        SfxFactory = () => [new UnrealSfx()];
    }
}

// UnrealShellPlatform
public class UnrealShellPlatform : Platform {
    public static readonly Platform This = new UnrealShellPlatform();
    UnrealShellPlatform() : base("UR", "Unreal") { }
}

#endregion