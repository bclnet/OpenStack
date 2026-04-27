using OpenStack.Gfx.Unity.Components;
using System;
using System.Collections;
using UnityEngine;
using static OpenStack.CellManager;

namespace OpenStack.Gfx.Unity;

public class UnityOpenEngine : IDisposable {
    const bool RenderSunShadows = true;
    const float AmbientIntensity = 1.5f;
    const float DesiredWorkTimePerFrame = 1.0f / 200;
    //const int CellRadiusOnLoad = 2;
    //static Color DefaultAmbientColor = new(137, 140, 160, 255);
    //public static UnityOpenEngine Current;
    readonly IQuery Query;
    readonly CellManager CellManager;
    readonly CoroutineQueue Queue = new();
    readonly GameObject SunObj;
    protected int World;
    protected Transform PlayerTransform;
    protected Transform CameraTransform;
    protected Func<GameObject> PlayerPrefabFunc;

    public UnityOpenEngine(Func<CoroutineQueue, CellManager> manager, bool sunCycle = false) {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        CellManager = manager(Queue) ?? throw new ArgumentNullException(nameof(manager));
        Query = CellManager.Query;

        // ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = AmbientIntensity;

        // sun
        SunObj = GameObjectX.CreateDirectionalLight(Vector3.zero, Quaternion.Euler(new Vector3(50, 330, 0)));
        SunObj.GetComponent<Light>().shadows = RenderSunShadows ? LightShadows.Soft : LightShadows.None;
        SunObj.SetActive(false);
        if (sunCycle) SunObj.AddComponent<SunCycleComponent>();

        //Cursor.SetCursor(Asset.LoadTexture("tx_cursor", 1), Vector2.zero, CursorMode.Auto);
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
        if (_cell == null || !_cell.IsInterior) CellManager.UpdateCells(CameraTransform.position.FromUnity());
        Queue.Run(DesiredWorkTimePerFrame);
    }

    void CreatePlayer(Vector3 position, Quaternion rotation) {
        var player = GameObject.FindWithTag("Player");
        if (player == null && PlayerPrefabFunc != null) { player = GameObject.Instantiate(PlayerPrefabFunc()); player.name = "Player"; }
        PlayerTransform = player.transform;
        PlayerTransform.position = position;
        PlayerTransform.rotation = rotation;
        CameraTransform = player.GetComponentInChildren<Camera>().transform;
    }

    /// <summary>
    /// Spawns the player inside using the cell's grid coordinates.
    /// </summary>
    /// <param name="playerPrefab">The player prefab.</param>
    /// <param name="position">The target position of the player.</param>
    public void SpawnPlayer(ICellDatabase db) {
        Cell = Query.FindCell(db.CellId);
        //Debug.Assert(_cell != null);
        CreatePlayer(db.PlayerPosition.ToUnity(), db.PlayerRotation.ToUnity());
        var cell = CellManager.BeginCell(db.CellId);
        Queue.WaitFor(cell.Task);
    }

    //protected virtual void OnCell(ICell cell) {
    //    if (cell.IsInterior) {
    //        if (cell.AmbientLight != null) RenderSettings.ambientLight = cell.AmbientLight.Value.ToUnity();
    //        SunObj.SetActive(false);
    //        //UnderwaterEffect.enabled = cell.WHGT != null;
    //        //if (cell.WHGT != null)
    //        //{
    //        //    var offset = 1.6f; // Interiors cells needs this offset to render at the correct location.
    //        //    Water.transform.position = new Vector3(0, (cell.WHGT.value / Convert.meterInMWUnits) - offset, 0);
    //        //    Water.SetActive(true);
    //        //    UnderwaterEffect.Level = Water.transform.position.y;
    //        //}
    //        //else Water.SetActive(false);
    //    }
    //    else {
    //        RenderSettings.ambientLight = DefaultAmbientColor;
    //        SunObj.SetActive(true);
    //        //Water.transform.position = Vector3.zero;
    //        //Water.SetActive(true);
    //        //UnderwaterEffect.enabled = true;
    //        //UnderwaterEffect.Level = 0.0f;
    //    }
    //}
}
