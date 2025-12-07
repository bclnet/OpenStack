from __future__ import annotations
import os, io, numpy as np
from openstk.gfx import IOpenGfxModel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureManager, TextureBuilderBase
from openstk.platforms import Platform, SystemSfx

# typedefs
class ISource: pass

#region OpenGfx

# PygameObjectModelBuilder
class PygameObjectModelBuilder(ObjectModelBuilderBase):
    def ensurePrefab(self) -> None: pass
    def createNewObject(self, prefab: object) -> object: raise NotImplementedError()
    def createObject(self, path: object, materialManager: MaterialManager) -> object: raise NotImplementedError()

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

    def _createDefaultTexture(self) -> int: return self.createSolidTexture(4, 4, np.array([
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
        ], dtype = np.float32))

    def createTexture(self, reuse: int, source: ITexture, level2: range = None) -> int:
        pass

    def createSolidTexture(self, width: int, height: int, pixels: np.array) -> int:
        pass

    def createNormalMap(self, source: int, strength: float) -> int: raise NotImplementedError()

    def deleteTexture(self, texture: int) -> None: pass

# PygameMaterialBuilder
class PygameMaterialBuilder(MaterialBuilderBase):
    _defaultMaterial: GLRenderMaterial
    @property
    def defaultMaterial(self) -> int:
        if self._defaultMaterial: return self._defaultMaterial
        self._defaultMaterial = self._createDefaultMaterial(-1)
        return self._defaultMaterial

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefaultMaterial(type: int) -> GLRenderMaterial:
        m = GLRenderMaterial(None)
        m.textures['g_tColor'] = self.textureManager.defaultTexture
        m.material.shaderName = 'vrf.error'
        return m

    def createMaterial(self, key: object) -> GLRenderMaterial:
        match key:
            case s if isinstance(key, IMaterial):
                match s:
                    case m if isinstance(key, IFixedMaterial): return m
                    case p if isinstance(key, IMaterial):
                        for tex in p.textureParams: m.textures[tex.key], _ = self.textureManager.createTexture(f'{tex.Value}_c')
                        if 'F_SOLID_COLOR' in p.intParams and p.intParams['F_SOLID_COLOR'] == 1:
                            a = p.vectorParams['g_vColorTint']
                            m.textures['g_tColor'] = self.textureManager.buildSolidTexture(1, 1, a[0], a[1], a[2], a[3])
                        if not 'g_tColor' in m.textures: m.textures['g_tColor'] = self.textureManager.defaultTexture

                        # Since our shaders only use g_tColor, we have to find at least one texture to use here
                        if m.textures['g_tColor'] == self.textureManager.defaultTexture:
                            for name in ['g_tColor2', 'g_tColor1', 'g_tColorA', 'g_tColorB', 'g_tColorC']:
                                if name in m.textures:
                                    m.textures['g_tColor'] = m.textures[name]
                                    break

                        # Set default values for scale and positions
                        if not 'g_vTexCoordScale' in p.vectorParams: p.vectorParams['g_vTexCoordScale'] = np.ones(4)
                        if not 'g_vTexCoordOffset' in p.vectorParams: p.vectorParams['g_vTexCoordOffset'] = np.zeros(4)
                        if not 'g_vColorTint' in p.vectorParams: p.vectorParams['g_vColorTint'] = np.ones(4)
                        return m
                    case _: raise Exception(f'Unknown: {s}')
            case _: raise Exception(f'Unknown: {key}')

# PygameGfxModel
class PygameGfxModel(IOpenGfxModel):
    source: ISource
    textureManager: TextureManager
    materialManager: MaterialManager
    objectManager: ObjectModelManager
    shaderManager: ShaderManager

    def __init__(self, source: PakFile):
        self.source = source
        self.textureManager = TextureManager(source, PygameTextureBuilder())
        self.materialManager = MaterialManager(source, self.textureManager, PygameMaterialBuilder(self.textureManager))
        self.objectManager = ObjectModelManager(source, self.materialManager, PygameObjectModelBuilder())
        self.shaderManager = ShaderManager(source, PygameShaderBuilder())

    def createTexture(self, path: object, level: range = None) -> int: return self.textureManager.createTexture(path, level)[0]
    def preloadTexture(self, path: object) -> None: self.textureManager.preloadTexture(path)
    def createObject(self, path: object) -> (object, dict[str, object]): return self.objectManager.createObject(path)[0]
    def preloadObject(self, path: object) -> None: self.objectManager.preloadObject(path)
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(path, args)[0]
    def loadFileObject(self, type: type, path: object) -> object: return self.source.loadFileObject(type, path)


# PygamePlatform
class PygamePlatform(Platform):
    def __init__(self):
        super().__init__('PG', 'Pygame')
        self.gfxFactory = staticmethod(lambda source: [None, None, PygameGfxModel(source)])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
PygamePlatform.This = PygamePlatform()

#endregion