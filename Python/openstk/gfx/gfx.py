from __future__ import annotations
import sys
from numpy import ndarray
from enum import Enum, Flag
from dataclasses import dataclass

# types
type Vector4 = ndarray

# typedefs
class B_Object: pass
class B_Material: pass
class B_Sprite: pass
class B_Texture: pass

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
class GfxAlphaMode(Enum): Never = 0x0200; Less = 0x0201; Equal = 0x0202; LEqual = 0x0203; Greater = 0x0204; NotEqual = 0x0205; GEqual = 0x0206; Always = 0x0207

# GfxBlendMode
class GfxBlendMode(Enum): Zero = 0; One = 1; SrcColor = 0x0300; OneMinusSrcColor = 0x0301; SrcAlpha = 0x0302; OneMinusSrcAlpha = 0x0303; DstAlpha = 0x0304; OneMinusDstAlpha = 0x0305; DstColor = 0x0306; OneMinusDstColor = 0x0307; SrcAlphaSaturate = 0x0308 

#endregion

#region ObjectSprite

# ObjectSpriteBuilder
class ObjectSpriteBuilder:
    def instance(self, src: B_Object) -> B_Object: pass
    def create(self, path: object) -> B_Object: pass
    def ensure(self) -> None: pass

# ObjectSpriteManager
class ObjectSpriteManager:
    _cached: dict[object, (B_Object, object)] = {}
    def __init__(self, builder: ObjectSpriteBuilder):
        self._builder: ObjectSpriteBuilder = builder
        self._tasks: dict[object, object] = {}

    async def create(self, source: ISource, path: object, parent: B_Object = None) -> tuple[B_Object, object]:
        key = (source, path); tag = None
        if not key in self._cached: obj = self._cached[key] = (await self._load(source, path), tag)
        else: obj = self._cached[key]
        return (self._builder.instance(obj[0], parent), obj[1])

    def preload(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cached: return
        if not key in self._tasks: self._tasks[key] = source.getAsset(object, path)

    async def _load(self, path: object) -> tuple[B_Object, object]:
        key = (source, path)
        assert(not key in self._cached)
        self._builder.ensure()
        self.preload(source, path)
        obj = await self._tasks[key]
        self._tasks.pop(key)
        return (self._builder.create(obj), obj)

#endregion

#region ObjectModel

# IObjectModel
class IObjectModel:
    def create(self, platform: str, func: callable) -> object: pass

# ObjectModelBuilder
class ObjectModelBuilder:
    def instance(self, src: B_Object) -> B_Object: pass
    def create(self, source: ISource, path: object, static_: bool, materialManager: MaterialManager) -> B_Object: pass
    def ensure(self) -> None: pass

# ObjectModelManager
class ObjectModelManager:
    _cached: dict[object, (B_Object, object)] = {}
    def __init__(self, materialManager: MaterialManager, builder: ObjectModelBuilder):
        self._materialManager: MaterialManager = materialManager
        self._builder: ObjectModelBuilder = builder
        self._tasks: dict[object, object] = {}

    async def create(self, source: ISource, path: object, static_: bool, parent: B_Object = None) -> tuple[B_Object, object]:
        key = (source, path)
        try:
            if not key in self._cached: s = self._cached[key] = await self._load(source, path, static_)
            else: s = self._cached[key]
            return (self._builder.instance(s[0]), s[1])
        except: return (None, None)

    def preload(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cached: return
        if not key in self._tasks: self._tasks[key] = source.getAsset(object, path)

    async def _load(self, source: ISource, path: object, static_: bool) -> tuple[B_Object, object]:
        key = (source, path)
        assert(not key in self._cached)
        self._builder.ensure()
        self.preload(source, path)
        try:
            obj = await self._tasks[key]
            return (await self._builder.create(source, obj, static_, self._materialManager), obj)
        except: print(sys.exc_info()[1]); raise
        finally: self._tasks.pop(key)

#endregion

#region Shader

# ShaderSeed: int = 0x13141516
# _cachedShaders: dict[int, B_Shader] = {}
# _shaderDefines: dict[str, list[str]] = {}
# cache = not name.startswith('#')
# def _calculateShaderCacheHash(self, name: str, args: dict[str, bool]) -> int:
#     b = [name]
#     parameters = set(self._shaderDefines[name]).intersection(args.keys())
#     for key in parameters: b.append(key); b.append('t' if args[key] else 'f')
#     return hash('\n'.join(b))
# # cache
# if cache and shaderFileName in self._shaderDefines:
#     shaderCacheHash = self._calculateShaderCacheHash(shaderFileName, args)
#     if shaderCacheHash in self._cachedShaders: return self._cachedShaders[shaderCacheHash]
# cache shader
# if cache:
#     self._shaderDefines[shaderFileName] = defines
#     newShaderCacheHash = self._calculateShaderCacheHash(shaderFileName, args)
#     self._cachedShaders[newShaderCacheHash] = shader

# GfxShader
class GfxShader:
    def __init__(self, uniformLocation: callable, attribLocation: callable, name: str = None, program: int = None, parameters: dict[str, bool] = None, renderModes: list[str] = None):
        self._uniformLocation: callable = uniformLocation or _throw('Null')
        self._attribLocation: callable = attribLocation or _throw('Null')
        self.name: str = name
        self.program: int = program
        self.parameters: dict[str, bool] = parameters
        self.renderModes: list[str] = renderModes
        self._uniforms: dict[str, int] = {}

    def uniformLocation(self, name: str) -> int:
        if name in self._uniforms: return self._uniforms[name]
        value = self._uniformLocation(self.program, name); self._uniforms[name] = value;
        return value

    def attribLocation(self, name: str) -> int: return self._attribLocation(self.program, name)

# ShaderBuilder
class ShaderBuilder:
    def create(self, path: object, args: dict[str, bool]) -> B_Shader: pass

# ShaderManager
class ShaderManager:
    _cached: dict[object, (B_Shader, object)] = {}
    def __init__(self, builder: ShaderBuilder):
        self._builder: ShaderBuilder = builder
        self.emptyArgs: dict[str, bool] = {}

    async def create(self, source: ISource, path: object, args: dict[str, bool] = None) -> tuple[B_Shader, object]:
        argx = ','.join([k for k, v in args.items() if v]) if args else None
        key = (source, path, argx)
        return (self._builder.create(path, args or self.emptyArgs), None)

#endregion

#region Sprite

# ISprite
class ISprite:
    width: int
    height: int
    def create(self, platform: str, func: callable) -> object: pass

# SpriteBuilder
class SpriteBuilder:
    default: B_Sprite
    def create(self, spr: ISprite) -> B_Sprite: pass
    def delete(self, spr: B_Sprite) -> None: pass

# SpriteManager
class SpriteManager:
    _cached: dict[object, (B_Sprite, object)] = {}
    def __init__(self, builder: SpriteBuilder):
        self._builder: SpriteBuilder = builder
        self._tasks: dict[object, object] = {}

    @property
    def default(self) -> B_Sprite: return self._builder.default

    async def create(self, source: ISource, path: object, level: range = None) -> tuple[B_Sprite, object]:
        key = (source, path)
        if key in self._cached: return self._cached[key]
        tag = path if isinstance(path, ISprite) else await self._load(source, path)
        obj = self._builder.create(tag) if tag else self._builder.default
        self._cached[key] = (obj, tag); return (obj, tag)

    def preload(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cached: return
        if not key in self._tasks: self._tasks[key] = source.getAsset(type(ISprite), path)

    def delete(self, source: ISource, path: object) -> None:
        key = (source, path)
        if not key in self._cached: return
        self._builder.delete(self._cached[0])
        self._cached.pop(key)

    async def _load(self, source: ISource, path: object) -> ISprite:
        key = (source, path)
        assert(not key in self._cached)
        self.preload(source, s)
        obj = await self._tasks[key]
        self._tasks.pop(key)
        return obj

#endregion

#region Texture

# TextureAsDds
@dataclass
class TextureAsDds:
    bytes: bytes

# TextureAsBytes
@dataclass
class TextureAsBytes:
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
    maxId: int
    def select(self, id: int) -> None: pass

# ITextureFrames
class ITextureFrames(ITexture):
    fps: int
    hasFrames: bool
    def nextFrame(self) -> bool: pass

# TextureBuilder
class TextureBuilder:
    maxTextureMaxAnisotropy: int = GfX.maxTextureMaxAnisotropy
    default: B_Texture
    def create(self, reuse: B_Texture, tex: ITexture, level: range = None) -> B_Texture: pass
    def createSolid(self, width: int, height: int, rgba: list[float]) -> B_Texture: pass
    def createNormalMap(self, tex: B_Texture, strength: float) -> B_Texture: pass
    def delete(self, tex: B_Texture) -> None: pass

# TextureManager
class TextureManager:
    class Solid:
        def __init__(self, width: int, height: int, rgbas: list[float]):
            self.width = width
            self.height = height
            self.rgbas = rgbas
        def __hash__(self): return hash((self.width, self.height, hash((s for s in self.rgbas))))

    normalMapIntensity: float = 0.75
    _cachedNormalMaps: dict[B_Texture, B_Texture] = {}
    _cachedSolids: dict[Solid, B_Texture] = {}
    _cached: dict[object, (B_Texture, object)] = {}
    def __init__(self, builder: TextureBuilder):
        self._builder: TextureBuilder = builder
        self._tasks: dict[object, object] = {}

    @property
    def default(self) -> B_Texture: return self._builder.default

    def createNormalMap(self, src: B_Texture, strength: float = -1) -> B_Texture:
        if src in self._cachedNormalMaps: return self._cachedNormalMaps[src]
        s = self._builder.createNormalMap(src, TextureManager.normalMapIntensity if strength < 0 else strength)
        self._cachedNormalMaps[src] = s
        return s

    def createSolid(self, width: int, height: int, rgbas: object = None) -> B_Texture:
        src = TextureManager.Solid(width, height, rgbas)
        if src in self._cachedSolids: return self._cachedSolids[src]
        s = self._builder.createSolid(width, height, rgbas)
        self._cachedSolids[src] = s
        return s

    async def create(self, source: ISource, path: object, level: range = None) -> tuple[B_Texture, object]:
        key = (source, path)
        if key in self._cached: return self._cached[key]
        tag = path if isinstance(path, ITexture) else await self._load(source, path)
        obj = self._builder.create(None, tag, level) if tag else self._builder.default
        self._cached[key] = (obj, tag); return (obj, tag)

    def reload(self, source: ISource, path: object, level: range = None) -> tuple[B_Texture, object]:
        key = (source, path)
        if key not in self._cached: return (None, None)
        c = self._cached[key]
        self._builder.create(c[0], c[1], level)
        return c

    def preload(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cached: return
        if not key in self._tasks: self._tasks[key] = source.getAsset(type(ITexture), path)

    def delete(self, source: ISource, path: object) -> None:
        key = (source, path)
        if not key in self._cached: return
        self._builder.delete(self._cached[0])
        self._cached.pop(key)

    async def _load(self, source: ISource, path: object) -> ITexture:
        key = (source, path)
        assert(not key in self._cached)
        self.preload(source, path)
        obj = await self._tasks[key]
        self._tasks.pop(key)
        return obj

#endregion

#region Material

# IMaterial
class IMaterial:
    def create(self, platform: str, func: callable) -> object: pass

# MaterialProp
class MaterialProp:
    tag: object = None

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

# MaterialBuilder
class MaterialBuilder:
    textureManager : TextureManager
    defaultMaterial: B_Material
    terrainMaterial: B_Material
    def __init__(self, textureManager: TextureManager): self.textureManager = textureManager
    def create(self, source: ISource, path: object) -> B_Material: pass

# MaterialManager
class MaterialManager:
    _cached: dict[object, (B_Material, object)] = {}
    def __init__(self, textureManager: TextureManager, builder: MaterialBuilder):
        self._textureManager: TextureManager = textureManager
        self._builder: MaterialBuilder = builder
        self._tasks: dict[object, object] = {}

    async def create(self, source: ISource, path: object) -> tuple[B_Material, object]:
        key = (source, path)
        if key in self._cached: return self._cached[key]
        src = path if isinstance(path, MaterialProp) else await self._load(source, path)
        obj = await self._builder.create(source, src) if src else self._builder.default
        tag = src.tag if src else None
        self._cached[key] = (obj, tag); return (obj, tag)

    def preload(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cached: return
        if not key in self._tasks: self._tasks[key] = surce.getAsset(typeof(MaterialProp), path)

    async def _load(self, source: ISource, path: object) -> MaterialProp:
        key = (source, path)
        assert(not key in self._cached)
        self.preload(source, key)
        obj = await self._tasks[key]
        self._tasks.pop(key)
        return obj

#endregion

#region OpenGfx

# IOpenGfx:
class IOpenGfx: pass

# IOpenGfxApi
class IOpenGfxApi(IOpenGfx):
    def createObject(self, name: str, tag: str = None, parent: object = None) -> B_Object: pass
    def createMesh(self, mesh: object) -> object: pass
    def addMeshRenderer(self, src: B_Object, mesh: object, material: B_Material, enabled: bool, static_: bool) -> None: pass
    def addMeshCollider(self, src: B_Object, mesh: object, isKinematic: bool, static_: bool) -> None: pass
    def setParent(self, src: B_Object, parent: B_Object) -> None: pass
    def transform(self, src: B_Object, position: ndarray, rotation: ndarray, localScale: ndarray) -> None: pass
    def addMissingMeshCollidersRecursively(self, src: B_Object, static_: bool) -> None: pass
    def setLayerRecursively(self, src: B_Object, layer: int) -> None: pass

# IOpenGfxSprite
class IOpenGfxSprite(IOpenGfx):
    spriteManager: SpriteManager
    def preload(self, source: ISource, path: object) -> None: pass
    def create(self, source: ISource, path: object, parent: B_Object = None) -> B_Object: pass

# IOpenGfxModel
class IOpenGfxModel(IOpenGfx):
    materialManager: MaterialManager
    objectManager: ObjectModelManager
    shaderManager: ShaderManager
    textureManager: TextureManager
    def preload(self, source: ISource, path: object) -> None: pass
    def preloadTexture(self, source: ISource, path: object) -> None: pass
    def create(self, source: ISource, path: object, static_: bool, parent: B_Object = None) -> tuple[B_Object, object]: pass
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> tuple[B_Shader, object]: pass
    def createTexture(self, source: ISource, path: object, level: range = None) -> tuple[B_Texture, object]: pass
    def post(self, src: B_Object, position: Vector3, eulerAngles: Vector3, scale: float, parent: B_Object = None) -> None: pass

# IOpenGfxLight
class IOpenGfxLight(IOpenGfx):
    def create(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: B_Object = None) -> B_Object: pass
    def createProbe(self, name: str, position: Vector3, parent: B_Object = None) -> B_Object: pass

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

# IOpenGfxTerrain
class IOpenGfxTerrain(IOpenGfx):
    def createData(self, offset: int, heights: list[list[float]], heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer[B_Texture]], alphaMap: list[list[list[float]]]) -> B_Object: pass
    def create(self, name: str, position: Vector3, data: object, parent: B_Object = None) -> B_Object: pass

#endregion