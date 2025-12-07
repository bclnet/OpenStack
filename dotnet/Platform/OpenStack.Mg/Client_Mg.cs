using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenStack.Client;
using OpenStack.Mg;
using SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static SDL.SDL3;
#pragma warning disable CS0649, CS0169, CS8500

namespace OpenStack;

public unsafe class MgClientHost : Game, IClientHost {
    readonly Platform Platform = PlatformX.Activate(MgPlatform.This);

    const bool RunMouseInASeparateThread = false;
    const int MIN_FPS = 12;
    const int MAX_FPS = 250;
    int Settings_FPS = 60;
    string[] Settings_Plugins = [];
    Point? Settings_WindowPosition;
    Action Settings_Save;
    uint CurrentRefreshRate;

    Texture2D _background;

    //static SDL_EventFilter _filter;
    delegate* unmanaged[Cdecl]<nint, SDL_Event*, SDLBool> _filter;
    bool _ignoreNextTextInput;
    readonly float[] _intervalFixedUpdate = new float[2];
    double _totalElapsed, _currentFpsTime;
    uint _totalFrames;
    Batcher2D _spriteBatch;
    bool _suppressedDraw;
    bool _pluginsInitialized = false;
    readonly List<(uint, Action)> _queuedActions = [];
    static GCHandle _pinned;

    public MgClientHost(Func<ClientBase> client) {
        Client = client();
        //IPluginHost pluginHost, string title
        GraphicManager = new GraphicsDeviceManager(this);
        GraphicManager.PreparingDeviceSettings += (sender, e) => { e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents; };
        GraphicManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        SetVSync(false);
        //
        Window.ClientSizeChanged += WindowOnClientSizeChanged;
        Window.AllowUserResizing = true;
        Window.Title = "title";
        IsMouseVisible = RunMouseInASeparateThread;
        //
        IsFixedTimeStep = false;
        TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 250.0);
        //PluginHost = pluginHost;
    }

    public ClientBase Client { get; private set; }
    public SceneBase Scene { get; private set; }
    public IPluginHost PluginHost { get; private set; }
    public GraphicsDeviceManager GraphicManager { get; }
    public readonly uint[] FrameDelay = new uint[2];

    public void SetClient(ClientBase client) => Client = client;

    public void EnqueueAction(int time, Action action) => _queuedActions.Add(((uint)(GlobalTime.Ticks + time), action));

    protected override void Initialize() {
        if (GraphicManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef)) GraphicManager.GraphicsProfile = GraphicsProfile.HiDef;
        GraphicManager.ApplyChanges();
        SetRefreshRate(Settings_FPS);
        _spriteBatch = new Batcher2D(GraphicsDevice);
        // SDL
        _pinned = GCHandle.Alloc(this);
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS)) throw new InvalidOperationException($"SDL_Init failed: {SDL_GetError()}");
        SDL_SetEventFilter(&HandleSdlEvent, (nint)_pinned);
        //Microsoft.Xna.Framework.Input.TextInputEXT.StartTextInput();
        base.Initialize();
    }

    protected override void LoadContent() {
        base.LoadContent();
        using var ms = typeof(MgClientHost).Assembly.GetManifestResourceStream("OpenStack.Mg.Client_Mg.png");
        _background = Texture2D.FromStream(GraphicsDevice, ms);
        Log.Trace("Loading plugins...");
        PluginHost?.Initialize();
        foreach (string p in Settings_Plugins) Plugin.Create(p);
        _pluginsInitialized = true;
        Log.Trace("Done!");
        SetWindowPositionBySettings();
    }

    protected override void UnloadContent() {
        int top, left;
        SDL_GetWindowBordersSize((SDL_Window*)Window.Handle, &top, &left, null, null);
        Settings_WindowPosition = new Point(Math.Max(0, Window.ClientBounds.X - left), Math.Max(0, Window.ClientBounds.Y - top));
        Settings_Save?.Invoke();
        Plugin.OnClosing();
        base.UnloadContent();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetScene<T>() where T : SceneBase => Scene as T;

    public void SetScene(SceneBase scene) { Scene?.Dispose(); Scene = scene; Scene?.Load(); }

    public void SetVSync(bool value) => GraphicManager.SynchronizeWithVerticalRetrace = value;

    public void SetRefreshRate(int rate) {
        if (rate < MIN_FPS) rate = MIN_FPS;
        else if (rate > MAX_FPS) rate = MAX_FPS;
        var frameDelay = rate == MIN_FPS ? 80f : 1000.0f / rate;
        FrameDelay[0] = FrameDelay[1] = (uint)frameDelay;
        FrameDelay[1] = FrameDelay[1] >> 1;
        Settings_FPS = rate;
        _intervalFixedUpdate[0] = frameDelay;
        _intervalFixedUpdate[1] = 217; // 5 FPS
    }

    void SetWindowPosition(int x, int y) => SDL_SetWindowPosition((SDL_Window*)Window.Handle, x, y);

    public void SetWindowSize(int width, int height) {
        GraphicManager.PreferredBackBufferWidth = width;
        GraphicManager.PreferredBackBufferHeight = height;
        GraphicManager.ApplyChanges();
    }

    public void SetWindowBorderless(bool borderless) {
        var flags = SDL_GetWindowFlags((SDL_Window*)Window.Handle);
        if (((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0 && borderless) || ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) == 0 && !borderless)) return;
        SDL_SetWindowBordered((SDL_Window*)Window.Handle, !borderless);
        var displayMode = SDL_GetCurrentDisplayMode(SDL_GetDisplayForWindow((SDL_Window*)Window.Handle));
        //
        int width = displayMode->w, height = displayMode->h;
        if (borderless) {
            SetWindowSize(width, height);
            SDL_Rect rect;
            SDL_GetDisplayUsableBounds(SDL_GetDisplayForWindow((SDL_Window*)Window.Handle), &rect);
            SDL_SetWindowPosition((SDL_Window*)Window.Handle, rect.x, rect.y);
        }
        else {
            int top, bottom;
            SDL_GetWindowBordersSize((SDL_Window*)Window.Handle, &top, null, &bottom, null);
            SetWindowSize(width, height - (top - bottom));
            SetWindowPositionBySettings();
        }
        //
        //WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();
        //if (viewport != null && ProfileManager.CurrentProfile.GameWindowFullSize) {
        //    viewport.ResizeGameWindow(new Point(width, height));
        //    viewport.X = -5;
        //    viewport.Y = -5;
        //}
    }

    public void MaximizeWindow() {
        SDL_MaximizeWindow((SDL_Window*)Window.Handle);
        GraphicManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
        GraphicManager.PreferredBackBufferHeight = Window.ClientBounds.Height;
        GraphicManager.ApplyChanges();
    }

    public bool IsWindowMaximized() {
        var flags = SDL_GetWindowFlags((SDL_Window*)Window.Handle);
        return (flags & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
    }

    public void RestoreWindow() => SDL_RestoreWindow((SDL_Window*)Window.Handle);

    public void SetWindowPositionBySettings() {
        int top, left;
        SDL_GetWindowBordersSize((SDL_Window*)Window.Handle, &top, &left, null, null);
        if (Settings_WindowPosition.HasValue) SetWindowPosition(Math.Max(0, left + Settings_WindowPosition.Value.X), Math.Max(0, top + Settings_WindowPosition.Value.Y));
    }

    protected override void Update(GameTime gameTime) {
        if (Profiler.InContext("OutOfContext")) Profiler.ExitContext("OutOfContext");
        GlobalTime.Ticks = (uint)gameTime.TotalGameTime.TotalMilliseconds;
        GlobalTime.Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //Mouse.Update();

        //var data = NetClient.Socket.CollectAvailableData();
        //var packetsCount = PacketHandlers.Handler.ParsePackets(NetClient.Socket, UO.World, data);

        //NetClient.Socket.Statistics.TotalPacketsReceived += (uint)packetsCount;
        //NetClient.Socket.Flush();

        Plugin.Tick();

        if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed) { Profiler.EnterContext("Update"); Scene.Update(); Profiler.ExitContext("Update"); }

        //UIManager.Update();

        // fps
        _totalElapsed += gameTime.ElapsedGameTime.TotalMilliseconds;
        _currentFpsTime += gameTime.ElapsedGameTime.TotalMilliseconds;
        if (_currentFpsTime >= 1000) { CurrentRefreshRate = _totalFrames; _totalFrames = 0; _currentFpsTime = 0; }
        // supress draw
        var x = _intervalFixedUpdate[!IsActive ? 1 : 0]; // !IsActive && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ReduceFPSWhenInactive ? 1 : 0];
        _suppressedDraw = false;
        if (_totalElapsed > x) _totalElapsed %= x;
        else {
            _suppressedDraw = true;
            SuppressDraw();
            if (!gameTime.IsRunningSlowly) Thread.Sleep(1);
        }
        // queued actions
        for (var i = _queuedActions.Count - 1; i >= 0; i--) {
            (var time, var fn) = _queuedActions[i];
            if (GlobalTime.Ticks > time) { fn(); _queuedActions.RemoveAt(i); break; }
        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        Profiler.EndFrame(); Profiler.BeginFrame();
        if (Profiler.InContext("OutOfContext")) Profiler.ExitContext("OutOfContext");
        Profiler.EnterContext("RenderFrame");
        _totalFrames++;
        GraphicsDevice.Clear(Color.Black);
        // batch
        _spriteBatch.Begin();
        var rect = new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight);
        _spriteBatch.DrawTiled(_background, rect, _background.Bounds, new Vector3(0, 0, 0.1f));
        _spriteBatch.End();
        if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed) Scene.Draw();
        Profiler.ExitContext("RenderFrame");
        Profiler.EnterContext("OutOfContext");
        Plugin.ProcessDrawCmdList(GraphicsDevice);
        base.Draw(gameTime);
    }

    protected override bool BeginDraw() => !_suppressedDraw && base.BeginDraw();

    void WindowOnClientSizeChanged(object sender, EventArgs e) {
        int width = Window.ClientBounds.Width, height = Window.ClientBounds.Height;
        //if (!IsWindowMaximized()) ProfileManager.CurrentProfile?.WindowClientBounds = new Point(width, height);
        SetWindowSize(width, height);
        //WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();
        //if (viewport != null && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize) {
        //    viewport.ResizeGameWindow(new Point(width, height));
        //    viewport.X = -5;
        //    viewport.Y = -5;
        //}
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static SDLBool HandleSdlEvent(nint userData, SDL_Event* sdlEvent) {
        Log.Info($"{(nint)sdlEvent}");
        var s = (MgClientHost*)userData;
        // Don't pass SDL events to the plugin host before the plugins are initialized or the garbage collector can get screwed up
        if (s->_pluginsInitialized && Plugin.ProcessWndProc(*sdlEvent) != 0) {
            //if ((SDL_EventType)sdlEvent->type == SDL_EventType.SDL_EVENT_MOUSE_MOTION) UO.GameCursor?.AllowDrawSDLCursor = false;
            return true;
        }
        switch ((SDL_EventType)sdlEvent->type) {
            case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_ADDED: Console.WriteLine("AUDIO ADDED: {0}", sdlEvent->adevice.which); break;
            case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED: Console.WriteLine("AUDIO REMOVED: {0}", sdlEvent->adevice.which); break;
            //case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER: Mouse.MouseInWindow = true; break;
            //case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE: Mouse.MouseInWindow = false; break;
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED: Plugin.OnFocusGained(); break;
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST: Plugin.OnFocusLost(); break;
                //case SDL_EventType.SDL_EVENT_KEY_DOWN:
                //    Keyboard.OnKeyDown(sdlEvent->key);
                //    if (Plugin.ProcessHotkeys((int)sdlEvent->key.key, (int)sdlEvent->key.mod, true)) {
                //        s->_ignoreNextTextInput = false;
                //        UIManager.KeyboardFocusControl?.InvokeKeyDown(
                //            (SDL_Keycode)sdlEvent->key.key,
                //            sdlEvent->key.mod
                //        );
                //        s->Scene.OnKeyDown(sdlEvent->key);
                //    }
                //    else _ignoreNextTextInput = true;
                //    break;
                //case SDL_EventType.SDL_EVENT_KEY_UP:
                //    Keyboard.OnKeyUp(sdlEvent->key);
                //    UIManager.KeyboardFocusControl?.InvokeKeyUp((SDL_Keycode)sdlEvent->key.key, sdlEvent->key.mod);
                //    Scene.OnKeyUp(sdlEvent->key);
                //    Plugin.ProcessHotkeys(0, 0, false);
                //    if ((SDL_Keycode)sdlEvent->key.key == SDL_Keycode.SDLK_PRINTSCREEN) TakeScreenshot();
                //    break;
                //case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                //    if (_ignoreNextTextInput) break;
                //    byte* ptr = sdlEvent->text.text;
                //    while (*ptr != 0) ptr++;
                //    var s = System.Text.Encoding.UTF8.GetString(sdlEvent->text.text, (int)(ptr - sdlEvent->text.text));
                //    if (!string.IsNullOrEmpty(s)) {
                //        UIManager.KeyboardFocusControl?.InvokeTextInput(s);
                //        Scene.OnTextInput(s);
                //    }
                //    break;
                //case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                //    //if (UO.GameCursor != null && !UO.GameCursor.AllowDrawSDLCursor) {
                //    //    UO.GameCursor.AllowDrawSDLCursor = true;
                //    //    UO.GameCursor.Graphic = 0xFFFF;
                //    //}
                //    Mouse.Update();
                //    //if (Mouse.IsDragging) if (!Scene.OnMouseDragging()) UIManager.OnMouseDragging();
                //    break;
                //case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                //    Mouse.Update();
                //    var isScrolledUp = sdlEvent->wheel.y > 0;
                //    Plugin.ProcessMouse(0, (int)sdlEvent->wheel.y);
                //    if (!Scene.OnMouseWheel(isScrolledUp)) UIManager.OnMouseWheel(isScrolledUp);
                //    break;
                //case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN: {
                //        SDL_MouseButtonEvent mouse = sdlEvent->button;
                //        // The values in MouseButtonType are chosen to exactly match the SDL values
                //        MouseButtonType buttonType = (MouseButtonType)mouse.button;
                //        var lastClickTime = 0U;
                //        switch (buttonType) {
                //            case MouseButtonType.Left: lastClickTime = Mouse.LastLeftButtonClickTime; break;
                //            case MouseButtonType.Middle: lastClickTime = Mouse.LastMidButtonClickTime; break;
                //            case MouseButtonType.Right: lastClickTime = Mouse.LastRightButtonClickTime; break;
                //            case MouseButtonType.XButton1:
                //            case MouseButtonType.XButton2: break;
                //            default: Log.Warn($"No mouse button handled: {mouse.button}"); break;
                //        }
                //        Mouse.ButtonPress(buttonType);
                //        Mouse.Update();
                //        uint ticks = Time.Ticks;
                //        if (lastClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks) {
                //            lastClickTime = 0;
                //            bool res = Scene.OnMouseDoubleClick(buttonType) || UIManager.OnMouseDoubleClick(buttonType);
                //            if (!res) {
                //                if (!Scene.OnMouseDown(buttonType)) UIManager.OnMouseButtonDown(buttonType);
                //            }
                //            else lastClickTime = 0xFFFF_FFFF;
                //        }
                //        else {
                //            if (buttonType != MouseButtonType.Left && buttonType != MouseButtonType.Right) Plugin.ProcessMouse(sdlEvent->button.button, 0);
                //            if (!Scene.OnMouseDown(buttonType)) UIManager.OnMouseButtonDown(buttonType);
                //            lastClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                //        }
                //        switch (buttonType) {
                //            case MouseButtonType.Left: Mouse.LastLeftButtonClickTime = lastClickTime; break;
                //            case MouseButtonType.Middle: Mouse.LastMidButtonClickTime = lastClickTime; break;
                //            case MouseButtonType.Right: Mouse.LastRightButtonClickTime = lastClickTime; break;
                //        }
                //        break;
                //    }
                //case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP: {
                //        SDL_MouseButtonEvent mouse = sdlEvent->button;
                //        // The values in MouseButtonType are chosen to exactly match the SDL values
                //        MouseButtonType buttonType = (MouseButtonType)mouse.button;
                //        var lastClickTime = 0U;
                //        switch (buttonType) {
                //            case MouseButtonType.Left: lastClickTime = Mouse.LastLeftButtonClickTime; break;
                //            case MouseButtonType.Middle: lastClickTime = Mouse.LastMidButtonClickTime; break;
                //            case MouseButtonType.Right: lastClickTime = Mouse.LastRightButtonClickTime; break;
                //            default: Log.Warn($"No mouse button handled: {mouse.button}"); break;
                //        }
                //        if (lastClickTime != 0xFFFF_FFFF) {
                //            //if (!Scene.OnMouseUp(buttonType) || UIManager.LastControlMouseDown(buttonType) != null) UIManager.OnMouseButtonUp(buttonType);
                //        }
                //        Mouse.ButtonRelease(buttonType);
                //        Mouse.Update();
                //        break;
                //    }
        }
        return default;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args) { Scene?.Dispose(); base.OnExiting(sender, args); }

    void TakeScreenshot() {
        //var screenshotsFolder = FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Data", "Client", "Screenshots");
        //var  path = Path.Combine(screenshotsFolder, $"screenshot_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.png");
        //var colors = new Color[GraphicManager.PreferredBackBufferWidth * GraphicManager.PreferredBackBufferHeight];
        //GraphicsDevice.GetBackBufferData(colors);
        //using var texture = new Texture2D(GraphicsDevice, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight, false, SurfaceFormat.Color);
        //using var fileStream = File.Create(path);
        //texture.SetData(colors);
        //texture.SaveAsPng(fileStream, texture.Width, texture.Height);
        //var message = string.Format(ResGeneral.ScreenshotStoredIn0, path);
        //if (ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.HideScreenshotStoredInMessage) Log.Info(message);
        //else GameActions.Print(UO.World, message, 0x44, MessageType.System);
    }
}
