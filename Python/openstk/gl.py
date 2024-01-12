import numpy as np
from OpenGL import GL as gl
from openstk.gfx import IOpenGraphic

# typedefs
class GLMeshBufferCache: pass
class QuadIndexBuffer: pass

class IOpenGLGraphic(IOpenGraphic):
    # cache
    meshBufferCache: GLMeshBufferCache
    quadIndices: QuadIndexBuffer