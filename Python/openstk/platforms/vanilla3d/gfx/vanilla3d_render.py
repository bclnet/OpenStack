from __future__ import annotations
import os, numpy as np
from pyengine3d import *
from openstk.gfx import Renderer

# typedefs
class Vanilla3dGfxModel: pass
class Shader: pass
class Camera: pass

#region Vanilla3dTextureRenderer

# Vanilla3dTextureRenderer
class Vanilla3dTextureRenderer(Renderer):
    gfx: Vanilla3dGfxModel
    obj: object
    tex: int
    frameDelay: int = 0

    def __init__(self, gfx: Vanilla3dGfxModel, obj: object):
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

#region Vanilla3dObjectRenderer

# Vanilla3dObjectRenderer
class Vanilla3dObjectRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

#endregion

#region Vanilla3dMaterialRenderer

# Vanilla3dMaterialRenderer
class Vanilla3dMaterialRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

#endregion

#region Vanilla3dGridRenderer

# Vanilla3dGridRenderer
class Vanilla3dGridRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

#endregion

#region Vanilla3dParticleRenderer

# Vanilla3dParticleRenderer
class Vanilla3dParticleRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

#endregion

#region Vanilla3dCellRenderer

# Vanilla3dCellRenderer
class Vanilla3dCellRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

#endregion

#region Vanilla3dWorldRenderer

# Vanilla3dWorldRenderer
class Vanilla3dWorldRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

#endregion

#region Vanilla3dTestTriRenderer

# OpenGLTestTriRenderer
class Vanilla3dTestTriRenderer(Renderer):
    def __init__(self, gfx: Vanilla3dGfxModel, obj: object): pass

    def start(self):
        scene = self.scene = base.loader.loadModel('models/environment')
        scene.reparentTo(base.render)
        scene.setScale(0.25, 0.25, 0.25)
        scene.setPos(-8, 42, 0)
        # self.scene = self.loader.loadModel('teapot')
        # self.scene.reparentTo(self.render)

#endregion
