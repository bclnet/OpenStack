import numpy as np
from OpenGL import GL as gl
from openstk.gfx import IOpenGraphicAny

# typedefs
class GLMeshBufferCache: pass
class QuadIndexBuffer: pass

# IOpenGLGraphic
class IOpenGLGraphic(IOpenGraphicAny):
    # cache
    meshBufferCache: GLMeshBufferCache
    quadIndices: QuadIndexBuffer