from __future__ import annotations
import traceback
from numpy import array, ones, zeros, float32
from OpenGL.GL import *
from OpenGL.GL.EXT import texture_compression_s3tc as s3tc
from openstk.core import ISource, Platform
from openstk.gfx import IOpenGfxSprite, IOpenGfxModel, IOpenGfxLight, IOpenGfxTerrain, Texture_Bytes, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureBuilderBase, TextureManager
from openstk.platforms.opengl.gfx import ShaderDebugLoader
from openstk.platforms.opengl.egin import QuadIndexBuffer, GLMeshBufferCache, GLRenderMaterial
from openstk.platforms.system import SystemSfx
from openstk.client import IClientHost

# typedefs
class Shader: pass

#region Client

# OpenGLClientHost
class OpenGLClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# OpenGLObjectModelBuilder
class OpenGLObjectModelBuilder(ObjectModelBuilderBase):
    def instanceObject(self, src: object, parent: object = None) -> object:
        return 'clone'
    async def createObject(self, source: ISource, path: object, isStatic: bool, materialManager: MaterialManager) -> object:
        builder = OpenGLPlatform.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, isStatic, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensurePrefab(self) -> None: pass

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

    def _createDefaultTexture(self) -> int: return self.createSolidTexture(4, 4, array([
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
        ], dtype = float32))

    def createNormalMapTexture(self, src: int, strength: float) -> int:
        return 0

    def createSolidTexture(self, width: int, height: int, pixels: object) -> int:
        s = glGenTextures(1)
        glBindTexture(GL_TEXTURE_2D, s)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, width, height, 0, GL_RGBA, GL_FLOAT, pixels)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT)
        glBindTexture(GL_TEXTURE_2D, 0) # unbind texture
        return s

    def createTexture(self, reuse: int, src: ITexture, level2: range = None) -> int:
        @staticmethod
        def _lambdax(x: object) -> int:
            match x:
                case Texture_Bytes():
                    tex = reuse if reuse != None else glGenTextures(1)
                    numMipMaps = max(1, src.mipMaps)
                    level = range(level2.start if level2 else 0, numMipMaps)
                    # bind
                    glBindTexture(GL_TEXTURE_2D, tex)
                    if level.start > 0: glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_BASE_LEVEL, level.start)
                    glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, level.stop - 1)
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
                            if not internalFormat or not compressedTexImage2D(src, level, internalFormat): return self.defaultTexture
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
                                case TextureFormat.ARGB32: internalFormat, format, type = GL_RGBA, GL_RGB, GL_UNSIGNED_INT_8_8_8_8_REV
                                case TextureFormat.BGRA32: internalFormat, format, type = GL_RGBA, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8
                                case TextureFormat.BGRA1555: internalFormat, format, type = GL_RGBA, GL_BGRA, GL_UNSIGNED_SHORT_1_5_5_5_REV
                                case _: raise Exception(f'Unknown format: {formatx}')
                            if not internalFormat or not texImage2D(src, level, internalFormat, format, type): return self.defaultTexture
                    else: raise Exception(f'Unknown format: {fmt}')
                    # texture
                    if self.maxTextureMaxAnisotropy >= 4:
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, self.maxTextureMaxAnisotropy)
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR)
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR)
                    else:
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
                        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)
                    glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP if (src.texFlags & TextureFlags.SUGGEST_CLAMPS.value) != 0 else GL_REPEAT)
                    glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP if (src.texFlags & TextureFlags.SUGGEST_CLAMPT.value) != 0 else GL_REPEAT)
                    glBindTexture(GL_TEXTURE_2D, 0) # unbind texture
                    return tex
                case _: raise Exception(f'Unknown x: {x}')
        return src.create('GL', _lambdax)

    def deleteTexture(self, src: int) -> None: glDeleteTexture(src)

# OpenGLMaterialBuilder
class OpenGLMaterialBuilder(MaterialBuilderBase):
    _defaultMaterial: GLRenderMaterial = None; _terrainMaterial: GLRenderMaterial = None
    @property
    def defaultMaterial(self) -> GLRenderMaterial:
        if self._defaultMaterial: return self._defaultMaterial
        self._defaultMaterial = self._createDefaultMaterial()
        return self._defaultMaterial
    @property
    def terrainMaterial(self) -> GLRenderMaterial:
        if self._terrainMaterial: return self._terrainMaterial
        self._terrainMaterial = self._createTerrainMaterial()
        return self._terrainMaterial

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefaultMaterial(self) -> GLRenderMaterial:
        m = GLRenderMaterial(MaterialShaderProp())
        m.textures['g_tColor'] = self.textureManager.defaultTexture
        m.material.shaderName = 'vrf.error'
        return m

    def _createTerrainMaterial(self) -> GLRenderMaterial:
        m = GLRenderMaterial(MaterialShaderProp())
        m.material.shaderName = 'vrf.error'
        return m

    async def createMaterial(self, source: ISource, path: object) -> GLRenderMaterial:
        match path:
            case p if isinstance(path, MaterialShaderVProp):
                m = GLRenderMaterial(MaterialShaderProp())
                for tex in p.textureParams: m.textures[tex.key], _ = await self.textureManager.createTexture(source, f'{tex.Value}_c')
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
                if not 'g_vTexCoordScale' in p.vectorParams: p.vectorParams['g_vTexCoordScale'] = ones(4)
                if not 'g_vTexCoordOffset' in p.vectorParams: p.vectorParams['g_vTexCoordOffset'] = zeros(4)
                if not 'g_vColorTint' in p.vectorParams: p.vectorParams['g_vColorTint'] = ones(4)
                return m
            # case s if isinstance(path, MaterialShaderProp): return m
            case _: raise Exception(f'Unknown: {path}')

# OpenGLGfxApi
class OpenGLGfxApi(IOpenGfxSprite):
    def __init__(self): pass
    def addMeshCollider(self, src: object, mesh: object, isKinematic: bool, isStatic: bool) -> None: raise NotImplementedError();
    def addMeshRenderer(self, src: object, mesh: object, material: GLRenderMaterial, enabled: bool, isStatic: bool) -> None: raise NotImplementedError();
    def addMissingMeshCollidersRecursively(self, src: object, isStatic: bool) -> None: raise NotImplementedError();
    def attach(self, method: GfxAttach, src: object, args: list[object]) -> None: pass
    def createMesh(self, mesh: object) -> object: raise NotImplementedError();
    def createObject(self, name: str, tag: str = None, parent: object = None) -> object: return name
    def setLayerRecursively(self, src: object, layer: int) -> None: raise NotImplementedError();
    def parent(self, src: object, parent: object) -> None: raise NotImplementedError();
    def transform(self, src: object, position: Vector3, rotation: quaternion, localScale: Vector3) -> None: raise NotImplementedError();
    def transform(self, src: object, position: Vector3, rotation: Matrix4x4, localScale: Vector3) -> None: raise NotImplementedError();
    def setVisible(self, src: object, visible: bool) -> None: pass
    def destroy(self, src: object) -> None: pass

# OpenGLGfxSprite3D
class OpenGLGfxSprite3D(IOpenGfxSprite):
    def __init__(self):
        #self.spriteManager: SpriteManager = SpriteManager(OpenGLTextureBuilder())
        pass
    def preloadSprite(self, source: ISource, path: object) -> None: self.spriteManager.spriteManager(source, path)
    def createSprite(self, source: ISource, path: object, parent: object = None) -> tuple[int, object]: return self.spriteManager.createSprite(source, path)

# OpenGLGfxModel
class OpenGLGfxModel(IOpenGfxModel):
    def __init__(self):
        self.textureManager: TextureManager = TextureManager(OpenGLTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, OpenGLMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, OpenGLObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(OpenGLShaderBuilder())
        self.meshBufferCache: GLMeshBufferCache = GLMeshBufferCache()
    def preloadObject(self, source: ISource, path: object) -> None: self.objectManager.preloadObject(source, path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preloadTexture(source, path)
    def createObject(self, source: ISource, path: object, isStatic: bool, parent: object = None) -> tuple[object, object]: return self.objectManager.createObject(source, path, isStatic, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> int: return self.textureManager.createTexture(source, path, level)

    # cache
    _quadIndices: QuadIndexBuffer
    @property
    def quadIndices(self) -> QuadIndexBuffer:
        if not self._quadIndices: self._quadIndices = QuadIndexBuffer(65532)
        return self._quadIndices

# OpenGLGfxLight
class OpenGLGfxLight(IOpenGfxLight):
    def __init__(self): pass
    def createLight(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: object = None) -> object: return 'light'
    def createReflectionProbe(self, name: str, position: Vector3, parent: object = None) -> object: return 'probe'

# OpenGLGfxTerrain
class OpenGLGfxTerrain(IOpenGfxTerrain):
    def __init__(self): pass
    def createTerrainData(self, offset: int, heights: ndarray, heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer], alphaMap: ndarray) -> object: return f't{offset}'
    def createTerrain(self, name: str, position: Vector3, data: object, parent: object = None) -> object: return 'terrain'

# OpenGLPlatform
class OpenGLPlatform(Platform):
    buildersByType: dict[type, callable] = {}
    def __init__(self):
        super().__init__('GL', 'OpenGL')
        self.gfxFactory = staticmethod(lambda: [OpenGLGfxApi(), None, OpenGLGfxSprite3D(), OpenGLGfxModel(), OpenGLGfxLight(), OpenGLGfxTerrain()])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
OpenGLPlatform.this = OpenGLPlatform()

#endregion