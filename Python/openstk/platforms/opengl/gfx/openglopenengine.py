from __future__ import annotations
import os, re
from numpy import ndarray
from importlib import resources
from OpenGL.GL import *
from openstk.core import _throw, CoroutineQueue, CellManager, CellBuilder
from openstk.gfx import Shader


class OpenGLOpenEngine:
    desiredWorkTimePerFrame: float = 1.0 / 200
    def __init__(self, manager: callable, sunCycle: bool = False):
        if not manager: raise Exception('manager')
        self.queue: CoroutineQueue = CoroutineQueue()
        self.cellManager: CellManager = manager(self.queue) or _throw('manager')
        self.query = self.cellManager.query
        self.world: int = 0
        self.cell: ICell = None
        #self.playerTransform: Transform
        #self.playerComponent: PlayerComponent
        self.playerCamera: object = None

    def dispose(self) -> None: pass

    def update(self) -> None:
        if not self.playerCamera: return
        # The current cell can be null if the player is outside of the defined game world.
        #if not self.currentCell or not self.currentCell.isInterior: self.cellManager.updateCells(self.playerCamera.transform.position.fromUnity(), self.currentWorld)
        self.queue.run(OpenGLOpenEngine.desiredWorkTimePerFrame)

    #region Player Spawn

    def createPlayer(self, playerPrefab: object, position: Vector3) -> tuple[object, object]:
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
        cellId = self.query.getCellId(position, self.world)
        self.cell = self.query.findCell(cellId)
        assert(self.cell)
        player, self.playerCamera = self.createPlayer(playerPrefab, position)
        if update:
            #self.cellManager.updateCells(self.playerCamera.transform.position.fromUnity(), self.currentWorld, True, CellRadiusOnLoad)
            self.onCell(self.cell)
        else:
            cell = self.cellManager.beginCell(cellId)
            if not cell: return
            self.queue.waitFor(cell.task)
            self.onCell(self.cell)

    def onCell(self, cell: ICell): pass

    #endregion
