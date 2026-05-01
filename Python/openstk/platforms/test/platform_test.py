from __future__ import annotations
import os, io, pathlib
from openstk.core.platform import Platform

#region Platform

# TestGfxApi
class TestGfxApi:
    def __init__(self, source): self.source: object = source
    async def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)

# TestGfxSprite
class TestGfxSprite:
    def __init__(self, source): self.source: object = source
    async def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)
    def preloadSprite(self, path: object) -> None: raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()

# TestGfxModel
class TestGfxModel:
    def __init__(self, source): self.source: object = source
    async def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)
    def preloadTexture(self, path: object) -> None: raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()

# TestGfxLight
class TestGfxLight:
    def __init__(self, source): self.source: object = source
    async def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)
    def createLight(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: object = None) -> object: print(f'light: {radius}'); return 'light'
    def createReflectionProbe(self, name: str, position: Vector3, parent: object = None) -> object: print(f'probe: {name}'); return 'probe'

# TestGfxTerrain
class TestGfxTerrain:
    def __init__(self, source): self.source: object = source
    async def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)
    def createTerrainData(self, offset: int, heights: ndarray, heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer], alphaMap: ndarray) -> object: return f't{offset}'
    def createTerrain(self, name: str, position: Vector3, data: object, parent: object = None) -> object: print(f'terrain: {data}'); return 'terrain'

# TestSfx
class TestSfx:
    def __init__(self, source): self.source: object = source

# TestPlatform
class TestPlatform(Platform):
    def __init__(self):
        super().__init__('TT', 'Test')
        self.gfxFactory = staticmethod(lambda source: [TestGfxApi(source), TestGfxSprite(source), TestGfxSprite(source), TestGfxModel(source), TestGfxLight(source), TestGfxTerrain(source)])
        self.sfxFactory = staticmethod(lambda source: [TestSfx(source)])
TestPlatform.This = TestPlatform()

#endregion
