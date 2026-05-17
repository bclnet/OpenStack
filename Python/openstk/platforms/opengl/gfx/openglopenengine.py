from __future__ import annotations
import os, re
from numpy import ndarray
from quaternion import quaternion
from OpenGL.GL import *
from openstk.core import _throw, AsyncCoroutineQueue, CellManager, CellBuilder
from openstk.gfx import Shader

class OpenGLOpenEngine:
    desiredWorkTimePerFrame: float = 1.0 / 200
    def __init__(self, manager: callable, sunCycle: bool = False):
        if not manager: raise Exception('manager')
        self.queue: AsyncCoroutineQueue = AsyncCoroutineQueue()
        self.cellManager: CellManager = manager(self.queue) or _throw('manager')
        self.query = self.cellManager.query
        self.world: int = 0
        self._cell: ICell = None
        #self.playerTransform: Transform
        self.camera: object = None

    def dispose(self) -> None: pass

    @property
    def cell(self) -> ICell: return self._cell
    @cell.setter
    def cell(self, value: ICell) -> None:
        if self._cell == value: return
        self._cell = value
        if self.cellChanged: self.cellChanged(self._cell)

    cellChanged: callable = None

    async def update(self) -> None:
        # The current cell can be null if the player is outside of the defined game world.
        if self.camera and (not self._cell or not self._cell.isInterior): await self.cellManager.updateCells(self.camera.location)
        await self.queue.run(OpenGLOpenEngine.desiredWorkTimePerFrame)

    def _createPlayer(self, position: Vector3, rotation: quaternion) -> None:
        self.camera = position

    # Spawns the player inside using the cell's grid coordinates.
    async def spawnPlayer(self, db: ICellDatabase):
        self.cell = self.query.findCell(db.cellId)
        assert(self._cell)
        self._createPlayer(db.playerPosition, db.playerRotation)
        cell = self.cellManager.beginCell(db.cellId)
        await self.queue.waitFor(cell.task)
