from __future__ import annotations
import os, numpy as np
from pyengine3d import *
from openstk.gfx import Renderer

# typedefs
class Tiny3dGfxModel: pass
class Shader: pass
class Camera: pass

#region Tiny3dTextureRenderer

# Tiny3dTextureRenderer
class Tiny3dTextureRenderer(Renderer):
    gfx: Tiny3dGfxModel
    obj: object
    tex: int
    frameDelay: int = 0

    def __init__(self, gfx: Tiny3dGfxModel, obj: object):
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

#region Tiny3dObjectRenderer

# Tiny3dObjectRenderer
class Tiny3dObjectRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

#endregion

#region Tiny3dMaterialRenderer

# Tiny3dMaterialRenderer
class Tiny3dMaterialRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

#endregion

#region Tiny3dGridRenderer

# Tiny3dGridRenderer
class Tiny3dGridRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

#endregion

#region Tiny3dParticleRenderer

# Tiny3dParticleRenderer
class Tiny3dParticleRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

#endregion

#region Tiny3dCellRenderer

# Tiny3dCellRenderer
class Tiny3dCellRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

#endregion

#region Tiny3dWorldRenderer

# Tiny3dWorldRenderer
class Tiny3dWorldRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

#endregion

#region Tiny3dTestTriRenderer

# OpenGLTestTriRenderer
class Tiny3dTestTriRenderer(Renderer):
    def __init__(self, gfx: Tiny3dGfxModel, obj: object): pass

    def start(self):
        scene = self.scene = base.loader.loadModel('models/environment')
        scene.reparentTo(base.render)
        scene.setScale(0.25, 0.25, 0.25)
        scene.setPos(-8, 42, 0)
        # self.scene = self.loader.loadModel('teapot')
        # self.scene.reparentTo(self.render)

#endregion
