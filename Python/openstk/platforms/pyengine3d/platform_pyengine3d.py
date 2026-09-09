from __future__ import annotations
import traceback
from numpy import ones, zeros
from openstk.core import ISource, Platform
from openstk.client import IClientHost
from openstk.gfx import IOpenGfxApi, IOpenGfxModel, IOpenGfxLight, IOpenGfxTerrain, TextureAsDds, TextureAsBytes, TextureFlags, TextureFormat, TexturePixel, ObjectModelBuilder, ObjectModelManager, IMaterial, MaterialStdProp, MaterialBuilder, MaterialManager, GfxShader, ShaderBuilder, ShaderManager, TextureBuilder, TextureManager
from openstk.platforms.pyengine3d.gfx.pyengine3d import PyEngine3dX
from openstk.platforms.system import SystemSfx

#region Client

# PyEngine3dClientHost
class PyEngine3dClientHost(IClientHost):
    def __init__(self, client: callable): pass

#endregion

#region Platform

# PyEngine3dObjectModelBuilder
class PyEngine3dObjectModelBuilder(ObjectModelBuilder):
    def instance(self, src: object) -> object:
        return 'clone'
    async def create(self, source: ISource, path: object, static_: bool, materialManager: MaterialManager) -> object:
        builder = PyEngine3dX.buildersByType[path.__class__.__name__]
        try:
            s = await builder(source, path, static_, materialManager)
            return s
        except Exception as e: print(e); traceback.print_exc()
    def ensure(self) -> None: pass

# PyEngine3dShaderBuilder
class PyEngine3dShaderBuilder(ShaderBuilder):
    def createShader(self, path: object, args: dict[str, bool]) -> Shader: raise NotImplementedError()

# PyEngine3dTextureBuilder
class PyEngine3dTextureBuilder(TextureBuilder):
    _default: Texture = None
    @property
    def default(self) -> Texture:
        if self._default: return self._default
        self._default = self._createDefault()
        return self._default

    def release(self) -> None:
        if self._default: self._default.release(); self._default = None

    def _createDefault(self) -> int: return base.loader.loadModel('maps/noise.rgb')

    def create(self, reuse: int, source: ITexture, level2: range = None) -> int:
        try:
            if not bytes: return self.default
            return base.loader.loadModel('maps/noise.rgb')
        finally: source.end()

    def delete(self, texture: int) -> None: texture.release()


# PyEngine3dGfxApi
class PyEngine3dGfxApi(IOpenGfxApi):
    def __init__(self): pass
    # def addMeshCollider(self, src: NodePath, mesh: object, isKinematic: bool, static_: bool) -> None: raise NotImplementedError();
    # def addMeshRenderer(self, src: NodePath, mesh: object, material: Material, enabled: bool, static_: bool) -> None: raise NotImplementedError();
    # def addMissingMeshCollidersRecursively(self, src: NodePath, static_: bool) -> None: raise NotImplementedError();
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
class PyEngine3dMaterialBuilder(MaterialBuilder):
    _default: Material = None; _terrain: Material = None
    @property
    def default(self) -> Material:
        if self._default: return self._default
        self._default = self._createDefault()
        return self._default
    @property
    def terrainMaterial(self) -> Material:
        if self._terrain: return self._terrain
        self._terrain = self._createTerrain()
        return self._terrain

    def __init__(self, textureManager: TextureManager):
        super().__init__(textureManager)

    def _createDefault() -> Material:
        m = Material()
        m.textures['g_tColor'] = self.textureManager.default
        m.material.shaderName = 'vrf.error'
        return m
    
    def _createTerrain() -> Material:
        m = Material()
        m.material.shaderName = 'vrf.error'
        return m

    def create(self, key: object) -> Material:
        match key:
            case _: raise Exception(f'Unknown: {key}')

# PyEngine3dGfx
class PyEngine3dGfxModel(IOpenGfxModel):
    def __init__(self):
        self.textureManager: TextureManager = TextureManager(PyEngine3dTextureBuilder())
        self.materialManager: MaterialManager = MaterialManager(self.textureManager, PyEngine3dMaterialBuilder(self.textureManager))
        self.objectManager: ObjectModelManager = ObjectModelManager(self.materialManager, PyEngine3dObjectModelBuilder())
        self.shaderManager: ShaderManager = ShaderManager(PyEngine3dShaderBuilder())
    def preload(self, source: ISource, path: object) -> None: self.objectManager.preload(source, path)
    def preloadTexture(self, source: ISource, path: object) -> None: self.textureManager.preload(source, path)
    def create(self, source: ISource, path: object, static_: bool, parent: object = None) -> tuple[object, dict[str, object]]: return self.objectManager.create(source, path, static_, parent)
    def createShader(self, source: ISource, path: object, args: dict[str, bool] = None) -> Shader: return self.shaderManager.create(source, path, args)
    def createTexture(self, source: ISource, path: object, level: range = None) -> int: return self.textureManager.create(source, path, level)

# PyEngine3dPlatform
class PyEngine3dPlatform(Platform):
    def __init__(self):
        super().__init__('P3', 'PyEngine3D')
        self.gfxFactory = staticmethod(lambda: [PyEngine3dGfxApi(), None, None, PyEngine3dGfxModel(), None, None])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
PyEngine3dPlatform.this = PyEngine3dPlatform()

#endregion