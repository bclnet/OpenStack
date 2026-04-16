using OpenStack.Gfx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using static OpenStack.CellManager;

namespace OpenStack;

#region Database

public interface IDatabase {
    object Convert(object s);
    object Query(object s);
}

#endregion

#region CellManager

public abstract class CellManager(IQuery query, CoroutineQueue queue, Func<ICell, ILand, object, object, IEnumerator> taskFunc) {
    const int DefaultRadius = 4;
    const int DetailRadius = 3;

    public class Cell(object cellObj, object contObj, ICell record, IEnumerator task) {
        public object CellObj = cellObj;
        public object ContObj = contObj;
        public ICell Record = record;
        public IEnumerator Task = task;
    }

    public class CellRef {
        public object Obj;
        public object Record;
        public string ModelPath;
    }

    public interface ICell {
        uint Id { get; }
        bool IsInterior { get; }
        Int3 GridId { get; }
        string EDID { get; }
    }

    public interface ILand {
        Int3 GridId { get; }
        ushort[] VTEX { get; }
    }

    public interface ILtex {
        long INTV { get; }
        string ICON { get; }
    }

    public interface ILigh { }

    public interface IQuery {
        Int3 GetCellId(Vector3 point, int world);
        ICell FindCell(Int3 cell);
        ICell FindCellByName(string name, int id, int world);
        ILand FindLand(Int3 cell);
        ILtex FindLtex(int index);
    }

    public IQuery Query = query;
    public CoroutineQueue Queue = queue;
    public Func<ICell, ILand, object, object, IEnumerator> TaskFunc = taskFunc;
    public Dictionary<Int3, Cell> Cells = [];

    public abstract (object, object) GfxCreateContainers(string name);
    public abstract void GfxSetVisible(object container, bool visible);

    //: StartCreatingCell
    public Cell BeginCell(Int3 point) {
        var record = Query.FindCell(point);
        if (record == null) return null;
        var cell = BuildCell(record);
        Cells[point.Z != -1 ? point : Int3.Zero] = cell;
        return cell;
    }

    //: StartCreatingCellByName
    public Cell BeginCellByName(string name, int id, int world = -1) {
        if (world != -1) throw new ArgumentOutOfRangeException("world");
        var record = Query.FindCellByName(name, id, world);
        if (record == null) return null;
        var cell = BuildCell(record);
        Cells[Int3.Zero] = cell;
        return cell;
    }

    //: UpdateCells
    public void UpdateCells(Vector3 position, int world = -1, bool immediate = false, int radius = -1) {
        var point = Query.GetCellId(position, world);
        if (radius < 0) radius = DefaultRadius;
        int minX = point.X - radius, maxX = point.X + radius, minY = point.Y - radius, maxY = point.Y + radius;

        // destroy out of range cells
        var outOfRange = new List<Int3>();
        foreach (var s in Cells.Keys) if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY) outOfRange.Add(s);
        foreach (var s in outOfRange) DestroyCell(s);

        // create new cells
        for (var r = 0; r <= radius; r++)
            for (var x = minX; x <= maxX; x++)
                for (var y = minY; y <= maxY; y++) {
                    var p = new Int3(x, y, world);
                    var d = Math.Max(Math.Abs(point.X - p.X), Math.Abs(point.Y - p.Y));
                    if (d == r && !Cells.ContainsKey(p)) { var cell = BeginCell(p); if (cell != null && immediate) Queue.WaitFor(cell.Task); }
                }

        // update LODs
        foreach (var (p, cell) in Cells) { var d = Math.Max(Math.Abs(point.X - p.X), Math.Abs(point.Y - p.Y)); GfxSetVisible(cell.ContObj, d <= DetailRadius); }
    }

    //: StartInstantiatingCell
    Cell BuildCell(ICell cell) {
        Debug.Assert(cell != null);
        string cellName;
        ILand land = null;
        if (!cell.IsInterior) { cellName = $"cell {cell.GridId}"; land = Query.FindLand(cell.GridId); }
        else cellName = cell.EDID;
        var (contObj, cellObj) = GfxCreateContainers(cellName);
        var task = TaskFunc(cell, land, contObj, cellObj);
        Queue.Add(task);
        return new Cell(contObj, cellObj, cell, task);
    }

    void DestroyCell(Int3 point) {
        if (Cells.TryGetValue(point, out var s)) { Queue.Cancel(s.Task); Cells.Remove(point); /*Object.Destroy(s.Obj);*/ }
        else Log.Error("Tried to destroy a cell that isn't created.");
    }

    public void DestroyAllCells() {
        foreach (var s in Cells.Values) { Queue.Cancel(s.Task); /*Object.Destroy(s.Obj);*/ }
        Cells.Clear();
    }
}

public abstract class CellBuilder<Object, MaterialBuilderBase, TextureBuilderBase, Shader> {
    const string DefaultLandTexturePath = "textures/_land_default.dds";
    protected IOpenGfxModel<Object, MaterialBuilderBase, TextureBuilderBase, Shader> GfxModel;
    protected IQuery Query;

    protected abstract object GfxCreateLight(ILigh light, bool indoors);

    /// <summary>
    /// A coroutine that instantiates the terrain for, and all objects in, a cell.
    /// </summary>
    IEnumerator CellCoroutine(ICell cell, ILand land, object contObj, object cellObj) {
        // Start pre-loading all required textures for the terrain.
        if (land != null) {
            var landTextures = GetLandTextures(land);
            if (landTextures != null)
                foreach (var landTexture in landTextures) GfxModel.PreloadTexture(landTexture);
            yield return null;
        }

        // Extract information about referenced objects.
        var refs = GetCellRefs(cell); yield return null;

        // Start pre-loading all required files for referenced objects. The NIF manager will load the textures as well.
        foreach (var r in refs) if (r.ModelPath != null) GfxModel.PreloadObject(r.ModelPath);
        yield return null;

        // Instantiate terrain.
        if (land != null) {
            var task = LandCoroutine(land, cellObj);
            while (task.MoveNext()) yield return null;
            yield return null;
        }

        // Instantiate objects.
        foreach (var r in refs) { CellObject(cell, contObj, r); yield return null; }
    }

    CellRef[] GetCellRefs(ICell cell) {
        return [];

        //if (_data.Format != GameFormatId.TES3) return [];
        //var refCellObjInfos = new CellRef[cell.RefObjs.Count];
        //for (var i = 0; i < cell.RefObjs.Count; i++) {
        //    var r = new CellRef { Obj = cell.RefObjs[i] };
        //    // Get the record the RefObjDataGroup references.
        //    var refObj = (CELLRecord.RefObj)r.RefObj;
        //    _data._MANYsById.TryGetValue(refObj.EDID.Value, out r.ReferencedRecord);
        //    if (r.Record != null) {
        //        var modelFileName = (r.ReferencedRecord is IHaveMODL modl ? modl.MODL.Value : null);
        //        // If the model file name is valid, store the model file path.
        //        if (!string.IsNullOrEmpty(modelFileName))
        //            r.ModelFilePath = "meshes\\" + modelFileName;
        //    }
        //    refCellObjInfos[i] = r;
        //}
        //return refCellObjInfos;
    }

    /// <summary>
    /// Instantiates an object in a cell. Called by InstantiateCellObjectsCoroutine after the object's assets have been pre-loaded.
    /// </summary>
    void CellObject(ICell cell, object parent, CellRef r) {
        if (r.Record == null) { Log.Info("Unknown Object: ((CELLRecord.RefObj)r.Obj).EDID"); return; }
        object modelObj = null;
        // If the object has a model, instantiate it.
        if (r.ModelPath != null) { modelObj = GfxModel.CreateObject(r.ModelPath, parent); PostCellObject(modelObj, r); }
        // If the object has a light, instantiate it.
        if (r.Record is ILigh record) {
            var lightObj = GfxCreateLight(record, cell.IsInterior);
            // If the object also has a model, parent the model to the light.
            if (modelObj != null) GfxModel.AttachObject(AttachObjectMethod.Find, lightObj, modelObj, "AttachLight");
            // If the light has no associated model, instantiate the light as a standalone object.
            else { PostCellObject(lightObj, r); GfxModel.AttachObject(AttachObjectMethod.Transform, lightObj, parent); }
        }
    }

    

    /// <summary>
    /// Finishes initializing an instantiated cell object.
    /// </summary>
    protected void PostCellObject(object gameObject, CellRef r) {
        //    var refObj = (CELLRecord.RefObj)refCellObjInfo.RefObj;
        //    // Handle object transforms.
        //    if (refObj.XSCL != null) gameObject.transform.localScale = Vector3.one * refObj.XSCL.Value.Value;
        //    gameObject.transform.position += NifUtils.NifPointToUnityPoint(refObj.DATA.Position.ToVector3());
        //    gameObject.transform.rotation *= NifUtils.NifEulerAnglesToUnityQuaternion(refObj.DATA.EulerAngles.ToVector3());
        //    var tagTarget = gameObject;
        //    var coll = gameObject.GetComponentInChildren<Collider>(); // if the collider is on a child object and not on the object with the component, we need to set that object's tag instead.
        //    if (coll != null) tagTarget = coll.gameObject;
        //    ProcessObjectType<DOORRecord>(tagTarget, refCellObjInfo, "Door");
        //    ProcessObjectType<ACTIRecord>(tagTarget, refCellObjInfo, "Activator");
        //    ProcessObjectType<CONTRecord>(tagTarget, refCellObjInfo, "ContObj");
        //    ProcessObjectType<LIGHRecord>(tagTarget, refCellObjInfo, "Light");
        //    ProcessObjectType<LOCKRecord>(tagTarget, refCellObjInfo, "Lock");
        //    ProcessObjectType<PROBRecord>(tagTarget, refCellObjInfo, "Probe");
        //    ProcessObjectType<REPARecord>(tagTarget, refCellObjInfo, "RepairTool");
        //    ProcessObjectType<WEAPRecord>(tagTarget, refCellObjInfo, "Weapon");
        //    ProcessObjectType<CLOTRecord>(tagTarget, refCellObjInfo, "Clothing");
        //    ProcessObjectType<ARMORecord>(tagTarget, refCellObjInfo, "Armor");
        //    ProcessObjectType<INGRRecord>(tagTarget, refCellObjInfo, "Ingredient");
        //    ProcessObjectType<ALCHRecord>(tagTarget, refCellObjInfo, "Alchemical");
        //    ProcessObjectType<APPARecord>(tagTarget, refCellObjInfo, "Apparatus");
        //    ProcessObjectType<BOOKRecord>(tagTarget, refCellObjInfo, "Book");
        //    ProcessObjectType<MISCRecord>(tagTarget, refCellObjInfo, "MiscObj");
        //    ProcessObjectType<CREARecord>(tagTarget, refCellObjInfo, "Creature");
        //    ProcessObjectType<NPC_Record>(tagTarget, refCellObjInfo, "NPC");
    }

    //void ProcessObjectType<RecordType>(GameObject gameObject, RefCellObjInfo info, string tag) where RecordType : Record {
    //    var record = info.ReferencedRecord;
    //    if (record is RecordType) {
    //        var obj = GameObjectUtils.FindTopLevelObject(gameObject);
    //        if (obj == null) return;
    //        //var component = GenericObjectComponent.Create(obj, record, tag);
    //        ////only door records need access to the cell object data group so far
    //        //if (record is DOORRecord)
    //        //    ((DoorComponent)component).RefObj = info.RefObj;
    //    }
    //}

    List<string> GetLandTextures(ILand land) {
        // Don't return anything if the LAND doesn't have height data or texture data.
        if (land.VTEX == null) return null;
        var textureFilePaths = new List<string>();
        var indices = land.VTEX.Distinct().ToList();
        for (var i = 0; i < indices.Count; i++) {
            var textureIndex = ((short)indices[i] - 1);
            if (textureIndex < 0) { textureFilePaths.Add(DefaultLandTexturePath); continue; }
            var ltex = Query.FindLtex(textureIndex);
            var textureFilePath = ltex.ICON;
            textureFilePaths.Add(textureFilePath);
        }
        return textureFilePaths;
    }

    /// <summary>
    /// Creates terrain representing a LAND record.
    /// </summary>
    IEnumerator LandCoroutine(ILand land, object parent) {
        //    Debug.Assert(land != null);
        //    // Don't create anything if the LAND doesn't have height data.
        //    if (land.VHGT.HeightData == null) yield break;
        //    // Return before doing any work to provide an IEnumerator handle to the coroutine.
        //    yield return null;
        //    const int LAND_SIDELENGTH_IN_SAMPLES = 65;
        //    var heights = new float[LAND_SIDELENGTH_IN_SAMPLES, LAND_SIDELENGTH_IN_SAMPLES];
        //    // Read in the heights in Morrowind units.
        //    const int VHGTIncrementToUnits = 8;
        //    var rowOffset = land.VHGT.ReferenceHeight;
        //    for (var y = 0; y < LAND_SIDELENGTH_IN_SAMPLES; y++) {
        //        rowOffset += land.VHGT.HeightData[y * LAND_SIDELENGTH_IN_SAMPLES];
        //        heights[y, 0] = rowOffset * VHGTIncrementToUnits;
        //        var colOffset = rowOffset;
        //        for (var x = 1; x < LAND_SIDELENGTH_IN_SAMPLES; x++) {
        //            colOffset += land.VHGT.HeightData[(y * LAND_SIDELENGTH_IN_SAMPLES) + x];
        //            heights[y, x] = colOffset * VHGTIncrementToUnits;
        //        }
        //    }
        //    // Change the heights to percentages.
        //    heights.GetExtrema(out var minHeight, out var maxHeight);
        //    for (var y = 0; y < LAND_SIDELENGTH_IN_SAMPLES; y++)
        //        for (var x = 0; x < LAND_SIDELENGTH_IN_SAMPLES; x++)
        //            heights[y, x] = Utils.ChangeRange(heights[y, x], minHeight, maxHeight, 0, 1);

        //    // Texture the terrain.
        //    SplatPrototype[] splatPrototypes = null;
        //    const int LAND_TEXTUREINDICES = 256;
        //    var textureIndices = land.VTEX != null ? land.VTEX.Value.TextureIndicesT3 : new ushort[LAND_TEXTUREINDICES];
        //    // Create splat prototypes.
        //    var splatPrototypeList = new List<SplatPrototype>();
        //    var texInd2SplatInd = new Dictionary<ushort, int>();
        //    for (var i = 0; i < textureIndices.Length; i++) {
        //        var textureIndex = (int)(textureIndices[i] - 1);
        //        if (!texInd2SplatInd.ContainsKey((ushort)textureIndex)) {
        //            // Load terrain texture.
        //            string textureFilePath;
        //            if (textureIndex < 0)
        //                textureFilePath = _defaultLandTextureFilePath;
        //            else {
        //                var LTEX = _data.FindLTEXRecord(textureIndex);
        //                textureFilePath = LTEX.ICON.Value;
        //            }
        //            var texture = _asset.LoadTexture(textureFilePath);
        //            // Yield after loading each texture to avoid doing too much work on one frame.
        //            yield return null;
        //            // Create the splat prototype.
        //            var splat = new SplatPrototype {
        //                texture = texture,
        //                smoothness = 0,
        //                metallic = 0,
        //                tileSize = new Vector2(6, 6)
        //            };
        //            // Update collections.
        //            var splatIndex = splatPrototypeList.Count;
        //            splatPrototypeList.Add(splat);
        //            texInd2SplatInd.Add((ushort)textureIndex, splatIndex);
        //        }
        //    }
        //    splatPrototypes = splatPrototypeList.ToArray();

        //    // Create the alpha map.
        //    var VTEX_ROWS = 16;
        //    var VTEX_COLUMNS = VTEX_ROWS;
        //    float[,,] alphaMap = null;
        //    alphaMap = new float[VTEX_ROWS, VTEX_COLUMNS, splatPrototypes.Length];
        //    for (var y = 0; y < VTEX_ROWS; y++) {
        //        var yMajor = y / 4;
        //        var yMinor = y - (yMajor * 4);
        //        for (var x = 0; x < VTEX_COLUMNS; x++) {
        //            var xMajor = x / 4;
        //            var xMinor = x - (xMajor * 4);
        //            var texIndex = ((short)textureIndices[(yMajor * 64) + (xMajor * 16) + (yMinor * 4) + xMinor] - 1);
        //            if (texIndex >= 0) { var splatIndex = texInd2SplatInd[(ushort)texIndex]; alphaMap[y, x, splatIndex] = 1; }
        //            else alphaMap[y, x, 0] = 1;
        //        }
        //    }

        // Yield before creating the terrain GameObject because it takes a while.
        yield return null;

        //    // Create the terrain.
        //    var heightRange = maxHeight - minHeight;
        //    var terrainPosition = new Vector3(ConvertUtils.ExteriorCellSideLengthInMeters * land.GridId.x, minHeight / ConvertUtils.MeterInUnits, ConvertUtils.ExteriorCellSideLengthInMeters * land.GridId.y);
        //    var heightSampleDistance = ConvertUtils.ExteriorCellSideLengthInMeters / (LAND_SIDELENGTH_IN_SAMPLES - 1);
        //    var terrain = GameObjectUtils.CreateTerrain(-1, heights, heightRange / ConvertUtils.MeterInUnits, heightSampleDistance, splatPrototypes, alphaMap, terrainPosition);
        //    terrain.GetComponent<Terrain>().materialType = Terrain.MaterialType.BuiltInLegacyDiffuse;
        //    terrain.transform.parent = parent.transform;
        //    terrain.isStatic = true;
    }
}

#endregion

//public class PlayerManager() {}