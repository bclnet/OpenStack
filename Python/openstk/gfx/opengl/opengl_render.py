from __future__ import annotations
import ctypes, numpy as np
from enum import Enum
from OpenGL.GL import *
from openstk.gfx import Renderer, ITextureFrames
from openstk.gfx.egin import AABB, EginRenderer
from openstk.gfx.opengl.egin import GLRenderMaterial

sizeof_float = ctypes.sizeof(GLfloat)

# typedefs
class OpenGLGfxModel: pass
class Shader: pass
class Camera: pass

#region OpenGLTextureRenderer

FACTOR = 0

# OpenGLTextureRenderer
class OpenGLTextureRenderer(EginRenderer):
    gfx: OpenGLGfxModel
    obj: object
    level: range
    tex: int
    shader: Shader
    shaderTag: object
    vao: int
    background: bool
    boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)
    frameDelay: int = 0

    def __init__(self, gfx: OpenGLGfxModel, obj: object, level: range, background: bool = False):
        self.gfx = gfx
        self.obj = obj
        self.level = level
        gfx.textureManager.deleteTexture(obj)
        self.tex = gfx.textureManager.createTexture(obj, self.level)[0] or -1
        self.shader, self.shaderTag = gfx.shaderManager.createShader('plane')
        self.vao = self._setupVao()
        self.background = background

    def getViewport(self, size: tuple) -> tuple:
        return None if not (o := self.obj) else \
            size if o.width > 1024 or o.height > 1024 or False else (o.width << FACTOR, o.height << FACTOR)

    def _setupVao(self) -> int:
        glUseProgram(self.shader.program)
        # create and bind vao
        vao = glGenVertexArrays(1); glBindVertexArray(vao) 
        vbo = glGenBuffers(1); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = np.array([
            # position      :normal        :texcoord  :tangent
            -1., -1., +0.,  +0., +0., 1.,  +0., +1.,  +1., +0., +0.,
            -1., +1., +0.,  +0., +0., 1.,  +0., +0.,  +1., +0., +0.,
            +1., -1., +0.,  +0., +0., 1.,  +1., +1.,  +1., +0., +0.,
            +1., +1., +0.,  +0., +0., 1.,  +1., +0.,  +1., +0., +0.
            ], dtype = np.float32)
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
        self.gfx.textureManager.reloadTexture(obj)

#endregion

#region OpenGLObjectRenderer

# OpenGLObjectRenderer
class OpenGLObjectRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion

#region OpenGLMaterialRenderer

# OpenGLMaterialRenderer
class OpenGLMaterialRenderer(EginRenderer):
    gfx: OpenGLGfxModel
    material: GLRenderMaterial
    shader: Shader
    shaderTag: object
    vao: int
    boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)

    def __init__(self, gfx: IOpenGLGfx, obj: object):
        self.gfx = gfx
        gfx.textureManager.deleteTexture(obj)
        self.material = gfx.materialManager.createMaterial(obj)[0]
        self.shader, self.shaderTag = gfx.shaderManager.createShader(material.material.shaderName, material.material.getShaderArgs())
        self.vao = self._setupVao()

    def _setupVao(self) -> int:
        glUseProgram(self.shader.program)

        # create and bind vao
        vao = glGenVertexArrays(1); glBindVertexArray(vao)
        vbo = glGenBuffers(1); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = np.array([
            # position      :normal        :texcoord  :tangent        :blendindices        :blendweight
            -1., -1., +0.,  +0., +0., 1.,  +0., +1.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.,
            -1., +1., +0.,  +0., +0., 1.,  +0., +0.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.,
            +1., -1., +0.,  +0., +0., 1.,  +1., +1.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.,
            +1., +1., +0.,  +0., +0., 1.,  +1., +0.,  +1., +0., +0.,  +0., +0., +0., +0.,  +0., +0., +0., +0.
            ], dtype = np.float32)
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
        identity = np.identity(4)
        glUseProgram(self.shader.program)
        glBindVertexArray(self.vao)
        glEnableVertexAttribArray(0)
        location = self.shader.getUniformLocation('m_vTintColorSceneObject')
        if location > -1: glUniform4(uniformLocation, np.ones(4))
        location = self.shader.getUniformLocation('m_vTintColorDrawCall')
        if location > -1: glUniform3(uniformLocation, np.ones(3))
        location = self.shader.getUniformLocation('uProjectionViewMatrix')
        if location > -1: glUniformMatrix4(uniformLocation, False, identity)
        location = self.shader.getUniformLocation('transform')
        if location > -1: glUniformMatrix4(uniformLocation, False, identity)
        self.material.render(self.shader)
        glDrawArrays(GL_TRIANGLE_STRIP, 0, 4)
        self.material.postRender()
        glBindVertexArray(0) # unbind vao
        glUseProgram(0) # unbind program

    def update(self, frameTime: float) -> None: pass

#endregion

#region OpenGLGridRenderer

# OpenGLGridRenderer
class OpenGLGridRenderer(EginRenderer):
    shader: Shader
    shaderTag: object
    vao: int
    vertexCount: int
    boundingBox: AABB

    def __init__(self, gfx: OpenGLGfxModel, cellWidth: float, gridWidthInCells: int):
        self.boundingBox = AABB(
            -cellWidth * 0.5 * gridWidthInCells, -cellWidth * 0.5 * gridWidthInCells, 0,
            cellWidth * 0.5 * gridWidthInCells, cellWidth * 0.5 * gridWidthInCells, 0)
        self.shader, self.shaderTag = gfx.shaderManager.createShader('vrf.grid')
        self.vao = self._setupVao(cellWidth, gridWidthInCells)

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

#region OpenGLParticleRenderer

# OpenGLParticleRenderer
class OpenGLParticleRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion

#region OpenGLCellRenderer

# OpenGLCellRenderer
class OpenGLCellRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion

#region OpenGLWorldRenderer

# OpenGLWorldRenderer
class OpenGLWorldRenderer(EginRenderer):
    def __init__(self, gfx: OpenGLGfxModel, obj: object): pass

#endregion

#region OpenGLTestTriRenderer

# OpenGLTestTriRenderer
class OpenGLTestTriRenderer(EginRenderer):
    gfx: IOpenGLGfx
    texture: int
    shader: Shader
    shaderTag: object
    quadVao: int
    background: bool
    boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)

    def __init__(self, gfx: OpenGLGfxModel, obj: object):
        self.gfx = gfx
        self.shader, self.shaderTag = gfx.shaderManager.createShader('testtri')
        self.vao = self._setupVao()

    def _setupVao(self) -> int:
        glUseProgram(self.shader.program)

        # create and bind vao
        vao = glGenVertexArrays(1); glBindVertexArray(vao)
        vbo = glGenBuffers(1); glBindBuffer(GL_ARRAY_BUFFER, vbo)
        vertices = np.array([
            # xyz,           :rgb
           -0.5, -0.5, 0.0,  1.0, 0.0, 0.0,
            0.5, -0.5, 0.0,  0.0, 1.0, 0.0,
            0.0,  0.5, 0.0,  0.0, 0.0, 1.0
            ], dtype = np.float32)
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
