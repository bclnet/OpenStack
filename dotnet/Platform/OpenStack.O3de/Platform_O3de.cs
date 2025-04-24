using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region OpenGfx

// O3deGfxSprite3D
public class O3deGfxSprite3D : IOpenGfxSprite<object, object>
{
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public O3deGfxSprite3D(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new ObjectSpriteManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public ObjectSpriteManager<object, object> ObjectManager => _objectManager;
    public object CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public object CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// O3deGfxModel
public class O3deGfxModel : IOpenGfxModel<object, object, object, object>
{
    readonly ISource _source;
    readonly TextureManager<object> _textureManager;
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;

    public O3deGfxModel(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new O3deTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public TextureManager<object> TextureManager => _textureManager;
    public MaterialManager<object, object> MaterialManager => _materialManager;
    public ObjectModelManager<object, object, object> ObjectManager => _objectManager;
    public ShaderManager<object> ShaderManager => _shaderManager;
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
        GfxFactory = source => [null, new O3deGfxSprite3D(source), new O3deGfxModel(source)];
        SfxFactory = source => [new O3deSfx(source)];
    }
}

// O3deShellPlatform
public class O3deShellPlatform : Platform
{
    public static readonly Platform This = new O3deShellPlatform();
    O3deShellPlatform() : base("O3", "O3de") { }
}

#endregion