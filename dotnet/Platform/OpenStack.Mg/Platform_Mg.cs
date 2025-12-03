using OpenStack.Gfx;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region OpenGfx

// MgObjectBuilder
// MISSING

// MgGfxSprite2D
public class MgGfxSprite2D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public MgGfxSprite2D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new Object2dManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public ObjectSpriteManager<object, object> ObjectManager => _objectManager;
    public object CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public object CreateAsset(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public Task<T> GetAsset<T>(object path) => _source.GetAsset<T>(path);
}

// MgSfx
public class MgSfx(ISource source) : SystemSfx(source) { }

// MgPlatform
public class MgPlatform : Platform {
    public static readonly Platform This = new MgPlatform();
    MgPlatform() : base("MG", "MonoGame") {
        GfxFactory = source => [new MgGfxSprite2D(source), null, null];
        SfxFactory = source => [new MgSfx(source)];
    }
}

#endregion