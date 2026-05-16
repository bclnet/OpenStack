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
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    public EginXGfxSprite2D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(object spr, object tag)> CreateSprite(object path, object parent = null) => _spriteManager.CreateSprite(path);
}

// EginXPlatform
public class EginXPlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new EginXPlatform();
    EginXPlatform() : base("EX", "EginX") {
        GfxFactory = source => [null, new EginXGfxSprite2D(source), null, null, null, null];
        SfxFactory = source => [new SystemSfx(source)];
    }
}

#endregion