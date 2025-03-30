using OpenStack.Gfx;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Ogre;

// OgreExtensions
public static class OgreExtensions { }

// IOgreGfx3d
public interface IOgreGfx3d : IOpenGfx3dAny<object, object, object, object> { }

// OgreGfx3d
public class OgreGfx3d : IOgreGfx3d
{
    readonly ISource _source;
    readonly ITextureManager<object> _textureManager;
    readonly MaterialManager<object, object> _materialManager;
    readonly Object3dManager<object, object, object> _objectManager;
    readonly ShaderManager<object> _shaderManager;

    public OgreGfx3d(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<object>(source, new GodotSpriteBuilder());
        //_textureManager = new TextureManager<object>(source, new OgreTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new GodotMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new GodotObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new GodotShaderBuilder());
    }

    public ISource Source => _source;
    public ITextureManager<object> TextureManager => _textureManager;
    public IMaterialManager<object, object> MaterialManager => _materialManager;
    public IObject3dManager<object, object, object> ObjectManager => _objectManager;
    public IShaderManager<object> ShaderManager => _shaderManager;
    public object CreateTexture(object path, System.Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public object CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public object CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// OgreSfx
public class OgreSfx(ISource source) : SystemSfx(source) { }

// OgrePlatform
public class OgrePlatform : Platform
{
    public static readonly Platform This = new OgrePlatform();
    OgrePlatform() : base("OG", "Ogre")
    {
        GfxFactory = source => false ? null : new OgreGfx3d(source);
        SfxFactory = source => new OgreSfx(source);
    }
}

// OgreShellPlatform
public class OgreShellPlatform : Platform
{
    public static readonly Platform This = new OgreShellPlatform();
    OgreShellPlatform() : base("OG", "Ogre") { }
}