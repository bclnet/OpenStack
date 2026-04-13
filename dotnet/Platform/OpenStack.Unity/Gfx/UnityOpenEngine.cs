using OpenStack.Gfx.Unity.Components;
using System;
using System.Collections;
using UnityEngine;

namespace OpenStack.Gfx.Unity;

public class UnityOpenEngine : IDisposable {
    const bool RenderSunShadows = true;
    const float AmbientIntensity = 1.5f;
    const float DesiredWorkTimePerFrame = 1.0f / 200;
    const int CellRadiusOnLoad = 2;
    static Color DefaultAmbientColor = new(137, 140, 160, 255);
    public static UnityOpenEngine Current;

    readonly CellManager.IQuery Query;
    readonly CellManager CellManager;
    readonly CoroutineQueue Queue = new();
    readonly GameObject SunObj;

    public UnityOpenEngine(Func<CellManager.IQuery, CoroutineQueue, CellManager> manager, CellManager.IQuery query, bool sunCycle = false) {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        Query = query ?? throw new ArgumentNullException(nameof(Query));
        CellManager = manager(Query, Queue) ?? throw new ArgumentNullException(nameof(manager));

        // ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = AmbientIntensity;

        // sun
        SunObj = GameObjectX.CreateDirectionalLight(Vector3.zero, Quaternion.Euler(new Vector3(50, 330, 0)));
        SunObj.GetComponent<Light>().shadows = RenderSunShadows ? LightShadows.Soft : LightShadows.None;
        SunObj.SetActive(false);
        if (sunCycle) SunObj.AddComponent<SunCycleComponent>();

        //// water
        //Water = GameObject.Instantiate(TesGame.instance.WaterPrefab);
        //Water.SetActive(false);
        //var water = Water.GetComponent<Water>();
        //water.waterMode = game.instance.WaterQuality;
        //if (!TesGame.instance.WaterBackSideTransparent)
        //{
        //    var side = Water.transform.GetChild(0);
        //    var sideMaterial = side.GetComponent<Renderer>().sharedMaterial;
        //    sideMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        //    sideMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        //    sideMaterial.SetInt("_ZWrite", 1);
        //    sideMaterial.DisableKeyword("_ALPHATEST_ON");
        //    sideMaterial.DisableKeyword("_ALPHABLEND_ON");
        //    sideMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        //    sideMaterial.renderQueue = -1;
        //}

        //Cursor.SetCursor(Asset.LoadTexture("tx_cursor", 1), Vector2.zero, CursorMode.Auto);
    }

    public void Dispose() => Query.Dispose();

    public virtual void Update() {
        if (PlayerCamera == null) return;
        // The current cell can be null if the player is outside of the defined game world.
        if (CurrentCell == null || !CurrentCell.IsInterior) CellManager.UpdateCells(PlayerCamera.transform.position.FromUnity(), CurrentWorld);
        Queue.Run(DesiredWorkTimePerFrame);
    }

    #region Player Spawn

    protected int CurrentWorld;
    protected CellManager.ICellRecord CurrentCell;
    protected Transform PlayerTransform;
    protected PlayerComponent PlayerComponent;
    protected GameObject PlayerCamera;

    //GameObject Water;
    //UnderwaterEffect UnderwaterEffect;

    protected virtual GameObject CreatePlayer(GameObject playerPrefab, Vector3 position, out GameObject playerCamera) {
        if (playerPrefab == null) throw new InvalidOperationException("playerPrefab missing");
        var player = GameObject.FindWithTag("Player");
        if (player == null) { player = GameObject.Instantiate(playerPrefab); player.name = "Player"; }
        player.transform.position = position;
        PlayerTransform = player.GetComponent<Transform>();
        var cameraInPlayer = player.GetComponentInChildren<Camera>() ?? throw new InvalidOperationException("Player:Camera missing");
        playerCamera = cameraInPlayer.gameObject;
        PlayerComponent = player.GetComponent<PlayerComponent>();
        //UnderwaterEffect = playerCamera.GetComponent<UnderwaterEffect>();
        return player;
    }

    /// <summary>
    /// Spawns the player inside using the cell's grid coordinates.
    /// </summary>
    /// <param name="playerPrefab">The player prefab.</param>
    /// <param name="position">The target position of the player.</param>
    public void SpawnPlayer(GameObject playerPrefab, Vector3 position, bool update = false) {
        var cellId = Query.GetCellId(position, CurrentWorld);
        CurrentCell = Query.FindCellRecord(cellId);
        Debug.Assert(CurrentCell != null);
        CreatePlayer(playerPrefab, position, out PlayerCamera);
        if (update) {
            CellManager.UpdateCells(PlayerCamera.transform.position, CurrentWorld, true, CellRadiusOnLoad);
            OnExteriorCell(CurrentCell);
        }
        else {
            var cell = CellManager.BeginCell(cellId);
            Queue.WaitFor(cell.Task);
            if (cellId.Z != -1) OnExteriorCell(CurrentCell);
            else OnInteriorCell(CurrentCell);
        }
    }

    protected virtual void OnExteriorCell(object cell) {
        RenderSettings.ambientLight = DefaultAmbientColor;
        SunObj.SetActive(true);
        //Water.transform.position = Vector3.zero;
        //Water.SetActive(true);
        //UnderwaterEffect.enabled = true;
        //UnderwaterEffect.Level = 0.0f;
    }

    protected virtual void OnInteriorCell(object cell) {
        if (cell.AmbientLight != null) RenderSettings.ambientLight = cell.AmbientLight;
        SunObj.SetActive(false);
        //UnderwaterEffect.enabled = cell.WHGT != null;
        //if (cell.WHGT != null)
        //{
        //    var offset = 1.6f; // Interiors cells needs this offset to render at the correct location.
        //    Water.transform.position = new Vector3(0, (cell.WHGT.value / Convert.meterInMWUnits) - offset, 0);
        //    Water.SetActive(true);
        //    UnderwaterEffect.Level = Water.transform.position.y;
        //}
        //else Water.SetActive(false);
    }

    #endregion
}
