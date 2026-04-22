using System.Collections.Generic;
using UnityEngine;
#pragma warning disable CS9113

namespace OpenStack.Gfx.Unity;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
}

#endregion

#region ObjectRenderer

public class ObjectRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        var path = Obj is string z ? z : null;
        if (!string.IsNullOrEmpty(path)) MakeObject(path, null);
    }

    void MakeObject(object path, GameObject parent) => GfxModel.ObjectManager.CreateObject(path, parent);
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
public class TextureRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        var path = Obj is string z ? z : null;
        if (!string.IsNullOrEmpty(path)) MakeTexture(path);
        //if (!string.IsNullOrEmpty(path)) MakeCursor(path);
    }

    GameObject MakeTexture(string path) {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        obj.transform.rotation = Quaternion.Euler(-90f, 180f, -180f);
        var meshRenderer = obj.GetComponent<MeshRenderer>();
        (meshRenderer.material, _) = GfxModel.MaterialManager.CreateMaterial(new MaterialStdProp { Textures = new Dictionary<string, string> { { "Main", path } } });
        return obj;
    }

    //void MakeCursor(string path) => Cursor.SetCursor((Texture2D)Gfx.TextureManager.CreateTexture(path).tex, Vector2.zero, CursorMode.Auto);
}

#endregion

#region EngineRenderer

//static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Morrowind.bsa#Morrowind")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Bloodmoon.bsa#Morrowind")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Tribunal.bsa#Morrowind")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Oblivion.bsa#Oblivion")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Skyrim.esm#SkyrimVR")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("game:/Fallout4.esm#Fallout4")));
////static TesUnityPakFile PakFile = new TesUnityPakFile(Estate.OpenPakFile(new Uri("Fallout4.esm#Fallout4VR")));

public class EngineRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    IOpenGfx[] Gfx = gfx;
    readonly ICellDatabase Obj = obj as ICellDatabase;

    UnityOpenEngine Engine;
    GameObject PlayerPrefab = GameObject.Find("Player0");

    public override void Dispose() { base.Dispose(); Engine?.Dispose(); }

    public override void Start() {
        //Log.Info($"PlayerPrefab: {PlayerPrefab}");
        var arc = (ISourceWithPlatform)Obj.Archive;
        Gfx = arc.Gfx;
        var query = Obj.Query;
        Engine = new UnityOpenEngine(queue => new CellManager(query, queue, new UnityCellBuilder(query, Gfx)), false);
        Engine.SpawnPlayer(PlayerPrefab, Obj.Start);
    }

    public override void Update(float deltaTime) => Engine?.Update();

    //TestLoadCell(new Vector3(((-2 << 5) + 1) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, ((-1 << 5) + 1) * ConvertUtils.ExteriorCellSideLengthInMeters));
    //TestLoadCell(new Vector3((-1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, (-1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters));
    //TestLoadCell(new Vector3(0 * ConvertUtils.ExteriorCellSideLengthInMeters, 0, 0 * ConvertUtils.ExteriorCellSideLengthInMeters));
    //TestLoadCell(new Vector3((1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, (1 << 3) * ConvertUtils.ExteriorCellSideLengthInMeters));
    //TestLoadCell(new Vector3((1 << 5) * ConvertUtils.ExteriorCellSideLengthInMeters, 0, (1 << 5) * ConvertUtils.ExteriorCellSideLengthInMeters));
    //TestAllCells();

    //static void TestLoadCell(Vector3 position) {
    //    var cellid = GetCellId(position, 60);
    //    var cell = datfile.findcellrecord(cellid);
    //    var land = ((tesdatapack)datfile).findlandrecord(cellid);
    //    Log.Info($"land #{land?.id}");
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
