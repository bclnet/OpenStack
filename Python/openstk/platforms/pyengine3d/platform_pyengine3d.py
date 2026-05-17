from __future__ import annotations
import traceback
from numpy import ones, zeros
from openstk.core import ISource, Platform
from openstk.client import IClientHost
from openstk.gfx import IOpenGfxModel, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, Shader, ShaderBuilderBase, ShaderManager, TextureBuilderBase, TextureManager
from openstk.platforms.system import SystemSfx

#region Client

# PyEngine3dClientHost
class PyEngine3dClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# PyEngine3dObjectModelBuilder
class PyEngine3dObjectModelBuilder(ObjectModelBuilderBase):
    def instanceObject(self, src: object) -> object:
        return 'clone'
    async def createObject(self, source: ISource, path: object, isStatic: bool, materialManager: MaterialManager) -> object:
        builder = PyEngine3dPlatform.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, isStatic, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensurePrefab(self) -> None: pass

# PyEngine3dShaderBuilder
class PyEngine3dShaderBuilder(ShaderBuilderBase):
    _loader: ShaderLoader = None
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: return self._loader.createShader(path, args)

# PyEngine3dTextureBuilder
class PyEngine3dTextureBuilder(TextureBuilderBase):
    _defaultTexture: Texture = None
    @property
    def defaultTexture(self) -> int:
        if self._defaultTexture: return self._defaultTexture
        self._defaultTexture = self._createDefaultTexture()
        return self._defaultTexture

    def release(self) -> None:
        if self._defaultTexture: self._defaultTexture.release(); self._defaultTexture = None

    def _createDefaultTexture(self) -> int: return base.loader.loadModel('maps/noise.rgb')

    def createTexture(self, reuse: int, source: ITexture, level2: range = None) -> int:
        try:
            if not bytes: return self.defaultTexture
            return base.loader.loadModel('maps/noise.rgb')
        finally: source.end()

    def deleteTexture(self, texture: int) -> None: texture.release()

# PyEngine3dMaterialBuilder
class PyEngine3dMaterialBuilder(MaterialBuilderBase):
    _defaultMaterial: Material = None; _terrainMaterial: Material = None
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

# PyEngine3dGfx
class PyEngine3dGfxModel(IOpenGfxModel):
    def __init__(self):
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, PyEngine3dMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, PyEngine3dObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(PyEngine3dShaderBuilder())
        self.textureManager: TextureManager = TextureManager(PyEngine3dTextureBuilder())
    def preloadObject(self, source: ISource, path: object) -> None: self.objectManager.preloadObject(source, path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preloadTexture(source, path)
    def createObject(self, source: ISource, path: object, isStatic: bool, parent: object = None) -> tuple[object, dict[str, object]]: return self.objectManager.createObject(source, path, isStatic, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> int: return self.textureManager.createTexture(source, path, level)

# PyEngine3dPlatform
class PyEngine3dPlatform(Platform):
    buildersByType: dict[type, callable] = {}
    def __init__(self):
        super().__init__('P3', 'PyEngine3D')
        self.gfxFactory = staticmethod(lambda: [None, None, None, PyEngine3dGfxModel(), None, None])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
PyEngine3dPlatform.this = PyEngine3dPlatform()

#endregion