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
    readonly SpriteManager<object> _spriteManager;
    public SdlGfxSprite2D() {
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(ISource source, object path) => _spriteManager.PreloadSprite(source, path);
    public Task<(object spr, object tag)> CreateSprite(ISource source, object path, object parent = default) => _spriteManager.CreateSprite(source, path);
}

// SdlSfx
public class SdlSfx : SystemSfx { }

// SdlPlatform
public class SdlPlatform : Platform {
    public static readonly Platform This = new SdlPlatform();
    SdlPlatform() : base("SD", "SDL 3") {
        GfxFactory = () => [new SdlGfxSprite2D(), null, null, null, null, null];
        SfxFactory = () => [new SdlSfx()];
    }
}

#endregion