from __future__ import annotations
import numpy as np

# typedefs
class IVBIB: pass
class Object: pass
class Material: pass
class Texture: pass

#region Object

# ObjectBuilderBase
class ObjectBuilderBase:
    def ensurePrefab(self) -> None: pass
    def createNewObject(self, prefab: object) -> object: pass
    def createObject(self, source: object, materialManager: IMaterialManager) -> object: pass

# IObjectManager
class IObjectManager:
    def createObject(self, path: object) -> (Object, object): pass
    def preloadObject(self, path: object) -> None: pass

# ObjectManager
class ObjectManager(IObjectManager):
    _source: ISource
    _materialManager: IMaterialManager
    _builder: ObjectBuilderBase
    _cachedObjects: dict[str, object] = {}
    _preloadTasks: dict[str, object] = {}

    def __init__(self, source: ISource, materialManager: IMaterialManager, builder: ObjectBuilderBase):
        self._source = source
        self._materialManager = materialManager
        self._builder = builder

    def createNewObject(self, path: object) -> (object, object):
        tag = None
        self._builder.ensurePrefab()
        # load & cache the prefab.
        if not path in self._cachedObjects: prefab = self._cachedObjects[path] = (self._loadObject(path), tag)
        else: prefab = self._cachedObjects[path]
        # instantiate the prefab.
        return (self._builder.createNewObject(prefab[0]), prefab[1])
 
    def preloadObject(self, path: object) -> None:
        if path in self._cachedPrefabs: return
        # start loading the object asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(object, path)

    async def _loadObject(self, path: object) -> object:
        assert(not path in self._cachedPrefabs)
        self.preloadObject(path)
        source = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return self._builder.buildObject(source, self._materialManager)

#endregion

#region Shader

# Shader
class Shader:
    _getUniformLocation: callable
    _getAttribLocation: callable
    _uniforms: dict[str, int] = {}
    name: str
    program: int
    parameters: dict[str, bool]
    renderModes: list[str]

    def __init__(self, getUniformLocation: callable, getAttribLocation: callable, name: str = None, program: int = None, parameters: dict[str, bool] = None, renderModes: list[str] = None):
        self._getUniformLocation = getUniformLocation or _throw('Null')
        self._getAttribLocation = getAttribLocation or _throw('Null')
        self.name = name
        self.program = program
        self.parameters = parameters
        self.renderModes = renderModes
    
    def getUniformLocation(self, name: str) -> int:
        if name in self._uniforms: return self._uniforms[name]
        value = self._getUniformLocation(self.program, name); self._uniforms[name] = value;
        return value

    def getAttribLocation(self, name: str) -> int: return self._getAttribLocation(self.program, name)

# ShaderBuilderBase
class ShaderBuilderBase:
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: pass

# IShaderManager
class IShaderManager:
    def createShader(self, path: object, args: dict[str: bool] = None) -> (Shader, object): pass

# ShaderManager
class ShaderManager(IShaderManager):
    emptyArgs: dict[str, bool] = {}
    _source: ISource
    _builder: ShaderBuilderBase

    def __init__(self, source: ISource, builder: ShaderBuilderBase):
        self._source = source
        self._builder = builder
    
    def createShader(self, path: object, args: dict[str, bool] = None) -> (Shader, object):
        return (self._builder.createShader(path, args or self.emptyArgs), None)

#endregion

#region Texture

# TextureBuilderBase
class TextureBuilderBase:
    maxTextureMaxAnisotropy: int = GfxStats.maxTextureMaxAnisotropy
    defaultTexture: Texture

    def createTexture(self, reuse: Texture, source: ITexture, level: range = None) -> Texture: pass
    def createSolidTexture(self, width: int, height: int, rgba: list[float]) -> Texture: pass
    def createNormalMap(self, texture: Texture, strength: float) -> Texture: pass
    def deleteTexture(self, texture: Texture) -> None: pass

# ITextureManager:
class ITextureManager:
    defaultTexture: Texture

    def createNormalMap(self, texture: Texture, strength: float) -> Texture: pass
    def createSolidTexture(self, width: int, height: int, rgba: list[float]) -> Texture: pass
    def createTexture(self, path: object, level: range = None) -> (Texture, object): pass
    def reloadTexture(self, path: object, level: range = None) -> (Texture, object): pass
    def preloadTexture(self, path: object) -> None: pass
    def deleteTexture(self, path: object) -> None: pass

# TextureManager
class TextureManager(ITextureManager):
    _source: ISource
    _builder: TextureBuilderBase
    _cachedTextures: dict[object, (Texture, object)] = {}
    _preloadTasks: dict[object, ITexture] = {}

    def __init__(self, source: ISource, builder: TextureBuilderBase):
        self._source = source
        self._builder = builder

    def createSolidTexture(self, width: int, height: int, rgba: list[float] = None) -> Texture: return self._builder.createSolidTexture(width, height, rgba)

    def createNormalMap(self, source: Texture, strength: float) -> Texture: return self._builder.createNormalMap(source, strength)

    @property
    def defaultTexture(self) -> Texture: return self._builder.defaultTexture

    def createTexture(self, path: object, level: range = None) -> (Texture, object):
        if path in self._cachedTextures: return self._cachedTextures[path]
        # load & cache the texture.
        tag = path if isinstance(path, ITexture) else self._loadTexture(path)
        texture = self._builder.createTexture(None, tag, level) if tag else self._builder.defaultTexture
        self._cachedTextures[path] = (texture, tag)
        return (texture, tag)

    def reloadTexture(self, path: object, level: range = None) -> (Texture, object):
        if path not in self._cachedTextures: return (None, None)
        c = self._cachedTextures[path]
        self._builder.createTexture(c[0], c[1], level)
        return c

    def preloadTexture(self, path: object) -> None:
        if path in self._cachedTextures: return
        # start loading the texture file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(object, path)

    def deleteTexture(self, path: object) -> None:
        if not path in self._cachedTextures: return
        self._builder.deleteTexture(self._cachedTextures[0])
        self._cachedTextures.remove(path)

    async def _loadTexture(self, path: object) -> ITexture:
        assert(not path in self._cachedTextures)
        self.preloadTexture(s)
        source = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return source

#endregion

#region Material

# MaterialProp
class MaterialProp:
    tag: object

# MaterialStandardProp
class MaterialStandardProp(MaterialProp):
    basePath: str
    alphaBlended: bool
    alphaTest: bool

# MaterialTerrainProp
class MaterialTerrainProp(MaterialProp):
    pass

# # IMaterial
# class IMaterial:
#     name: str
#     shaderName: str
#     data: dict[str, object]
#     def getShaderArgs(self) -> dict[str, bool]: pass

# # IFixedMaterial
# class IFixedMaterial(IMaterial):
#     mainFilePath: str
#     darkFilePath: str
#     detailFilePath: str
#     glossFilePath: str
#     glowFilePath: str
#     bumpFilePath: str
#     alphaBlended: bool
#     srcBlendMode: int
#     dstBlendMode: int
#     alphaTest: bool
#     alphaCutoff: float
#     zwrite: bool

# # IParamMaterial
# class IParamMaterial(IMaterial):
#     intParams: dict[str, int]
#     floatParams: dict[str, float]
#     vectorParams: dict[str, np.ndarray]
#     textureParams: dict[str, str]
#     intAttributes: dict[str, int]

# MaterialBuilderBase
class MaterialBuilderBase:
    textureManager : ITextureManager
    normalGeneratorIntensity: float = 0.75
    defaultMaterial: Material

    def __init__(self, textureManager: ITextureManager): self.TextureManager = textureManager
    def createMaterial(self, path: object) -> Material: pass

# IMaterialManager
class IMaterialManager:
    textureManager: ITextureManager

    def createMaterial(self, key: object) -> (Material, object): pass
    def preloadMaterial(self, key: object) -> None: pass

# MaterialManager
class MaterialManager(IMaterialManager):
    _source: ISource
    _builder: MaterialBuilderBase
    _cachedMaterials: dict[object, (Material, object)] = {}
    _preloadTasks: dict[object, IMaterial] = {}
    textureManager: ITextureManager

    def __init__(self, source: ISource, textureManager: ITextureManager, builder: MaterialBuilderBase):
        self._source = source
        self._textureManager = textureManager
        self._builder = builder

    def createMaterial(self, path: object) -> (Material, object):
        if path in self._cachedMaterials: return self._cachedMaterials[path]
        # load & cache the material.
        source = path if isinstance(path, MaterialProp) else self._loadMaterial(path)
        material = self._builder.createMaterial(source) if source else self._builder.defaultMaterial
        tag = None #source.data if source else None
        self._cachedMaterials[path] = (material, tag)
        return (material, tag)

    def preloadMaterial(self, path: object) -> None:
        if path in self._cachedMaterials: return
        # start loading the material file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(MaterialProp, path)

    async def _loadMaterial(self, path: object) -> MaterialProp:
        assert(not path in self._cachedMaterials)
        self.preloadMaterial(path)
        source = await self.preloadTasks[path]
        self.preloadTasks.remove(path)
        return source

#endregion

#region Particles

# IParticleSystem
class IParticleSystem:
    data: dict[str, object]
    renderers: list[dict[str, object]]
    operators: list[dict[str, object]]
    initializers: list[dict[str, object]]
    emitters: list[dict[str, object]]
    def getChildParticleNames(self, enabledOnly: bool = False) -> list[str]: pass

#endregion

#region Model

# IModel
class IModel:
    data: dict[str, object]
    def remapBoneIndices(self, vbib: IVBIB, meshIndex: int) -> IVBIB: pass

#endregion

# IOpenGfx2d:
class IOpenGfx2d:
    def loadFileObject(self, path: object): pass

# IOpenGfx2dAny
class IOpenGfx2dAny(IOpenGfx2d):
    pass

# IOpenGfx3d:
class IOpenGfx3d:
    def loadFileObject(self, path: object): pass
    def preloadTexture(self, path: object): pass
    def preloadObject(self, path: object): pass

# IOpenGfx3dAny
class IOpenGfx3dAny(IOpenGfx3d):
    textureManager: ITextureManager
    materialManager: IMaterialManager
    objectManager: IObjectManager
    shaderManager: IShaderManager
    def createTexture(self, path: object, level: range = None) -> Texture: pass
    def createObject(self, path: object) -> Object: pass
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: pass

# GfxStats:
class GfxStats:
    maxTextureMaxAnisotropy: int = 0
