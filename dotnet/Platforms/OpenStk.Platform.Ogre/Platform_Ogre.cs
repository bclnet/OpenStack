using OpenStk.Client;
using OpenStk.Gfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStk.GfxTests")]

namespace OpenStk;

#region Client

public class OgreClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}


#endregion

#region Platform

// OgreGfxSprite3D
public class OgreGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly SpriteManager<object> _spriteManager;
    public OgreGfxSprite3D() {
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public SpriteManager<object> SpriteManager => _spriteManager;
    public void Preload(ISource source, object path) => _spriteManager.Preload(source, path);
    public Task<(object spr, object tag)> Create(ISource source, object path, object parent = default) => _spriteManager.Create(source, path);
}

// OgreGfxModel
public class OgreGfxModel : IOpenGfxModel<object, object, object, object> {
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;
    readonly TextureManager<object> _textureManager;
    public OgreGfxModel() {
        //_textureManager = new TextureManager<object>(source, new OgreTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public MaterialManager<object, object> MaterialManager => _materialManager;
    public ObjectModelManager<object, object, object> ObjectManager => _objectManager;
    public ShaderManager<object> ShaderManager => _shaderManager;
    public TextureManager<object> TextureManager => _textureManager;
    public void Preload(ISource source, object path) => throw new NotImplementedException();
    public void PreloadTexture(ISource source, object path) => _textureManager.Preload(source, path);
    public Task<(object obj, object tag)> Create(ISource source, object path, bool isStatic, object parent = default) => throw new NotImplementedException();
    public Task<(object sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(object tex, object tag)> CreateTexture(ISource source, object path, System.Range? level = null) => _textureManager.Create(source, path, level);
    public void Post(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

// OgreSfx
public class OgreSfx : SystemSfx { }

// OgrePlatform
public class OgrePlatform : Platform {
    public static readonly Platform This = new OgrePlatform();
    OgrePlatform() : base("OG", "Ogre") {
        Caps = PlatformX.Caps.Drawing;
        GfxFactory = () => [null, null, new OgreGfxSprite3D(), new OgreGfxModel(), null, null];
        SfxFactory = () => [new OgreSfx()];
    }
}

// OgreShellPlatform
public class OgreShellPlatform : Platform {
    public static readonly Platform This = new OgreShellPlatform();
    OgreShellPlatform() : base("OG", "Ogre") { }
}

#endregion