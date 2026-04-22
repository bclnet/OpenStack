# @see https://pyopengl.sourceforge.net/documentation/manual-3.0/glGetProgram.html
# @see https://github.com/jcteng/python-opengl-tutorial/blob/master/utils/textureLoader.py
from __future__ import annotations
import os, re
from numpy import ndarray
from importlib import resources
from OpenGL.GL import *
from openstk.core import CellManager, CellBuilder
from openstk.gfx import Shader

# typedefs
class OpenGLGfxModel: pass

#region Extensions

#endregion

#region Shader

# ShaderSeed = 0x13141516
RenderMode = 'renderMode_'; RenderModeLength = len(RenderMode)

# ShaderLoader
class ShaderLoader:
    ShaderSeed: int = 0x13141516
    _cachedShaders: dict[int, Shader] = {}
    _shaderDefines: dict[str, list[str]] = {}
    
    def _calculateShaderCacheHash(self, name: str, args: dict[str, bool]) -> int:
        b = [name]
        parameters = set(self._shaderDefines[name]).intersection(args.keys())
        for key in parameters: b.append(key); b.append('t' if args[key] else 'f')
        return hash('\n'.join(b))

    def getShaderFileByName(self, name: str) -> str: pass

    def getShaderSource(self, name: str) -> str: pass

    def createShader(self, path: object, args: dict[str, bool]) -> Shader:
        name = str(path)
        cache = not name.startswith('#')
        shaderFileName = self.getShaderFileByName(name)
        
        # cache
        if cache and shaderFileName in self._shaderDefines:
            shaderCacheHash = self._calculateShaderCacheHash(shaderFileName, args)
            if shaderCacheHash in self._cachedShaders: return self._cachedShaders[shaderCacheHash]

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
        renderModes = [k[RenderModeLength] for k in defines if k.startswith(RenderMode)]

        # build shader
        shader = Shader(glGetUniformLocation, glGetAttribLocation,
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

        # cache shader
        if cache:
            self._shaderDefines[shaderFileName] = defines
            newShaderCacheHash = self._calculateShaderCacheHash(shaderFileName, args)
            self._cachedShaders[newShaderCacheHash] = shader
            print(f'Shader {name}({', '.join(args.keys())}) compiled and linked succesfully')
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

# ShaderDebugLoader
class ShaderDebugLoader(ShaderLoader):
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

    def getShaderSource(self, name: str) -> str: return resources.files().joinpath('shaders', name).read_text(encoding='utf-8')

# print(ShaderDebugLoader().loadShader('water_dota.vfx', {'fulltangent': 1}))

#endregion

#region CellManager

# OpenGLCellManager
class OpenGLCellManager(CellManager):
    def __init__(self, query: IQuery, queue: CoroutineQueue, taskFunc: callable): super().__init__(query, queue, taskFunc)

    def gfxCreateContainers(self, name: str) -> (object, object):
        return (None, None)
        #cellObj = GameObject(name) { tag = 'Cell' }
        #contObj = GameObject('objects'); contObj.transform.parent = cellObj.transform
        #return (contObj, cellObj)

    def gfxSetVisible(self, source: object, visible: bool) -> None:
        pass
        #c = (GameObject)source
        #if visible: if not c.activeSelf: c.setActive(True)
        #else if c.activeSelf: c.setActive(False)

# RenderLightShadows: False
# RenderExteriorCellLights: False
class OpenGLCellBuilder(CellBuilder):
    def __init__(self, query: CellManager.IQuery, gfxModel: OpenGLGfxModel): super().__init__(query, gfxModel)

    def gfxCreateLight(self, light: CellManager.ILigh, indoors: bool) -> object:
        return None

    def gfxCreateTerrain(self, offset: int, heights: ndarray, heightRange: float, sampleDistance: float, layers: list[TerrainLayer], alphaMap: ndarray, position: Vector3, material: object, parent: object) -> object:
        return None

#endregion
