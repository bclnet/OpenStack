using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable CS9113

namespace OpenStack.Client;

#region Client

/// <summary>
/// GlobalTime
/// </summary>
public static class GlobalTime {
    public static uint Ticks;
    public static float Delta;
}

/// <summary>
/// Plugin
/// </summary>
public class Plugin {
    public readonly static List<Plugin> Plugins = [];
    public readonly string Path;
    public bool IsValid => true;
    public static Plugin Create(string path) => default;
    public static void OnClosing() { }
    public static void OnFocusGained() { }
    public static void OnFocusLost() { }
    //public static void OnConnected() { }
    //public static void OnDisconnected() { }
    public static bool ProcessHotkeys(int key, int mod, bool ispressed) => true;
    //public static bool ProcessMouse(int button, int wheel) => true;
    public static void ProcessDrawCmdList(object device) { }
    public static int ProcessWndProc(object e) => 0;
    //public static void UpdatePlayerPosition(int x, int y, int z) { }
    public static void Tick() { }
}

/// <summary>
/// IPluginHost
/// </summary>
public interface IPluginHost {
    void Initialize();
    void LoadPlugin(string pluginPath);
    void Tick();
    void Closing();
    void FocusGained();
    void FocusLost();
    void Connected();
    void Disconnected();
    bool Hotkey(int key, int mod, bool pressed);
    void Mouse(int button, int wheel);
    void GetCommandList(out IntPtr listPtr, out int listCount);
    int Event(object ev);
    void UpdatePlayerPosition(int x, int y, int z);
    bool PacketIn(ArraySegment<byte> buffer);
    bool PacketOut(Span<byte> buffer);
}

/// <summary>
/// IClientHost
/// </summary>
public interface IClientHost : IDisposable {
    void Run();
}

/// <summary>
/// ClientBase
/// </summary>
public abstract class ClientBase() : IDisposable {
    public virtual void Dispose() { }
    protected virtual async Task LoadContent() { }
    protected virtual async Task UnloadContent() { }
}

/// <summary>
/// SceneBase
/// </summary>
public abstract class SceneBase : IDisposable {
    public bool IsDestroyed;
    public bool IsLoaded;
    public int RenderedObjectsCount;
    //public Camera Camera { get; } = new Camera(0.5f, 2.5f, 0.1f);

    public virtual void Dispose() {
        if (IsDestroyed) return;
        Unload();
        IsDestroyed = true;
    }
    public virtual void Update() { } // Camera.Update(true, Time.Delta, Mouse.Position);
    public virtual bool Draw() => true;
    public virtual void Load() => IsLoaded = true;
    public virtual void Unload() => IsLoaded = false;
    // input
    //public virtual bool OnMouseUp(MouseButtonType button) => false;
    //public virtual bool OnMouseDown(MouseButtonType button) => false;
    //public virtual bool OnMouseDoubleClick(MouseButtonType button) => false;
    public virtual bool OnMouseWheel(bool up) => false;
    public virtual bool OnMouseDragging() => false;
    public virtual void OnTextInput(string text) { }
    //public virtual void OnKeyDown(SDL_KeyboardEvent e) { }
    //public virtual void OnKeyUp(SDL_KeyboardEvent e) { }
}

#endregion
