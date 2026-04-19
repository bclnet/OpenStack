from __future__ import annotations
import os, re
from numpy import ndarray
from importlib import resources
from OpenGL.GL import *
from openstk.core import _throw, CoroutineQueue, CellManager, CellBuilder
from openstk.gfx import Shader

#RenderSunShadows: bool = True
#AmbientIntensity: float = 1.5
DesiredWorkTimePerFrame: float = 1.0 / 200
#CellRadiusOnLoad: int = 2

class OpenGLOpenEngine:
    def __init__(self, manager: callable, sunCycle: bool = False):
        if not manager: raise Exception('manager')
        self.queue: CoroutineQueue = CoroutineQueue()
        self.cellManager: CellManager = manager(self.queue) or _throw('manager')
        self.query = self.cellManager.query

    def dispose(self) -> None: pass

    def update(self) -> None:
        if not self.playerCamera: return
        # The current cell can be null if the player is outside of the defined game world.
        #if not self.currentCell or not self.currentCell.isInterior: self.cellManager.updateCells(self.playerCamera.transform.position.fromUnity(), self.currentWorld)
        self.queue.run(DesiredWorkTimePerFrame)

    #region Player Spawn

    currentWorld: int = 0
    currentCell: ICell = None
    #playerTransform: Transform
    #playerComponent: PlayerComponent
    playerCamera: object = None

    def createPlayer(self, playerPrefab: object, position: Vector3) -> (object, object):
        return (1, None)
        #if not playerPrefab: throw new InvalidOperationException("playerPrefab missing")
        #player = GameObject.FindWithTag("Player")
        #if not player: { player = GameObject.Instantiate(playerPrefab); player.name = "Player"; }
        #player.transform.position = position
        #PlayerTransform = player.GetComponent<Transform>()
        #cameraInPlayer = player.GetComponentInChildren<Camera>() ?? throw new InvalidOperationException("Player:Camera missing")
        #playerCamera = cameraInPlayer.gameObject
        #PlayerComponent = player.GetComponent<PlayerComponent>()
        #return player

    # Spawns the player inside using the cell's grid coordinates.
    def spawnPlayer(self, playerPrefab: object, position: Vector3, update: bool = False):
        cellId = self.query.getCellId(position, self.currentWorld)
        self.currentCell = self.query.findCell(cellId)
        assert(self.currentCell)
        player, self.playerCamera = self.createPlayer(playerPrefab, position)
        if update:
            #self.cellManager.updateCells(self.playerCamera.transform.position.fromUnity(), self.currentWorld, True, CellRadiusOnLoad)
            self.onExteriorCell(self.currentCell)
        else:
            cell = self.cellManager.beginCell(cellId)
            if not cell: return
            self.queue.waitFor(cell.task)
            if cellId.z != -1: self.onExteriorCell(self.currentCell)
            else: self.onInteriorCell(self.currentCell)

    def onExteriorCell(self, cell: ICell): pass

    def onInteriorCell(self, cell: ICell): pass

    #endregion
