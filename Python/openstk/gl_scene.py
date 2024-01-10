import numpy as np
from typing import Any
from OpenGL import GL as gl
# from enum import Enum
from openstk.gl import IOpenGLGraphic
from openstk.gfx_octree import Octree
from openstk.gfx_render import Camera, Shader, AABB, RenderPass

STRIDE = 4 * 7

# OctreeDebugRenderer
class OctreeDebugRenderer:
    _shader: Shader
    _octree: Octree
    _vaoHandle: int
    _vboHandle: int
    _dynamic: bool
    _vertexCount: int

    def __init__(self, octree: Octree, graphic: IOpenGLGraphic, dynamic: bool):
        self._octree = octree
        self._dynamic = dynamic

        self._shader = graphic.loadShader('vrf.grid')
        gl.glUseProgram(_shader.program)

        self._vboHandle = gl.glGenBuffer()
        if not dynamic:
            self.rebuild()

        self._vaoHandle = gl.glGenVertexArray()
        gl.bindVertexArray(_vaoHandle)
        gl.bindBuffer(gl.GL_ARRAY_BUFFER, _vboHandle)

        positionAttributeLocation = gl.glGetAttribLocation(self._shader.program, 'aVertexPosition')
        gl.glEnableVertexAttribArray(positionAttributeLocation)
        gl.glVertexAttribPointer(positionAttributeLocation, 3, gl.GL_FLOAT, False, STRIDE, 0)

        colorAttributeLocation = gl.glGetAttribLocation(self._shader.program, 'aVertexColor')
        gl.glEnableVertexAttribArray(colorAttributeLocation)
        gl.glVertexAttribPointer(colorAttributeLocation, 4, gl.GL_FLOAT, False, STRIDE, sizeof(float) * 3);

        gl.glBindVertexArray(0)

    def addLine(self, vertices: list[float], from_: np.ndarray, to: np.ndarray, r: float, g: float, b: float, a: float) -> None:
        vertices.append(from_[0]); vertices.append(from_[1]); vertices.append(from_[2])
        vertices.append(r); vertices.append(g); vertices.append(b); vertices.append(a)
        vertices.append(to[0]); vertices.append(to[1]); vertices.append(to[2])
        vertices.append(r); vertices.append(g); vertices.append(b); vertices.append(a)

    def addBox(self, vertices: list[float], box: AABB, r: float, g: float, b: float, a: float) -> None:
        self.addLine(vertices, np.array([box.Min[0], box.Min[1], box.Min[2]]), np.array([box.Max[0], box.Min[1], box.Min[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Max[0], box.Min[1], box.Min[2]]), np.array([box.Max[0], box.Max[1], box.Min[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Max[0], box.Max[1], box.Min[2]]), np.array([box.Min[0], box.Max[1], box.Min[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Min[0], box.Max[1], box.Min[2]]), np.array([box.Min[0], box.Min[1], box.Min[2]]), r, g, b, a)

        self.addLine(vertices, np.array([box.Min[0], box.Min[1], box.Max[2]]), np.array([box.Max[0], box.Min[1], box.Max[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Max[0], box.Min[1], box.Max[2]]), np.array([box.Max[0], box.Max[1], box.Max[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Max[0], box.Max[1], box.Max[2]]), np.array([box.Min[0], box.Max[1], box.Max[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Min[0], box.Max[1], box.Max[2]]), np.array([box.Min[0], box.Min[1], box.Max[2]]), r, g, b, a)

        self.addLine(vertices, np.array([box.Min[0], box.Min[1], box.Min[2]]), np.array([box.Min[0], box.Min[1], box.Max[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Max[0], box.Min[1], box.Min[2]]), np.array([box.Max[0], box.Min[1], box.Max[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Max[0], box.Max[1], box.Min[2]]), np.array([box.Max[0], box.Max[1], box.Max[2]]), r, g, b, a)
        self.addLine(vertices, np.array([box.Min[0], box.Max[1], box.Min[2]]), np.array([box.Min[0], box.Max[1], box.Max[2]]), r, g, b, a)

    def _addOctreeNode(vertices: list[float], node: Octree.Node, depth: int) -> None:
        self.addBox(vertices, node.region, 1., 1., 1., 1. if node.hasElements else 0.1)
        if node.hasElements:
            for element in node.Elements:
                shading = math.min(1., depth * 0.1)
                self.addBox(vertices, element.boundingBox, 1., shading, 0., 1.)
        if node.hasChildren:
            for child in node.children:
                self.addOctreeNode(vertices, child, depth + 1)

    def rebuild(self) -> None:
        vertices = []
        self.addOctreeNode(vertices, _octree.root, 0)
        self._vertexCount = vertices.Count / 7
        gl.glBindBuffer(gl.GL_ARRAY_BUFFER, _vboHandle)
        gl.glBufferData(gl.GL_ARRAY_BUFFER, vertices.count * 4, vertices, gl.GL_DYNAMIC_DRAW if _dynamic else gl.GL_STATIC_DRAW)

    def render(self, camera: Camera, renderPass: RenderPass):
        if renderPass == RenderPass.Translucent or renderPass == RenderPass.Both:
            if _dynamic: self.rebuild()

            gl.glEnable(gl.GL_BLEND)
            gl.glEnable(gl.GL_DEPTH_TEST)
            gl.glDepthMask(False)
            gl.glBlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA)
            gl.glUseProgram(self._shader.program)

            projectionViewMatrix = camera.viewProjectionMatrix
            gl.glUniformMatrix4(_shader.getUniformLocation('uProjectionViewMatrix'), False, self.projectionViewMatrix)

            gl.glBindVertexArray(_vaoHandle)
            gl.glDrawArrays(gl.GL_LINES, 0, _vertexCount)
            gl.glBindVertexArray(0)
            gl.glUseProgram(0)
            gl.glDepthMask(True)
            gl.glDisable(gl.GL_BLEND)
            gl.glDisable(gl.GL_DEPTH_TEST)