using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
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
    readonly SpriteManager<object> _spriteManager;
    public OgreGfxSprite3D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(object spr, object tag)> CreateSprite(object path, object parent = default) => _spriteManager.CreateSprite(path);
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
    public void PreloadObject(object path) => throw new NotImplementedException();
    public void PreloadTexture(object path) => _textureManager.PreloadTexture(path);
    public Task<(object obj, object tag)> CreateObject(object path, bool isStatic, object parent = default) => throw new NotImplementedException();
    public Task<(object sha, object tag)> CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(object tex, object tag)> CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level);
    public void PostObject(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

// OgreSfx
public class OgreSfx(ISource source) : SystemSfx(source) { }

// OgrePlatform
public class OgrePlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new OgrePlatform();
    OgrePlatform() : base("OG", "Ogre") {
        GfxFactory = source => [null, null, new OgreGfxSprite3D(source), new OgreGfxModel(source), null, null];
        SfxFactory = source => [new OgreSfx(source)];
    }
}

// OgreShellPlatform
public class OgreShellPlatform : Platform {
    public static readonly Platform This = new OgreShellPlatform();
    OgreShellPlatform() : base("OG", "Ogre") { }
}

#endregion