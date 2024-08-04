import numpy as np
from OpenGL.GL import *
from openstk.gfx.gfx import IOpenGfxAny

#ref https://pyopengl.sourceforge.net/documentation/manual-3.0/glGetProgram.html

# typedefs
class GLMeshBufferCache: pass
class QuadIndexBuffer: pass

# IOpenGLGfx
class IOpenGLGfx(IOpenGfxAny):
    # cache
    meshBufferCache: GLMeshBufferCache
    quadIndices: QuadIndexBuffer