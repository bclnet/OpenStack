using OpenStack.Gfx;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region OpenGfx

// UnrealGfxSprite3D
public class UnrealGfxSprite3D : IOpenGfxSprite<object, object>
{
    readonly ISource _source;
    readonly ISpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public UnrealGfxSprite3D(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new ObjectSpriteManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<object> SpriteManager => _spriteManager;
    public IObjectSpriteManager<object, object> ObjectManager => _objectManager;
    public object CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public object CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// UnrealGfxModel
public class UnrealGfxModel : IOpenGfxModel<object, object, object, object>
{
    readonly ISource _source;
    readonly ITextureManager<object> _textureManager;
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;

    public UnrealGfxModel(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new UnrealTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public ITextureManager<object> TextureManager => _textureManager;
    public IMaterialManager<object, object> MaterialManager => _materialManager;
    public IObjectModelManager<object, object, object> ObjectManager => _objectManager;
    public IShaderManager<object> ShaderManager => _shaderManager;
    public object CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public object CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public object CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// UnrealSfx
public class UnrealSfx(ISource source) : SystemSfx(source) { }

// UnrealPlatform
public class UnrealPlatform : Platform
{
    public static readonly Platform This = new UnrealPlatform();
    UnrealPlatform() : base("UR", "Unreal")
    {
        GfxFactory = source => [null, new UnrealGfxSprite3D(source), new UnrealGfxModel(source)];
        SfxFactory = source => [new UnrealSfx(source)];
    }
}

// UnrealShellPlatform
public class UnrealShellPlatform : Platform
{
    public static readonly Platform This = new UnrealShellPlatform();
    UnrealShellPlatform() : base("UR", "Unreal") { }
}

#endregion