from __future__ import annotations
import os, io, pathlib
from openstk.core.platform import Platform

#region Platform

# UnknownPlatform
class UnknownPlatform(Platform):
    def __init__(self):
        super().__init__('UK', 'Unknown')
        self.gfxFactory = staticmethod(lambda source: [UnknownGfxApi(source), UnknownGfxSprite(source), UnknownGfxSprite(source), UnknownGfxModel(source), UnknownGfxTerrain(source)])
        self.sfxFactory = staticmethod(lambda source: [UnknownSfx(source)])
UnknownPlatform.This = UnknownPlatform()

#endregion
