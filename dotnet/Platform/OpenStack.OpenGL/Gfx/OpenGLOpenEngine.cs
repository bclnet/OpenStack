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
    protected ICell Cell;
    //protected Transform PlayerTransform;
    //protected PlayerComponent PlayerComponent;
    protected object PlayerCamera;

    public OpenGLOpenEngine(Func<CoroutineQueue, CellManager> manager, bool sunCycle = false) {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        CellManager = manager(Queue) ?? throw new ArgumentNullException(nameof(manager));
        Query = CellManager.Query;
    }

    public void Dispose() { } // Query.Dispose();

    public virtual void Update() {
        if (PlayerCamera == null) return;
        // The current cell can be null if the player is outside of the defined game world.
        //if (Cell == null || !Cell.IsInterior) CellManager.UpdateCells(PlayerCamera.transform.position.FromUnity(), World);
        Queue.Run(DesiredWorkTimePerFrame);
    }

    #region Player Spawn

    object CreatePlayer(object playerPrefab, Vector3 position, out object playerCamera) {
        playerCamera = 1;
        return null;
        //if (playerPrefab == null) throw new InvalidOperationException("playerPrefab missing");
        //var player = GameObject.FindWithTag("Player");
        //if (player == null) { player = GameObject.Instantiate(playerPrefab); player.name = "Player"; }
        //player.transform.position = position;
        //PlayerTransform = player.GetComponent<Transform>();
        //var cameraInPlayer = player.GetComponentInChildren<Camera>() ?? throw new InvalidOperationException("Player:Camera missing");
        //playerCamera = cameraInPlayer.gameObject;
        //PlayerComponent = player.GetComponent<PlayerComponent>();
        //return player;
    }

    /// <summary>
    /// Spawns the player inside using the cell's grid coordinates.
    /// </summary>
    /// <param name="playerPrefab">The player prefab.</param>
    /// <param name="position">The target position of the player.</param>
    public void SpawnPlayer(object playerPrefab, Vector3 position, bool update = false) {
        var cellId = Query.GetCellId(position);
        Cell = Query.FindCell(cellId);
        Debug.Assert(Cell != null);
        CreatePlayer(playerPrefab, position, out PlayerCamera);
        if (update) {
            //CellManager.UpdateCells(PlayerCamera.transform.position.FromUnity(), true, CellRadiusOnLoad);
            OnCell(Cell);
        }
        else {
            var cell = CellManager.BeginCell(cellId);
            if (cell == null) return;
            Queue.WaitFor(cell.Task);
            OnCell(Cell);
        }
    }

    protected virtual void OnCell(ICell cell) {
    }

    #endregion
}
