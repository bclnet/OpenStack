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
    const int CellRadiusOnLoad = 2;
    static Color DefaultAmbientColor = new(137, 140, 160, 255);
    //public static UnityOpenEngine Current;
    readonly IQuery Query;
    readonly CellManager CellManager;
    readonly CoroutineQueue Queue = new();
    readonly GameObject SunObj;

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

        //// water
        //Water = GameObject.Instantiate(TesGame.instance.WaterPrefab);
        //Water.SetActive(false);
        //var water = Water.GetComponent<Water>();
        //water.waterMode = game.instance.WaterQuality;
        //if (!TesGame.instance.WaterBackSideTransparent)
        //{
        //    var side = Water.transform.GetChild(0);
        //    var m = side.GetComponent<Renderer>().sharedMaterial;
        //    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        //    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        //    m.SetInt("_ZWrite", 1);
        //    m.DisableKeyword("_ALPHATEST_ON");
        //    m.DisableKeyword("_ALPHABLEND_ON");
        //    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        //    m.renderQueue = -1;
        //}

        //Cursor.SetCursor(Asset.LoadTexture("tx_cursor", 1), Vector2.zero, CursorMode.Auto);
    }

    public void Dispose() { } // Query.Dispose();

    public virtual void Update() {
        if (PlayerCamera == null) return;
        // The current cell can be null if the player is outside of the defined game world.
        if (Cell == null || !Cell.IsInterior) CellManager.UpdateCells(PlayerCamera.transform.position.FromUnity());
        Queue.Run(DesiredWorkTimePerFrame);
    }

    #region Player Spawn

    protected int World;
    protected ICell Cell;
    protected Transform PlayerTransform;
    protected PlayerComponent PlayerComponent;
    protected GameObject PlayerCamera;

    //GameObject Water;
    //UnderwaterEffect UnderwaterEffect;

    GameObject GfxCreatePlayer(GameObject playerPrefab, Vector3 position, out GameObject playerCamera) {
        if (playerPrefab == null) throw new InvalidOperationException("PlayerPrefab missing");
        var player = GameObject.FindWithTag("Player");
        if (player == null) { player = GameObject.Instantiate(playerPrefab); player.name = "Player"; }
        player.transform.position = position;
        PlayerTransform = player.GetComponent<Transform>();
        var cameraInPlayer = player.GetComponentInChildren<Camera>() ?? throw new InvalidOperationException("Player: Camera missing");
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
    public void SpawnPlayer(GameObject playerPrefab, System.Numerics.Vector3 position, bool update = false) {
        var cellId = Query.GetCellId(position);
        Cell = Query.FindCell(cellId);
        Debug.Assert(Cell != null);
        GfxCreatePlayer(playerPrefab, position.ToUnity(), out PlayerCamera);
        if (update) {
            CellManager.UpdateCells(PlayerCamera.transform.position.FromUnity(), true, CellRadiusOnLoad);
            OnCell(Cell);
        }
        else {
            var cell = CellManager.BeginCell(cellId);
            Queue.WaitFor(cell.Task);
            OnCell(Cell);
        }
    }

    protected virtual void OnCell(ICell cell) {
        if (cell.IsInterior) {
            if (cell.AmbientLight != null) RenderSettings.ambientLight = cell.AmbientLight.Value.ToUnity();
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
        else {
            RenderSettings.ambientLight = DefaultAmbientColor;
            SunObj.SetActive(true);
            //Water.transform.position = Vector3.zero;
            //Water.SetActive(true);
            //UnderwaterEffect.enabled = true;
            //UnderwaterEffect.Level = 0.0f;
        }
    }

    #endregion
}
