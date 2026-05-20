from __future__ import annotations
import traceback
from numpy import ndarray, array, ones, zeros, float32
from openstk.core import ISource, Platform
from openstk.gfx import IOpenGfxModel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureManager, TextureBuilderBase
from openstk.platforms.pygame.gfx.pygame import PygameX
from openstk.platforms.system import SystemSfx
from openstk.client import IClientHost

#region Client

# PygameClientHost
class PygameClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# PygameObjectModelBuilder
class PygameObjectModelBuilder(ObjectModelBuilderBase):
    def instanceObject(self, src: object) -> object:
        return 'clone'
    async def createObject(self, source: ISource, path: object, isStatic: bool, materialManager: MaterialManager) -> object:
        builder = PygameX.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, isStatic, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensurePrefab(self) -> None: pass

# PygameShaderBuilder
class PygameShaderBuilder(ShaderBuilderBase):
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: raise NotImplementedError()

# PygameTextureBuilder
class PygameTextureBuilder(TextureBuilderBase):
    _defaultTexture: int = -1
    @property
    def defaultTexture(self) -> int:
        if self._defaultTexture > -1: return self._defaultTexture
        self._defaultTexture = self._createDefaultTexture()
        return self._defaultTexture

    def release(self) -> None:
        if self._defaultTexture > -1: glDeleteTexture(self._defaultTexture); self._defaultTexture = -1

    def _createDefaultTexture(self) -> int: return self.createSolidTexture(4, 4, array([
        0.9, 0.2, 0.8, 1.0,
        0.0, 0.9, 0.0, 1.0,
        0.9, 0.2, 0.8, 1.0,
        0.0, 0.9, 0.0, 1.0,

        0.0, 0.9, 0.0, 1.0,
        0.9, 0.2, 0.8, 1.0,
        0.0, 0.9, 0.0, 1.0,
        0.9, 0.2, 0.8, 1.0,

        0.9, 0.2, 0.8, 1.0,
        0.0, 0.9, 0.0, 1.0,
        0.9, 0.2, 0.8, 1.0,
        0.0, 0.9, 0.0, 1.0,

        0.0, 0.9, 0.0, 1.0,
        0.9, 0.2, 0.8, 1.0,
        0.0, 0.9, 0.0, 1.0,
        0.9, 0.2, 0.8, 1.0
        ], dtype = float32))

    def createTexture(self, reuse: int, source: ITexture, level2: range = None) -> int:
        pass

    def createSolidTexture(self, width: int, height: int, pixels: array) -> int:
        pass

    def createNormalMap(self, source: int, strength: float) -> int: raise NotImplementedError()

    def deleteTexture(self, texture: int) -> None: pass

# PygameMaterialBuilder
class PygameMaterialBuilder(MaterialBuilderBase):
    _defaultMaterial: Material; _terrainMaterial: Material
    @property
    def defaultMaterial(self) -> Material:
        if self._defaultMaterial: return self._defaultMaterial
        self._defaultMaterial = self._createDefaultMaterial()
        return self._defaultMaterial
    @property
    def terrainMaterial(self) -> Material:
        if self._terrainMaterial: return self._terrainMaterial
        self._terrainMaterial = self._createTerrainMaterial()
        return self._terrainMaterial

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefaultMaterial() -> Material:
        m = Material()
        m.textures['g_tColor'] = self.textureManager.defaultTexture
        m.material.shaderName = 'vrf.error'
        return m

    def _createTerrainMaterial() -> Material:
        m = Material()
        m.material.shaderName = 'vrf.error'
        return m

    def createMaterial(self, key: object) -> Material:
        match key:
            case _: raise Exception(f'Unknown: {key}')

# PygameGfxModel
class PygameGfxModel(IOpenGfxModel):
    def __init__(self):
        self.textureManager: TextureManager = TextureManager(PygameTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, PygameMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, PygameObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(PygameShaderBuilder())
    def preloadObject(self, source: ISource, path: object) -> None: self.objectManager.preloadObject(path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preloadTexture(path)
    def createObject(self, source: ISource, path: object, isStatic: bool, parent: object = None) -> tuple[object, object]: return self.objectManager.createObject(source, path, isStatic, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> tuple[Shader, object]: return self.shaderManager.createShader(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> tuple[int, object]: return self.textureManager.createTexture(source, path, level)

# PygamePlatform
class PygamePlatform(Platform):
    def __init__(self):
        super().__init__('PG', 'Pygame')
        self.gfxFactory = staticmethod(lambda: [None, None, None, PygameGfxModel(), None, None])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
PygamePlatform.this = PygamePlatform()

#endregion