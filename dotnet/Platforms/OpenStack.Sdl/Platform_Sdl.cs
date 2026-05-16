using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region Client

public class SdlClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Plaform

// SdlObjectBuilder
// MISSING

// SdlGfxSprite2D
public class SdlGfxSprite2D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    public SdlGfxSprite2D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(object spr, object tag)> CreateSprite(object path, object parent = default) => _spriteManager.CreateSprite(path);
}

// SdlSfx
public class SdlSfx(ISource source) : SystemSfx(source) { }

// SdlPlatform
public class SdlPlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new SdlPlatform();
    SdlPlatform() : base("SD", "SDL 3") {
        GfxFactory = source => [new SdlGfxSprite2D(source), null, null, null, null, null];
        SfxFactory = source => [new SdlSfx(source)];
    }
}

#endregion