using OpenStack.Client;
using OpenStack.Gfx;
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

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

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
class StrideTextureBuilder : TextureBuilderBase<Texture> {
    Texture _defaultTexture;
    public override Texture DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release() {
        if (_defaultTexture != null) { /*DeleteTexture(_defaultTexture);*/ _defaultTexture = null; }
    }

    Texture CreateDefaultTexture() => CreateSolidTexture(4, 4, [
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

    public override Texture CreateNormalMapTexture(Texture src, float strength) => throw new NotImplementedException();
    public override Texture CreateSolidTexture(int width, int height, float[] pixels) => null;
    public override Texture CreateTexture(Texture reuse, ITexture src, Range? level = null) => throw new NotImplementedException();
    public override void DeleteTexture(Texture src) { }
}

// StrideMaterialBuilder : MISSING

// StrideGfxSprite3D
public class StrideGfxSprite3D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    public StrideGfxSprite3D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(object spr, object tag)> CreateSprite(object path, object parent = default) => _spriteManager.CreateSprite(path);
}

// StrideGfxModel
public class StrideGfxModel : IOpenGfxModel<Entity, Material, Texture, int> {
    readonly ISource _source;
    readonly MaterialManager<Material, Texture> _materialManager = default;
    readonly ObjectModelManager<Entity, Material, Texture> _objectManager = default;
    readonly ShaderManager<int> _shaderManager = default;
    readonly TextureManager<Texture> _textureManager;
    public StrideGfxModel(ISource source) {
        _source = source;
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new StrideMaterialBuilder(_textureManager));
        //_objectManager = new Object3dModelManager<Model, Material, int>(source, _materialManager, new StrideObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new StrideShaderBuilder());
        _textureManager = new TextureManager<Texture>(source, new StrideTextureBuilder());
    }

    public ISource Source => _source;
    public MaterialManager<Material, Texture> MaterialManager => _materialManager;
    public ObjectModelManager<Entity, Material, Texture> ObjectManager => _objectManager;
    public ShaderManager<int> ShaderManager => _shaderManager;
    public TextureManager<Texture> TextureManager => _textureManager;
    public void PreloadObject(object path) => throw new NotImplementedException();
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public Task<(Entity obj, object tag)> CreateObject(object path, bool isStatic, Entity parent = default) => throw new NotImplementedException();
    public Task<(int sha, object tag)> CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(Texture tex, object tag)> CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level);
    public void PostObject(Entity src, Vector3 position, Vector3 eulerAngles, float? scale, Entity parent) => throw new NotImplementedException();
}

// StrideSfx
public class StrideSfx(ISource source) : SystemSfx(source) { }

// StridePlatform
public class StridePlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, Entity>> BuildersByType = [];
    public static readonly Platform This = new StridePlatform();
    static Logger Log;
    StridePlatform() : base("ST", "Stride") {
        Log = GlobalLogger.GetLogger(typeof(StridePlatform).FullName);
        Log.Debug("Start loading MyTexture");
        GfxFactory = source => [null, null, new StrideGfxSprite3D(source), new StrideGfxModel(source), null, null];
        SfxFactory = source => [new StrideSfx(source)];
        LogFunc = a => Log.Info(a);
    }
}

#endregion