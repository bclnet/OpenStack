from __future__ import annotations
import os, math
from openstk import CoroutineQueue
from openstk.core.drawing import Point3D

# CellManager
class CellManager:
    pointFactor: float = .5
    cellRadius: int = 1 #4
    detailRadius: int = 1 #3
    defaultLandTexturePath: str = 'textures/_land_default.dds'

    class Cell:
        def __init__(self, obj: object, container: object, record: object, action: Enumerator):
            self.obj = obj
            self.container = container
            self.record = record
            self.action = action
        def setVisible(self, visible: bool):
            pass
            # if visible:
            #     if not self.container.activeSelf: self.container.SetActive(True)
            # else:
            #     if self.container.activeSelf: self.container.setActive(False)

    class Reference:
        def __init__(self, obj: object, record: object, path: str):
            self.obj = obj
            self.record = record
            self.path = path

    def __init__(self, archive: Archive, queue: CoroutineQueue):
        self.archive = archive
        self.queue = queue
        self.cells: dict[int, Cell] = {}

        def getPoint(self, position: Vector3, world: int = -1) -> Point3D: return Point3D(position.x // CellManager.pointFactor), position.z // CellManager.pointFactor, world)

        def beginCell(self, point: Point3D) -> Cell:
            record = self.data.findCellRecord(point)
            if not record: return None
            cell = self.buildCell(record)
            self.cells[point if point.z != -1 else Point3D.zero] = cell
            return cell

        def beginCellByName(self, name: str, id: int, world: int = -1) -> Cell:
            record = self.data.findCellRecordByName(name, id, world)
            if not record: return None
            cell = self.buildCell(record)
            self.cells[Point3D.zero] = cell
            return cell
        
        def updateCells(self, position: Vector3, world: int = -1, immediate: bool = False, radius: int = -1) -> None:
            point = getPoint(position, world)
            if radius < 0: radius = CellManager.defaultRadius
            minX = point.x - radius, maxX = point.x + radius, minY = point.y - radius, maxY = point.y + radius

            # destroy out of range cells
            outOfRange = []
            for s in self.cells.keys():
                if s.x < minX or s.x > maxX or s.y < minY or s.y > maxY: outOfRange.append(s)
            for s in outOfRange: self.destroyCell(s)

            # create new cells
            for r in range(radius + 1):
                for s in range(minX, maxX + 1):
                    for y in range(minY, maxY + 1):
                        p = Point3D(s, y, world)
                        d = math.max(math.abs(point.x - p.x), math.abs(point.y - p.y))
                        if d == r and p not in self.cells:
                            cell = beginCell(p)
                            if cell and immediate: self.queue.waitFor(cell.action)

            # update LODs
            for p, cell in self.cells:
                d = math.max(math.abs(point.x - p.x), math.abs(point.y - p.y))
                cell.setVisible(d <= self.detailRadius)
              