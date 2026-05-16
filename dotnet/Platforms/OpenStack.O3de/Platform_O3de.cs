using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region Client

public class O3deClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}


#endregion

#region Platform

// O3deGfxSprite3D
public class O3deGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    public O3deGfxSprite3D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new ObjectSpriteManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(object spr, object tag)> CreateSprite(object path, object parent = default) => _spriteManager.CreateSprite(path);
}

// O3deGfxModel
public class O3deGfxModel : IOpenGfxModel<object, object, object, object> {
    readonly ISource _source;
    readonly MaterialManager<object, object> _materialManager;
    readonly ObjectModelManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;
    readonly TextureManager<object> _textureManager;
    public O3deGfxModel(ISource source) {
        _source = source;
        //_textureManager = new TextureManager<object>(source, new O3deTextureBuilder());
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
    public Task<(object obj, object tag)> CreateObject(object path, bool isStatic, object parent = default) => _objectManager.CreateObject(path, isStatic, parent);
    public Task<(object sha, object tag)> CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(object tex, object tag)> CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level);
    public void PostObject(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

// O3deSfx
public class O3deSfx(ISource source) : SystemSfx(source) { }

// O3dePlatform
public class O3dePlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new O3dePlatform();
    O3dePlatform() : base("O3", "O3de") {
        GfxFactory = source => [null, null, new O3deGfxSprite3D(source), new O3deGfxModel(source), null, null];
        SfxFactory = source => [new O3deSfx(source)];
    }
}

// O3deShellPlatform
public class O3deShellPlatform : Platform {
    public static readonly Platform This = new O3deShellPlatform();
    O3deShellPlatform() : base("O3", "O3de") { }
}

#endregion