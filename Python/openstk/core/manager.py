from __future__ import annotations
import os, math
from openstk.core.poly.pool import CoroutineQueue
from openstk.core.poly.poly import Int3
import openstk.core.poly.log as log

# IDatabase
class IDatabase:
    def convert(self, source: object) -> object: pass
    def query(self, source: object) -> list[object]: pass

# CellManager
class CellManager:
    cellRadius: int = 1 #4
    detailRadius: int = 1 #3

    class Cell:
        def __init__(self, cellObj: object, contObj: object, record: object, task: Enumerator):
            self.cellObj = cellObj
            self.contObj = contObj
            self.record = record
            self.task = task
   
    class CellRef:
        def __init__(self, obj: object, record: object, modelPath: str):
            self.obj = obj
            self.record = record
            self.modelPath = modelPath

    class ICell:
        id: int
        isInterior: bool
        GridId: Int3
        EDID: str

    class ILand:
        GridId: Int3
        VTEX: object
    
    class ILtex:
        INTV: int
        ICON: str
    
    class ILigh:
        radius: float
        lightColor: Color

    class IQuery:
        def getCellId(self, point: Vector3, world: int) -> Int3: pass
        def findCell(self, cell: Int3) -> ICell: pass
        def findCellByName(self, name: str, id: int, world: int) -> ICell: pass
        def findLand(self, cell: Int3) -> ILand: pass
        def findLtex(self, index: int) -> ILtex: pass

    def __init__(self, query: IQuery, queue: CoroutineQueue, taskFunc: callable):
        self.query = query
        self.queue = queue
        self.taskFunc = taskFunc
        self.cells: dict[Int3, Cell] = {}

        def gfxCreateContainers(self, name: str) -> name: pass
        def gfxSetVisible(self, cont: str) -> name: pass

        def beginCell(self, point: Int3) -> Cell:
            record = self.query.findCell(point)
            if not record: return None
            cell = self.buildCell(record); self.cells[point if point.z != -1 else Int3.zero] = cell
            return cell

        def beginCellByName(self, name: str, id: int, world: int = -1) -> Cell:
            record = self.query.findCellByName(name, id, world)
            if not record: return None
            cell = self.buildCell(record); self.cells[Int3.zero] = cell
            return cell
        
        def updateCells(self, position: Vector3, world: int = -1, immediate: bool = False, radius: int = -1) -> None:
            point = self.query.getCellId(position, world)
            if radius < 0: radius = CellManager.defaultRadius
            minX = point.x - radius; maxX = point.x + radius; minY = point.y - radius; maxY = point.y + radius

            # destroy out of range cells
            outOfRange = []
            for s in self.cells.keys():
                if s.x < minX or s.x > maxX or s.y < minY or s.y > maxY: outOfRange.append(s)
            for s in outOfRange: self.destroyCell(s)

            # create new cells
            for r in range(radius + 1):
                for x in range(minX, maxX + 1):
                    for y in range(minY, maxY + 1):
                        p = Int3(x, y, world); d = math.max(math.abs(point.x - p.x), math.abs(point.y - p.y))
                        if d == r and p not in self.cells:
                            cell = beginCell(p)
                            if cell and immediate: self.queue.waitFor(cell.task)

            # update LODs
            for p, cell in self.cells: d = math.max(math.abs(point.x - p.x), math.abs(point.y - p.y)); gfxSetVisible(cell, d <= self.detailRadius)

    def buildCell(self, cell: ICell) -> Cell:
        # Debug.assert(cell != null)
        cellName: str
        land: ILand = None
        if not cell.isInterior: cellName = f'cell {cell.gridId}'; land = self.query.findLand(cell.gridId)
        else: cellName = cell.EDID
        (contObj, cellObj) = gfxCreateContainers(cellName)
        task = self.taskFunc(cell, land, contObj, cellObj); self.queue.add(task)
        return Cell(contObj, cellObj, cell, task)

    def destroyCell(self, point: Int3) -> None:
        if point in self.cells: s = self.cells[point]; self.queue.cancel(s.task); self.cells.remove(point); # Object.Destroy(s.Obj)
        else: log.error('Tried to destroy a cell that isn\'t created.')

    def destroyAllCells(self) -> None:
        for s in self.cells.values(): self.queue.cancel(s.task) # Object.Destroy(s.Obj)
        self.cells.clear()

class CellBuilder:
    defaultLandTexturePath: str = 'textures/_land_default.dds'
    gfxModel: IOpenGfxModel 
    query: IQuery

    def gfxCreateLight(self, light: ILigh, indoors: bool) -> object: pass

    # A coroutine that instantiates the terrain for, and all objects in, a cell.
    def cellCoroutine(cell: ICell, land: ILand, contObj: object, cellObj: object) -> IEnumerator:
        # Start pre-loading all required textures for the terrain.
        if land:
            landTextures = self.getLandTextures(land)
            if landTextures:
                for landTexture in landTextures: self.gfxModel.preloadTexture(landTexture)
            yield None

        # Extract information about referenced objects.
        refs = getCellRefs(cell); yield None

        # Start pre-loading all required files for referenced objects. The NIF manager will load the textures as well.
        for r in refs:
            if r.modelPath: self.gfxModel.preloadObject(r.modelPath)
        yield None

        # Instantiate terrain.
        if land:
            task = landCoroutine(land, cellObj)
            while task.moveNext(): yield None
            yield None

        # Instantiate objects.
        for r in refs: cellObject(cell, contObj, r); yield None

    def getCellRefs(self, cell: ICell) -> list[CellRef]:
        return []

    # Instantiates an object in a cell. Called by InstantiateCellObjectsCoroutine after the object's assets have been pre-loaded.
    def cellObject(self, cell: ICell, parent: object, r: CellRef) -> None:
        if not r.record: log.Info('Unknown Object: ((CELLRecord.RefObj)r.Obj).EDID'); return
        modelObj: object = None
        # If the object has a model, instantiate it.
        if not r.modelPath: modelObj = self.gfxModel.createObject(r.modelPath, parent); postCellObject(modelObj, r)
        # If the object has a light, instantiate it.
        if isinstance(r.record, ILigh):
            record = r.record
            lightObj = self.gfxCreateLight(record, cell.isInterior)
            # If the object also has a model, parent the model to the light.
            if modelObj: self.gfxModel.attachObject(AttachObjectMethod.Find, lightObj, modelObj, 'AttachLight')
            # If the light has no associated model, instantiate the light as a standalone object.
            else: postCellObject(lightObj, r); GfxModel.AttachObject(AttachObjectMethod.Transform, lightObj, parent)

    def postCellObject(self, gameObject: object, r: CellRef) -> None:
        pass

    def getLandTextures(self, land: ILand) -> list[str]:
        if not land.VTEX: return None
        paths = []
        indexs = land.VTEX.distinct()
        for i in range(len(indexs)):
            index = indexs[i] - 1
            if index < 0: paths.append(CellBuilder.defaultLandTexturePath); continue
            ltex = self.query.findLtex(index)
            paths.append(ltex.ICON)
        return paths

    def landCoroutine(self, land: ILand, parent: object):
        pass

    # pointFactor: float = .5
    # def getPoint(self, position: Vector3, world: int = -1) -> Point3D: return Point3D(position.x // CellManager.pointFactor), position.z // CellManager.pointFactor, world)
