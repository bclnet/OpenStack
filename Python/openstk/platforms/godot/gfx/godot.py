from __future__ import annotations
import os
from openstk.core import CellManager, CellBuilder

#region Extensions

class GodotX:
    buildersByType: dict[type, callable] = {}

#endregion

#region CellManager

# GodotCellBuilder
class GodotCellBuilder(CellBuilder):
    def __init__(self, query: CellManager.IQuery, gfx: list[IOpenGfx]): super().__init__(query, gfx)

#endregion