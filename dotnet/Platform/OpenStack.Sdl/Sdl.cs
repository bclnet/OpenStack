using OpenStack.Gfx;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Sdl;

// SdlExtensions
public static class SdlExtensions { }

// SdlObjectBuilder
// MISSING

// ISdlGfx2d
//public interface ISdlGfx2dSprite : IOpenGfx2dSpriteAny<object, object> { }

// SdlGfx2dSprite
public class SdlGfx2dSprite : IOpenGfxSprite<object, object>
{
    readonly ISource _source;
    readonly ISpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public SdlGfx2dSprite(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new Object2dManager<Node, Sprite2D>(source, new GodotObjectBuilder());
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

// SdlSfx
public class SdlSfx(ISource source) : SystemSfx(source) { }

// SdlPlatform
public class SdlPlatform : Platform
{
    public static readonly Platform This = new SdlPlatform();
    SdlPlatform() : base("SD", "SDL 3")
    {
        GfxFactory = source => [new SdlGfx2dSprite(source), null, null];
        SfxFactory = source => [new SdlSfx(source)];
    }
}
