from __future__ import annotations
import os, numpy as np
from openstk.gfx import Renderer

# typedefs
class PyEngine3dGfxModel: pass
class Shader: pass
class Camera: pass

#region PyEngine3dTextureRenderer

# PyEngine3dTextureRenderer
class PyEngine3dTextureRenderer(Renderer):
    gfx: PyEngine3dGfxModel
    obj: object
    tex: int
    frameDelay: int = 0

    def __init__(self, gfx: PyEngine3dGfxModel, obj: object):
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

#region PyEngine3dObjectRenderer

# PyEngine3dObjectRenderer
class PyEngine3dObjectRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

#endregion

#region PyEngine3dMaterialRenderer

# PyEngine3dMaterialRenderer
class PyEngine3dMaterialRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

#endregion

#region PyEngine3dGridRenderer

# PyEngine3dGridRenderer
class PyEngine3dGridRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

#endregion

#region PyEngine3dParticleRenderer

# PyEngine3dParticleRenderer
class PyEngine3dParticleRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

#endregion

#region PyEngine3dCellRenderer

# PyEngine3dCellRenderer
class PyEngine3dCellRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

#endregion

#region PyEngine3dWorldRenderer

# PyEngine3dWorldRenderer
class PyEngine3dWorldRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

#endregion

#region PyEngine3dTestTriRenderer

# OpenGLTestTriRenderer
class PyEngine3dTestTriRenderer(Renderer):
    def __init__(self, gfx: PyEngine3dGfxModel, obj: object): pass

    def start(self):
        scene = self.scene = base.loader.loadModel('models/environment')
        scene.reparentTo(base.render)
        scene.setScale(0.25, 0.25, 0.25)
        scene.setPos(-8, 42, 0)
        # self.scene = self.loader.loadModel('teapot')
        # self.scene.reparentTo(self.render)

#endregion
