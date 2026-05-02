from __future__ import annotations
import os, io, math
from numpy import ndarray, array, ones, zeros
from openstk.core import Platform
from openstk.client import IClientHost
from openstk.gfx import IOpenGfxApi, IOpenGfxModel, IOpenGfxLight, IOpenGfxTerrain, Texture_Bytes, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, Shader, ShaderBuilderBase, ShaderManager, TextureBuilderBase, TextureManager
from openstk.platforms.system import SystemSfx
from panda3d.core import PandaNode, NodePath, Texture, TextureStage, PNMImage, PTAUchar, CPTAUchar, PointLight, GeoMipTerrain

# types
type Vector3 = ndarray

#region Client

# Panda3dClientHost
class Panda3dClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# Panda3dObjectModelBuilder
class Panda3dObjectModelBuilder(ObjectModelBuilderBase):
    def ensurePrefab(self) -> None: pass
    def instanceObject(self, src: object) -> object:
        return 'clone'
    def createObject(self, src: object, materialManager: MaterialManager) -> object:
        file = src #Binary_Nif
        textureManager = materialManager._textureManager
        for texturePath in file.getTexturePaths(): textureManager.preloadTexture(texturePath)
        s = f'obj: {file.name}'
        return s

# Panda3dShaderBuilder
class Panda3dShaderBuilder(ShaderBuilderBase):
    _loader: ShaderLoader = None
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: return self._loader.createShader(path, args)

# Panda3dTextureBuilder
# https://docs.panda3d.org/1.10/python/programming/texturing/simple-texturing#simple-texturing
# https://docs.panda3d.org/1.10/python/programming/texturing/creating-textures#creating-new-textures-from-scratch
class Panda3dTextureBuilder(TextureBuilderBase):
    _defaultTexture: Texture = None
    @property
    def defaultTexture(self) -> Texture:
        if self._defaultTexture: return self._defaultTexture
        self._defaultTexture = self._createDefaultTexture()
        return self._defaultTexture

    def release(self) -> None:
        if self._defaultTexture: self._defaultTexture.release(); self._defaultTexture = None

    def _createDefaultTexture(self) -> Texture: return base.loader.loadModel('maps/noise.rgb')

    def createNormalMapTexture(self, src: Texture, strength: float) -> Texture:
        return 0

    def createSolidTexture(self, width: int, height: int, pixels: object) -> Texture:
        tex = Texture('texture')
        tex.setup2dTexture(width, height, Texture.TUnsignedByte, Texture.F_rgb)
        # tex.setClearColor(pixels)
        return 0

    def createTexture(self, reuse: Texture, src: ITexture, level2: range = None) -> Texture:
        tex = reuse if reuse != None else Texture('texture')
        numMipMaps = max(1, src.mipMaps)
        width = src.width; height = src.height
        # create
        @staticmethod
        def _lambdax(x: object) -> int:
            match x:
                case Texture_Bytes():
                    bytes, fmt, spans = (x.bytes, x.format, x.spans)
                    # process
                    if not bytes: return self.defaultTexture
                    elif isinstance(fmt, tuple):
                        formatx, pixel = fmt
                        s = pixel & TexturePixel.Signed
                        f = pixel & TexturePixel.Float
                        if formatx & TextureFormat.Compressed:
                            match formatx:
                                case TextureFormat.DXT1: format, compression = Texture.FRgb, Texture.CMDxt1 # if s else Texture.FRgb, Texture.CMDxt1
                                case TextureFormat.DXT1A: format, compression = Texture.FRgb, Texture.CMDxt1
                                case TextureFormat.DXT3: format, compression = Texture.FRgba, Texture.CMDxt3
                                case TextureFormat.DXT5: format, compression = Texture.FRgba, Texture.CMDxt5
                                case TextureFormat.BC4: format, compression = Texture.FRgba, Texture.CMRgtc
                                case TextureFormat.BC5: format, compression = Texture.FRgba, Texture.CMRgtc
                                # case TextureFormat.BC6H: format, compression = Texture.FRgba, Texture.??
                                # case TextureFormat.BC7: format, compression = Texture.FRgba, Texture.??
                                case TextureFormat.ETC2: format, compression = Texture.FRgba, Texture.CMEtc2
                                case TextureFormat.ETC2_EAC: format, compression = Texture.FRgba, Texture.CMEac
                                case _: raise Exception(f'Unknown format: {formatx}')
                            tex.setup2dTexture(width, height, Texture.TUnsignedByte, format)
                            tex.setRamImage(bytes, compression)
                        else:
                            match formatx:
                                case TextureFormat.I8: component_type, format = Texture.TUnsignedByte, Texture.FLuminance
                                case TextureFormat.L8: component_type, format = Texture.TUnsignedByte, Texture.FLuminance
                                case TextureFormat.R8: component_type, format = Texture.TUnsignedByte, Texture.FR8i
                                case TextureFormat.R16: component_type, format = Texture.TFloat, Texture.FR16 if f else Texture.TUnsignedShort, Texture.FR16i
                                case TextureFormat.RG16: component_type, format = Texture.TFloat, Texture.FRg16 if f else Texture.TUnsignedShort, Texture.FRg16i
                                case TextureFormat.RGB24: component_type, format = Texture.TUnsignedByte, Texture.FRgb8
                                case TextureFormat.RGB565: component_type, format = Texture.TUnsignedByte, Texture.FRgb565
                                case TextureFormat.RGBA32: component_type, format = Texture.TUnsignedByte, Texture.FRgba8
                                case TextureFormat.ARGB32: component_type, format = Texture.TUnsignedInt, Texture.FRgba8
                                case TextureFormat.BGRA32: component_type, format = Texture.TUnsignedInt, Texture.FRgba8
                                case TextureFormat.BGRA1555: component_type, format = Texture.TUnsignedShort, Texture.FRgba8
                                case _: raise Exception(f'Unknown format: {formatx}')
                            tex.setup2dTexture(width, height, Texture.TUnsignedByte, format)
                    else: raise Exception(f'Unknown format: {fmt}')
                    # set mip-maps
                    tex.setMinfilter(Texture.FTLinearMipmapLinear)
                    offset = 0; width2 = width; height2 = height
                    for level in range(numMipMaps):
                        size = tex.getExpectedRamMipmapImageSize(level+1)
                        image = bytes[offset:offset+size]
                        tex.setRamMipmapImage(level, CPTAUchar(image))
                        offset += size; width2 = max(1, width2 // 2); height2 = max(1, height2 // 2)
                    # tex.prepare(base.win.getGsg())
                    # tex.prepare(base.graphicsEngine.getGs())
                    return tex
                case _: raise Exception(f'Unknown x: {x}')
        return src.create('PD', _lambdax)

    def deleteTexture(self, texture: Texture) -> None: texture.release()

# Panda3dMaterialBuilder
# https://docs.panda3d.org/1.10/python/programming/render-attributes/materials
class Panda3dMaterialBuilder(MaterialBuilderBase):
    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    _defaultMaterial: GLRenderMaterial; _terrainMaterial: GLRenderMaterial
    @property
    def defaultMaterial(self) -> int:
        if self._defaultMaterial: return self._defaultMaterial
        self._defaultMaterial = self._createDefaultMaterial()
        return self._defaultMaterial
    @property
    def terrainMaterial(self) -> int:
        if self._terrainMaterial: return self._terrainMaterial
        self._terrainMaterial = self._createDefaultMaterial()
        return self._terrainMaterial

    def _createDefaultMaterial() -> GLRenderMaterial:
        m = GLRenderMaterial(None)
        m.textures['g_tColor'] = self.textureManager.defaultTexture
        m.material.shaderName = 'vrf.error'
        return m

    def _createTerrainMaterial() -> GLRenderMaterial:
        m = GLRenderMaterial(None)
        m.material.shaderName = 'vrf.error'
        return m

    def createMaterial(self, key: object) -> GLRenderMaterial:
        match key:
            case s if isinstance(key, IMaterial):
                match s:
                    case m if isinstance(key, IFixedMaterial): return m
                    case p if isinstance(key, IMaterial):
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
                        if not 'g_vTexCoordScale' in p.vectorParams: p.vectorParams['g_vTexCoordScale'] = ones(4)
                        if not 'g_vTexCoordOffset' in p.vectorParams: p.vectorParams['g_vTexCoordOffset'] = zeros(4)
                        if not 'g_vColorTint' in p.vectorParams: p.vectorParams['g_vColorTint'] = ones(4)
                        return m
                    case _: raise Exception(f'Unknown: {s}')
            case _: raise Exception(f'Unknown: {key}')

# Panda3dGfxApi
class Panda3dGfxApi(IOpenGfxApi):
    def __init__(self, source: ISource):
        self.source: ISource = source
    def addMeshCollider(self, src: NodePath, mesh: object, isKinematic: bool, isStatic: bool) -> None: raise NotImplementedError();
    def addMeshRenderer(self, src: NodePath, mesh: object, material: GLRenderMaterial, enabled: bool, isStatic: bool) -> None: raise NotImplementedError();
    def addMissingMeshCollidersRecursively(self, src: NodePath, isStatic: bool) -> None: raise NotImplementedError();
    def attach(self, method: GfxAttach, src: NodePath, args: list[object]) -> None: pass
    def createMesh(self, mesh: object) -> NodePath: raise NotImplementedError();
    def createObject(self, name: str, tag: str = None, parent: NodePath = None) -> NodePath:
        n = PandaNode(name)
        if tag: n.setTag('tag', tag)
        p = parent or base.render
        s = p.attachNewNode(n)
        return s
    def setLayerRecursively(self, src: NodePath, layer: int) -> None: raise NotImplementedError();
    def parent(self, src: PandNodePathaNode, parent: NodePath) -> None: raise NotImplementedError();
    def transform(self, src: NodePath, position: Vector3, rotation: quaternion, localScale: Vector3) -> None: raise NotImplementedError();
    def transform(self, src: NodePath, position: Vector3, rotation: Matrix4x4, localScale: Vector3) -> None: raise NotImplementedError();
    def setVisible(self, src: NodePath, visible: bool) -> None:
        src.show()
        # if visible:
        #     if src.isHidden(): src.show()
        # else:
        #     if not src.isHidden(): src.hide()
    def destroy(self, src: NodePath) -> None: src.removeNode()

# Panda3dGfx
class Panda3dGfxModel(IOpenGfxModel):
    def __init__(self, source: ISource):
        self.source: ISource = source
        self.textureManager: TextureManager = TextureManager(source, Panda3dTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(source, self.textureManager, Panda3dMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(source, self.materialManager, Panda3dObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(source, Panda3dShaderBuilder())

    def preloadObject(self, path: object) -> None: self.objectManager.preloadObject(path)
    def preloadTexture(self, path: object) -> None: self.textureManager.preloadTexture(path)
    def createObject(self, path: object) -> tuple[object, dict[str, object]]: return self.objectManager.createObject(path)[0]
    def createShader(self, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(path, args)[0]
    def createTexture(self, path: object, level: range = None) -> int: return self.textureManager.createTexture(path, level)[0]

# Panda3dGfxLight
class Panda3dGfxLight(IOpenGfxLight):
    def __init__(self, source: ISource):
        self.source: ISource = source
    def createLight(self, name: str, position: Vector3, radius: float, color: Color, indoors: bool, parent: NodePath = None) -> NodePath:
        n = PointLight(name)
        n.setColor((0.7, 0.7, 0.7, 1))
        p = parent or base.render
        s = p.attachNewNode(n)
        if position: s.setPos(vector3ToPanda(position))
        base.render.setLight(s)
        return s
    def createReflectionProbe(self, name: str, position: Vector3, parent: object = None) -> object: return 'probe'

# Panda3dGfxTerrain
class Panda3dGfxTerrain(IOpenGfxTerrain):
    class TerrainLayer:
        def __init__(self, diffuseTexture: Texture, smoothness: float, metallic: float, maskMapTexture: Texture, normalMapTexture: Texture, tileSize: Vector3):
            self.diffuseTexture = diffuseTexture
            self.smoothness = smoothness
            self.metallic = metallic
            self.maskMapTexture = maskMapTexture
            self.normalMapTexture = normalMapTexture
            self.tileSize = tileSize
    class TerrainData:
        def __init__(self, heightmapResolution: int):
            self.heightmapResolution: int = heightmapResolution
            self.heights: PNMImage = PNMImage(heightmapResolution, heightmapResolution, 1); self.heights.setMaxval(0xffff)
            self.size: Vector3 = None
            self.terrainLayers: list[TerrainLayer] = None
            self.alphamapResolution: int = 0
            self.alphamap: ndarray = None
        def setHeights(self, x: int, y: int, heights: ndarray) -> None:
            s = self.heights; z = self.heightmapResolution
            for y in range(z):
                for x in range(z):
                    h = heights[y, x] # Get float value (0.0 to 1.0)
                    # h = (h / 2) + .5
                    s.setGray(x, y, h) # Set pixel: 0.0 (black) is lowest, 1.0 (white) is highest
        def setAlphamaps(self, x: int, y: int, alphamap: ndarray) -> None: self.alphamap = alphamap
    def __init__(self, source: ISource):
        self.source: ISource = source
    def createTerrainData(self, offset: int, heights: ndarray, heightRange: float, sampleDistance: float, layers: list[GfxTerrainLayer], alphaMap: ndarray) -> object:
        hShape = heights.shape; aShape = alphaMap.shape
        assert(hShape[0] == hShape[1] and heightRange >= 0 and sampleDistance >= 0)
        resolution = hShape[0]
        s = Panda3dGfxTerrain.TerrainData(heightmapResolution=resolution)
        terrainWidth = (resolution + offset) * sampleDistance
        if not math.isclose(heightRange, 0): s.size = array([terrainWidth, heightRange, terrainWidth]); s.setHeights(0, 0, heights)
        else: s.size = array([terrainWidth, 1., terrainWidth])
        s.terrainLayers = [Panda3dGfxTerrain.TerrainLayer(
            diffuseTexture=s.texture,
            smoothness=s.smoothness,
            metallic=s.metallic,
            maskMapTexture=s.maskMapTexture,
            normalMapTexture=s.normalMapTexture,
            tileSize=s.tileSize) for s in layers]
        if alphaMap.size == 0: assert(aShape[0] == aShape[1]); s.alphamapResolution = alphaMap[0]; s.setAlphamaps(0, 0, alphaMap)
        return s
    def createTerrain(self, name: str, position: Vector3, data: object, parent: NodePath = None) -> NodePath:
        # print(f't: {parent}')
        t = GeoMipTerrain(name) # Create the GeoMipTerrain instance
        t.setHeightfield(data.heights) # Load a heightfield image (preferably power-of-two plus one, e.g., 513x513)
        # t.setBlockSize(32) #data.size[0]
        # t.setNear(40); t.setFar(100)
        t.setBruteforce(True) # Bruteforce disables Level of Detail (LOD) for simple, small maps
        t.generate()
        s = t.getRoot()
        s.setTexture(base.loader.loadTexture('maps/envir-ground.jpg'))
        # s.setTexture(data.terrainLayers[0].diffuseTexture)
        s.reparentTo(parent or base.render)
        # for i, l in enumerate(data.terrainLayers):
        #     print(l)
        #     ts = TextureStage(f'{i}'); ts.setSort(i)
        #     if i > 0:
        #         # ts.setCombineAlpha(TextureStage.CMReplace, TextureStage.CSTexture, TextureStage.COSrcColor)
        #         ts.setMode(TextureStage.MBlend)
        #     n.setTexture(ts, l.diffuseTexture)
        s.setSz(10) # data.size[1]
        if position.size != 0: s.setPos(vector3ToPanda(position))
        return s

# Panda3dPlatform
class Panda3dPlatform(Platform):
    def __init__(self):
        super().__init__('PD', 'Panda3D')
        self.gfxFactory = staticmethod(lambda source: [Panda3dGfxApi(source), None, None, Panda3dGfxModel(source), Panda3dGfxLight(source), Panda3dGfxTerrain(source)])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
Panda3dPlatform.This = Panda3dPlatform()

#endregion

def vector3ToPanda(pos: Vector3) -> tuple: return (pos[0]+1000, pos[1]+1000, pos[2])