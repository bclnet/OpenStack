from __future__ import annotations
import os, io, numpy as np
from openstk.gfx import IOpenGfxModel, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, Shader, ShaderBuilderBase, ShaderManager, TextureBuilderBase, TextureManager
from openstk.platform import Platform, SystemSfx

#region OpenGfx

# Panda3dObjectModelBuilder
class Panda3dObjectModelBuilder(ObjectModelBuilderBase):
    def ensurePrefab(self) -> None: pass
    def createNewObject(self, prefab: object) -> object: raise NotImplementedError()
    def createObject(self, path: object, materialManager: MaterialManager) -> object: raise NotImplementedError()

# Panda3dShaderBuilder
class Panda3dShaderBuilder(ShaderBuilderBase):
    _loader: ShaderLoader = None
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: return self._loader.createShader(path, args)

# Panda3dTextureBuilder
# https://docs.panda3d.org/1.10/python/programming/texturing/simple-texturing#simple-texturing
# https://docs.panda3d.org/1.10/python/programming/texturing/creating-textures#creating-new-textures-from-scratch
class Panda3dTextureBuilder(TextureBuilderBase):
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

# Panda3dMaterialBuilder
# https://docs.panda3d.org/1.10/python/programming/render-attributes/materials
class Panda3dMaterialBuilder(MaterialBuilderBase):
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

# Panda3dGfx
class Panda3dGfxModel(IOpenGfxModel):
    source: ISource
    textureManager: TextureManager
    materialManager: MaterialManager
    objectManager: ObjectModelManager
    shaderManager: ShaderManager

    def __init__(self, source: ISource):
        self.source = source
        self.textureManager = TextureManager(source, Panda3dTextureBuilder())
        self.materialManager = MaterialManager(source, self.textureManager, Panda3dMaterialBuilder(self.textureManager))
        self.objectManager = ObjectModelManager(source, self.materialManager, Panda3dObjectModelBuilder())
        self.shaderManager = ShaderManager(source, Panda3dShaderBuilder())

    def createTexture(self, path: object, level: range = None) -> int: return self.textureManager.createTexture(path, level)[0]
    def preloadTexture(self, path: object) -> None: self.textureManager.preloadTexture(path)
    def createObject(self, path: object) -> (object, dict[str, object]): return self.objectManager.createObject(path)[0]
    def preloadObject(self, path: object) -> None: self.objectManager.preloadObject(path)
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(path, args)[0]
    def loadFileObject(self, type: type, path: object) -> object: return self.source.loadFileObject(type, path)

# Panda3dPlatform
class Panda3dPlatform(Platform):
    def __init__(self):
        super().__init__('PD', 'Panda3D')
        self.gfxFactory = staticmethod(lambda source: [None, None, Panda3dGfxModel(source)])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
Panda3dPlatform.This = Panda3dPlatform()

#endregion