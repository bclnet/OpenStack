from __future__ import annotations
import numpy as np
from enum import Enum, Flag
from dataclasses import dataclass

# typedefs
class Object: pass
class Material: pass
class Sprite: pass
class Texture: pass

#region Extensions

# GfxX:
class GfxX:
    XSprite2D = 0
    XSprite3D = 1
    XModel = 2
    maxTextureMaxAnisotropy: int = 0

# GfxExtensions
class GfxExtensions: pass

# GfxAlphaMode
class GfxAlphaMode(Enum):
    Always = 0
    Less = 1
    LEqual = 2
    Equal = 3
    GEqual = 4
    Greater = 5
    NotEqual = 6
    Never = 7

# GfxBlendMode
class GfxBlendMode(Enum):
    Zero = 0
    One = 1
    DstColor = 2
    SrcColor = 3
    OneMinusDstColor = 4
    SrcAlpha = 5
    OneMinusSrcColor = 6
    DstAlpha = 7
    OneMinusDstAlpha = 8
    SrcAlphaSaturate = 9
    OneMinusSrcAlpha = 10

#endregion

#region ObjectSprite

# ObjectSpriteBuilderBase
class ObjectSpriteBuilderBase:
    def ensurePrefab(self) -> None: pass
    def createNewObject(self, prefab: Object) -> Object: pass
    def createObject(self, src: object) -> Object: pass

# IObjectSpriteManager
class IObjectSpriteManager:
    def createObject(self, path: object) -> (Object, object): pass
    def preloadObject(self, path: object) -> None: pass

# ObjectSpriteManager
class ObjectSpriteManager(IObjectSpriteManager):
    _source: ISource
    _builder: ObjectSpriteBuilderBase
    _cachedObjects: dict[object, (Object, object)] = {}
    _preloadTasks: dict[object, object] = {}
    def __init__(self, source: ISource, builder: ObjectSpriteBuilderBase):
        self._source = source
        self._builder = builder

    def createObject(self, path: object) -> (Object, object):
        tag = None
        self._builder.ensurePrefab()
        # load & cache the prefab.
        if not path in self._cachedObjects: prefab = self._cachedObjects[path] = (self._loadObject(path), tag)
        else: prefab = self._cachedObjects[path]
        return (self._builder.createNewObject(prefab[0]), prefab[1])
 
    def preloadObject(self, path: object) -> None:
        if path in self._cachedObjects: return
        # start loading the object asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(object, path)

    async def _loadObject(self, path: object) -> (Object, object):
        assert(not path in self._cachedObjects)
        self.preloadObject(path)
        obj = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return (self._builder.createObject(obj), obj)

#endregion

#region ObjectModel

# ObjectModelBuilderBase
class ObjectModelBuilderBase:
    def ensurePrefab(self) -> None: pass
    def createNewObject(self, prefab: Object) -> Object: pass
    def createObject(self, src: object, materialManager: IMaterialManager) -> Object: pass

# IObjectModelManager
class IObjectModelManager:
    def createObject(self, path: object) -> (Object, object): pass
    def preloadObject(self, path: object) -> None: pass

# ObjectModelManager
class ObjectModelManager(IObjectModelManager):
    _source: ISource
    _materialManager: IMaterialManager
    _builder: ObjectModelBuilderBase
    _cachedObjects: dict[object, (Object, object)] = {}
    _preloadTasks: dict[object, object] = {}
    def __init__(self, source: ISource, materialManager: IMaterialManager, builder: ObjectModelBuilderBase):
        self._source = source
        self._materialManager = materialManager
        self._builder = builder

    def createObject(self, path: object) -> (Object, object):
        tag = None
        self._builder.ensurePrefab()
        # load & cache the prefab.
        if not path in self._cachedObjects: prefab = self._cachedObjects[path] = (self._loadObject(path), tag)
        else: prefab = self._cachedObjects[path]
        return (self._builder.createNewObject(prefab[0]), prefab[1])
 
    def preloadObject(self, path: object) -> None:
        if path in self._cachedPrefabs: return
        # start loading the object asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(object, path)

    async def _loadObject(self, path: object) -> (Object, object):
        assert(not path in self._cachedObjects)
        self.preloadObject(path)
        obj = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return (self._builder.buildObject(obj, self._materialManager), obj)

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

#region Sprite

# ISprite
class ISprite:
    width: int
    height: int
    def create(self, platform: str, func: callable) -> object: pass

# SpriteBuilderBase
class SpriteBuilderBase:
    defaultSprite: Sprite
    def createSprite(self, spr: ISprite) -> Sprite: pass
    def deleteSprite(self, spr: Sprite) -> None: pass

# ISpriteManager:
class ISpriteManager:
    defaultSprite: Sprite
    def createSprite(self, path: object) -> (Sprite, object): pass
    def preloadSprite(self, path: object) -> None: pass
    def deleteSprite(self, path: object) -> None: pass

# SpriteManager
class SpriteManager(ISpriteManager):
    _source: ISource
    _builder: SpriteBuilderBase
    _cachedSprites: dict[object, (Sprite, object)] = {}
    _preloadTasks: dict[object, object] = {}
    def __init__(self, source: ISource, builder: SpriteBuilderBase):
        self._source = source
        self._builder = builder

    @property
    def defaultSprite(self) -> Sprite: return self._builder.defaultSprite

    def createSprite(self, path: object, level: range = None) -> (Sprite, object):
        if path in self._cachedSprites: return self._cachedSprites[path]
        # load & cache the texture.
        tag = path if isinstance(path, ISprite) else self._loadSprite(path)
        obj = self._builder.createSprite(tag) if tag else self._builder.defaultSprite
        self._cachedSprites[path] = (obj, tag)
        return (obj, tag)

    def preloadSprite(self, path: object) -> None:
        if path in self._cachedSprites: return
        # start loading the texture file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(object, path)

    def deleteSprite(self, path: object) -> None:
        if not path in self._cachedSprites: return
        self._builder.deleteTexture(self._cachedSprites[0])
        self._cachedSprites.remove(path)

    async def _loadSprite(self, path: object) -> ISprite:
        assert(not path in self._cachedSprites)
        self.preloadSprite(s)
        obj = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return obj

#endregion

#region Texture

# Texture_Bytes
@dataclass
class Texture_Bytes:
    bytes: bytes
    format: object
    spans: list[range]

# ITexture
class ITexture:
    width: int
    height: int
    depth: int
    mipMaps: int
    texFlags: TextureFlags
    def create(self, platform: str, func: callable) -> object: pass

# ITextureSelect
class ITextureSelect(ITexture):
    def select(self, id: int) -> None: pass

# ITextureVideo
class ITextureVideo(ITexture):
    fps: int
    hasFrames: bool
    def decodeFrame(self) -> bool: pass

# ITextureFrames
class ITextureFrames(ITextureVideo):
    frameMax: int
    def frameSelect(self, id: int) -> None: pass

# TextureBuilderBase
class TextureBuilderBase:
    maxTextureMaxAnisotropy: int = GfxX.maxTextureMaxAnisotropy
    defaultTexture: Texture
    def createTexture(self, reuse: Texture, tex: ITexture, level: range = None) -> Texture: pass
    def createSolidTexture(self, width: int, height: int, rgba: list[float]) -> Texture: pass
    def createNormalMap(self, tex: Texture, strength: float) -> Texture: pass
    def deleteTexture(self, tex: Texture) -> None: pass

# ITextureManager:
class ITextureManager:
    defaultTexture: Texture
    def createNormalMap(self, tex: Texture, strength: float) -> Texture: pass
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
    _preloadTasks: dict[object, object] = {}
    def __init__(self, source: ISource, builder: TextureBuilderBase):
        self._source = source
        self._builder = builder

    def createSolidTexture(self, width: int, height: int, rgba: list[float] = None) -> Texture: return self._builder.createSolidTexture(width, height, rgba)

    def createNormalMap(self, tex: Texture, strength: float) -> Texture: return self._builder.createNormalMap(tex, strength)

    @property
    def defaultTexture(self) -> Texture: return self._builder.defaultTexture

    def createTexture(self, path: object, level: range = None) -> (Texture, object):
        if path in self._cachedTextures: return self._cachedTextures[path]
        # load & cache the texture.
        tag = path if isinstance(path, ITexture) else self._loadTexture(path)
        obj = self._builder.createTexture(None, tag, level) if tag else self._builder.defaultTexture
        self._cachedTextures[path] = (obj, tag)
        return (obj, tag)

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
        obj = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return obj

#endregion

#region Material

# IMaterial
class IMaterial:
    def create(self, platform: str, func: callable) -> object: pass

# MaterialProp
class MaterialProp:
    tag: object

# MaterialStdProp
class MaterialStdProp(MaterialProp):
    textures: dict[str, str] = {}
    alphaBlended: bool
    srcBlendMode: GfxBlendMode
    dstBlendMode: GfxBlendMode
    alphaTest: bool
    alphaCutoff: float

# MaterialStd2Prop
class MaterialStd2Prop(MaterialStdProp):
    zwrite: bool
    diffuseColor: Color
    specularColor: Color
    emissiveColor: Color
    glossiness: float
    alpha: float

# MaterialShaderProp
class MaterialShaderProp(MaterialProp):
    shaderName: str
    shaderArgs: dict[str, bool] = {}

# MaterialShaderVProp
class MaterialShaderVProp(MaterialShaderProp):
    intParams: dict[str, int]
    floatParams: dict[str, float]
    vectorParams: dict[str, np.ndarray]
    textureParams: dict[str, str]
    intAttributes: dict[str, int]
    # floatAttributes: dict[str, float]
    # vectorAttributes: dict[str, np.ndarray]
    # stringAttributes: dict[str, string]

# MaterialTerrainProp
class MaterialTerrainProp(MaterialProp):
    pass

# MaterialBuilderBase
class MaterialBuilderBase:
    textureManager : ITextureManager
    normalGeneratorIntensity: float = 0.75
    defaultMaterial: Material
    def __init__(self, textureManager: ITextureManager): self.textureManager = textureManager
    def createMaterial(self, path: object) -> Material: pass

# IMaterialManager
class IMaterialManager:
    textureManager: ITextureManager
    def createMaterial(self, path: object) -> (Material, object): pass
    def preloadMaterial(self, path: object) -> None: pass

# MaterialManager
class MaterialManager(IMaterialManager):
    _source: ISource
    _builder: MaterialBuilderBase
    _cachedMaterials: dict[object, (Material, object)] = {}
    _preloadTasks: dict[object, object] = {}
    textureManager: ITextureManager
    def __init__(self, source: ISource, textureManager: ITextureManager, builder: MaterialBuilderBase):
        self._source = source
        self._textureManager = textureManager
        self._builder = builder

    def createMaterial(self, path: object) -> (Material, object):
        if path in self._cachedMaterials: return self._cachedMaterials[path]
        # load & cache the material.
        src = path if isinstance(path, MaterialProp) else self._loadMaterial(path)
        obj = self._builder.createMaterial(src) if src else self._builder.defaultMaterial
        tag = obj[1] if src else None
        self._cachedMaterials[path] = (obj, tag)
        return (obj, tag)

    def preloadMaterial(self, path: object) -> None:
        if path in self._cachedMaterials: return
        # start loading the material file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.loadFileObject(MaterialProp, path)

    async def _loadMaterial(self, path: object) -> MaterialProp:
        assert(not path in self._cachedMaterials)
        self.preloadMaterial(path)
        obj = await self.preloadTasks[path]
        self.preloadTasks.remove(path)
        return obj

#endregion

#region Model

# IModel
class IModel:
    def create(self, platform: str, func: callable) -> object: pass

# IModelApi
class IModelApi:
    def createObject(self, name: str) -> Object: pass
    def createMesh(self, mesh: object) -> object: pass
    def addMeshRenderer(self, src: Object, mesh: object, material: Material, enabled: bool, isStatic: bool) -> None: pass
    def addMeshCollider(self, src: Object, mesh: object, isKinematic: bool, isStatic: bool) -> None: pass
    #
    def setParent(self, src: Object, parent: Object) -> None: pass
    def transform(self, src: Object, position: np.ndarray, rotation: np.ndarray, localScale: np.ndarray) -> None: pass
    def addMissingMeshCollidersRecursively(self, src: Object, isStatic: bool) -> None: pass
    def setLayerRecursively(self, src: Object, layer: int) -> None: pass

#endregion

#region Renderer

# Renderer
class Renderer:
    def start(self) -> None: pass
    def stop(self) -> None: pass
    def update(self, deltaTime: float) -> None: pass
    def dispose(self) -> None: pass

#endregion

#region OpenGfx

# IOpenGfx:
class IOpenGfx:
    def loadFileObject(self, type: type, path: object): pass
    def preloadObject(self, path: object) -> None: pass

# IOpenGfxSprite
class IOpenGfxSprite(IOpenGfx):
    def preloadSprite(self, path: object) -> None: pass

# IOpenGfxSprite2
class IOpenGfxSprite2(IOpenGfxSprite):
    spriteManager: ISpriteManager
    objectManager: IObjectSpriteManager
    def createObject(self, path: object) -> Object: pass

# IOpenGfxModel:
class IOpenGfxModel:
    def preloadTexture(self, path: object) -> None: pass

# IOpenGfxModel2
class IOpenGfxModel2(IOpenGfxModel):
    textureManager: ITextureManager
    materialManager: IMaterialManager
    objectManager: IObjectManager
    shaderManager: IShaderManager
    def createTexture(self, path: object, level: range = None) -> Texture: pass
    def createObject(self, path: object) -> Object: pass
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: pass

#endregion