using OpenStack.Gfx;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.O3de;

// O3deExtensions
public static class O3deExtensions { }

// O3deGfx3dSprite
public class O3deGfx3dSprite : IOpenGfx3dSprite<object, object>
{
    readonly ISource _source;
    readonly ISpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public O3deGfx3dSprite(ISource source)
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

// O3deGfx3dModel
public class O3deGfx3dModel : IOpenGfx3dModel<object, object, object, object>
{
    readonly ISource _source;
    readonly ITextureManager<object> _textureManager;
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;

    public O3deGfx3dModel(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new O3deTextureBuilder());
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

// O3deSfx
public class O3deSfx(ISource source) : SystemSfx(source) { }

// O3dePlatform
public class O3dePlatform : Platform
{
    public static readonly Platform This = new O3dePlatform();
    O3dePlatform() : base("O3", "O3de")
    {
        GfxFactory = source => [null, new O3deGfx3dSprite(source), new O3deGfx3dModel(source)];
        SfxFactory = source => [new O3deSfx(source)];
    }
}

// O3deShellPlatform
public class O3deShellPlatform : Platform
{
    public static readonly Platform This = new O3deShellPlatform();
    O3deShellPlatform() : base("O3", "O3de") { }
}