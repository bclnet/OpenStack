using OpenStack.Gfx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using static OpenStack.CellManager;

namespace OpenStack;

#region Database

public interface IDatabase {
    object Convert(object src);
    object Query(object src);
}

public interface ICellDatabase {
    ISource Archive { get; }
    IQuery Query { get; }
    Vector3 Start { get; }
}

#endregion

#region CellManager

public class CellManager(IQuery query, CoroutineQueue queue, CellBuilder builder) {
    public class Cell(object cellObj, object objectsObj, ICell record, IEnumerator task) {
        public object Obj = cellObj;
        public object ObjectsObj = objectsObj;
        public ICell Record = record;
        public IEnumerator Task = task;
    }

    public class CellRef {
        public ICellXref Obj;
        public object Record;
        public string ModelPath;
    }

    public interface ICellXref {
        string Name { get; }
        float? Scale { get; }
        Vector3 Position { get; }
        Vector3 EulerAngles { get; }
    }

    public interface ICellXrefModel {
        string ModelPath { get; }
    }

    public interface ICell {
        uint Id { get; }
        bool IsInterior { get; }
        Int3 GridId { get; }
        string Name { get; }
        Color? AmbientLight { get; }
        List<ICellXref> Xrefs { get; }
    }

    public interface ILand {
        Int3 GridId { get; }
        uint[] Vtex { get; }
        float HeightOffset { get; }
        sbyte[] Heights { get; }
    }

    public interface ILtex {
        long Intv { get; }
        string Path { get; }
    }

    public interface ILigh {
        float Radius { get; }
        Color LightColor { get; }
    }

    public interface IQueryFunc {
        IQuery GetQuery();
    }

    public interface IQuery {
        float MeterInUnits { get; }
        float CellLengthInMeters { get; }
        int[] Radius { get; }
        int World { get; }
        void SetWorld(int world);
        Int3 GetCellId(Vector3 point);
        object FindAnyByName(string name);
        ICell FindCell(Int3 cell);
        ICell FindCellByName(string name);
        ILand FindLand(Int3 cell);
        ILtex FindLtex(int index);
    }

    public readonly IQuery Query = query;
    protected readonly CoroutineQueue Queue = queue;
    protected readonly CellBuilder Builder = builder;
    readonly Dictionary<Int3, Cell> Cells = [];
    readonly int Radius = query.Radius[0];
    readonly int Radius2 = query.Radius[1];

    public Cell BeginCell(Int3 point) {
        var record = Query.FindCell(point);
        if (record == null) return null;
        var cell = BuildCell(record); Cells[point] = cell;
        return cell;
    }

    public Cell BeginCellByName(string name) {
        var record = Query.FindCellByName(name);
        if (record == null) return null;
        var cell = BuildCell(record); Cells[Int3.Zero] = cell;
        return cell;
    }

    public void UpdateCells(Vector3 position, bool immediate = false, int radius = -1) {
        if (radius < 0) radius = Radius;
        var point = Query.GetCellId(position);
        int minX = point.X - radius, maxX = point.X + radius, minY = point.Y - radius, maxY = point.Y + radius;

        // destroy out of range cells
        var outOfRange = new List<Int3>();
        foreach (var s in Cells.Keys)
            if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY) outOfRange.Add(s);
        foreach (var s in outOfRange) DestroyCell(s);

        // create new cells
        var world = Query.World;
        for (var r = 0; r <= radius; r++)
            for (var x = minX; x <= maxX; x++)
                for (var y = minY; y <= maxY; y++) {
                    var p = new Int3(x, y, world); var d = Math.Max(Math.Abs(point.X - p.X), Math.Abs(point.Y - p.Y));
                    if (d == r && !Cells.ContainsKey(p)) {
                        var cell = BeginCell(p);
                        if (cell != null && immediate) Queue.WaitFor(cell.Task);
                    }
                }

        // update LODs
        foreach (var (p, cell) in Cells) { var d = Math.Max(Math.Abs(point.X - p.X), Math.Abs(point.Y - p.Y)); Builder.SetVisible(cell.ObjectsObj, d <= Radius2); }
    }

    Cell BuildCell(ICell cell) {
        Debug.Assert(cell != null);
        string cellName;
        ILand land;
        if (!cell.IsInterior) { cellName = $"cell {cell.GridId}"; land = Query.FindLand(cell.GridId); }
        else { cellName = $"cell {cell.Name}"; land = null; }
        var (obj, objectsObj) = Builder.CreateContainers(cellName);
        var task = Builder.CellCoroutine(cell, land, obj, objectsObj); Queue.Add(task);
        return new Cell(obj, objectsObj, cell, task);
    }

    void DestroyCell(Int3 point) {
        if (Cells.TryGetValue(point, out var s)) { Queue.Cancel(s.Task); Builder.Destroy(s.Obj); Cells.Remove(point); }
        else Log.Error("Tried to destroy a cell that isn't created.");
    }

    public void DestroyAllCells() {
        foreach (var s in Cells.Values) { Queue.Cancel(s.Task); Builder.Destroy(s.Obj); }
        Cells.Clear();
    }
}

public abstract class CellBuilder {
    public abstract (object, object) CreateContainers(string name);
    public abstract void SetVisible(object src, bool visible);
    public abstract IEnumerator CellCoroutine(ICell cell, ILand land, object obj, object objectsObj);
    public abstract void Destroy(object src);
}

public class CellBuilder<Object, Material, Texture, Shader>(IQuery query, IOpenGfx[] gfx) : CellBuilder {
    const string DefaultLandTexturePath = "textures/_land_default.dds";
    protected IQuery Query = query;
    protected IOpenGfxApi<Object, Material> GfxApi = (IOpenGfxApi<Object, Material>)gfx[GfX.XApi];
    protected IOpenGfxModel<Object, Material, Texture, Shader> GfxModel = (IOpenGfxModel<Object, Material, Texture, Shader>)gfx[GfX.XModel];
    protected IOpenGfxLight<Object> GfxLight = (IOpenGfxLight<Object>)gfx[GfX.XLight];
    protected IOpenGfxTerrain<Object, Material, Texture> GfxTerrain = (IOpenGfxTerrain<Object, Material, Texture>)gfx[GfX.XTerrain];
    protected float MeterInUnits = query.MeterInUnits;
    protected float CellLengthInMeters = query.CellLengthInMeters;

    public override (object, object) CreateContainers(string name) {
        var obj = GfxApi.CreateObject(name, "Cell");
        var objectsObj = GfxApi.CreateObject("objects", parent: obj);
        return (obj, objectsObj);
    }

    public override void SetVisible(object src, bool visible) => GfxApi.SetVisible((Object)src, visible);
    public override void Destroy(object src) => GfxApi.Destroy((Object)src);

    /// <summary>
    /// A coroutine that instantiates the terrain for, and all objects in, a cell.
    /// </summary>
    public override IEnumerator CellCoroutine(ICell cell, ILand land, object obj, object objectsObj) {
        // Start pre-loading all required textures for the terrain.
        if (land != null) {
            var landTextures = GetLandTextures(land);
            if (landTextures != null)
                foreach (var landTexture in landTextures) GfxModel.PreloadTexture(landTexture);
            yield return null;
        }

        // Extract information about referenced objects.
        var cellRefs = GetCellRefs(cell); yield return null;

        // Start pre-loading all required files for referenced objects. The NIF manager will load the textures as well.
        foreach (var s in cellRefs)
            if (s.ModelPath != null) GfxModel.PreloadObject(s.ModelPath);
        yield return null;

        // Instantiate terrain.
        if (land != null) {
            var task = LandCoroutine(land, (Object)obj);
            while (task.MoveNext()) yield return null;
            yield return null;
        }

        // Instantiate objects.
        foreach (var s in cellRefs) { CellObject(cell, (Object)objectsObj, s); yield return null; }
    }

    CellRef[] GetCellRefs(ICell cell) => false ? [] : [.. cell.Xrefs.Select(s => {
        var record = Query.FindAnyByName(s.Name);
        return new CellRef { Obj = s, Record = record, ModelPath = record != null && record is ICellXrefModel modl ? modl.ModelPath : null };
    })];

    /// <summary>
    /// Instantiates an object in a cell. Called by InstantiateCellObjectsCoroutine after the object's assets have been pre-loaded.
    /// </summary>
    void CellObject(ICell cell, Object parent, CellRef r) {
        if (r.Record == null) { Log.Info($"Unknown Object: {r.Obj.Name}"); return; }
        Object modelObj = default;
        var obj = r.Obj;
        if (r.ModelPath != null) { modelObj = GfxModel.CreateObject(r.ModelPath); GfxModel.PostObject(modelObj, obj.Position, obj.EulerAngles, obj.Scale, parent); }
        if (r.Record is ILigh ligh) {
            var s = GfxLight.CreateLight(ligh.Radius, ligh.LightColor, cell.IsInterior);
            if (modelObj != null) GfxApi.Attach(GfxAttach.Find, s, modelObj, "AttachLight");
            else GfxModel.PostObject(s, obj.Position, obj.EulerAngles, obj.Scale, parent);
        }
    }

    //void ProcessObjectType<RecordType>(Object gameObject, RefCellObjInfo info, string tag) where RecordType : Record {
    //    if (info.Record is RecordType r) {
    //        var obj = GameObjectUtils.FindTopLevelObject(gameObject);
    //        if (obj == null) return;
    //        //var component = GenericObjectComponent.Create(obj, record, tag);
    //        ////only door records need access to the cell object data group so far
    //        //if (record is DOORRecord)
    //        //    ((DoorComponent)component).RefObj = info.RefObj;
    //    }
    //}

    List<string> GetLandTextures(ILand land) {
        if (land.Vtex == null) return null;
        var paths = new List<string>();
        var indexs = land.Vtex.Distinct().ToArray();
        for (var i = 0; i < indexs.Length; i++) {
            var index = (int)indexs[i] - 1;
            if (index < 0) { paths.Add(DefaultLandTexturePath); continue; }
            var ltex = Query.FindLtex(index);
            paths.Add(ltex.Path);
        }
        return paths;
    }

    /// <summary>
    /// Creates terrain representing a LAND record.
    /// </summary>
    IEnumerator LandCoroutine(ILand land, Object parent) {
        const int LAND_SIDELENGTH_IN_SAMPLES = 65;
        const int VHGTIncrementToUnits = 8;
        const int LAND_TEXTUREINDICES = 256;
        const int VTEX_ROWS = 16;
        const int VTEX_COLUMNS = VTEX_ROWS;

        var heights = land.Heights;
        if (heights == null) yield break;
        yield return null; // Return before doing any work to provide an IEnumerator handle to the coroutine.

        // Read in the heights in Morrowind units.
        var newHeights = new float[LAND_SIDELENGTH_IN_SAMPLES, LAND_SIDELENGTH_IN_SAMPLES];
        var rowOffset = land.HeightOffset;
        for (var y = 0; y < LAND_SIDELENGTH_IN_SAMPLES; y++) {
            rowOffset += heights[y * LAND_SIDELENGTH_IN_SAMPLES];
            newHeights[y, 0] = rowOffset * VHGTIncrementToUnits;
            var colOffset = rowOffset;
            for (var x = 1; x < LAND_SIDELENGTH_IN_SAMPLES; x++) {
                colOffset += heights[(y * LAND_SIDELENGTH_IN_SAMPLES) + x];
                newHeights[y, x] = colOffset * VHGTIncrementToUnits;
            }
        }

        // Change the heights to percentages.
        newHeights.GetExtrema(out var minHeight, out var maxHeight);
        for (var y = 0; y < LAND_SIDELENGTH_IN_SAMPLES; y++)
            for (var x = 0; x < LAND_SIDELENGTH_IN_SAMPLES; x++)
                newHeights[y, x] = System.Polyfill.ChangeRange(newHeights[y, x], minHeight, maxHeight, 0f, 1f);

        // Texture the terrain.
        var indexs = land.Vtex ?? new uint[LAND_TEXTUREINDICES]; var layers = new List<GfxTerrainLayer<Texture>>(); var layerIndexs = new Dictionary<int, int>();
        for (var i = 0; i < indexs.Length; i++) {
            var index = (int)(indexs[i] - 1);
            if (layerIndexs.ContainsKey(index)) continue;
            // Load terrain texture.
            var path = index >= 0 ? Query.FindLtex(index).Path : DefaultLandTexturePath;
            var texture = GfxModel.CreateTexture(path);
            Log.Info($"{path}: {texture}");
            yield return null; // Yield after loading each texture to avoid doing too much work on one frame.
            // Create the splat prototype.
            var layerIndex = layers.Count; layers.Add(new GfxTerrainLayer<Texture> { Texture = texture, TileSize = new Vector2(6, 6) }); layerIndexs.Add(index, layerIndex);
        }

        // Create the alpha map.
        var alphaMap = new float[VTEX_ROWS, VTEX_COLUMNS, layers.Count];
        for (var y = 0; y < VTEX_ROWS; y++) {
            int yMajor = y / 4, yMinor = y - (yMajor * 4);
            for (var x = 0; x < VTEX_COLUMNS; x++) {
                int xMajor = x / 4, xMinor = x - (xMajor * 4);
                var texIndex = (int)indexs[(yMajor * 64) + (xMajor * 16) + (yMinor * 4) + xMinor] - 1;
                alphaMap[y, x, texIndex >= 0 ? layerIndexs[texIndex] : 0] = 1;
            }
        }

        // Create the terrain.
        yield return null; // Yield before creating the terrain GameObject because it takes a while.
        var heightRange = (maxHeight - minHeight) / MeterInUnits;
        var position = new Vector3(land.GridId.X * CellLengthInMeters, land.GridId.Y * CellLengthInMeters, minHeight / MeterInUnits);
        var sampleDistance = CellLengthInMeters / (LAND_SIDELENGTH_IN_SAMPLES - 1);
        var terrainData = GfxTerrain.CreateTerrainData(0, newHeights, heightRange, sampleDistance, [.. layers], alphaMap);
        GfxTerrain.CreateTerrain(terrainData, position, parent);
    }
}

#endregion

//public class PlayerManager() {}