from __future__ import annotations
import os, io, pathlib
from openstk.core.platform import Platform

#region Platform

# TestGfxSprite
class TestGfxSprite:
    def __init__(self, source): self.source: object = source
    def getAsset(self, type: type, path: object): raise NotImplementedError()
    def preloadSprite(self, path: object) -> None: raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()

# TestGfxModel
class TestGfxModel:
    def __init__(self, source): self.source: object = source
    def getAsset(self, t: type, path: object): raise NotImplementedError()
    def preloadTexture(self, path: object) -> None: raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()

# TestSfx
class TestSfx:
    def __init__(self, source): self.source: object = source

# TestPlatform
class TestPlatform(Platform):
    def __init__(self):
        super().__init__('TT', 'Test')
        self.gfxFactory = staticmethod(lambda source: [TestGfxApi(source), TestGfxSprite(source), TestGfxSprite(source), TestGfxModel(source), TestGfxTerrain(source)])
        self.sfxFactory = staticmethod(lambda source: [TestSfx(source)])
TestPlatform.This = TestPlatform()

#endregion
