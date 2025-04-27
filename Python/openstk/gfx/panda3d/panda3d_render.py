from __future__ import annotations
import os, numpy as np
from panda3d.core import *
from openstk.gfx import Renderer

# typedefs
class Panda3dGfxModel: pass
class Shader: pass
class Camera: pass

#region Panda3dTextureRenderer

# Panda3dTextureRenderer
class Panda3dTextureRenderer(Renderer):
    gfx: Panda3dGfxModel
    obj: object
    tex: int
    frameDelay: int = 0

    def __init__(self, gfx: Panda3dGfxModel, obj: object):
        self.gfx = gfx
        self.obj = obj
        # gfx.textureManager.deleteTexture(obj)
        # self.tex = gfx.textureManager.createTexture(obj, self.level)[0]

    def start(self):
        base = self.base
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

#region Panda3dObjectRenderer

# Panda3dObjectRenderer
class Panda3dObjectRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

#endregion

#region Panda3dMaterialRenderer

# Panda3dMaterialRenderer
class Panda3dMaterialRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

#endregion

#region Panda3dGridRenderer

# Panda3dGridRenderer
class Panda3dGridRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

#endregion

#region Panda3dParticleRenderer

# Panda3dParticleRenderer
class Panda3dParticleRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

#endregion

#region Panda3dCellRenderer

# Panda3dCellRenderer
class Panda3dCellRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

#endregion

#region Panda3dWorldRenderer

# Panda3dWorldRenderer
class Panda3dWorldRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

#endregion

#region Panda3dTestTriRenderer

# OpenGLTestTriRenderer
class Panda3dTestTriRenderer(Renderer):
    def __init__(self, gfx: Panda3dGfxModel, obj: object): pass

    def start(self):
        scene = self.scene = base.loader.loadModel('models/environment')
        scene.reparentTo(base.render)
        scene.setScale(0.25, 0.25, 0.25)
        scene.setPos(-8, 42, 0)
        # self.scene = self.loader.loadModel('teapot')
        # self.scene.reparentTo(self.render)

#endregion
