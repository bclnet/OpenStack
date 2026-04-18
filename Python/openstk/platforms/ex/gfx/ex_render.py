from __future__ import annotations
import os, numpy as np
from pyengine3d import *
from openstk.gfx import Renderer

# typedefs
class ExGfxModel: pass
class Shader: pass
class Camera: pass

#region ExTextureRenderer

# ExTextureRenderer
class ExTextureRenderer(Renderer):
    gfx: ExGfxModel
    obj: object
    tex: int
    frameDelay: int = 0

    def __init__(self, gfx: ExGfxModel, obj: object):
        self.gfx = gfx
        self.obj = obj
        # gfx.textureManager.deleteTexture(obj)
        # self.tex = gfx.textureManager.createTexture(obj, self.level)[0]

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

#region ExObjectRenderer

# ExObjectRenderer
class ExObjectRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

#endregion

#region ExMaterialRenderer

# ExMaterialRenderer
class ExMaterialRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

#endregion

#region ExGridRenderer

# ExGridRenderer
class ExGridRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

#endregion

#region ExParticleRenderer

# ExParticleRenderer
class ExParticleRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

#endregion

#region ExCellRenderer

# ExCellRenderer
class ExCellRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

#endregion

#region ExWorldRenderer

# ExWorldRenderer
class ExWorldRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

#endregion

#region ExTestTriRenderer

# OpenGLTestTriRenderer
class ExTestTriRenderer(Renderer):
    def __init__(self, gfx: ExGfxModel, obj: object): pass

    def start(self):
        scene = self.scene = base.loader.loadModel('models/environment')
        scene.reparentTo(base.render)
        scene.setScale(0.25, 0.25, 0.25)
        scene.setPos(-8, 42, 0)
        # self.scene = self.loader.loadModel('teapot')
        # self.scene.reparentTo(self.render)

#endregion
