# @see https://pyopengl.sourceforge.net/documentation/manual-3.0/glGetProgram.html
# @see https://github.com/jcteng/python-opengl-tutorial/blob/master/utils/textureLoader.py
from __future__ import annotations
from numpy import ndarray
from OpenGL.GL import *
from openstk.core import CellManager, CellBuilder
from openstk.gfx import GfxShader

# typedefs
class OpenGLGfxModel: pass

#region Extensions

class OpenGLX:
    buildersByType: dict[type, callable] = {}

#endregion

#region CellManager

# OpenGLCellBuilder
class OpenGLCellBuilder(CellBuilder):
    def __init__(self, source: ISource, query: CellManager.IQuery, gfx: list[IOpenGfx]): super().__init__(source, query, gfx)

#endregion