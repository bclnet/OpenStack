from __future__ import annotations
import os, numpy as np
from panda3d.core import *
from openstk.core import ISource, log, CellManager
from openstk.gfx import GfX, Renderer
from openstk.gfx.egin import AABB
from openstk.platforms.panda3d.gfx.panda3d import Panda3dCellBuilder
from openstk.platforms.panda3d.gfx.panda3dopenengine import Panda3dOpenEngine

# typedefs
class Panda3dGfxModel: pass
class Shader: pass
class Camera: pass
class Texture: pass
class IOpenGfx: pass

#region TestTriRenderer

# TestTriRenderer
class TestTriRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], source: ISource, obj: object): pass

    def start(self):
        scene = self.scene = base.loader.loadModel('models/environment')
        scene.reparentTo(base.render)
        scene.setScale(0.25, 0.25, 0.25)
        scene.setPos(-8, 42, 0)
        # self.scene = self.loader.loadModel('teapot')
        # self.scene.reparentTo(self.render)

#endregion

#region TextureRenderer

# TextureRenderer
class TextureRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], source: ISource, obj: object):
        self.gfxModel: Panda3dGfxModel = gfx[GfX.XModel]
        self.obj: object = obj
        self.boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)
        self.frameDelay: int = 0

    def start(self):
        # self.gfxModel.textureManager.deleteTexture(self.obj)
        obj = base.render.attachNewNode(CardMaker('card').generate())
        tex = self.gfxModel.textureManager.createTexture(self.obj, None)[0]
        # tex = base.loader.loadModel('maps/noise.rgb')
        obj.setTexture(tex)
        obj.setScale(8.0, 8.0, 8.0)
        obj.setPos(-8, 42, 0)

    # def update(self, deltaTime: float) -> None:
    #     obj: ITextureFrames = self.obj
    #     if not self.gfxModel or not obj or not obj.hasFrames(): return
    #     self.frameDelay += deltaTime
    #     if self.frameDelay <= obj.fps or not obj.decodeFrame(): return
    #     self.frameDelay = 0 # reset delay between frames
    #     self.gfxModel.textureManager.reloadTexture(obj)

#endregion

#region EngineRenderer

# EngineRenderer
class EngineRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], source: ISource, obj: object):
        self.gfx: list[IOpenGfx] = gfx
        self.db: ICellDatabase = obj
        self.engine: Panda3dOpenEngine
        self.playerPrefab: object = None

    def dispose(self) -> None:
        if self.engine: self.engine.dispose()

    def start(self) -> None:
        # log.info(f'db: {self.db}')
        arc = self.db.archive
        self.gfx = arc.gfx
        query = self.db.query
        self.engine = Panda3dOpenEngine(lambda queue: CellManager(query, queue, Panda3dCellBuilder(query, self.gfx)), False)
        self.engine.spawnPlayer(self.db)

    def update(self, deltaTime: float) -> None:
        if self.engine: self.engine.update()

#endregion
