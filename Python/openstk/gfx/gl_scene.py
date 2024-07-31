import numpy as np
from OpenGL.GL import *
from openstk.gfx.gl import IOpenGLGraphic
from openstk.gfx.gfx_render import RenderPass
from openstk.gfx.gfx_scene import Octree

# typedefs
class Camera: pass
class Shader: pass
class AABB: pass

STRIDE = 4 * 7

# OctreeDebugRenderer
class OctreeDebugRenderer:
    shader: Shader
    octree: Octree
    vaoHandle: int
    vboHandle: int
    dynamic: bool
    vertexCount: int

    def __init__(self, octree: Octree, graphic: IOpenGLGraphic, dynamic: bool):
        self.octree = octree
        self.dynamic = dynamic
        self.shader, self.shaderTag = graphic.createShader('vrf.grid')
        glUseProgram(shader.program)
        self.vboHandle = glGenBuffer()
        if not dynamic: self.rebuild()
        self.vaoHandle = glGenVertexArray()
        bindVertexArray(self.vaoHandle)
        bindBuffer(GL_ARRAY_BUFFER, self.vboHandle)
        location = self.shader.getAttribLocation('aVertexPosition')
        glEnableVertexAttribArray(location)
        glVertexAttribPointer(location, 3, GL_FLOAT, False, STRIDE, 0)
        location = self.shader.getAttribLocation('aVertexColor')
        glEnableVertexAttribArray(location)
        glVertexAttribPointer(location, 4, GL_FLOAT, False, STRIDE, sizeof(float) * 3)
        glBindVertexArray(0)

    def _addLine(self, vertices: list[float], from_: np.ndarray, to: np.ndarray, r: float, g: float, b: float, a: float) -> None:
        vertices.append(from_[0]); vertices.append(from_[1]); vertices.append(from_[2])
        vertices.append(r); vertices.append(g); vertices.append(b); vertices.append(a)
        vertices.append(to[0]); vertices.append(to[1]); vertices.append(to[2])
        vertices.append(r); vertices.append(g); vertices.append(b); vertices.append(a)

    def _addBox(self, vertices: list[float], box: AABB, r: float, g: float, b: float, a: float) -> None:
        self._addLine(vertices, np.array([box.Min[0], box.Min[1], box.Min[2]]), np.array([box.Max[0], box.Min[1], box.Min[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Max[0], box.Min[1], box.Min[2]]), np.array([box.Max[0], box.Max[1], box.Min[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Max[0], box.Max[1], box.Min[2]]), np.array([box.Min[0], box.Max[1], box.Min[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Min[0], box.Max[1], box.Min[2]]), np.array([box.Min[0], box.Min[1], box.Min[2]]), r, g, b, a)
        #
        self._addLine(vertices, np.array([box.Min[0], box.Min[1], box.Max[2]]), np.array([box.Max[0], box.Min[1], box.Max[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Max[0], box.Min[1], box.Max[2]]), np.array([box.Max[0], box.Max[1], box.Max[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Max[0], box.Max[1], box.Max[2]]), np.array([box.Min[0], box.Max[1], box.Max[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Min[0], box.Max[1], box.Max[2]]), np.array([box.Min[0], box.Min[1], box.Max[2]]), r, g, b, a)
        #
        self._addLine(vertices, np.array([box.Min[0], box.Min[1], box.Min[2]]), np.array([box.Min[0], box.Min[1], box.Max[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Max[0], box.Min[1], box.Min[2]]), np.array([box.Max[0], box.Min[1], box.Max[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Max[0], box.Max[1], box.Min[2]]), np.array([box.Max[0], box.Max[1], box.Max[2]]), r, g, b, a)
        self._addLine(vertices, np.array([box.Min[0], box.Max[1], box.Min[2]]), np.array([box.Min[0], box.Max[1], box.Max[2]]), r, g, b, a)

    def _addOctreeNode(vertices: list[float], node: Octree.Node, depth: int) -> None:
        self._addBox(vertices, node.region, 1., 1., 1., 1. if node.hasElements else 0.1)
        if node.hasElements:
            for element in node.Elements:
                shading = math.min(1., depth * 0.1)
                self._addBox(vertices, element.boundingBox, 1., shading, 0., 1.)
                # self._addLine(vertices, element.boundingBox.min, element.region.min, 1., shading, 0., 0.5)
                # self._addLine(vertices, element.boundingBox.max, element.region.max, 1., shading, 0., 0.5)
        if node.hasChildren:
            for child in node.children:
                self._addOctreeNode(vertices, child, depth + 1)

    def _rebuild(self) -> None:
        vertices = []
        self._addOctreeNode(vertices, self.octree.root, 0)
        self.vertexCount = vertices.Count / 7
        glBindBuffer(GL_ARRAY_BUFFER, self.vboHandle)
        glBufferData(GL_ARRAY_BUFFER, vertices.count * 4, vertices, GL_DYNAMIC_DRAW if self.dynamic else GL_STATIC_DRAW)

    def render(self, camera: Camera, renderPass: RenderPass):
        if renderPass == RenderPass.Translucent or renderPass == RenderPass.Both:
            if self.dynamic: self._rebuild()
            glEnable(GL_BLEND)
            glEnable(GL_DEPTH_TEST)
            glDepthMask(False)
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
            glUseProgram(self.shader.program)
            projectionViewMatrix = camera.viewProjectionMatrix
            glUniformMatrix4(self.shader.getUniformLocation('uProjectionViewMatrix'), False, self.projectionViewMatrix)
            glBindVertexArray(self.vaoHandle)
            glDrawArrays(GL_LINES, 0, self.vertexCount)
            glBindVertexArray(0)
            glUseProgram(0)
            glDepthMask(True)
            glDisable(GL_BLEND)
            glDisable(GL_DEPTH_TEST)