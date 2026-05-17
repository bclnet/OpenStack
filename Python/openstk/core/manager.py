from __future__ import annotations
import os, math
from numpy import ndarray, array, zeros
from openstk.core.core import ISource
from openstk.core.poly.pool import CoroutineQueue
from openstk.core.poly.poly import Int3, Float3, Float4
from openstk.core.poly.system import getExtrema, changeRange
import openstk.core.poly.log as log
from openstk.gfx.gfx import GfX, GfxTerrainLayer, GfxAttach
from openstk.sys.drawing import Color

# types
type Vector2 = ndarray
type Vector3 = ndarray

# typedefs
class Object: pass
class Texture: pass
class Material: pass

# IDatabase
class IDatabase:
    def convert(self, src: object) -> object: pass
    def query(self, src: object) -> list[object]: pass

# ICellDatabase
class ICellDatabase:
    archive: ISource
    query: IQuery
    cellId: Int3
    cellInterior: bool
    playerPostion: Vector3
    playerRotation: quaternion

# CellManager
class CellManager:
    class Cell:
        def __init__(self, obj: object, objectsObj: object, record: object, task: Enumerator):
            self.obj = obj
            self.objectsObj = objectsObj
            self.record = record
            self.task = task
   
    class CellRef:
        def __init__(self, obj: ICellXref, record: object, modelPath: str):
            self.obj = obj
            self.record = record
            self.modelPath = modelPath

    class ICellXref:
        name: str
        scale: float
        position: Float3
        eulerAngles: Float3

    class ICellXrefModel:
        modelPath: str

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
        radius: list[int]
        def setWorld(self, world: int) -> None: pass
        def getCellId(self, point: Vector3) -> Int3: pass
        def findAnyByName(self, name: str) -> object: pass
        def findCell(self, cell: Int3) -> ICell: pass
        def findCellByName(self, name: str) -> ICell: pass
        def findLand(self, cell: Int3) -> ILand: pass
        def findLtex(self, index: int) -> ILtex: pass

    def __init__(self, query: IQuery, queue: CoroutineQueue, builder: CellBuilderX):
        self.query: IQuery = query
        self.queue: CoroutineQueue = queue
        self.builder: CellBuilderX = builder
        self.cells: dict[Int3, Cell] = {}
        self.radius: int = query.radius[0]
        self.radius2: int = query.radius[1]

    def beginCell(self, point: Int3) -> Cell:
        record = self.query.findCell(point)
        if not record: return None
        cell = self.buildCell(record); self.cells[point] = cell
        return cell

    def beginCellByName(self, name: str) -> Cell:
        record = self.query.findCellByName(name)
        if not record: return None
        cell = self.buildCell(record); self.cells[Int3.zero] = cell
        return cell
    
    def updateCells(self, position: Vector3, immediate: bool = False, radius: int = -1) -> None:
        if radius < 0: radius = self.radius
        point = self.query.getCellId(position)
        minX = point.x - radius; maxX = point.x + radius; minY = point.y - radius; maxY = point.y + radius

        # destroy out of range cells
        outOfRange: list[Int3] = []
        for s in self.cells.keys():
            if s.x < minX or s.x > maxX or s.y < minY or s.y > maxY: outOfRange.append(s)
        for s in outOfRange: self.destroyCell(s)

        # create new cells
        world = self.query.world
        for r in range(radius + 1):
            for x in range(minX, maxX + 1):
                for y in range(minY, maxY + 1):
                    p = Int3(x, y, world); d = max(abs(point.x - p.x), abs(point.y - p.y))
                    if d == r and p not in self.cells:
                        cell = self.beginCell(p)
                        if cell and immediate: self.queue.waitFor(cell.task)

        # update LODs
        for p, cell in self.cells.items(): d = max(abs(point.x - p.x), abs(point.y - p.y)); self.builder.setVisible(cell.objectsObj, d <= self.radius2)

    def buildCell(self, cell: ICell) -> CellManager.Cell:
        assert(cell)
        cellName: str
        land: ILand = None
        if not cell.isInterior: cellName = f'cell {cell.gridId}'; land = self.query.findLand(cell.gridId)
        else: cellName = cell.name
        (objectsObj, obj) = self.builder.createContainers(cellName)
        task = self.builder.coroutine(cell, land, objectsObj, obj); self.queue.add(task)
        return CellManager.Cell(objectsObj, obj, cell, task)

    def destroyCell(self, point: Int3) -> None:
        if point in self.cells: s = self.cells[point]; self.queue.cancel(s.task); self.builder.destroy(s.obj); self.cells.pop(point)
        else: log.error('Tried to destroy a cell that is not created.')

    def destroyAllCells(self) -> None:
        for s in self.cells.values(): self.queue.cancel(s.task); self.builder.destroy(s.obj)
        self.cells.clear()

# CellBuilder
class CellBuilderX:
    def createContainers(self, name: str) -> tuple[object, object]: pass
    def setVisible(self, src: object, visible: bool) -> None: pass
    def coroutine(self, cell: ICell, land: ILand, obj: object, objectsObj: object) -> Enumerator: pass

# CellBuilder
class CellBuilder(CellBuilderX):
    defaultLandTexturePath: str = 'textures/_land_default.dds'
    terrainLayers: dict[Texture, GfxTerrainLayer[Texture]] = {}

    def __init__(self, source: ISource, query: IQuery, gfx: list[IOpenGfx]):
        self.source = source
        self.query = query
        self.gfxApi = gfx[GfX.XApi]
        self.gfxModel = gfx[GfX.XModel]
        self.gfxLight = gfx[GfX.XLight]
        self.gfxTerrain = gfx[GfX.XTerrain]
        self.meterInUnits = query.meterInUnits
        self.cellLengthInMeters = query.cellLengthInMeters

    def createContainers(self, name: str) -> tuple[object, object]:
        obj = self.gfxApi.createObject(name, 'Cell')
        objectsObj = self.gfxApi.createObject('objects', parent=obj)
        return (obj, objectsObj)

    def setVisible(self, src: object, visible: bool) -> None: self.gfxApi.setVisible(src, visible)
    def destroy(self, src: object) -> None: self.gfxApi.destroy(src)

    # A coroutine that instantiates the terrain for, and all objects in, a cell.
    def coroutine(self, cell: ICell, land: ILand, obj: object, objectsObj: object) -> IEnumerator:
        if not cell and not land: return
        cellRefs = self.getCellRefs(cell)
        if land and self.gfxTerrain: yield None; self.createLand(land, obj); yield None
        for s in cellRefs: self.createCell(cell, objectsObj, s); yield None
        if self.gfxLight: self.createReflectionProbe(cell, obj)

    def getCellRefs(self, cell: ICell) -> list[CellRef]:
        @staticmethod
        def _(s):
            record = self.query.findAnyByName(s.name)
            modelPath = record.modelPath if record and isinstance(record, CellManager.ICellXrefModel) else None
            return CellManager.CellRef(obj=s, record=record, modelPath=modelPath)
        return [_(s) for s in cell.Xrefs]

    # Instantiates an object in a cell. Called by InstantiateCellObjectsCoroutine after the object's assets have been pre-loaded.
    async def createCell(self, cell: ICell, parent: object, r: CellRef) -> None:
        if not r.record: return #log.info(f'Unknown Object: {r.obj.name}'); return
        modelObj: object = None; obj = r.obj
        if r.modelPath: modelObj, _ = await self.gfxModel.createObject(self.source, r.modelPath, True); self.gfxModel.postObject(modelObj, obj.position, obj.eulerAngles, obj.scale, parent)
        if self.gfxLight and isinstance(r.record, CellManager.ILigh):
            ligh = r.record
            s = self.gfxLight.createLight('Light', None, ligh.radius, ligh.lightColor, cell.isInterior)
            if modelObj: self.gfxApi.attach(GfxAttach.Find, s, [modelObj, 'AttachLight'])
            else: self.gfxModel.postObject(s, obj.position, obj.eulerAngles, obj.scale, parent)

    def getLandTextures(self, land: ILand) -> list[str]:
        vtex = land.vtex
        if not land.heights or not vtex: return None
        paths = []
        indexs = list(set(vtex))
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

    async def createLand(self, land: ILand, parent: object):
        heights = land.heights
        if not heights: return

        # Read in the heights in Morrowind units.
        newHeights = zeros((CellBuilder.LAND_SIDELENGTH_IN_SAMPLES, CellBuilder.LAND_SIDELENGTH_IN_SAMPLES), dtype=float) 
        rowOffset = land.heightOffset
        for y in range(CellBuilder.LAND_SIDELENGTH_IN_SAMPLES):
            rowOffset += heights[y * CellBuilder.LAND_SIDELENGTH_IN_SAMPLES]
            newHeights[y, 0] = rowOffset * CellBuilder.VHGTIncrementToUnits
            colOffset = rowOffset
            for x in range(1, CellBuilder.LAND_SIDELENGTH_IN_SAMPLES):
                colOffset += heights[(y * CellBuilder.LAND_SIDELENGTH_IN_SAMPLES) + x]
                newHeights[y, x] = colOffset * CellBuilder.VHGTIncrementToUnits

        # Change the heights to percentages.
        minHeight, maxHeight = getExtrema(newHeights)
        for y in range(CellBuilder.LAND_SIDELENGTH_IN_SAMPLES):
            for x in range(CellBuilder.LAND_SIDELENGTH_IN_SAMPLES):
                newHeights[y, x] = changeRange(newHeights[y, x], minHeight, maxHeight, 0, 1)

        # Texture the terrain.
        textureManager = self.gfxModel.textureManager
        indexs = land.vtex or [0]*CellBuilder.LAND_TEXTUREINDICES; layers = []; layerIndexs = {}
        for i in range(len(indexs)):
            index = indexs[i] - 1
            if index in layerIndexs: continue
            # Load terrain texture.
            path = self.query.findLtex(index).path if index >= 0 else CellBuilder.defaultLandTexturePath
            tex, _ = await self.gfxModel.createTexture(self.source, path)
            layer = CellBuilder.terrainLayers.get(tex)
            if not layer:
                layer = GfxTerrainLayer[Texture](
                    texture=tex,
                    smoothness=.3,
                    metallic=.2,
                    specular=Color.black,
                    tileSize=array([6, 6]))
                layer.maskMapTexture = textureManager.createSolidTexture(1, 1, [layer.metallic, .0, .0, layer.smoothness])
                layer.normalMapTexture = textureManager.createNormalMapTexture(tex)
                CellBuilder.terrainLayers[tex] = layer
            layerIndex = len(layers); layers.append(layer); layerIndexs[index] = layerIndex
        newlayers = layers

        # Create the alpha map.
        alphaMap = zeros((CellBuilder.VTEX_ROWS, CellBuilder.VTEX_COLUMNS, len(newlayers)))
        for y in range(CellBuilder.VTEX_ROWS):
            yMajor = y // 4; yMinor = y - (yMajor * 4)
            for x in range(CellBuilder.VTEX_COLUMNS):
                xMajor = x // 4; xMinor = x - (xMajor * 4)
                texIndex = indexs[(yMajor * 64) + (xMajor * 16) + (yMinor * 4) + xMinor] - 1
                alphaMap[y, x, layerIndexs[texIndex] if texIndex >= 0 else 0] = 1
    
        # Create the terrain.
        heightRange = (maxHeight - minHeight) / self.meterInUnits
        position = array([land.gridId.y * self.cellLengthInMeters, land.gridId.y * self.cellLengthInMeters, minHeight / self.meterInUnits])
        sampleDistance = self.cellLengthInMeters / (CellBuilder.LAND_SIDELENGTH_IN_SAMPLES - 1)
        data = self.gfxTerrain.createTerrainData(-1, newHeights, heightRange, sampleDistance, newlayers, alphaMap)
        self.gfxTerrain.createTerrain('terrain', position, data, parent)
        # _terrainError: 5
        # _treeDistance: 80

    def createReflectionProbe(self, cell: ICell, parent: Object) -> None:
        if cell.isInterior: return
        gridId = cell.gridId
        position = array([gridId.x * self.cellLengthInMeters, .0, gridId.y * self.cellLengthInMeters])
        self.gfxLight.createReflectionProbe('probe', position, parent)


# A coroutine that instantiates the terrain for, and all objects in, a cell.
# def coroutine(self, cell: ICell, land: ILand, obj: object, objectsObj: object) -> IEnumerator:
#     if not cell and not land: return

#     # Extract information about referenced objects.
#     cellRefs = self.getCellRefs(cell)

#     # Start pre-loading all required textures for the terrain.
#     if land:
#         landTextures = self.getLandTextures(land)
#         if landTextures:
#             for landTexture in landTextures: self.gfxModel.preloadTexture(landTexture)
#         yield None

#     # Start pre-loading all required files for referenced objects. The NIF manager will load the textures as well.
#     for s in cellRefs:
#         if s.modelPath: self.gfxModel.preloadObject(s.modelPath)
#     yield None

#     # Extract information about referenced objects.
#     cellRefs = self.getCellRefs(cell); yield None

#     # Start pre-loading all required files for referenced objects. The NIF manager will load the textures as well.
#     for s in cellRefs:
#         if s.modelPath: self.gfxModel.preloadObject(s.modelPath)
#     yield None

#     # Instantiate terrain.
#     if land:
#         task = self.landCoroutine(land, obj)
#         while task.moveNext(): yield None
#         yield None

#     # Instantiate objects.
#     for s in cellRefs: self.cellObject(cell, objectsObj, s); yield None