from __future__ import annotations
import traceback
from numpy import ones, zeros
from openstk.core import ISource, Platform
from openstk.client import IClientHost
from openstk.gfx import IOpenGfxApi, IOpenGfxModel, IOpenGfxLight, IOpenGfxTerrain, Texture_Dds, Texture_Bytes, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilderBase, ObjectModelManager, IMaterial, MaterialStdProp, MaterialBuilderBase, MaterialManager, Shader, ShaderBuilderBase, ShaderManager, TextureBuilderBase, TextureManager
from openstk.platforms.pygame.gfx.pygame import PyEngine3dX
from openstk.platforms.system import SystemSfx

#region Client

# PyEngine3dClientHost
class PyEngine3dClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# PyEngine3dObjectModelBuilder
class PyEngine3dObjectModelBuilder(ObjectModelBuilderBase):
    def instanceObject(self, src: object) -> object:
        return 'clone'
    async def createObject(self, source: ISource, path: object, isStatic: bool, materialManager: MaterialManager) -> object:
        builder = PyEngine3dX.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, isStatic, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensurePrefab(self) -> None: pass

# PyEngine3dShaderBuilder
class PyEngine3dShaderBuilder(ShaderBuilderBase):
    _loader: ShaderLoader = None
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: return self._loader.createShader(path, args)

# PyEngine3dTextureBuilder
class PyEngine3dTextureBuilder(TextureBuilderBase):
    _defaultTexture: Texture = None
    @property
    def defaultTexture(self) -> Texture:
        if self._defaultTexture: return self._defaultTexture
        self._defaultTexture = self._createDefaultTexture()
        return self._defaultTexture

    def release(self) -> None:
        if self._defaultTexture: self._defaultTexture.release(); self._defaultTexture = None

    def _createDefaultTexture(self) -> int: return base.loader.loadModel('maps/noise.rgb')

    def createTexture(self, reuse: int, source: ITexture, level2: range = None) -> int:
        try:
            if not bytes: return self.defaultTexture
            return base.loader.loadModel('maps/noise.rgb')
        finally: source.end()

    def deleteTexture(self, texture: int) -> None: texture.release()


# PyEngine3dMaterialBuilder
# class PyEngine3dMaterialBuilder(MaterialBuilderBase):
#     def __init__(self, textureManager: TextureManager):
#         super().__init__(textureManager)

#     _defaultMaterial: MaterialPoly; _terrainMaterial: MaterialPoly
#     @property
#     def defaultMaterial(self) -> MaterialPoly:
#         if self._defaultMaterial: return self._defaultMaterial
#         self._defaultMaterial = self._createDefaultMaterial()
#         return self._defaultMaterial
#     @property
#     def terrainMaterial(self) -> MaterialPoly:
#         if self._terrainMaterial: return self._terrainMaterial
#         self._terrainMaterial = self._createDefaultMaterial()
#         return self._terrainMaterial

#     def _createDefaultMaterial() -> MaterialPoly:
#         m = MaterialPoly(Material(), {'Main': self.textureManager.defaultTexture})
#         return m

#     def _createTerrainMaterial() -> MaterialPoly:
#         m = MaterialPoly(Material(), {'Main': self.textureManager.terrainTexture})
#         return m

#     async def createMaterial(self, source: ISource, path: object) -> MaterialPoly:
#         match path:
#             case p if isinstance(path, MaterialStdProp):
#                 m = MaterialPoly(Material(), {k:(await self.textureManager.createTexture(source, v))[0] for k, v in p.textures.items() if k == 'Main' or k == 'Bump'})
#                 return m
#             # case s if isinstance(path, MaterialShaderProp): return m
#             case _: raise Exception(f'Unknown: {path}')

# PyEngine3dGfxApi
class PyEngine3dGfxApi(IOpenGfxApi):
    def __init__(self): pass
    # def addMeshCollider(self, src: NodePath, mesh: object, isKinematic: bool, isStatic: bool) -> None: raise NotImplementedError();
    # def addMeshRenderer(self, src: NodePath, mesh: object, material: Material, enabled: bool, isStatic: bool) -> None: raise NotImplementedError();
    # def addMissingMeshCollidersRecursively(self, src: NodePath, isStatic: bool) -> None: raise NotImplementedError();
    # def attach(self, method: GfxAttach, src: NodePath, args: list[object]) -> None: pass
    # def createMesh(self, mesh: object) -> NodePath: raise NotImplementedError();
    # def createObject(self, name: str, tag: str = None, parent: NodePath = None) -> NodePath:
    #     n = PandaNode(name)
    #     if tag: n.setTag('tag', tag)
    #     p = parent or base.render
    #     s = p.attachNewNode(n)
    #     return s
    # def setLayerRecursively(self, src: NodePath, layer: int) -> None: raise NotImplementedError();
    # def parent(self, src: PandNodePathaNode, parent: NodePath) -> None: raise NotImplementedError();
    # def transform(self, src: NodePath, position: Vector3, rotation: quaternion, localScale: Vector3) -> None: raise NotImplementedError();
    # def transform(self, src: NodePath, position: Vector3, rotation: Matrix4x4, localScale: Vector3) -> None: raise NotImplementedError();
    # def setVisible(self, src: NodePath, visible: bool) -> None:
    #     src.show()
    #     # if visible:
    #     #     if src.isHidden(): src.show()
    #     # else:
    #     #     if not src.isHidden(): src.hide()
    # def destroy(self, src: NodePath) -> None: src.removeNode()

# PyEngine3dMaterialBuilder
class PyEngine3dMaterialBuilder(MaterialBuilderBase):
    _defaultMaterial: Material = None; _terrainMaterial: Material = None
    @property
    def defaultMaterial(self) -> Material:
        if self._defaultMaterial: return self._defaultMaterial
        self._defaultMaterial = self._createDefaultMaterial()
        return self._defaultMaterial
    @property
    def terrainMaterial(self) -> Material:
        if self._terrainMaterial: return self._terrainMaterial
        self._terrainMaterial = self._createTerrainMaterial()
        return self._terrainMaterial

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefaultMaterial() -> Material:
        m = Material()
        m.textures['g_tColor'] = self.textureManager.defaultTexture
        m.material.shaderName = 'vrf.error'
        return m
    
    def _createTerrainMaterial() -> Material:
        m = Material()
        m.material.shaderName = 'vrf.error'
        return m

    def createMaterial(self, key: object) -> Material:
        match key:
            case _: raise Exception(f'Unknown: {key}')

# PyEngine3dGfx
class PyEngine3dGfxModel(IOpenGfxModel):
    def __init__(self):
        self.textureManager: TextureManager = TextureManager(PyEngine3dTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, PyEngine3dMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, PyEngine3dObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(PyEngine3dShaderBuilder())
    def preloadObject(self, source: ISource, path: object) -> None: self.objectManager.preloadObject(source, path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preloadTexture(source, path)
    def createObject(self, source: ISource, path: object, isStatic: bool, parent: object = None) -> tuple[object, dict[str, object]]: return self.objectManager.createObject(source, path, isStatic, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.createShader(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> int: return self.textureManager.createTexture(source, path, level)

# PyEngine3dPlatform
class PyEngine3dPlatform(Platform):
    def __init__(self):
        super().__init__('P3', 'PyEngine3D')
        self.gfxFactory = staticmethod(lambda: [PyEngine3dGfxApi(), None, None, PyEngine3dGfxModel(), None, None])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
PyEngine3dPlatform.this = PyEngine3dPlatform()

#endregion