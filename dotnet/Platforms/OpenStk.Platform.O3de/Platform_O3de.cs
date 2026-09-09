using OpenStk.Client;
using OpenStk.Gfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStk.GfxTests")]

namespace OpenStk;

#region Client

public class O3deClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}


#endregion

#region Platform

// O3deGfxSprite3D
public class O3deGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly SpriteManager<object> _spriteManager;
    public O3deGfxSprite3D() {
        //_spriteManager = new SpriteManager<Sprite2D>(new GodotSpriteBuilder());
        //_objectManager = new ObjectSpriteManager<Node, Sprite2D>(new GodotObjectBuilder());
    }

    public SpriteManager<object> SpriteManager => _spriteManager;
    public void Preload(ISource source, object path) => _spriteManager.Preload(source, path);
    public Task<(object spr, object tag)> Create(ISource source, object path, object parent = default) => _spriteManager.Create(source, path);
}

// O3deGfxModel
public class O3deGfxModel : IOpenGfxModel<object, object, object, object> {
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;
    readonly TextureManager<object> _textureManager;
    public O3deGfxModel() {
        //_textureManager = new TextureManager<object>(source, new O3deTextureBuilder());
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
    public Task<(object obj, object tag)> Create(ISource source, object path, bool isStatic, object parent = default) => _objectManager.Create(source, path, isStatic, parent);
    public Task<(object sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(object tex, object tag)> CreateTexture(ISource source, object path, System.Range? level = null) => _textureManager.Create(source, path, level);
    public void Post(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

// O3deSfx
public class O3deSfx : SystemSfx { }

// O3dePlatform
public class O3dePlatform : Platform {
    public static readonly Platform This = new O3dePlatform();
    O3dePlatform() : base("O3", "O3de") {
        Caps = PlatformX.Caps.Drawing;
        GfxFactory = () => [null, null, new O3deGfxSprite3D(), new O3deGfxModel(), null, null];
        SfxFactory = () => [new O3deSfx()];
    }
}

// O3deShellPlatform
public class O3deShellPlatform : Platform {
    public static readonly Platform This = new O3deShellPlatform();
    O3deShellPlatform() : base("O3", "O3de") { }
}

#endregion