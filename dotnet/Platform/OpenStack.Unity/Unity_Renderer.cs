using OpenStack.Gfx;
using UnityEngine;
using Renderer = OpenStack.Gfx.Renderer;

namespace OpenStack.Unity.Renderers;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer(IUnityGfx3d gfx, object obj) : Renderer
{
    readonly IUnityGfx3d Gfx = gfx;
}

#endregion

#region CellRenderer

//static Estate Estate = EstateManager.GetEstate("Tes");
//static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Morrowind.bsa#Morrowind")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Bloodmoon.bsa#Morrowind")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Tribunal.bsa#Morrowind")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Oblivion.bsa#Oblivion")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Skyrim.esm#SkyrimVR")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Fallout4.esm#Fallout4")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("Fallout4.esm#Fallout4VR")));

public class CellRenderer(IUnityGfx3d gfx, object obj) : Renderer
{
    readonly IUnityGfx3d Gfx = gfx;
    readonly object Obj = obj;

    public override void Start()
    {
        //TestLoadCell(new Vector3(((-2 << 5) + 1) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, ((-1 << 5) + 1) * ConvertUtils.ExteriorCellSideLengthInMeters));
        //TestLoadCell(new Vector3((-1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, (-1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters));
        //TestLoadCell(new Vector3(0 * ConvertUtils.ExteriorCellSideLengthInMeters, 0, 0 * ConvertUtils.ExteriorCellSideLengthInMeters));
        //TestLoadCell(new Vector3((1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, (1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters));
        //TestLoadCell(new Vector3((1 << 5) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, (1 << 5) * ConvertUtils.ExteriorCellSideLengthInMeters));
        //TestAllCells();
    }

    //public static Int3 GetCellId(Vector3 point, int world) => new Int3(Mathf.FloorToInt(point.x / ConvertUtils.ExteriorCellSideLengthInMeters), Mathf.FloorToInt(point.z / ConvertUtils.ExteriorCellSideLengthInMeters), world);

    //static void TestLoadCell(Vector3 position)
    //{
    //    var cellId = GetCellId(position, 60);
    //    var cell = DatFile.FindCellRecord(cellId);
    //    var land = ((TesDataPack)DatFile).FindLANDRecord(cellId);
    //    Log($"LAND #{land?.Id}");
    //}

    //static void TestAllCells()
    //{
    //    var cells = ((TesDataPack)DatFile).GroupByLabel["CELL"].Records;
    //    Log($"CELLS: {cells.Count}");
    //    foreach (var record in cells.Cast<CELLRecord>())
    //        Log(record.EDID.Value);
    //}
}

#endregion

#region EngineRenderer

public class EngineRenderer(IUnityGfx3d gfx, object obj) : Renderer
{
    readonly IUnityGfx3d Gfx = gfx;
    readonly object Obj = obj;

    //object Engine;
    GameObject PlayerPrefab = GameObject.Find("Player00");

    //public override void Dispose()
    //{
    //    base.Dispose();
    //    //Engine?.Dispose();
    //}

    public override void Start()
    {
        //var assetUri = new Uri("http://192.168.1.3/ASSETS/Morrowind/Morrowind.bsa#Morrowind");
        //var dataUri = new Uri("http://192.168.1.3/ASSETS/Morrowind/Morrowind.esm#Morrowind");

        //var assetUri = new Uri("game:/Morrowind.bsa#Morrowind");
        //var dataUri = new Uri("game:/Morrowind.esm#Morrowind");

        ////var assetUri = new Uri("game:/Oblivion*#Oblivion");
        ////var dataUri = new Uri("game:/Oblivion.esm#Oblivion");

        //Engine = new SimpleEngine(TesEstateHandler.Handler, assetUri, dataUri);

        //// engine
        //Engine.SpawnPlayer(PlayerPrefab, new Vector3(-137.94f, 2.30f, -1037.6f)); // new Int3(-2, -9)

        // engine - oblivion
        //Engine.SpawnPlayer(PlayerPrefab, new Int3(0, 0, 60), new Vector3(0, 0, 0));
    }

    //public override void Update() => Engine?.Update();
}

#endregion

#region ObjectRenderer

public class ObjectRenderer(IUnityGfx3d gfx, object obj) : Renderer
{
    readonly IUnityGfx3d Gfx = gfx;
    readonly object Obj = obj;

    public override void Start()
    {
        var path = Obj is string z ? z : null;
        if (!string.IsNullOrEmpty(path)) MakeObject(path);
    }

    void MakeObject(object path) => Gfx.ObjectManager.CreateObject(path);
}

#endregion

#region TextureRenderer

// game:/Morrowind.bsa#Morrowind
// http://192.168.1.3/ASSETS/Morrowind/Morrowind.bsa#Morrowind
// game:/Skyrim*#SkyrimVR
// game:/Fallout4*#Fallout4VR

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer(IUnityGfx3d gfx, object obj) : Renderer
{
    readonly IUnityGfx3d Gfx = gfx;
    readonly object Obj = obj;

    public override void Start()
    {
        var path = Obj is string z ? z : null;
        if (!string.IsNullOrEmpty(path)) MakeTexture(path);
        //if (!string.IsNullOrEmpty(path)) MakeCursor(path);
    }

    GameObject MakeTexture(string path)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        obj.transform.rotation = Quaternion.Euler(-90f, 180f, -180f);
        var meshRenderer = obj.GetComponent<MeshRenderer>();
        (meshRenderer.material, _) = Gfx.MaterialManager.CreateMaterial(new MaterialPropStandard { MainPath = path });
        return obj;
    }

    void MakeCursor(string path) => Cursor.SetCursor(Gfx.TextureManager.CreateTexture(path).tex, Vector2.zero, CursorMode.Auto);
}

#endregion
