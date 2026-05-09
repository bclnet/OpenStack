using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EnginX;

public class GraphicsDevice {
}

public class GraphicsDeviceManager(Game game) {
    public Game Game = game ?? throw new ArgumentNullException("The game cannot be null");

    public GraphicsDevice Device => null;

    public virtual bool BeginDraw() => true;
    public virtual void CreateDevice() { }
    public virtual void EndDraw() { }
}

#region Game

public interface IGameObject {
    void Initialize();
}

public class Game : IDisposable {
    protected static readonly TimeSpan MaxElapsedTime = TimeSpan.FromMilliseconds(500);

    List<IGameObject> Components = new();
    public GraphicsDeviceManager DeviceManager;
    bool IsDisposed;
    bool Initialized;
    bool Running;
    bool SuppressDraw;
    // tick
    public TimeSpan TotalGameTime;
    public TimeSpan ElapsedGameTime;
    public bool IsRunningSlowly;
    protected Stopwatch GameTimer;
    protected TimeSpan AccumulatedElapsedTime;
    protected long PreviousTicks;
    bool ForceElapsedTimeToZero;
    // events
    public event EventHandler<EventArgs> Activated;
    public event EventHandler<EventArgs> Deactivated;
    public event EventHandler<EventArgs> Disposed;
    public event EventHandler<EventArgs> Exiting;

    public Game() { }
    ~Game() { Dispose(false); }

    #region Dispose

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
        Disposed?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void Dispose(bool disposing) {
        if (IsDisposed) return;
        if (disposing) {
            //for (var i = 0; i < Components.Count; i += 1) {
            //    (Components[i] as IDisposable)?.Dispose();
            //}
            //Content?.Dispose();
            //if (graphicsDeviceService != null) {
            //    (graphicsDeviceService as IDisposable).Dispose();
            //}
            //if (Window != null) {
            //    FNAPlatform.DisposeWindow(Window);
            //}
            //ContentTypeReaderManager.ClearTypeCreators();
        }
        //AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        IsDisposed = true;
    }

    [DebuggerNonUserCode]
    protected void AssertNotDisposed() {
        if (IsDisposed) {
            var name = GetType().Name;
            throw new ObjectDisposedException(name, $"The {name} object was used after being Disposed.");
        }
    }

    #endregion

    public GraphicsDevice Device => DeviceManager.Device;

    bool _isActive;
    public bool IsActive {
        get => _isActive;
        set {
            if (_isActive == value) return;
            _isActive = value;
            if (value) Activated?.Invoke(this, EventArgs.Empty);
            else Deactivated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Run() {
        AssertNotDisposed();
        if (!Initialized) { Initialize(); Initialized = true; }
        BeginRun();
        IsActive = true;
        GameTimer = Stopwatch.StartNew();
        Running = true;
        while (Running) {
            Tick();
            // Draw unless suppressed
            if (SuppressDraw) SuppressDraw = false;
            else if (BeginDraw()) { Draw(); EndDraw(); }
        }
        Exiting?.Invoke(this, EventArgs.Empty);
        EndRun();
    }

    public virtual void Tick() {
        AdvanceElapsedTime();
        if (AccumulatedElapsedTime > MaxElapsedTime) AccumulatedElapsedTime = MaxElapsedTime;
        // advance
        if (ForceElapsedTimeToZero) { ElapsedGameTime = TimeSpan.Zero; ForceElapsedTimeToZero = false; }
        else { ElapsedGameTime = AccumulatedElapsedTime; TotalGameTime += ElapsedGameTime; }
        AccumulatedElapsedTime = TimeSpan.Zero;
        AssertNotDisposed();
        Update();
    }

    TimeSpan AdvanceElapsedTime() {
        var currentTicks = GameTimer.Elapsed.Ticks;
        var timeAdvanced = TimeSpan.FromTicks(currentTicks - PreviousTicks);
        AccumulatedElapsedTime += timeAdvanced;
        PreviousTicks = currentTicks;
        return timeAdvanced;
    }

    public void Exit() {
        Running = false;
        SuppressDraw = true;
    }

    public void ResetElapsedTime() {
        ForceElapsedTimeToZero = true;
    }

    protected virtual bool BeginDraw() => DeviceManager?.BeginDraw() ?? true;

    protected virtual void EndDraw() => DeviceManager?.EndDraw();

    protected virtual void BeginRun() { }

    protected virtual void EndRun() { }

    protected virtual Task LoadContent() => Task.CompletedTask;

    protected virtual Task UnloadContent() => Task.CompletedTask;

    protected virtual void Initialize() {
        foreach (var s in Components) s.Initialize();
        LoadContent().Wait();
    }

    protected virtual void Draw() {
        //lock (drawableComponents) {
        //    for (int i = 0; i < drawableComponents.Count; i += 1) {
        //        currentlyDrawingComponents.Add(drawableComponents[i]);
        //    }
        //}
        //foreach (IDrawable drawable in currentlyDrawingComponents) {
        //    if (drawable.Visible) {
        //        drawable.Draw(gameTime);
        //    }
        //}
        //currentlyDrawingComponents.Clear();
    }

    protected virtual void Update() {
        //lock (updateableComponents) {
        //    for (int i = 0; i < updateableComponents.Count; i += 1) {
        //        currentlyUpdatingComponents.Add(updateableComponents[i]);
        //    }
        //}
        //foreach (IUpdateable updateable in currentlyUpdatingComponents) {
        //    if (updateable.Enabled) {
        //        updateable.Update(gameTime);
        //    }
        //}
        //currentlyUpdatingComponents.Clear();
        //FrameworkDispatcher.Update();
    }
}

public class FixedStepGame : Game {
    int UpdateFrameLag;
    const int PreviousSleepTimeCount = 128;
    const int SleepTimeMask = PreviousSleepTimeCount - 1;
    int SleepTimeIndex;
    TimeSpan WorstCaseSleepPrecision = TimeSpan.FromMilliseconds(1);
    readonly TimeSpan TargetElapsedTime = TimeSpan.FromTicks(166667); // 60fps
    readonly TimeSpan[] PreviousSleepTimes = [.. Enumerable.Range(0, PreviousSleepTimeCount).Select(x => TimeSpan.FromMilliseconds(1))];

    public override void Tick() {
        AdvanceElapsedTime();
        while (AccumulatedElapsedTime + WorstCaseSleepPrecision < TargetElapsedTime) { System.Threading.Thread.Sleep(1); var timeAdvancedSinceSleeping = AdvanceElapsedTime(); UpdateEstimatedSleepPrecision(timeAdvancedSinceSleeping); }
        while (AccumulatedElapsedTime < TargetElapsedTime) { System.Threading.Thread.SpinWait(1); AdvanceElapsedTime(); }
        if (AccumulatedElapsedTime > MaxElapsedTime) AccumulatedElapsedTime = MaxElapsedTime;
        // advance
        ElapsedGameTime = TargetElapsedTime;
        var stepCount = 0;
        while (AccumulatedElapsedTime >= TargetElapsedTime) {
            TotalGameTime += TargetElapsedTime;
            AccumulatedElapsedTime -= TargetElapsedTime;
            stepCount += 1;
            AssertNotDisposed();
            Update();
        }
        UpdateFrameLag += Math.Max(0, stepCount - 1);
        if (IsRunningSlowly) {
            if (UpdateFrameLag == 0) IsRunningSlowly = false;
        }
        else if (UpdateFrameLag >= 5) IsRunningSlowly = true;
        if (stepCount == 1 && UpdateFrameLag > 0) UpdateFrameLag -= 1;
        ElapsedGameTime = TimeSpan.FromTicks(TargetElapsedTime.Ticks * stepCount);
    }

    TimeSpan AdvanceElapsedTime() {
        var currentTicks = GameTimer.Elapsed.Ticks;
        var timeAdvanced = TimeSpan.FromTicks(currentTicks - PreviousTicks);
        AccumulatedElapsedTime += timeAdvanced;
        PreviousTicks = currentTicks;
        return timeAdvanced;
    }

    void UpdateEstimatedSleepPrecision(TimeSpan timeSpentSleeping) {
        var upperTimeBound = TimeSpan.FromMilliseconds(4);
        if (timeSpentSleeping > upperTimeBound) timeSpentSleeping = upperTimeBound;
        if (timeSpentSleeping >= WorstCaseSleepPrecision) WorstCaseSleepPrecision = timeSpentSleeping;
        else if (PreviousSleepTimes[SleepTimeIndex] == WorstCaseSleepPrecision) {
            var maxSleepTime = TimeSpan.MinValue;
            for (var i = 0; i < PreviousSleepTimes.Length; i += 1)
                if (PreviousSleepTimes[i] > maxSleepTime) maxSleepTime = PreviousSleepTimes[i];
            WorstCaseSleepPrecision = maxSleepTime;
        }
        PreviousSleepTimes[SleepTimeIndex] = timeSpentSleeping;
        SleepTimeIndex = (SleepTimeIndex + 1) & SleepTimeMask;
    }
}


#endregion
