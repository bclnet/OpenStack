from __future__ import annotations
import os, io, pathlib
from openstk.core.core import ISource
from openstk.core.platform import Platform

#region Platform

# TestGfxApi
class TestGfxApi: pass

# TestGfxSprite
class TestGfxSprite:
    def preloadSprite(self, source: ISource, path: object) -> None: raise NotImplementedError()
    def createSprite(self, source: ISource, path: object,  parent: object = None) -> tuple[object, object]: raise NotImplementedError()

# TestGfxModel
class TestGfxModel:
    def preloadObject(self, source: ISource, path: object) -> None: raise NotImplementedError()
    def preloadTexture(self, source: ISource, path: object) -> None: raise NotImplementedError()
    def createObject(self, source: ISource, path: object, isStatic: bool, parent: object = None) -> tuple[object, object]: raise NotImplementedError()
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> tuple[object, object]: raise NotImplementedError()
    def createTexture(self, source: ISource, path: object, level: range = None) -> tuple[object, object]: raise NotImplementedError()

# TestGfxLight
class TestGfxLight:
    def createLight(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: object = None) -> object: print(f'light: {radius}'); return 'light'
    def createReflectionProbe(self, name: str, position: Vector3, parent: object = None) -> object: print(f'probe: {name}'); return 'probe'

# TestGfxTerrain
class TestGfxTerrain:
    def createTerrainData(self, offset: int, heights: ndarray, heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer], alphaMap: ndarray) -> object: return f't{offset}'
    def createTerrain(self, name: str, position: Vector3, data: object, parent: object = None) -> object: print(f'terrain: {data}'); return 'terrain'

# TestSfx
class TestSfx: pass

# TestPlatform
class TestPlatform(Platform):
    # buildersByType: dict[type, callable] = {}
    def __init__(self):
        super().__init__('TT', 'Test')
        self.gfxFactory = staticmethod(lambda: [TestGfxApi(), TestGfxSprite(), TestGfxSprite(), TestGfxModel(), TestGfxLight(), TestGfxTerrain()])
        self.sfxFactory = staticmethod(lambda: [TestSfx()])
TestPlatform.this = TestPlatform()

#endregion
