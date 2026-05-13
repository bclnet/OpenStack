from __future__ import annotations
import sys
from numpy import ndarray
from enum import Enum, Flag
from dataclasses import dataclass

# types
type Vector4 = ndarray

# typedefs
class Object: pass
class Material: pass
class Sprite: pass
class Texture: pass

#region GfX

# GfX:
class GfX:
    XApi = 0
    XSprite2D = 1
    XSprite3D = 2
    XModel = 3
    XLight = 4
    XTerrain = 5
    maxTextureMaxAnisotropy: int = 0

class GfxAttach(Enum): Find = 0; Transform = 1; All = 2; AllCenter = 3

# GfxAlphaMode
class GfxAlphaMode(Enum): Always = 0; Less = 1; LEqual = 2; Equal = 3; GEqual = 4; Greater = 5; NotEqual = 6; Never = 7

# GfxBlendMode
class GfxBlendMode(Enum): Zero = 0; One = 1; DstColor = 2; SrcColor = 3; OneMinusDstColor = 4; SrcAlpha = 5; OneMinusSrcColor = 6; DstAlpha = 7; OneMinusDstAlpha = 8; SrcAlphaSaturate = 9; OneMinusSrcAlpha = 10

#endregion

#region ObjectSprite

# ObjectSpriteBuilderBase
class ObjectSpriteBuilderBase:
    def instanceObject(self, src: Object) -> Object: pass
    def createObject(self, path: object) -> Object: pass
    def ensurePrefab(self) -> None: pass

# ObjectSpriteManager
class ObjectSpriteManager:
    _cachedObjects: dict[object, (Object, object)] = {}
    def __init__(self, builder: ObjectSpriteBuilderBase):
        self._source: ISource = source
        self._builder: ObjectSpriteBuilderBase = builder
        self._preloadTasks: dict[object, object] = {}

    async def createObject(self, path: object, parent: Object = None) -> tuple[Object, object]:
        tag = None
        # load & cache the prefab.
        if not path in self._cachedObjects: prefab = self._cachedObjects[path] = (await self._loadObject(path), tag)
        else: prefab = self._cachedObjects[path]
        return (self._builder.instanceObject(prefab[0]), prefab[1])

    def preloadObject(self, path: object) -> None:
        if path in self._cachedObjects: return
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.getAsset(object, path)

    async def _loadObject(self, path: object) -> tuple[Object, object]:
        assert(not path in self._cachedObjects)
        self._builder.ensurePrefab()
        self.preloadObject(path)
        obj = await self._preloadTasks[path]
        self._preloadTasks.pop(path)
        return (self._builder.createObject(obj), obj)

#endregion

#region ObjectModel

# ObjectModelBuilderBase
class ObjectModelBuilderBase:
    def instanceObject(self, src: Object) -> Object: pass
    def createObject(self, path: object, isStatic: bool, materialManager: MaterialManager) -> Object: pass
    def ensurePrefab(self) -> None: pass

# ObjectModelManager
class ObjectModelManager:
    _cachedObjects: dict[object, (Object, object)] = {}
    def __init__(self, source: ISource, materialManager: MaterialManager, builder: ObjectModelBuilderBase):
        self._source: ISource = source
        self._materialManager: MaterialManager = materialManager
        self._builder: ObjectModelBuilderBase = builder
        self._preloadTasks: dict[object, object] = {}

    async def createObject(self, path: object, isStatic: bool, parent: Object = None) -> tuple[Object, object]:
        try:
            # load & cache the prefab.
            if not path in self._cachedObjects: s = self._cachedObjects[path] = await self._loadObject(path, isStatic)
            else: s = self._cachedObjects[path]
            return (self._builder.instanceObject(s[0]), s[1])
        except: return (None, None)

    def preloadObject(self, path: object) -> None:
        if path in self._cachedObjects: return
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.getAsset(object, path)

    async def _loadObject(self, path: object, isStatic: bool) -> tuple[Object, object]:
        assert(not path in self._cachedObjects)
        self._builder.ensurePrefab()
        self.preloadObject(path)
        try:
            obj = await self._preloadTasks[path]
            return (self._builder.createObject(obj, isStatic, self._materialManager), obj)
        except: print(sys.exc_info()[1]); raise
        finally: self._preloadTasks.pop(path)

#endregion

#region Shader

# Shader
class Shader:
    def __init__(self, getUniformLocation: callable, getAttribLocation: callable, name: str = None, program: int = None, parameters: dict[str, bool] = None, renderModes: list[str] = None):
        self._getUniformLocation: callable = getUniformLocation or _throw('Null')
        self._getAttribLocation: callable = getAttribLocation or _throw('Null')
        self.name: str = name
        self.program: int = program
        self.parameters: dict[str, bool] = parameters
        self.renderModes: list[str] = renderModes
        self._uniforms: dict[str, int] = {}

    def getUniformLocation(self, name: str) -> int:
        if name in self._uniforms: return self._uniforms[name]
        value = self._getUniformLocation(self.program, name); self._uniforms[name] = value;
        return value

    def getAttribLocation(self, name: str) -> int: return self._getAttribLocation(self.program, name)

# ShaderBuilderBase
class ShaderBuilderBase:
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: pass

# ShaderManager
class ShaderManager:
    def __init__(self, source: ISource, builder: ShaderBuilderBase):
        self._source: ISource = source
        self._builder: ShaderBuilderBase = builder
        self.emptyArgs: dict[str, bool] = {}

    def createShader(self, path: object, args: dict[str, bool] = None) -> tuple[Shader, object]: return (self._builder.createShader(path, args or self.emptyArgs), None)

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

# SpriteManager
class SpriteManager:
    _cachedSprites: dict[object, (Sprite, object)] = {}
    def __init__(self, source: ISource, builder: SpriteBuilderBase):
        self._source: ISource = source
        self._builder: SpriteBuilderBase = builder
        self._preloadTasks: dict[object, object] = {}

    @property
    def defaultSprite(self) -> Sprite: return self._builder.defaultSprite

    async def createSprite(self, path: object, level: range = None) -> tuple[Sprite, object]:
        if path in self._cachedSprites: return self._cachedSprites[path]
        # load & cache the texture.
        tag = path if isinstance(path, ISprite) else await self._loadSprite(path)
        obj = self._builder.createSprite(tag) if tag else self._builder.defaultSprite
        self._cachedSprites[path] = (obj, tag)
        return (obj, tag)

    def preloadSprite(self, path: object) -> None:
        if path in self._cachedSprites: return
        # start loading the texture file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.getAsset(object, path)

    def deleteSprite(self, path: object) -> None:
        if not path in self._cachedSprites: return
        self._builder.deleteTexture(self._cachedSprites[0])
        self._cachedSprites.pop(path)

    async def _loadSprite(self, path: object) -> ISprite:
        assert(not path in self._cachedSprites)
        self.preloadSprite(s)
        obj = await self._preloadTasks[path]
        self._preloadTasks.pop(path)
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
    maxTextureMaxAnisotropy: int = GfX.maxTextureMaxAnisotropy
    defaultTexture: Texture
    def createTexture(self, reuse: Texture, tex: ITexture, level: range = None) -> Texture: pass
    def createSolidTexture(self, width: int, height: int, rgba: list[float]) -> Texture: pass
    def createNormalMap(self, tex: Texture, strength: float) -> Texture: pass
    def deleteTexture(self, tex: Texture) -> None: pass

# TextureManager
class TextureManager:
    class Solid:
        def __init__(self, width: int, height: int, rgbas: list[float]):
            self.width = width
            self.height = height
            self.rgbas = rgbas
        def __hash__(self): return hash((self.width, self.height, hash((s for s in self.rgbas))))

    normalMapIntensity: float = 0.75
    _cachedNormalMapTextures: dict[Texture, Texture] = {}
    _cachedSolidTextures: dict[Solid, Texture] = {}
    _cachedTextures: dict[object, (Texture, object)] = {}
    def __init__(self, source: ISource, builder: TextureBuilderBase):
        self._source: ISource = source
        self._builder: TextureBuilderBase = builder
        self._preloadTasks: dict[object, object] = {}

    @property
    def defaultTexture(self) -> Texture: return self._builder.defaultTexture

    def createNormalMapTexture(self, src: Texture, strength: float = -1) -> Texture:
        if src in self._cachedNormalMapTextures: return self._cachedNormalMapTextures[src]
        s = self._builder.createNormalMapTexture(src, TextureManager.normalMapIntensity if strength < 0 else strength)
        self._cachedNormalMapTextures[src] = s
        return s

    def createSolidTexture(self, width: int, height: int, rgbas: object = None) -> Texture:
        src = TextureManager.Solid(width, height, rgbas)
        if src in self._cachedSolidTextures: return self._cachedSolidTextures[src]
        s = self._builder.createSolidTexture(width, height, rgbas)
        self._cachedSolidTextures[src] = s
        return s

    async def createTexture(self, path: object, level: range = None) -> tuple[Texture, object]:
        if path in self._cachedTextures: return self._cachedTextures[path]
        # load & cache the texture.
        tag = path if isinstance(path, ITexture) else await self._loadTexture(path)
        obj = self._builder.createTexture(None, tag, level) if tag else self._builder.defaultTexture
        self._cachedTextures[path] = (obj, tag)
        return (obj, tag)

    def reloadTexture(self, path: object, level: range = None) -> tuple[Texture, object]:
        if path not in self._cachedTextures: return (None, None)
        c = self._cachedTextures[path]
        self._builder.createTexture(c[0], c[1], level)
        return c

    def preloadTexture(self, path: object) -> None:
        if path in self._cachedTextures: return
        # start loading the texture file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.getAsset(type(ITexture), path)

    def deleteTexture(self, path: object) -> None:
        if not path in self._cachedTextures: return
        self._builder.deleteTexture(self._cachedTextures[0])
        self._cachedTextures.pop(path)

    async def _loadTexture(self, path: object) -> ITexture:
        assert(not path in self._cachedTextures)
        self.preloadTexture(path)
        obj = await self._preloadTasks[path]
        self._preloadTasks.pop(path)
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
    vectorParams: dict[str, ndarray]
    textureParams: dict[str, str]
    intAttributes: dict[str, int]
    # floatAttributes: dict[str, float]
    # vectorAttributes: dict[str, ndarray]
    # stringAttributes: dict[str, string]

# MaterialTerrainProp
class MaterialTerrainProp(MaterialProp):
    pass

# MaterialBuilderBase
class MaterialBuilderBase:
    textureManager : TextureManager
    defaultMaterial: Material
    terrainMaterial: Material
    def __init__(self, textureManager: TextureManager): self.textureManager = textureManager
    def createMaterial(self, path: object) -> Material: pass

# MaterialManager
class MaterialManager:
    _cachedMaterials: dict[object, (Material, object)] = {}
    def __init__(self, source: ISource, textureManager: TextureManager, builder: MaterialBuilderBase):
        self._source: ISource = source
        self._textureManager: TextureManager = textureManager
        self._builder: MaterialBuilderBase = builder
        self._preloadTasks: dict[object, object] = {}

    async def createMaterial(self, path: object) -> tuple[Material, object]:
        if path in self._cachedMaterials: return self._cachedMaterials[path]
        # load & cache the material.
        src = path if isinstance(path, MaterialProp) else await self._loadMaterial(path)
        obj = self._builder.createMaterial(src) if src else self._builder.defaultMaterial
        tag = obj[1] if src else None
        self._cachedMaterials[path] = (obj, tag)
        return (obj, tag)

    def preloadMaterial(self, path: object) -> None:
        if path in self._cachedMaterials: return
        # start loading the material file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._source.getAsset(MaterialProp, path)

    async def _loadMaterial(self, path: object) -> MaterialProp:
        assert(not path in self._cachedMaterials)
        self.preloadMaterial(path)
        obj = await self.preloadTasks[path]
        self.preloadTasks.pop(path)
        return obj

#endregion

#region Model

# IModel
class IModel:
    def create(self, platform: str, func: callable) -> object: pass

#endregion

#region OpenGfx

# IOpenGfx:
class IOpenGfx:
    source: ISource

# IHaveOpenGfx
class IHaveOpenGfx:
    gfx: list[IOpenGfx]

# IOpenGfxApiX
class IOpenGfxApiX(IOpenGfx): pass

# IOpenGfxApi
class IOpenGfxApi(IOpenGfxApiX):
    def createObject(self, name: str, tag: str = None, parent: object = None) -> Object: pass
    def createMesh(self, mesh: object) -> object: pass
    def addMeshRenderer(self, src: Object, mesh: object, material: Material, enabled: bool, isStatic: bool) -> None: pass
    def addMeshCollider(self, src: Object, mesh: object, isKinematic: bool, isStatic: bool) -> None: pass
    def setParent(self, src: Object, parent: Object) -> None: pass
    def transform(self, src: Object, position: ndarray, rotation: ndarray, localScale: ndarray) -> None: pass
    def addMissingMeshCollidersRecursively(self, src: Object, isStatic: bool) -> None: pass
    def setLayerRecursively(self, src: Object, layer: int) -> None: pass

# IOpenGfxSpriteX
class IOpenGfxSpriteX(IOpenGfx):
    def preloadSprite(self, path: object) -> None: pass

# IOpenGfxSprite
class IOpenGfxSprite(IOpenGfxSpriteX):
    objectManager: ObjectSpriteManager
    spriteManager: SpriteManager
    async def createObject(self, path: object, parent: Object = None) -> Object: pass

# IOpenGfxModelX:
class IOpenGfxModelX:
    def preloadTexture(self, path: object) -> None: pass

# IOpenGfxModel
class IOpenGfxModel(IOpenGfxModelX):
    materialManager: MaterialManager
    objectManager: ObjectModelManager
    shaderManager: ShaderManager
    textureManager: TextureManager
    async def createObject(self, path: object, isStatic: bool, parent: Object = None) -> Object: pass
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: pass
    async def createTexture(self, path: object, level: range = None) -> Texture: pass
    def postObject(self, src: Object, position: Vector3, eulerAngles: Vector3, scale: float, parent: Object = None) -> None: pass

# IOpenGfxLightX:
class IOpenGfxLightX: pass

# IOpenGfxLight
class IOpenGfxLight(IOpenGfxLightX):
    def createLight(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: Object = None) -> Object: pass
    def createReflectionProbe(self, name: str, position: Vector3, parent: Object = None) -> Object: pass

# GfxTerrainLayer:
class GfxTerrainLayer[Texture_]:
    def __init__(self, texture: Texture_ = None, smoothness: float = .0, metallic: float = .0, specular: Color = None, maskMapTexture: Texture_ = None, normalMapTexture: Texture_ = None, tileSize: Vector2 = None):
        self.texture = texture
        self.smoothness = smoothness
        self.metallic = metallic
        self.specular = specular
        self.maskMapTexture = maskMapTexture
        self.normalMapTexture = normalMapTexture
        self.tileSize = tileSize

# IOpenGfxTerrainX:
class IOpenGfxTerrainX: pass

# IOpenGfxTerrain
class IOpenGfxTerrain(IOpenGfxTerrainX):
    def createTerrainData(self, offset: int, heights: list[list[float]], heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer[Texture]], alphaMap: list[list[list[float]]]) -> Object: pass
    def createTerrain(self, name: str, position: Vector3, data: object, parent: Object = None) -> Object: pass

#endregion