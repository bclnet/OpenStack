import numpy as np

# typedefs
class IVBIB: pass
class Object: pass
class Material: pass
class Texture: pass
class Shader: pass

# IObjectManager
class IObjectManager:
    def createObject(self, path: str) -> (Object, Object): pass
    def preloadObject(self, path: str) -> None: pass

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

# IShaderManager
class IShaderManager:
    def loadShader(self, path: str, args: dict[str: bool] = None): pass
    def loadPlaneShader(self, path: str, args: dict[str: bool] = None): pass

# ITextureManager:
class ITextureManager:
    defaultTexture: Texture
    def buildSolidTexture(self, width: int, height: int, rgba: list[float]) -> Texture: pass
    def buildNormalMap(self, source: Texture, strength: float) -> Texture: pass
    def loadTexture(self, key: object, level: range = None) -> (Texture, Object): pass
    def preloadTexture(self, key: object) -> None: pass
    def deleteTexture(self, key: object) -> None: pass

# IMaterialManager
class IMaterialManager:
    textureManager: ITextureManager
    def loadMaterial(self, key: object) -> (Material, Object): pass
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

# IOpenGraphic:
class IOpenGraphic:
    def loadFileObject(self, path: str): pass
    def preloadTexture(self, texturePath: str): pass
    def preloadObject(self, filePath: str): pass

# IOpenGraphicAny
class IOpenGraphicAny(IOpenGraphic):
    textureManager: ITextureManager
    materialManager: IMaterialManager
    objectManager: IObjectManager
    shaderManager: IShaderManager
    def loadTexture(self, path: str, level: range = None) -> (Texture, dict[str, object]): pass
    def createObject(self, path: str) -> (Object, dict[str, object]): pass
    def loadShader(self, path: str, args: dict[str, bool] = None) -> Shader: pass

# PlatformStats:
class PlatformStats:
    maxTextureMaxAnisotropy: int = 0
