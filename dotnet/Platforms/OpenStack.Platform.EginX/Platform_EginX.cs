using EginX;
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

public class ExClientHost : Game, IClientHost {
    public ClientBase Client;
    public SceneBase Scene;
    public IPluginHost PluginHost;

    public ExClientHost(Func<ClientBase> client) {
        Client = client();
        DeviceManager = new GraphicsDeviceManager(this);
        //PluginHost = pluginHost; IPluginHost pluginHost, string title
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetScene<T>() where T : SceneBase => Scene as T;

    public void SetScene(SceneBase scene) { Scene?.Dispose(); Scene = scene; Scene?.Load(); }

    protected override async Task LoadContent() {
        await base.LoadContent();
        await Client.LoadContent();
    }

    protected override async Task UnloadContent() {
        await Client.UnloadContent();
        await base.UnloadContent();
    }
}


#endregion

#region Platform

// EginXObjectBuilder
// MISSING

// EginXGfxSprite2D
public class EginXGfxSprite2D : IOpenGfxSprite<object, object> {
    readonly SpriteManager<object> _spriteManager;
    public EginXGfxSprite2D() {
        //_spriteManager = new SpriteManager<Sprite2D>(new GodotSpriteBuilder());
    }

    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(ISource source, object path) => _spriteManager.PreloadSprite(source, path);
    public Task<(object spr, object tag)> CreateSprite(ISource source, object path, object parent = null) => _spriteManager.CreateSprite(source, path);
}

// EginXPlatform
public class EginXPlatform : Platform {
    public static readonly Platform This = new EginXPlatform();
    EginXPlatform() : base("EX", "EginX") {
        Caps = PlatformX.Caps.Drawing;
        GfxFactory = () => [null, new EginXGfxSprite2D(), null, null, null, null];
        SfxFactory = () => [new SystemSfx()];
    }
}

#endregion