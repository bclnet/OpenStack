from __future__ import annotations
import traceback
from numpy import ndarray, array, ones, zeros, float32
from openstk.core import ISource, Platform
from openstk.gfx import IOpenGfxModel, ObjectModelBuilder, ObjectModelManager, MaterialBuilder, MaterialManager, ShaderBuilder, ShaderManager, TextureManager, TextureBuilder
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
class PygameObjectModelBuilder(ObjectModelBuilder):
    def instance(self, src: object) -> object:
        return 'clone'
    async def create(self, source: ISource, path: object, static_: bool, materialManager: MaterialManager) -> object:
        builder = PygameX.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, static_, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensure(self) -> None: pass

# PygameShaderBuilder
class PygameShaderBuilder(ShaderBuilder):
    def create(self, path: object, args: dict[str, bool]) -> GfxShader: raise NotImplementedError()

# PygameTextureBuilder
class PygameTextureBuilder(TextureBuilder):
    _default: int = -1
    @property
    def default(self) -> int:
        if self._default > -1: return self._default
        self._default = self._createDefault()
        return self._default

    def release(self) -> None:
        if self._default > -1: glDeleteTexture(self._default); self._default = -1

    def _createDefault(self) -> int: return self.createSolid(4, 4, array([
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

    def create(self, reuse: int, source: ITexture, level2: range = None) -> int:
        pass

    def createSolid(self, width: int, height: int, pixels: array) -> int:
        pass

    def createNormalMap(self, source: int, strength: float) -> int: raise NotImplementedError()

    def delete(self, texture: int) -> None: pass

# PygameMaterialBuilder
class PygameMaterialBuilder(MaterialBuilder):
    _defaultMaterial: Material; _terrainMaterial: Material
    @property
    def default(self) -> Material:
        if self._default: return self._default
        self._default = self._createDefault()
        return self._default
    @property
    def terrain(self) -> Material:
        if self._terrain: return self._terrain
        self._terrain = self._createTerrain()
        return self._terrain

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefault() -> Material:
        m = Material()
        m.textures['g_tColor'] = self.textureManager.default
        m.material.shaderName = 'vrf.error'
        return m

    def _createTerrain() -> Material:
        m = Material()
        m.material.shaderName = 'vrf.error'
        return m

    def create(self, key: object) -> Material:
        match key:
            case _: raise Exception(f'Unknown: {key}')

# PygameGfxModel
class PygameGfxModel(IOpenGfxModel):
    def __init__(self):
        self.textureManager: TextureManager = TextureManager(PygameTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, PygameMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, PygameObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(PygameShaderBuilder())
    def preload(self, source: ISource, path: object) -> None: self.objectManager.preload(path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preload(path)
    def create(self, source: ISource, path: object, static_: bool, parent: object = None) -> tuple[object, object]: return self.objectManager.create(source, path, static_, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> tuple[GfxShader, object]: return self.shaderManager.create(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> tuple[int, object]: return self.textureManager.create(source, path, level)

# PygamePlatform
class PygamePlatform(Platform):
    def __init__(self):
        super().__init__('PG', 'Pygame')
        self.gfxFactory = staticmethod(lambda: [None, None, None, PygameGfxModel(), None, None])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
PygamePlatform.this = PygamePlatform()

#endregion