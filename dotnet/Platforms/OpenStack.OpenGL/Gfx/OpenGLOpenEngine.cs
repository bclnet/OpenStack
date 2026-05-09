using OpenStack.Gfx.Egin;
using System;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using static OpenStack.CellManager;

namespace OpenStack.Gfx.OpenGL;

public class OpenGLOpenEngine : IDisposable {
    const float DesiredWorkTimePerFrame = 1.0f / 200;
    readonly IQuery Query;
    readonly CellManager CellManager;
    readonly CoroutineQueue Queue = new();
    protected int World;
    //protected Transform PlayerTransform;
    public Camera Camera;

    public OpenGLOpenEngine(Func<CoroutineQueue, CellManager> manager, bool sunCycle = false) {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        CellManager = manager(Queue) ?? throw new ArgumentNullException(nameof(manager));
        Query = CellManager.Query;
    }

    public void Dispose() { } // Query.Dispose();

    ICell _cell;
    public ICell Cell {
        get => _cell;
        private set {
            if (_cell == value) return;
            _cell = value;
            CellChanged?.Invoke(_cell);
        }
    }

    public event Action<ICell> CellChanged;

    public virtual void Update() {
        // The current cell can be null if the player is outside of the defined game world.
        if (Camera != null && (_cell == null || !_cell.IsInterior)) CellManager.UpdateCells(Camera.Location);
        Queue.Run(DesiredWorkTimePerFrame);
    }

    void CreatePlayer(Vector3 position, Quaternion roation) {
    }

    /// <summary>
    /// Spawns the player inside using the cell's grid coordinates.
    /// </summary>
    /// <param name="position">The target position of the player.</param>
    /// <param name="roation">The target position of the player.</param>
    public void SpawnPlayer(ICellDatabase db) {
        Cell = Query.FindCell(db.CellId);
        Debug.Assert(Cell != null);
        CreatePlayer(db.PlayerPosition, db.PlayerRotation);
        var cell = CellManager.BeginCell(db.CellId);
        Queue.WaitFor(cell.Task);
    }
}
