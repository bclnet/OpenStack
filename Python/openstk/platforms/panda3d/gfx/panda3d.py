from __future__ import annotations
import os
from openstk.core import CellManager, CellBuilder

#region Extensions

#endregion

#region CellManager

# Panda3dCellBuilder
class Panda3dCellBuilder(CellBuilder):
    def __init__(self, source: ISource, query: CellManager.IQuery, gfx: list[IOpenGfx]): super().__init__(source, query, gfx)

#endregion