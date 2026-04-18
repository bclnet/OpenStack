using EnginX;
using OpenStack.Client;
using OpenStack.Gfx;
using System;
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

// ExObjectBuilder
// MISSING

// ExGfxSprite2D
public class ExGfxSprite2D : IOpenGfxSprite<object, object> {
    readonly ISource _source;
    readonly SpriteManager<object> _spriteManager;
    readonly ObjectSpriteManager<object, object> _objectManager;

    public ExGfxSprite2D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite2D>(source, new GodotSpriteBuilder());
        //_objectManager = new Object2dManager<Node, Sprite2D>(source, new GodotObjectBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<object> SpriteManager => _spriteManager;
    public ObjectSpriteManager<object, object> ObjectManager => _objectManager;
    public Task<T> GetAsset<T>(object path) => _source.GetAsset<T>(path);
    public object CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public object CreateObject(object path, object parent) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public void AttachObject(AttachObjectMethod method, object source, params object[] args) => throw new NotImplementedException();
}

// ExPlatform
public class ExPlatform : Platform {
    public static readonly Platform This = new ExPlatform();
    ExPlatform() : base("EX", "EnginX") {
        GfxFactory = source => [new ExGfxSprite2D(source), null, null];
        SfxFactory = source => [new SystemSfx(source)];
    }
}

#endregion