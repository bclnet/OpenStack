using OpenStk.Client;
using OpenStk.Gfx;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStk.GfxTests")]

namespace OpenStk;

#region Client

public class StrideClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

// StrideObjectBuilder : MISSING

// StrideShaderBuilder : MISSING

// StrideTextureBuilder
class StrideTextureBuilder : TextureBuilder<Texture> {
    Texture _defaultTexture;
    public override Texture Default => _defaultTexture ??= CreateDefaultTexture();

    public void Release() {
        if (_defaultTexture != null) { /*DeleteTexture(_defaultTexture);*/ _defaultTexture = null; }
    }

    Texture CreateDefaultTexture() => CreateSolid(4, 4, [
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,

        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
    ]);

    public override Texture CreateNormalMap(Texture src, float strength) => throw new NotImplementedException();
    public override Texture CreateSolid(int width, int height, float[] pixels) => null;
    public override Texture Create(Texture reuse, ITexture src, Range? level = null) => throw new NotImplementedException();
    public override void Delete(Texture src) { }
}

// StrideMaterialBuilder : MISSING

// StrideGfxSprite3D
public class StrideGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly SpriteManager<object> _spriteManager;
    public StrideGfxSprite3D() {
        //_spriteManager = new SpriteManager<Sprite2D>(new GodotSpriteBuilder());
    }

    public SpriteManager<object> SpriteManager => _spriteManager;
    public void Preload(ISource source, object path) => _spriteManager.Preload(source, path);
    public Task<(object spr, object tag)> Create(ISource source, object path, object parent = default) => _spriteManager.Create(source, path);
}

// StrideGfxModel
public class StrideGfxModel : IOpenGfxModel<Entity, Material, Texture, int> {
    readonly MaterialManager<Material, Texture> _materialManager = default;
    readonly ObjectModelManager<Entity, Material, Texture> _objectManager = default;
    readonly ShaderManager<int> _shaderManager = default;
    readonly TextureManager<Texture> _textureManager;
    public StrideGfxModel() {
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new StrideMaterialBuilder(_textureManager));
        //_objectManager = new Object3dModelManager<Model, Material, int>(source, _materialManager, new StrideObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new StrideShaderBuilder());
        _textureManager = new TextureManager<Texture>(new StrideTextureBuilder());
    }

    public MaterialManager<Material, Texture> MaterialManager => _materialManager;
    public ObjectModelManager<Entity, Material, Texture> ObjectManager => _objectManager;
    public ShaderManager<int> ShaderManager => _shaderManager;
    public TextureManager<Texture> TextureManager => _textureManager;
    public void Preload(ISource source, object path) => throw new NotImplementedException();
    public void PreloadTexture(ISource source, object path) => throw new NotImplementedException();
    public Task<(Entity obj, object tag)> Create(ISource source, object path, bool isStatic, Entity parent = default) => throw new NotImplementedException();
    public Task<(int sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(Texture tex, object tag)> CreateTexture(ISource source, object path, Range? level = null) => _textureManager.Create(source, path, level);
    public void Post(Entity src, Vector3 position, Vector3 eulerAngles, float? scale, Entity parent) => throw new NotImplementedException();
}

// StrideSfx
public class StrideSfx : SystemSfx { }

// StridePlatform
public class StridePlatform : Platform {
    public static readonly Platform This = new StridePlatform();
    static Logger Log;
    StridePlatform() : base("ST", "Stride") {
        Log = GlobalLogger.GetLogger(typeof(StridePlatform).FullName);
        Log.Debug("Start loading MyTexture");
        Caps = PlatformX.Caps.Drawing;
        GfxFactory = () => [null, null, new StrideGfxSprite3D(), new StrideGfxModel(), null, null];
        SfxFactory = () => [new StrideSfx()];
        LogFunc = a => Log.Info(a);
    }
}

#endregion