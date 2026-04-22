from __future__ import annotations
import os, numpy as np
from openstk.gfx import GfX, Renderer

# typedefs
class Tiny3dGfxModel: pass
class Shader: pass
class Camera: pass
class IOpenGfx: pass

#region TestTriRenderer

# TestTriRenderer
class TestTriRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

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
    def __init__(self, gfx: list[IOpenGfx], obj: object):
        self.gfxModel: Tiny3dGfxModel = gfx[GfX.XModel]
        self.obj: object = obj
        # self.gfxModel.textureManager.deleteTexture(obj)
        # self.tex: int = self.gfxModel.textureManager.createTexture(obj, self.level)[0]
        self.frameDelay: int = 0

    def start(self):
        card = base.render.attachNewNode(CardMaker('card').generate())
        tex = loader.loadTexture('maps/noise.rgb')
        card.setTexture(tex)
        card.setScale(4.0, 4.0, 4.0)
        card.setPos(-8, 42, 0)

    # def update(self, deltaTime: float) -> None:
    #     obj: ITextureFrames = self.obj
    #     if not self.gfx or not obj or not obj.hasFrames(): return
    #     self.frameDelay += deltaTime
    #     if self.frameDelay <= obj.fps or not obj.decodeFrame(): return
    #     self.frameDelay = 0 # reset delay between frames
    #     self.gfx.textureManager.reloadTexture(obj)

#endregion

#region ObjectRenderer

# ObjectRenderer
class ObjectRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region MaterialRenderer

# MaterialRenderer
class MaterialRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region GridRenderer

# GridRenderer
class GridRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region ParticleRenderer

# ParticleRenderer
class ParticleRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region CellRenderer

# CellRenderer
class CellRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region EngineRenderer

# EngineRenderer
class EngineRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region WorldRenderer

# WorldRenderer
class WorldRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion
