using EnginX;
using OpenStack.Client;
using System;
using System.Runtime.CompilerServices;
#pragma warning disable CS0649, CS0169

namespace OpenStack;

public unsafe class ExClientHost : Game, IClientHost {
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
}
