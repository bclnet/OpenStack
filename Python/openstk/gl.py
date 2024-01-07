import numpy as np
from OpenGL import GL as gl
from openstk.gl_render import GLMeshBufferCache, QuadIndexBuffer
from openstk.gfx import IOpenGraphic

class IOpenGLGraphic(IOpenGraphic):
    # cache
    meshBufferCache: GLMeshBufferCache
    quadIndices: QuadIndexBuffer