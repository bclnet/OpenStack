import numpy as np

# typedefs
class IVBIB: pass
class Audio: pass
class Object: pass
class Material: pass
class Texture: pass

# IAudioManager
class IAudioManager:
    def createAudio(self, path: object) -> (Audio, object): pass
    def deleteAudio(self, path: object) -> None: pass

# IObjectManager
class IObjectManager:
    def createObject(self, path: object) -> (Object, object): pass
    def preloadObject(self, path: object) -> None: pass

# IModel
class IModel:
    data: dict[str, object]
    def remapBoneIndices(self, vbib: IVBIB, meshIndex: int) -> IVBIB: pass

# IParticleSystem
class IParticleSystem:
    data: dict[str, object]
    renderers: list[dict[str, object]]
    operators: list[dict[str, object]]
    initializers: list[dict[str, object]]
    emitters: list[dict[str, object]]
    def getChildParticleNames(self, enabledOnly: bool = False) -> list[str]: pass

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

# IShaderManager
class IShaderManager:
    def createShader(self, path: object, args: dict[str: bool] = None) -> (Shader, object): pass

# ITextureManager:
class ITextureManager:
    defaultTexture: Texture
    def createNormalMap(self, texture: Texture, strength: float) -> Texture: pass
    def createSolidTexture(self, width: int, height: int, rgba: list[float]) -> Texture: pass
    def createTexture(self, path: object, level: range = None) -> (Texture, object): pass
    def reloadTexture(self, path: object, level: range = None) -> (Texture, object): pass
    def preloadTexture(self, path: object) -> None: pass
    def deleteTexture(self, path: object) -> None: pass

# IMaterialManager
class IMaterialManager:
    textureManager: ITextureManager
    def createMaterial(self, key: object) -> (Material, object): pass
    def preloadMaterial(self, key: object) -> None: pass

# IMaterial
class IMaterial:
    name: str
    shaderName: str
    data: dict[str, object]
    def getShaderArgs(self) -> dict[str, bool]: pass

# IFixedMaterial
class IFixedMaterial(IMaterial):
    mainFilePath: str
    darkFilePath: str
    detailFilePath: str
    glossFilePath: str
    glowFilePath: str
    bumpFilePath: str
    alphaBlended: bool
    srcBlendMode: int
    dstBlendMode: int
    alphaTest: bool
    alphaCutoff: float
    zwrite: bool

# IParamMaterial
class IParamMaterial(IMaterial):
    intParams: dict[str, int]
    floatParams: dict[str, float]
    vectorParams: dict[str, np.ndarray]
    textureParams: dict[str, str]
    intAttributes: dict[str, int]

# IOpenGfx:
class IOpenGfx:
    def loadFileObject(self, path: object): pass
    def preloadTexture(self, path: object): pass
    def preloadObject(self, path: object): pass

# IOpenGfxAny
class IOpenGfxAny(IOpenGfx):
    audioManager: IAudioManager
    textureManager: ITextureManager
    materialManager: IMaterialManager
    objectManager: IObjectManager
    shaderManager: IShaderManager
    def createAudio(self, path: object) -> Audio: pass
    def createTexture(self, path: object, level: range = None) -> Texture: pass
    def createObject(self, path: object) -> Object: pass
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: pass

# PlatformStats:
class PlatformStats:
    maxTextureMaxAnisotropy: int = 0
