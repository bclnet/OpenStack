using OpenStack.Gfx;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region OpenGfx

// StrideObjectBuilder : MISSING

// StrideShaderBuilder : MISSING

// StrideTextureBuilder
class StrideTextureBuilder : TextureBuilderBase<Texture>
{
    Texture _defaultTexture;
    public override Texture DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release()
    {
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

    public override Texture CreateTexture(Texture reuse, ITexture source, Range? level = null)
    {
        throw new NotImplementedException();
    }

    public override Texture CreateSolidTexture(int width, int height, float[] pixels)
    {
        return null;
    }

    public override Texture CreateNormalMap(Texture texture, float strength)
    {
        throw new NotImplementedException();
    }

    public override void DeleteTexture(Texture texture) { }
}

// StrideMaterialBuilder : MISSING

// StrideGfxSprite3D
public class StrideGfxSprite3D : IOpenGfxSprite<object, object>
{
    readonly ISource _source;
    readonly ISpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public StrideGfxSprite3D(ISource source)
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

// StrideGfxModel
public class StrideGfxModel : IOpenGfxModel<Entity, Material, Texture, int>
{
    readonly ISource _source;
    readonly ITextureManager<Texture> _textureManager;
    readonly MaterialManager<Material, Texture> _materialManager = default;
    readonly ObjectModelManager<Entity, Material, Texture> _objectManager = default;
    readonly ShaderManager<int> _shaderManager = default;

    public StrideGfxModel(ISource source)
    {
        _source = source;
        _textureManager = new TextureManager<Texture>(source, new StrideTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new StrideMaterialBuilder(_textureManager));
        //_objectManager = new Object3dModelManager<Model, Material, int>(source, _materialManager, new StrideObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new StrideShaderBuilder());
    }

    public ISource Source => _source;
    public ITextureManager<Texture> TextureManager => _textureManager;
    public IMaterialManager<Material, Texture> MaterialManager => _materialManager;
    public IObjectModelManager<Entity, Material, Texture> ObjectManager => _objectManager;
    public IShaderManager<int> ShaderManager => _shaderManager;
    public Texture CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public Entity CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public int CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// StrideSfx
public class StrideSfx(ISource source) : SystemSfx(source) { }

// StridePlatform
public class StridePlatform : Platform
{
    public static readonly Platform This = new StridePlatform();
    static Logger Log;
    StridePlatform() : base("ST", "Stride")
    {
        Log = GlobalLogger.GetLogger(typeof(StridePlatform).FullName);
        Log.Debug("Start loading MyTexture");
        GfxFactory = source => [null, new StrideGfxSprite3D(source), new StrideGfxModel(source)];
        SfxFactory = source => [new StrideSfx(source)];
        LogFunc = a => Log.Info(a);
        LogFormatFunc = (a, b) => Log.Info(string.Format(a, b));
    }
}

#endregion