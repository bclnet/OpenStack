from __future__ import annotations
import os, math
from numpy import ndarray, zeros
from openstk.core.poly.pool import CoroutineQueue
from openstk.core.poly.poly import Int3
from openstk.core.poly.system import getExtrema, changeRange
import openstk.core.poly.log as log

# types
type Vector2 = ndarray
type Vector3 = ndarray

# forwards
class Object: pass
class Texture: pass
class Material: pass

# IDatabase
class IDatabase:
    def convert(self, source: object) -> object: pass
    def query(self, source: object) -> list[object]: pass

# ICellDatabase
class ICellDatabase:
    archive: ISource
    query: IQuery
    start: Vector3

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
        def __init__(self, obj: ICellXref, record: object, modelPath: str):
            self.obj = obj
            self.record = record
            self.modelPath = modelPath

    class ICellXref:
        name: str

    class ICell:
        id: int
        isInterior: bool
        gridId: Int3
        name: str
        ambientLight: Color
        Xrefs: list[ICellXref]

    class ILand:
        gridId: Int3
        vtex: list[int]
        heightOffset: float
        heights: list[int]
    
    class ILtex:
        intv: int
        path: str
    
    class ILigh:
        radius: float
        lightColor: Color

    class IQueryFunc:
        def getQuery(self) -> IQuery: pass

    class IQuery:
        meterInUnits: float
        cellLengthInMeters: float
        def getCellId(self, point: Vector3, world: int) -> Int3: pass
        def findByName(self, name: str) -> object: pass
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
                    p = Int3(x, y, world); d = max(abs(point.x - p.x), abs(point.y - p.y))
                    if d == r and p not in self.cells:
                        cell = beginCell(p)
                        if cell and immediate: self.queue.waitFor(cell.task)

        # update LODs
        for p, cell in self.cells: d = max(abs(point.x - p.x), abs(point.y - p.y)); gfxSetVisible(cell, d <= self.detailRadius)

    def buildCell(self, cell: ICell) -> CellManager.Cell:
        cellName: str
        land: ILand = None
        if not cell.isInterior: cellName = f'cell {cell.gridId}'; land = self.query.findLand(cell.gridId)
        else: cellName = cell.name
        (contObj, cellObj) = self.gfxCreateContainers(cellName)
        task = self.taskFunc(cell, land, contObj, cellObj); self.queue.add(task)
        return CellManager.Cell(contObj, cellObj, cell, task)

    def destroyCell(self, point: Int3) -> None:
        if point in self.cells: s = self.cells[point]; self.queue.cancel(s.task); self.cells.remove(point); # Object.Destroy(s.Obj)
        else: log.error('Tried to destroy a cell that isn\'t created.')

    def destroyAllCells(self) -> None:
        for s in self.cells.values(): self.queue.cancel(s.task) # Object.Destroy(s.Obj)
        self.cells.clear()

# CellBuilder
class CellBuilder:
    defaultLandTexturePath: str = 'textures/_land_default.dds'

    class TerrainLayer:
        def __init__(self, texture: texture, tileSize: tileSize):
            self.texture: texture
            self.tileSize: tileSize

    def __init__(self, query: IQuery, gfxModel: IOpenGfxModel):
        self.query = query
        self.gfxModel = gfxModel

    def gfxCreateLight(self, light: ILigh, indoors: bool) -> Object: pass
    def gfxCreateTerrain(self, offset: int, heights: any, heightRange: float, sampleDistance: float, layers: list[TerrainLayer], alphaMap: any, position: Vector3, materialTemplate: Material, parent: Object) -> Object: pass

    # A coroutine that instantiates the terrain for, and all objects in, a cell.
    def cellCoroutine(self, cell: ICell, land: ILand, contObj: object, cellObj: object) -> IEnumerator:
        # Start pre-loading all required textures for the terrain.
        if land:
            landTextures = self.getLandTextures(land)
            if landTextures:
                for landTexture in landTextures: self.gfxModel.preloadTexture(landTexture)
            yield None

        # Extract information about referenced objects.
        cellRefs = self.getCellRefs(cell); yield None

        # Start pre-loading all required files for referenced objects. The NIF manager will load the textures as well.
        for s in cellRefs:
            if s.modelPath: self.gfxModel.preloadObject(s.modelPath)
        yield None

        # Instantiate terrain.
        if land:
            task = self.landCoroutine(land, cellObj)
            while task.moveNext(): yield None
            yield None

        # Instantiate objects.
        for s in cellRefs: self.cellObject(cell, contObj, s); yield None

    def getCellRefs(self, cell: ICell) -> list[CellRef]:
        @staticmethod
        def _(s):
            record = self.query.findByName(s.name)
            return CellRef(obj=s, record=record, modelPath=f'meshes\\{record.modelPath}' if record and isinstance(record, ICellXrefModel) and record.modelPath else None)
        return [_(s) for s in cell.Xrefs]

    # Instantiates an object in a cell. Called by InstantiateCellObjectsCoroutine after the object's assets have been pre-loaded.
    def cellObject(self, cell: ICell, parent: object, r: CellRef) -> None:
        if not r.record: log.Info(f'Unknown Object: {r.obj.name}'); return
        modelObj: object = None
        # If the object has a model, instantiate it.
        if not r.modelPath: modelObj = self.gfxModel.createObject(r.modelPath, parent); self.postCellObject(modelObj, r)
        # If the object has a light, instantiate it.
        if isinstance(r.record, ILigh):
            record = r.record
            lightObj = self.gfxCreateLight(record, cell.isInterior)
            # If the object also has a model, parent the model to the light.
            if modelObj: self.gfxModel.attachObject(AttachObjectMethod.Find, lightObj, modelObj, 'AttachLight')
            # If the light has no associated model, instantiate the light as a standalone object.
            else: self.postCellObject(lightObj, r); self.gfxModel.attachObject(AttachObjectMethod.Transform, lightObj, parent)

    def postCellObject(self, gameObject: object, r: CellRef) -> None:
        pass

    def getLandTextures(self, land: ILand) -> list[str]:
        if not land.vtex: return None
        paths = []
        indexs = list(set(land.vtex))
        for i in range(len(indexs)):
            index = indexs[i] - 1
            if index < 0: paths.append(CellBuilder.defaultLandTexturePath); continue
            ltex = self.query.findLtex(index)
            paths.append(ltex.path)
        return paths

    LAND_SIDELENGTH_IN_SAMPLES: int = 65
    VHGTIncrementToUnits: int = 8
    LAND_TEXTUREINDICES: int = 256
    VTEX_ROWS: int = 16
    VTEX_COLUMNS: int = VTEX_ROWS

    def landCoroutine(self, land: ILand, parent: object):
        heights = land.height
        if not heights: return
        yield None # Return before doing any work to provide an IEnumerator handle to the coroutine.

        # Read in the heights in Morrowind units.
        newHeights = zeros((LAND_SIDELENGTH_IN_SAMPLES, LAND_SIDELENGTH_IN_SAMPLES), dtype=float) 
        rowOffset = land.heightOffset
        for y in range(LAND_SIDELENGTH_IN_SAMPLES):
            rowOffset += heights[y * LAND_SIDELENGTH_IN_SAMPLES]
            newHeights[y, 0] = rowOffset * VHGTIncrementToUnits
            colOffset = rowOffset
            for x in range(1, LAND_SIDELENGTH_IN_SAMPLES):
                colOffset += heights[(y * LAND_SIDELENGTH_IN_SAMPLES) + x]
                newHeights[y, x] = colOffset * VHGTIncrementToUnits

        # Change the heights to percentages.
        minHeight, maxHeight = getExtrema(newHeights)
        for y in range(LAND_SIDELENGTH_IN_SAMPLES):
            for x in range(LAND_SIDELENGTH_IN_SAMPLES):
                newHeights[y, x] = changeRange(newHeights[y, x], minHeight, maxHeight, 0, 1)

        # Texture the terrain.
        indexs = land.vtex or [0]*LAND_TEXTUREINDICES; layers = []; layerIndexs = {}
        for i in range(len(indexs)):
            index = indexs[i] - 1
            if index in layerIndexs: continue
            # Load terrain texture.
            path = self.query.findLtex(index).path if index >= 0 else CellBuilder.defaultLandTexturePath
            texture = self.gfxModel.createTexture(path)
            yield None # Yield after loading each texture to avoid doing too much work on one frame.
            # Create the splat prototype.
            layerIndex = len(layers); layers.append(TerrainLayer(texture=texture, tileSize=array([6, 6]))); layerIndexs[index] = layerIndex

        # Create the alpha map.
        alphaMap = float[VTEX_ROWS, VTEX_COLUMNS, layers.Count]
        for y in range(VTEX_ROWS):
            yMajor = y / 4; yMinor = y - (yMajor * 4)
            for x in range(VTEX_COLUMNS):
                xMajor = x / 4; xMinor = x - (xMajor * 4)
                texIndex = indexs[(yMajor * 64) + (xMajor * 16) + (yMinor * 4) + xMinor] - 1
                if texIndex >= 0: alphaMap[y, x, layerIndexs[texIndex]] = 1
                else: alphaMap[y, x, 0] = 1

        # Create the terrain.
        yield None # Yield before creating the terrain GameObject because it takes a while.
        heightRange = (maxHeight - minHeight) / MeterInUnits
        position = array([land.gridId.y * CellLengthInMeters, minHeight / MeterInUnits, land.gridId.y * CellLengthInMeters])
        sampleDistance = CellLengthInMeters / (LAND_SIDELENGTH_IN_SAMPLES - 1)
        self.gfxCreateTerrain(-1, newHeights, heightRange, sampleDistance, layers, alphaMap, position, default, parent)