from __future__ import annotations
import os, numpy as np
from OpenGL.GL import *
from OpenGL.GL.EXT import texture_compression_s3tc as s3tc
from openstk.gfx import IOpenGfxSprite, IOpenGfxModel, Texture_Bytes, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureBuilderBase, TextureManager
from openstk.gfx.opengl import ShaderDebugLoader
from openstk.gfx.opengl.egin import QuadIndexBuffer, GLMeshBufferCache, GLRenderMaterial
from openstk.platform import Platform, SystemSfx

# typedefs
class Shader: pass
class ISource: pass

#region OpenGfx

# OpenGLObjectModelBuilder
class OpenGLObjectModelBuilder(ObjectModelBuilderBase):
    def ensurePrefab(self) -> None: pass
    def createNewObject(self, prefab: object) -> object: raise NotImplementedError()
    def createObject(self, path: object, materialManager: MaterialManager) -> object: raise NotImplementedError()

# OpenGLShaderBuilder
class OpenGLShaderBuilder(ShaderBuilderBase):
    _loader: ShaderLoader = ShaderDebugLoader()
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: return self._loader.createShader(path, args)

# OpenGLTextureBuilder
class OpenGLTextureBuilder(TextureBuilderBase):
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

    def createTexture(self, reuse: int, tex: ITexture, level2: range = None) -> int:
        id = reuse if reuse != None else glGenTextures(1)
        numMipMaps = max(1, tex.mipMaps)
        level = range(level2.start if level2 else 0, numMipMaps)

        # bind
        glBindTexture(GL_TEXTURE_2D, id)
        if level.start > 0: glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_BASE_LEVEL, level.start)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, level.stop - 1)

        # create
        @staticmethod
        def lambdaX(x: object) -> int:
            match x:
                case Texture_Bytes():
                    bytes, fmt, spans = (x.bytes, x.format, x.spans)
                    pixels = []
                    # decode
                    def compressedTexImage2D(tex: ITexture, level: range, internalFormat: int) -> bool:
                        nonlocal pixels
                        width = tex.width; height = tex.height
                        if spans:
                            for l in level:
                                span = spans[l]
                                if span and span[0] < 0: return False
                                pixels = bytes[span.start:span.stop]
                                glCompressedTexImage2D(GL_TEXTURE_2D, l, internalFormat, width >> l, height >> l, 0, pixels)
                        else: glCompressedTexImage2D(GL_TEXTURE_2D, 0, internalFormat, width, height, 0, bytes)
                        return True
                    def texImage2D(tex: ITexture, level: range, internalFormat: int, format: int, type: int) -> bool:
                        nonlocal pixels, spans
                        width = tex.width; height = tex.height
                        if spans:
                            for l in level:
                                span = spans[l]
                                if span and span[0] < 0: return False
                                pixels = bytes[span.start:span.stop]
                                glTexImage2D(GL_TEXTURE_2D, l, internalFormat, width >> l, height >> l, 0, format, type, pixels)
                        else: glTexImage2D(GL_TEXTURE_2D, 0, internalFormat, width, height, 0, format, type, bytes)
                        return True
                    # process
                    if not bytes: return self.defaultTexture
                    elif isinstance(fmt, tuple):
                        formatx, pixel = fmt
                        s = pixel & TexturePixel.Signed
                        f = pixel & TexturePixel.Float
                        if formatx & TextureFormat.Compressed:
                            match formatx:
                                case TextureFormat.DXT1: internalFormat = s3tc.GL_COMPRESSED_SRGB_S3TC_DXT1_EXT if s else s3tc.GL_COMPRESSED_RGB_S3TC_DXT1_EXT
                                case TextureFormat.DXT1A: internalFormat = s3tc.GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT1_EXT if s else s3tc.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
                                case TextureFormat.DXT3: internalFormat = s3tc.GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT3_EXT if s else s3tc.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT
                                case TextureFormat.DXT5: internalFormat = s3tc.GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT if s else s3tc.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT
                                case TextureFormat.BC4: internalFormat = GL_COMPRESSED_SIGNED_RED_RGTC1 if s else GL_COMPRESSED_RED_RGTC1
                                case TextureFormat.BC5: internalFormat = GL_COMPRESSED_SIGNED_RG_RGTC2 if s else GL_COMPRESSED_RG_RGTC2
                                case TextureFormat.BC6H: internalFormat = GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT if s else GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT
                                case TextureFormat.BC7: internalFormat = GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM if s else GL_COMPRESSED_RGBA_BPTC_UNORM
                                case TextureFormat.ETC2: internalFormat = GL_COMPRESSED_SRGB8_ETC2 if s else GL_COMPRESSED_RGB8_ETC2
                                case TextureFormat.ETC2_EAC: internalFormat = GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC if s else GL_COMPRESSED_RGBA8_ETC2_EAC
                                case _: raise Exception(f'Unknown format: {formatx}')
                            if not internalFormat or not compressedTexImage2D(tex, level, internalFormat): return self.defaultTexture 
                        else:
                            match formatx:
                                case TextureFormat.I8: internalFormat, format, type = GL_INTENSITY8, GL_RED, GL_UNSIGNED_BYTE
                                case TextureFormat.L8: internalFormat, format, type = GL_LUMINANCE, GL_LUMINANCE, GL_UNSIGNED_BYTE
                                case TextureFormat.R8: internalFormat, format, type = GL_R8, GL_RED, GL_UNSIGNED_BYTE
                                case TextureFormat.R16: internalFormat, format, type = GL_R16F, GL_RED, GL_FLOAT if f else GL_R16, GL_RED, GL_UNSIGNED_SHORT
                                case TextureFormat.RG16: internalFormat, format, type = GL_RG16F, GL_RED, GL_FLOAT if f else GL_RG16, GL_RED, GL_UNSIGNED_SHORT
                                case TextureFormat.RGB24: internalFormat, format, type = GL_RGB8, GL_RGB, GL_UNSIGNED_BYTE
                                case TextureFormat.RGB565: internalFormat, format, type = GL_RGB5, GL_RGB, GL_UNSIGNED_BYTE #GL_UNSIGNED_SHORT_5_6_5
                                case TextureFormat.RGBA32: internalFormat, format, type = GL_RGBA8, GL_RGBA, GL_UNSIGNED_BYTE
                                case TextureFormat.ARGB32: internalFormat, format, type = GL_RGBA, GL_RGB, GL_UNSIGNED_INT_8_8_8_8_REVERSED
                                case TextureFormat.BGRA32: internalFormat, format, type = GL_RGBA, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8
                                case TextureFormat.BGRA1555: internalFormat, format, type = GL_RGBA, GL_BGRA, GL_UNSIGNED_SHORT_1_5_5_5_REVERSED
                                case _: raise Exception(f'Unknown format: {formatx}')
                            if not internalFormat or not texImage2D(tex, level, internalFormat, format, type): return self.defaultTexture
                    else: raise Exception(f'Unknown format: {fmt}')

                    # texture
                    if self.maxTextureMaxAnisotropy >= 4:
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, self.maxTextureMaxAnisotropy)
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR)
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR)
                    else:
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)
                    glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP if (tex.texFlags & TextureFlags.SUGGEST_CLAMPS.value) != 0 else GL_REPEAT)
                    glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP if (tex.texFlags & TextureFlags.SUGGEST_CLAMPT.value) != 0 else GL_REPEAT)
                    glBindTexture(GL_TEXTURE_2D, 0) # unbind texture
                    return id
                case _: raise Exception(f'Unknown x: {x}')
        return tex.create('GL', lambdaX)

    def createSolidTexture(self, width: int, height: int, pixels: np.array) -> int:
        id = glGenTextures(1)
        glBindTexture(GL_TEXTURE_2D, id)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, width, height, 0, GL_RGBA, GL_FLOAT, pixels)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT)
        glBindTexture(GL_TEXTURE_2D, 0) # unbind texture
        return id

    def createNormalMap(self, tex: int, strength: float) -> int: raise NotImplementedError()

    def deleteTexture(self, tex: int) -> None: glDeleteTexture(texture)

# OpenGLMaterialBuilder
class OpenGLMaterialBuilder(MaterialBuilderBase):
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

    def createMaterial(self, path: object) -> GLRenderMaterial:
        match path:
            case p if isinstance(path, MaterialShaderVProp):
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
            case _: raise Exception(f'Unknown: {path}')

# OpenGLGfxSprite3D
class OpenGLGfxSprite3D(IOpenGfxSprite):
    source: ISource
    spriteManager: SpriteManager
    objectManager: ObjectSpriteManager
    def __init__(self, source: ISource):
        self.source = source
        # self.spriteManager = SpriteManager(source, OpenGLTextureBuilder())
        # self.objectManager = ObjectManager(source, OpenGLObjectModelBuilder())

    def createSprite(self, path: object, level: range = None) -> int: return self.spriteManager.createSprite(path)[0]
    def preloadSprite(self, path: object) -> None: self.textureManager.spriteManager(path)
    def createObject(self, path: object) -> (object, dict[str, object]): raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()
    def loadFileObject(self, type: type, path: object) -> object: return self.source.loadFileObject(type, path)

# OpenGLGfxModel
class OpenGLGfxModel(IOpenGfxModel):
    source: ISource
    textureManager: TextureManager
    materialManager: MaterialManager
    objectManager: ObjectModelManager
    shaderManager: ShaderManager
    def __init__(self, source: ISource):
        self.source = source
        self.textureManager = TextureManager(source, OpenGLTextureBuilder())
        self.materialManager = MaterialManager(source, self.textureManager, OpenGLMaterialBuilder(self.textureManager))
        self.objectManager = ObjectModelManager(source, self.materialManager, OpenGLObjectModelBuilder())
        self.shaderManager = ShaderManager(source, OpenGLShaderBuilder())
        self.meshBufferCache = GLMeshBufferCache()

    def createTexture(self, path: object, level: range = None) -> int: return self.textureManager.createTexture(path, level)[0]
    def preloadTexture(self, path: object) -> None: self.textureManager.preloadTexture(path)
    def createObject(self, path: object) -> (object, dict[str, object]): return self.objectManager.createObject(path)[0]
    def preloadObject(self, path: object) -> None: self.objectManager.preloadObject(path)
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(path, args)[0]
    def loadFileObject(self, type: type, path: object) -> object: return self.source.loadFileObject(type, path)

    # cache
    _quadIndices: QuadIndexBuffer
    @property
    def quadIndices(self) -> QuadIndexBuffer: return self._quadIndices if self._quadIndices else (_quadIndices := QuadIndexBuffer(65532))
    meshBufferCache: GLMeshBufferCache

# OpenGLPlatform
class OpenGLPlatform(Platform):
    def __init__(self):
        super().__init__('GL', 'OpenGL')
        self.gfxFactory = staticmethod(lambda source: [None, OpenGLGfxSprite3D(source), OpenGLGfxModel(source)])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
OpenGLPlatform.This = OpenGLPlatform()

#endregion