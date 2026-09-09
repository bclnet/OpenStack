from __future__ import annotations
import re, traceback
from numpy import array, ones, zeros, float32
from importlib import resources
from OpenGL.GL import *
from OpenGL.GL.EXT import texture_compression_s3tc as s3tc
from openstk.core import ISource, Platform
from openstk.gfx import IOpenGfxSprite, IOpenGfxModel, IOpenGfxLight, IOpenGfxTerrain, TextureAsBytes, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilder, ObjectModelManager, MaterialBuilder, MaterialManager, GfxShader, ShaderBuilder, ShaderManager, TextureBuilder, TextureManager
from openstk.platforms.opengl.egin import QuadIndexBuffer, GLMeshBufferCache, GLRenderMaterial
from openstk.platforms.opengl.gfx.opengl import OpenGLX
from openstk.platforms.system import SystemSfx
from openstk.client import IClientHost

#region Client

# OpenGLClientHost
class OpenGLClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# OpenGLObjectModelBuilder
class OpenGLObjectModelBuilder(ObjectModelBuilder):
    def instance(self, src: object, parent: object = None) -> object:
        return 'clone'
    async def create(self, source: ISource, path: object, static_: bool, materialManager: MaterialManager) -> object:
        builder = OpenGLX.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, static_, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensure(self) -> None: pass

# OpenGLShaderBuilder
class OpenGLShaderBuilder(ShaderBuilder):
    RenderMode: str = 'renderMode_'; RenderModeLength: int = len(RenderMode)

    def getShaderFileByName(self, name: str) -> str:
        match name:
            case 'plane': return 'plane'
            case 'testtri': return 'testtri'
            case 'vrf.error': return 'error'
            case 'vrf.grid': return 'debug_grid'
            case 'vrf.picking': return 'picking'
            case 'vrf.particle.sprite': return 'particle_sprite'
            case 'vrf.particle.trail': return 'particle_trail'
            case 'tools_sprite.vfx': return 'sprite'
            case 'vr_unlit.vfx': return 'vr_unlit'
            case 'vr_black_unlit.vfx': return 'vr_black_unlit'
            case 'water_dota.vfx': return 'water'
            case 'hero.vfx' | 'hero_underlords.vfx': return 'dota_hero'
            case 'multiblend.vfx': return 'multiblend'
            case _:
                if name.startsWith('vr_'): return 'vr_standard'
                log.warn(f'Unknown shader {name}, defaulting to simple.')
                return 'simple'

    def getShaderSource(self, name: str) -> str: return resources.files().joinpath('gfx/shaders', name).read_text(encoding='utf-8')

    def create(self, path: object, args: dict[str, bool]) -> GfxShader:
        name = str(path)
        shaderFileName = self.getShaderFileByName(name)

        # defines
        defines = []

        # vertex shader
        vertexShader = glCreateShader(GL_VERTEX_SHADER)
        if True:
            shaderSource = self.getShaderSource(f'{shaderFileName}.vert')
            glShaderSource(vertexShader, self.preprocessVertexShader(shaderSource, args))
            # defines: find defines supported from source
            defines += self.findDefines(shaderSource)
        glCompileShader(vertexShader)
        shaderStatus = glGetShaderiv(vertexShader, GL_COMPILE_STATUS)
        if shaderStatus != 1:
            vsInfo = glGetShaderInfoLog(vertexShader)
            raise Exception(f'Error setting up Vertex Shader "{name}": {vsInfo}')

        # fragment shader
        fragmentShader = glCreateShader(GL_FRAGMENT_SHADER)
        if True:
            shaderSource = self.getShaderSource(f'{shaderFileName}.frag')
            glShaderSource(fragmentShader, self.updateDefines(shaderSource, args))
            # defines: find render modes supported from source, take union to avoid duplicates
            defines += self.findDefines(shaderSource)
        glCompileShader(fragmentShader)
        shaderStatus = glGetShaderiv(fragmentShader, GL_COMPILE_STATUS)
        if shaderStatus != 1:
            fsInfo = glGetShaderInfoLog(fragmentShader)
            raise Exception(f'Error setting up Fragment Shader "{name}": {fsInfo}')

        # defines find render modes
        renderModes = [k[RenderModeLength:] for k in defines if k.startswith(RenderMode)]

        # build shader
        shader = GfxShader(glGetUniformLocation, glGetAttribLocation,
            name = name,
            parameters = args,
            program = glCreateProgram(),
            renderModes = renderModes)
        glAttachShader(shader.program, vertexShader)
        glAttachShader(shader.program, fragmentShader)
        glLinkProgram(shader.program)
        glValidateProgram(shader.program)
        linkStatus = glGetProgramiv(shader.program, GL_LINK_STATUS)
        if linkStatus != 1:
            linkInfo = glGetProgramInfoLog(shader.program)
            raise Exception(f'Error linking shaders: {linkInfo} (link status = {linkStatus})')
        glDetachShader(shader.program, vertexShader)
        glDeleteShader(vertexShader)
        glDetachShader(shader.program, fragmentShader)
        glDeleteShader(fragmentShader)
        # print(f'Shader {name}({', '.join(args.keys())}) compiled and linked succesfully')
        return shader

    # Preprocess a vertex shader's source to include the #version plus #defines for parameters
    def preprocessVertexShader(self, source: str, args: dict[str, bool]) -> str: return self.resolveIncludes(self.updateDefines(source, args))

    # Update default defines with possible overrides from the model
    @staticmethod
    def updateDefines(source: str, args: dict[str, bool]) -> str:
        # find all #define param_(paramName) (paramValue) using regex
        defines = re.compile('#define param_(\\S*?) (\\S*?)\\s*?\\n').finditer(source)
        for define in defines:
            if (key := define[1]) in args: start, end = define.span(2); source = source[:start] + ('1' if args[key] else '0') + source[end:]
        return source

    # Remove any #includes from the shader and replace with the included code
    def resolveIncludes(self, source: str) -> str:
        includes = re.compile('#include "([^"]*?)";?\\s*\\n').finditer(source)
        for define in includes:
            includedCode = self.getShaderSource(define[1])
            # recursively resolve includes in the included code. (Watch out for cyclic dependencies!)
            includedCode = self.resolveIncludes(includedCode)
            if not includedCode.endswith('\n'): includedCode += '\n'
            start, end = define.span(0); source = source.replace(source[start:end], includedCode)
        return source

    @staticmethod
    def findDefines(source: str) -> list[str]: defines = re.compile('#define param_(\\S+)').finditer(source); return [x[1] for x in defines]

# print(OpenGLShaderBuilder().create('water_dota.vfx', {'fulltangent': 1}))

# OpenGLTextureBuilder
class OpenGLTextureBuilder(TextureBuilder):
    _default: int = -1
    @property
    def default(self) -> int:
        if self._default > -1: return self._default
        self._default = self._createDefault()
        return self._default

    def release(self) -> None:
        if self._default > -1: glDeleteTexture(self._default); self._default = -1

    def _createDefault(self) -> int: return self.createSolid(4, 4, array([
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

    def createNormalMap(self, src: int, strength: float) -> int:
        return 0

    def createSolid(self, width: int, height: int, pixels: object) -> int:
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

    def create(self, reuse: int, src: ITexture, level2: range = None) -> int:
        @staticmethod
        def _lambdax(x: object) -> int:
            match x:
                case TextureAsBytes():
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
                    if not bytes: return self.default
                    elif isinstance(fmt, tuple):
                        formatx, pixel = fmt
                        s = pixel & TexturePixel.Signed
                        f = pixel & TexturePixel.Float
                        if formatx & TextureFormat.Compressed:
                            match formatx:
                                case TextureFormat.DXT1: internalFormat = s3tc.GL_COMPRESSED_RGB_S3TC_DXT1_EXT if s else s3tc.GL_COMPRESSED_RGB_S3TC_DXT1_EXT
                                case TextureFormat.DXT1A: internalFormat = s3tc.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT if s else s3tc.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
                                case TextureFormat.DXT3: internalFormat = s3tc.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT if s else s3tc.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT
                                case TextureFormat.DXT5: internalFormat = s3tc.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT if s else s3tc.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT
                                case TextureFormat.BC4: internalFormat = GL_COMPRESSED_SIGNED_RED_RGTC1 if s else GL_COMPRESSED_RED_RGTC1
                                case TextureFormat.BC5: internalFormat = GL_COMPRESSED_SIGNED_RG_RGTC2 if s else GL_COMPRESSED_RG_RGTC2
                                case TextureFormat.BC6H: internalFormat = GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT if s else GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT
                                case TextureFormat.BC7: internalFormat = GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM if s else GL_COMPRESSED_RGBA_BPTC_UNORM
                                case TextureFormat.ETC2: internalFormat = GL_COMPRESSED_SRGB8_ETC2 if s else GL_COMPRESSED_RGB8_ETC2
                                case TextureFormat.ETC2_EAC: internalFormat = GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC if s else GL_COMPRESSED_RGBA8_ETC2_EAC
                                case _: raise Exception(f'Unknown format: {formatx}')
                            if not internalFormat or not compressedTexImage2D(src, level, internalFormat): return self.default
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
                            if not internalFormat or not texImage2D(src, level, internalFormat, format, type): return self.default
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

    def delete(self, src: int) -> None: glDeleteTexture(src)

# OpenGLMaterialBuilder
class OpenGLMaterialBuilder(MaterialBuilder):
    _default: GLRenderMaterial = None; _terrainMaterial: GLRenderMaterial = None
    @property
    def default(self) -> GLRenderMaterial:
        if self._default: return self._default
        self._default = self._createDefault()
        return self._default
    @property
    def terrain(self) -> GLRenderMaterial:
        if self._terrain: return self._terrain
        self._terrain = self._createTerrain()
        return self._terrain

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefault(self) -> GLRenderMaterial:
        m = GLRenderMaterial(MaterialShaderProp())
        m.textures['g_tColor'] = self.textureManager.default
        m.material.shaderName = 'vrf.error'
        return m

    def _createTerrain(self) -> GLRenderMaterial:
        m = GLRenderMaterial(MaterialShaderProp())
        m.material.shaderName = 'vrf.error'
        return m

    async def create(self, source: ISource, path: object) -> GLRenderMaterial:
        match path:
            case p if isinstance(path, MaterialShaderVProp):
                m = GLRenderMaterial(MaterialShaderProp())
                for tex in p.textureParams: m.textures[tex.key], _ = await self.textureManager.create(source, f'{tex.Value}_c')
                if 'F_SOLID_COLOR' in p.intParams and p.intParams['F_SOLID_COLOR'] == 1:
                    a = p.vectorParams['g_vColorTint']
                    m.textures['g_tColor'] = self.textureManager.buildSolid(1, 1, a[0], a[1], a[2], a[3])
                if not 'g_tColor' in m.textures: m.textures['g_tColor'] = self.textureManager.default

                # Since our shaders only use g_tColor, we have to find at least one texture to use here
                if m.textures['g_tColor'] == self.textureManager.default:
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
    def addMeshCollider(self, src: object, mesh: object, isKinematic: bool, static_: bool) -> None: raise NotImplementedError();
    def addMeshRenderer(self, src: object, mesh: object, material: GLRenderMaterial, enabled: bool, static_: bool) -> None: raise NotImplementedError();
    def addMissingMeshCollidersRecursively(self, src: object, static_: bool) -> None: raise NotImplementedError();
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
    def preload(self, source: ISource, path: object) -> None: self.spriteManager.sprite(source, path)
    def create(self, source: ISource, path: object, parent: object = None) -> tuple[int, object]: return self.spriteManager.create(source, path)

# OpenGLGfxModel
class OpenGLGfxModel(IOpenGfxModel):
    def __init__(self):
        self.textureManager: TextureManager = TextureManager(OpenGLTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, OpenGLMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, OpenGLObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(OpenGLShaderBuilder())
        self.meshBufferCache: GLMeshBufferCache = GLMeshBufferCache()
    def preload(self, source: ISource, path: object) -> None: self.objectManager.preload(source, path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preloadTexture(source, path)
    def create(self, source: ISource, path: object, static_: bool, parent: object = None) -> tuple[object, object]: return self.objectManager.create(source, path, static_, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> GfxShader: return self.shaderManager.create(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> int: return self.textureManager.create(source, path, level)

    # cache
    _quadIndices: QuadIndexBuffer
    @property
    def quadIndices(self) -> QuadIndexBuffer:
        if not self._quadIndices: self._quadIndices = QuadIndexBuffer(65532)
        return self._quadIndices

# OpenGLGfxLight
class OpenGLGfxLight(IOpenGfxLight):
    def __init__(self): pass
    def create(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: object = None) -> object: return 'light'
    def createProbe(self, name: str, position: Vector3, parent: object = None) -> object: return 'probe'

# OpenGLGfxTerrain
class OpenGLGfxTerrain(IOpenGfxTerrain):
    def __init__(self): pass
    def createData(self, offset: int, heights: ndarray, heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer], alphaMap: ndarray) -> object: return f't{offset}'
    def create(self, name: str, position: Vector3, data: object, parent: object = None) -> object: return 'terrain'

# OpenGLPlatform
class OpenGLPlatform(Platform):
    def __init__(self):
        super().__init__('GL', 'OpenGL')
        self.gfxFactory = staticmethod(lambda: [OpenGLGfxApi(), None, OpenGLGfxSprite3D(), OpenGLGfxModel(), OpenGLGfxLight(), OpenGLGfxTerrain()])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
OpenGLPlatform.this = OpenGLPlatform()

#endregion