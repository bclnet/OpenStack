import ctypes, numpy as np
from enum import Enum
from OpenGL.GL import *
from openstk.gfx_render import AABB, IRenderer, RenderPass
from openstk.gl_render import GLRenderMaterial

sizeof_float = ctypes.sizeof(GLfloat)

# typedefs
class IOpenGLGraphic: pass
class Shader: pass
class Camera: pass

# TextureRenderer
class TextureRenderer(IRenderer):
    graphic: IOpenGLGraphic
    texture: int
    shader: Shader
    shaderTag: object
    quadVao: int
    background: bool
    boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)

    def __init__(self, graphic: IOpenGLGraphic, texture: int, background: bool = False):
        self.graphic = graphic
        self.texture = texture
        self.shader, self.shaderTag = graphic.shaderManager.createPlaneShader('plane')
        self.quadVao = self._setupQuadBuffer()
        self.background = background

    def _setupQuadBuffer(self) -> int:
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
        glBufferData(GL_ARRAY_BUFFER, vertices.nbytes, vertices.tobytes(), GL_STATIC_DRAW)
        # arrayType = GLfloat * len(vertices)
        # glBufferData(GL_ARRAY_BUFFER, len(vertices) * sizeof_float, arrayType(*vertices), GL_STATIC_DRAW)
        # glBufferData(GL_ARRAY_BUFFER, len(vertices) * sizeof_float, (GLfloat * len(vertices))(*vertices), GL_STATIC_DRAW)
        # print(vertices.nbytes, glGetBufferParameteriv(GL_ARRAY_BUFFER, GL_BUFFER_SIZE))
        # attributes
        glEnableVertexAttribArray(0)
        attributes = [
            ('vPOSITION', 3),
            ('vNORMAL', 3),
            ('vTEXCOORD', 2),
            ('vTANGENT', 3)]
        stride = sizeof_float * sum([x[1] for x in attributes])
        offset = 0
        for name, size in attributes:
            location = self.shader.getAttribLocation(name)
            if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, size, GL_FLOAT, False, stride, offset)
            offset += sizeof_float * size
        glBindVertexArray(0) # unbind vao
        return vao

    def render(self, camera: Camera, renderPass: RenderPass) -> None:
        if self.background: glClearColor(255, 255, 255, 255); glClear(GL_COLOR_BUFFER_BIT)
        glUseProgram(self.shader.program)
        glBindVertexArray(self.quadVao)
        glEnableVertexAttribArray(0)
        if self.texture > -1: glActiveTexture(GL_TEXTURE0); glBindTexture(GL_TEXTURE_2D, self.texture)
        glDrawArrays(GL_TRIANGLE_STRIP, 0, 4)
        glBindVertexArray(0)
        glUseProgram(0)

    def update(self, frameTime: float) -> None: pass

# MaterialRenderer
class MaterialRenderer(IRenderer):
    graphic: IOpenGLGraphic
    material: GLRenderMaterial
    shader: Shader
    shaderTag: object
    quadVao: int
    boundingBox: AABB = AABB(-1., -1., -1., 1., 1., 1.)

    def __init__(self, graphic: IOpenGLGraphic, material: GLRenderMaterial):
        self.graphic = graphic
        self.material = material
        self.shader, self.shaderTag = graphic.shaderManager.createShader(material.material.shaderName, material.material.getShaderArgs())
        self.quadVao = self._setupQuadBuffer()

    def _setupQuadBuffer(self) -> int:
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
        stride = sizeof_float * sum([x[1] for x in attributes])
        offset = 0
        for name, size in attributes:
            location = self.shader.getAttribLocation(name)
            if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, size, GL_FLOAT, False, stride, offset)
            offset += sizeof_float * size
        glBindVertexArray(0) # unbind vao
        return vao

    def render(self, camera: Camera, renderPass: RenderPass) -> None:
        identity = np.identity(4)
        glUseProgram(self.shader.program)
        glBindVertexArray(self.quadVao)
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
        glBindVertexArray(0)
        glUseProgram(0)

    def update(self, frameTime: float) -> None: pass

# ParticleGridRenderer
class ParticleGridRenderer(IRenderer):
    shader: Shader
    shaderTag: object
    quadVao: int
    vertexCount: int
    boundingBox: AABB

    def __init__(self, graphic: IOpenGLGraphic, cellWidth: float, gridWidthInCells: int):
        self.boundingBox = AABB(
            -cellWidth * 0.5 * gridWidthInCells, -cellWidth * 0.5 * gridWidthInCells, 0,
            cellWidth * 0.5 * gridWidthInCells, cellWidth * 0.5 * gridWidthInCells, 0)
        self.shader, self.shaderTag = graphic.shaderManager.createShader('vrf.grid')
        self.quadVao = self._setupQuadBuffer(cellWidth, gridWidthInCells)

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

    def _setupQuadBuffer(self, cellWidth: float, gridWidthInCells: int) -> int:
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
        if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, 3, GL_FLOAT, False, STRIDE, 0)
        location = self.shader.getAttribLocation('aVertexColor')
        if location > -1: glEnableVertexAttribArray(location); glVertexAttribPointer(location, 4, GL_FLOAT, False, STRIDE, sizeof_float * 3)
        glBindVertexArray(0) # unbind vao
        glUseProgram(0)
        return vao

    def render(self, camera: Camera, renderPass: RenderPass) -> None:
        glEnable(GL_BLEND)
        glBlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)
        glUseProgram(Shader.Program)
        matrix = camera.ViewProjectionMatrix.ToOpenTK()
        glUniformMatrix4(self.shader.getUniformLocation('uProjectionViewMatrix'), False, matrix)
        glBindVertexArray(self.quadVao)
        glEnableVertexAttribArray(0)
        glDrawArrays(GL_LINES, 0, self.vertexCount)
        glBindVertexArray(0)
        glUseProgram(0)
        glDisable(GL_BLEND)

    def update(self, frameTime: float) -> None: pass