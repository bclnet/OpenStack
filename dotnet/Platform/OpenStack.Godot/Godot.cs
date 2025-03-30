using Godot;
using OpenStack.Gfx;
using OpenStack.Gfx.Texture;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using XShader = Godot.Shader;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Godot;

// GodotExtensions
public static class GodotExtensions { }

// GodotObjectBuilder : MISSING

// GodotShaderBuilder : MISSING

// GodotTextureBuilder
public class GodotTextureBuilder : TextureBuilderBase<Texture>
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

    public override Texture CreateTexture(Texture reuse, ITexture source, System.Range? level = null)
    {
        var (bytes, format, _) = source.Begin("ST");
        try
        {
            return null;
        }
        finally { source.End(); }
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

// GodotMaterialBuilder : MISSING

// IGodotGfx2d
public interface IGodotGfx2d : IOpenGfx2dAny<Node, Sprite2D> { }

// GodotGfx2d
public class GodotGfx2d : IGodotGfx2d
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite2D> _spriteManager;
    readonly Object2dManager<Node, Sprite2D> _objectManager;

    public GodotGfx2d(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new Object2dManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite2D> SpriteManager => _spriteManager;
    public IObject2dManager<Node, Sprite2D> ObjectManager => _objectManager;
    public Sprite2D CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public Node CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// IGodotGfx3d
public interface IGodotGfx3d : IOpenGfx3dAny<Node, Material, Texture, XShader> { }

// GodotGfx3d
public class GodotGfx3d : IGodotGfx3d
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite3D> _spriteManager;
    readonly ITextureManager<Texture> _textureManager;
    readonly MaterialManager<Material, Texture> _materialManager;
    readonly Object3dManager<Node, Material, Texture> _objectManager;
    readonly ShaderManager<XShader> _shaderManager;

    public GodotGfx3d(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        _textureManager = new TextureManager<Texture>(source, new GodotTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite3D> SpriteManager => _spriteManager;
    public ITextureManager<Texture> TextureManager => _textureManager;
    public IMaterialManager<Material, Texture> MaterialManager => _materialManager;
    public IObject3dManager<Node, Material, Texture> ObjectManager => _objectManager;
    public IShaderManager<XShader> ShaderManager => _shaderManager;
    public Texture CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public Node CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public XShader CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// GodotSfx
public class GodotSfx(ISource source) : SystemSfx(source) { }

// GodotPlatform
public class GodotPlatform : Platform
{
    public static readonly Platform This = new GodotPlatform();
    GodotPlatform() : base("GD", "Godot")
    {
        GfxFactory = source => false ? new GodotGfx2d(source) : new GodotGfx3d(source);
        SfxFactory = source => new GodotSfx(source);
        LogFunc = a => GD.Print(a?.Replace("\r", ""));
        LogFormatFunc = (a, b) => GD.Print(string.Format(a, b)?.Replace("\r", ""));
    }
}

// GodotShellPlatform
public class GodotShellPlatform : Platform
{
    public static readonly Platform This = new GodotShellPlatform();
    GodotShellPlatform() : base("GD", "Godot") { }
}