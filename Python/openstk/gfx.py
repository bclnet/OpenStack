import numpy as np
from typing import Any
from enum import Enum, Flag
from openstk.util import _throw

# ---- GFX ----

class IVBIB: pass
class Object: pass
class Material: pass
class Texture: pass
class Shader: pass

# IModel
class IModel:
    data: dict[str, object]
    def remapBoneIndices(vbib: IVBIB, meshIndex: int) -> IVBIB: pass

# IParticleSystem
class IParticleSystem:
    data: dict[str, object]
    renderers: list[dict[str, object]]
    operators: list[dict[str, object]]
    initializers: list[dict[str, object]]
    emitters: list[dict[str, object]]
    def getChildParticleNames(enabledOnly: bool = False) -> list[str]: pass

# IShaderManager
class IShaderManager:
    def loadShader(path: str, args: dict[str: bool] = None): pass
    def loadPlaneShader(path: str, args: dict[str: bool] = None): pass

# ITextureManager:
class ITextureManager:
    defaultTexture: Texture
    def buildSolidTexture(width: int, height: int, rgba: list[float]) -> Texture: pass
    def buildNormalMap(source: Texture, strength: float) -> Texture: pass
    def loadTexture(key: object, range: object = None) -> (Texture, dict[str, object]): pass
    def preloadTexture(path: str) -> None: pass
    def deleteTexture(key: object) -> None: pass

# IMaterialManager
class IMaterialManager:
    def textureManager() -> ITextureManager: pass
    def loadMaterial(key: object) -> (Material, dict[str, object]): pass
    def preloadMaterial(path: str) -> None: pass

class IMaterial:
    name: str
    shaderName: str
    data: dict[str, object]
    def getShaderArgs() -> dict[str, bool]: pass

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

class IParamMaterial(IMaterial):
    intParams: dict[str, int]
    floatParams: dict[str, float]
    vectorParams: dict[str, np.ndarray]
    textureParams: dict[str, str]
    intAttributes: dict[str, int]

# IOpenGraphic:
class IOpenGraphic:
    def loadFileObject(path: str): pass
    def preloadTexture(texturePath: str): pass
    def preloadObject(filePath: str): pass

class IOpenGraphicT(IOpenGraphic):
    def textureManager() -> ITextureManager: pass
    def materialManager() -> IMaterialManager: pass
    def shaderManager() -> IShaderManager: pass
    def loadTexture(path: str, range: range = None) -> (Texture, dict[str, object]): pass
    def createObject(path: str) -> (Object, dict[str, object]): pass
    def loadShader(path: str, args: dict[str, bool] = None) -> Shader: pass

# PlatformStats:
class PlatformStats:
    maxTextureMaxAnisotropy = 0