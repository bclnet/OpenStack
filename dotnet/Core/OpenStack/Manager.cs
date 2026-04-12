using OpenStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Xml.Linq;

namespace OpenStack;

#region Database

public interface IDatabase : IDisposable {
    object Convert(object s);
    object Query(object s);
}

#endregion

#region CellManager

public class CellManager(IDatabase db, CoroutineQueue queue) {
    public int DefaultRadius = 4;
    public int DetailRadius = 3;
    public string DefaultLandTexturePath = "textures/_land_default.dds";

    public class Cell(object obj, object container, CellRecord record, IEnumerator action) {
        public object Obj = obj;
        public object Container = container;
        public CellRecord Record = record;
        public IEnumerator Action = action;
        public void SetVisible(bool visible) {
            //if (visible) { if (!Container.activeSelf) Container.SetActive(true); }
            //else { if (Container.activeSelf) Container.SetActive(false); }
        }
    }

    //public class Reference {
    //    public object Obj;
    //    public object Record;
    //    public string Path;
    //}

    public class CellRecord { public bool IsInterior; public Int3 GridId;
        internal string EDID;
    }
    public class LandRecord { }

    public class FindCell(Int3 cell) { }
    public class FindCellByName(string name, int id, int world) { }
    public class FindLand(Int3 cell) { }

    const float PointFactor = 0.5f;
    public IDatabase Db = db;
    public CoroutineQueue Queue = queue;
    public Dictionary<Int3, Cell> Cells = [];

    public Int3 GetPoint(Vector3 point, int world) => new((int)Math.Floor(point.X / PointFactor), (int)Math.Floor(point.Z / PointFactor), world);

    //: StartCreatingCell
    public Cell BeginCell(Int3 point) {
        var record = (CellRecord)Db.Query(new FindCell(point));
        if (record == null) return null;
        var cell = BuildCell(record);
        Cells[point.Z != -1 ? point : Int3.Zero] = cell;
        return cell;
    }

    //: StartCreatingCellByName
    public Cell BeginCellByName(string name, int id, int world = -1) {
        if (world != -1) throw new ArgumentOutOfRangeException("world");
        var record = (CellRecord)Db.Query(new FindCellByName(name, id, world));
        if (record == null) return null;
        var cell = BuildCell(record);
        Cells[Int3.Zero] = cell;
        return cell;
    }

    //: UpdateCells
    public void UpdateCells(Vector3 position, int world = -1, bool immediate = false, int radius = -1) {
        var point = GetPoint(position, world);
        if (radius < 0) radius = DefaultRadius;
        int minX = point.X - radius, maxX = point.X + radius, minY = point.Y - radius, maxY = point.Y + radius;

        // destroy out of range cells
        var outOfRange = new List<Point3D>();
        foreach (var s in Cells.Keys) if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY) outOfRange.Add(s);
        foreach (var s in outOfRange) DestroyCell(s);

        // create new cells
        for (var r = 0; r <= radius; r++)
            for (var x = minX; x <= maxX; x++)
                for (var y = minY; y <= maxY; y++) {
                    var p = new Int3(x, y, world);
                    var d = Math.Max(Math.Abs(point.X - p.X), Math.Abs(point.Y - p.Y));
                    if (d == r && !Cells.ContainsKey(p)) { var cell = BeginCell(p); if (cell != null && immediate) Queue.WaitFor(cell.Action); }
                }

        // update LODs
        foreach (var (p, cell) in Cells) { var d = Math.Max(Math.Abs(point.X - p.X), Math.Abs(point.Y - p.Y)); cell.SetVisible(d <= DetailRadius); }
    }

    //: StartInstantiatingCell
    Cell BuildCell(CellRecord cell) {
        Debug.Assert(cell != null);
        string cellObjName = null;
        LandRecord land = null;
        if (!cell.IsInterior) {
            cellObjName = "cell " + cell.GridId.ToString();
            land = (LandRecord)db.Query(new FindLand(cell.GridId));
        }
        else cellObjName = cell.EDID;
        var s = new GameObject(cellObjName) { tag = "Cell" };
        var c = new GameObject("objects"); c.transform.parent = s.transform;
        var r = InstantiateCellObjectsCoroutine(cell, land, s, c);
        Queue.Add(r);
        return new Cell(s, c, cell, r);
    }

    void DestroyCell(Int3 point) {
        if (Cells.TryGetValue(point, out var s)) { Queue.Cancel(s.Action); Cells.Remove(point); /*Object.Destroy(s.Obj);*/ }
        else Log.Error("Tried to destroy a cell that isn't created.");
    }

    public void DestroyAllCells() {
        foreach (var s in Cells.Values) { Queue.Cancel(s.Action); /*Object.Destroy(s.Obj);*/ }
        Cells.Clear();
    }
}

#endregion

//public class PlayerManager() {}