using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region Client

public class OgreClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}


#endregion

#region Platform

// OgreGfxSprite3D
public class OgreGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly ObjectSpriteManager<object, object> _objectManager;
    readonly SpriteManager<object> _spriteManager;

    public OgreGfxSprite3D(ISource source) {
        _source = source;
        //_objectManager = new ObjectSpriteManager<Node, Sprite2D>(source, new GodotObjectBuilder());
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public ISource Source => _source;
    public ObjectSpriteManager<object, object> ObjectManager => _objectManager;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public Task<T> GetAsset<T>(object path) => _source.GetAsset<T>(path);
    public void PreloadObject(object path) => throw new NotImplementedException();
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public object CreateObject(object path, object parent = null) => throw new NotImplementedException();
    public object CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
}

// OgreGfxModel
public class OgreGfxModel : IOpenGfxModel<object, object, object, object> {
    readonly ISource _source;
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;
    readonly TextureManager<object> _textureManager;

    public OgreGfxModel(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new OgreTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public MaterialManager<object, object> MaterialManager => _materialManager;
    public ObjectModelManager<object, object, object> ObjectManager => _objectManager;
    public ShaderManager<object> ShaderManager => _shaderManager;
    public TextureManager<object> TextureManager => _textureManager;
    public Task<T> GetAsset<T>(object path) => _source.GetAsset<T>(path);
    public void PreloadObject(object path) => throw new NotImplementedException();
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public object CreateObject(object path, object parent = null) => throw new NotImplementedException();
    public object CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public object CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level).tex;
}

// OgreSfx
public class OgreSfx(ISource source) : SystemSfx(source) { }

// OgrePlatform
public class OgrePlatform : Platform {
    public static readonly Platform This = new OgrePlatform();
    OgrePlatform() : base("OG", "Ogre") {
        GfxFactory = source => [null, null, new OgreGfxSprite3D(source), new OgreGfxModel(source), null];
        SfxFactory = source => [new OgreSfx(source)];
    }
}

// OgreShellPlatform
public class OgreShellPlatform : Platform {
    public static readonly Platform This = new OgreShellPlatform();
    OgreShellPlatform() : base("OG", "Ogre") { }
}

#endregion