from __future__ import annotations
import ctypes
from numpy import array, ones, float32, identity
from enum import Enum
from OpenGL.GL import *
from openstk.core import log
from openstk.gfx import GfX, Renderer, ITextureFrames
from openstk.gfx.egin import AABB, EginRenderer
from openstk.platforms.opengl.egin import GLRenderMaterial
from openstk.platforms.opengl.gfx.opengl import OpenGLCellManager, OpenGLCellBuilder
from openstk.platforms.opengl.gfx.openglopenengine import OpenGLOpenEngine

sizeof_float = ctypes.sizeof(GLfloat)

# typedefs
class OpenGLGfxModel: pass
class Shader: pass
class Camera: pass
class IOpenGfx: pass

#region TestTriRenderer

# TestTriRenderer
class TestTriRenderer(EginRenderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object):
        self.gfxModel: OpenGLGfxModel = gfx[GfX.XModel]
        self.shader, self.shaderTag = self.gfxModel.shaderManager.createShader('testtri')
        self.vao: int = self._setupVao()
        self.boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)

    def _setupVao(self) -> int:
        glUseProgram(self.shader.program)

        # create and bind vao
        vao = glGenVertexArrays(1); glBindVertexArray(vao)
        vbo = glGenBuffers(1); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = array([
            # xyz,           :rgb
           -0.5, -0.5, 0.0,  1.0, 0.0, 0.0,
            0.5, -0.5, 0.0,  0.0, 1.0, 0.0,
            0.0,  0.5, 0.0,  0.0, 0.0, 1.0
            ], dtype=float32)
        glBufferData(GL_ARRAY_BUFFER, vertices.nbytes, vertices, GL_STATIC_DRAW)

        # attributes
        glEnableVertexAttribArray(0); glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 24, ctypes.c_void_p(0))
        glEnableVertexAttribArray(1); glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, 24, ctypes.c_void_p(12))
        glBindVertexArray(0) # unbind vao
        return vao

    def render(self, camera: Camera, passx: Pass) -> None:
        # glClear(GL_COLOR_BUFFER_BIT)
        glUseProgram(self.shader.program)
        glBindVertexArray(self.vao)
        glDrawArrays(GL_TRIANGLES, 0, 6)
        glBindVertexArray(0) # unbind vao
        glUseProgram(0) # unbind program

    def update(self, deltaTime: float) -> None: pass

#endregion

#region TextureRenderer

FACTOR = 1

# TextureRenderer
class TextureRenderer(EginRenderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object, level: range, background: bool = False):
        self.gfxModel: OpenGLGfxModel = gfx[GfX.XModel]
        self.obj: object = obj
        self.level: range = level
        self.gfxModel.textureManager.deleteTexture(obj)
        self.tex: int = self.gfxModel.textureManager.createTexture(obj, self.level)[0] or -1
        self.shader, self.shaderTag = self.gfxModel.shaderManager.createShader('plane')
        self.vao: int = self._setupVao()
        self.background: bool = background
        self.boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)
        self.frameDelay: int = 0

    def getViewport(self, size: tuple) -> tuple:
        return None if not (o := self.obj) else \
            size if o.width > 1024 or o.height > 1024 or False else (o.width << FACTOR, o.height << FACTOR)

    def _setupVao(self) -> int:
        glUseProgram(self.shader.program)
        # create and bind vao
        vao = glGenVertexArrays(1); glBindVertexArray(vao) 
        vbo = glGenBuffers(1); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = array([
            # position      :normal        :texcoord  :tangent
            -1., -1., +0.,  +0., +0., 1.,  +0., +1.,  +1., +0., +0.,
            -1., +1., +0.,  +0., +0., 1.,  +0., +0.,  +1., +0., +0.,
            +1., -1., +0.,  +0., +0., 1.,  +1., +1.,  +1., +0., +0.,
            +1., +1., +0.,  +0., +0., 1.,  +1., +0.,  +1., +0., +0.
            ], dtype = float32)
        glBufferData(GL_ARRAY_BUFFER, vertices.nbytes, vertices, GL_STATIC_DRAW)

        # attributes
        glEnableVertexAttribArray(0)
        attributes = [
            ('vPOSITION', 3),
            ('vNORMAL', 3),
            ('vTEXCOORD', 2),
            ('vTANGENT', 3)]
        offset = 0; stride = sizeof_float * sum([x[1] for x in attributes])
        for name, size in attributes:
            location = self.shader.getAttribLocation(name)
            if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, size, GL_FLOAT, GL_FALSE, stride, ctypes.c_void_p(offset))
            offset += sizeof_float * size
        glBindVertexArray(0) # unbind vao
        return vao

    def render(self, camera: Camera, passx: Pass) -> None:
        if self.background: glClearColor(255, 255, 255, 255); glClear(GL_COLOR_BUFFER_BIT)
        glUseProgram(self.shader.program)
        glBindVertexArray(self.vao)
        glEnableVertexAttribArray(0)
        if self.tex > -1: glActiveTexture(GL_TEXTURE0); glBindTexture(GL_TEXTURE_2D, self.tex)
        glDrawArrays(GL_TRIANGLE_STRIP, 0, 4)
        glBindVertexArray(0) # unbind vao
        glUseProgram(0) # unbind program

    def update(self, deltaTime: float) -> None:
        obj = self.obj if isinstance(self.obj, ITextureFrames) else None
        if not self.gfx or not obj or not obj.hasFrames(): return
        self.frameDelay += deltaTime
        if self.frameDelay <= obj.fps or not obj.decodeFrame(): return
        self.frameDelay = 0 # reset delay between frames
        self.gfxModel.textureManager.reloadTexture(obj)

#endregion

#region ObjectRenderer

# ObjectRenderer
class ObjectRenderer(EginRenderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object): pass

#endregion

#region MaterialRenderer

# MaterialRenderer
class MaterialRenderer(EginRenderer):
    def __init__(self, gfx: list[IOpenGfx], obj: object):
        self.gfxModel: OpenGLGfxModel = gfx[GfX.XModel]
        self.gfxModel.textureManager.deleteTexture(obj)
        self.material: GLRenderMaterial = self.gfxModel.materialManager.createMaterial(obj)[0]
        self.shader, self.shaderTag = self.gfxModel.shaderManager.createShader(material.material.shaderName, material.material.getShaderArgs())
        self.vao: int = self._setupVao()
        self.boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)

    def _setupVao(self) -> int:
        glUseProgram(self.shader.program)

        # create and bind vao
        vao = glGenVertexArrays(1); glBindVertexArray(vao)
        vbo = glGenBuffers(1); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = array([
            # position      :normal        :texcoord  :tangent        :blendindices        :blendweight
            -1., -1., +0.,  +0., +0., 1.,  +0., +1.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.,
            -1., +1., +0.,  +0., +0., 1.,  +0., +0.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.,
            +1., -1., +0.,  +0., +0., 1.,  +1., +1.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.,
            +1., +1., +0.,  +0., +0., 1.,  +1., +0.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.
            ], dtype=float32)
        glBufferData(GL_ARRAY_BUFFER, vertices.nbytes, vertices, GL_STATIC_DRAW)
        
        # attributes
        glEnableVertexAttribArray(0)
        attributes = [
            ('vPOSITION', 3),
            ('vNORMAL', 3),
            ('vTEXCOORD', 2),
            ('vTANGENT', 3)]
        offset = 0; stride = sizeof_float * sum([x[1] for x in attributes])
        for name, size in attributes:
            location = self.shader.getAttribLocation(name)
            if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, size, GL_FLOAT, GL_FALSE, stride, ctypes.c_void_p(offset))
            offset += sizeof_float * size
        glBindVertexArray(0) # unbind vao
        return vao

    def render(self, camera: Camera, passx: Pass) -> None:
        identity_ = identity(4)
        glUseProgram(self.shader.program)
        glBindVertexArray(self.vao)
        glEnableVertexAttribArray(0)
        location = self.shader.getUniformLocation('m_vTintColorSceneObject')
        if location > -1: glUniform4(uniformLocation, ones(4))
        location = self.shader.getUniformLocation('m_vTintColorDrawCall')
        if location > -1: glUniform3(uniformLocation, ones(3))
        location = self.shader.getUniformLocation('uProjectionViewMatrix')
        if location > -1: glUniformMatrix4(uniformLocation, False, identity_)
        location = self.shader.getUniformLocation('transform')
        if location > -1: glUniformMatrix4(uniformLocation, False, identity_)
        self.material.render(self.shader)
        glDrawArrays(GL_TRIANGLE_STRIP, 0, 4)
        self.material.postRender()
        glBindVertexArray(0) # unbind vao
        glUseProgram(0) # unbind program

    def update(self, frameTime: float) -> None: pass

#endregion

#region GridRenderer

# GridRenderer
class GridRenderer(EginRenderer):
    def __init__(self, gfx: list[IOpenGfx], cellWidth: float, gridWidthInCells: int):
        self.gfxModel: OpenGLGfxModel = gfx[GfX.XModel]
        self.shader, self.shaderTag = self.gfxModel.shaderManager.createShader('vrf.grid')
        self.vao: int = self._setupVao(cellWidth, gridWidthInCells)
        self.boundingBox = AABB(
            -cellWidth * 0.5 * gridWidthInCells, -cellWidth * 0.5 * gridWidthInCells, 0,
            cellWidth * 0.5 * gridWidthInCells, cellWidth * 0.5 * gridWidthInCells, 0)
        self.vertexCount: int = 0 

    @staticmethod
    def _generateGridVertexBuffer(cellWidth: float, gridWidthInCells: int) -> list[float]:
        vertices: list[float] = []
        width = cellWidth * gridWidthInCells
        color = [1., 1., 1., 1.]
        for i in range(gridWidthInCells):
            vertices.extend([width, i * cellWidth, 0.])
            vertices.extend(color)
            vertices.extend([-width, i * cellWidth, 0.])
            vertices.extend(color)
        for i in range(gridWidthInCells):
            vertices.extend([width, -i * cellWidth, 0.])
            vertices.extend(color)
            vertices.extend([-width, -i * cellWidth, 0.])
            vertices.extend(color)
        for i in range(gridWidthInCells):
            vertices.extend([i * cellWidth, width, 0.])
            vertices.extend(color)
            vertices.extend([i * cellWidth, -width, 0.])
            vertices.extend(color)
        for i in range(gridWidthInCells):
            vertices.extend([-i * cellWidth, width, 0.])
            vertices.extend(color)
            vertices.extend([-i * cellWidth, -width, 0.])
            vertices.extend(color)
        return vertices

    def _setupVao(self, cellWidth: float, gridWidthInCells: int) -> int:
        STRIDE: int = 28
        glUseProgram(self.shader.program)
        # create and bind vao
        vao = glGenVertexArray(); glBindVertexArray(vao)
        vbo = glGenBuffer(); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = _generateGridVertexBuffer(cellWidth, gridWidthInCells)
        self.vertexCount = len(vertices) / 3 # number of vertices in our buffer
        glBufferData(GL_ARRAY_BUFFER, len(vertices) * sizeof_float, vertices, GL_STATIC_DRAW)
        # attributes
        glEnableVertexAttribArray(0)
        location = self.shader.getAttribLocation('aVertexPosition')
        if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, 3, GL_FLOAT, GL_FALSE, STRIDE, ctypes.c_void_p(0))
        location = self.shader.getAttribLocation('aVertexColor')
        if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, 4, GL_FLOAT, GL_FALSE, STRIDE, ctypes.c_void_p(sizeof_float * 3))
        glBindVertexArray(0) # unbind vao
        glUseProgram(0) # unbind program
        return vao

    def render(self, camera: Camera, passx: Pass) -> None:
        glEnable(GL_BLEND)
        glBlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)
        glUseProgram(Shader.Program)
        matrix = camera.ViewProjectionMatrix.ToOpenTK()
        glUniformMatrix4(self.shader.getUniformLocation('uProjectionViewMatrix'), False, matrix)
        glBindVertexArray(self.vao)
        glEnableVertexAttribArray(0)
        glDrawArrays(GL_LINES, 0, self.vertexCount)
        glBindVertexArray(0) # unbind vao
        glUseProgram(0) # unbind program
        glDisable(GL_BLEND)

    def update(self, frameTime: float) -> None: pass

#endregion

#region ParticleRenderer

# ParticleRenderer
class ParticleRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion

#region CellRenderer

# CellRenderer
class CellRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion

#region EngineRenderer

# EngineRenderer
class EngineRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object):
        self.gfx: OpenGLGfxModel = gfx
        self.obj: ICellDatabase = obj
        self.engine: OpenGLOpenEngine
        self.playerPrefab: object = None

    def dispose(self) -> None:
        if self.engine: self.engine.dispose()

    def start(self) -> None:
        # log.info(f'Obj: {self.obj}')
        # log.info(f'PlayerPrefab: {self.playerPrefab}')
        arc = self.obj.archive
        self.gfx = arc.gfx[2]
        query = self.obj.query
        builder = OpenGLCellBuilder(query, self.gfx)
        self.engine = OpenGLOpenEngine(lambda queue: OpenGLCellManager(query, queue, lambda cell, land, contObj, cellObj: builder.cellCoroutine(cell, land, contObj, cellObj)), False)
        self.engine.spawnPlayer(self.playerPrefab, self.obj.start)

    def update(self, deltaTime: float) -> None:
        if self.engine: self.engine.update()

#endregion

#region WorldRenderer

# WorldRenderer
class WorldRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion
