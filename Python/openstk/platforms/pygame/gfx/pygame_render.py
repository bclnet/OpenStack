from __future__ import annotations
import os
import pygame
# from pygame.locals import *
from openstk.core import ISource
from openstk.gfx import GfX, Renderer

# typedefs
class PygameGfxModel: pass
class IOpenGfx: pass

#region TestTriRenderer

# TestTriRenderer
class TestTriRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], source: ISource, obj: object, surf: object): pass

#endregion

#region TextureRenderer

# TextureRenderer
class TextureRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], source: ISource, obj: object, surf: object, level: range):
        self.gfxModel: OpenGLGfxModel = gfx[GfX.XModel]
        self.source: ISource = source
        self.obj: object = obj
        self.surf: object = surf
        self.level: range = level
        self.gfxModel.textureManager.deleteTexture(source, obj)
        self.tex: int = asyncio.run(self.gfxModel.textureManager.createTexture(source, obj, self.level))[0] or -1
        self.shader, self.shaderTag = self.gfxModel.shaderManager.createShader(source, 'plane')
        self.vao: int = self._setupVao()
        self.background: bool = background
        self.boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)
        self.frameDelay: int = 0

    def start(self) -> None:
        self.x = 320 # Initial x position of the moving object
        self.dx = 5 # Speed of the moving object

    def update(self, deltaTime: float) -> None:
        w = self.surf.get_width()
        # Draw the moving object
        pygame.draw.circle(self.surf, (0, 0, 0), (self.x, 240), 30)  # Draw a black circle at the current
        # position
        self.x += self.dx # Update the position of the moving object
        if self.x + 30 > w or self.x - 30 < 0: # Check if the moving object has reached the edge of the surface
            self.dx = -self.dx # Reverse the direction of the moving object

#endregion

#region TestAnimRenderer

# TestAnimRenderer
class TestAnimRenderer(Renderer):
    def __init__(self, gfx: list[IOpenGfx], source: ISource, obj: object, surf: object):
        self.surf = surf

    def start(self) -> None:
        self.x = 320 # Initial x position of the moving object
        self.dx = 5 # Speed of the moving object

    def update(self, deltaTime: float) -> None:
        w = self.surf.get_width()
        # Draw the moving object
        pygame.draw.circle(self.surf, (0, 0, 0), (self.x, 240), 30)  # Draw a black circle at the current
        # position
        self.x += self.dx # Update the position of the moving object
        if self.x + 30 > w or self.x - 30 < 0: # Check if the moving object has reached the edge of the surface
            self.dx = -self.dx # Reverse the direction of the moving object

#endregion

