from __future__ import annotations
import traceback
from openstk.core import ISource, Platform
from openstk.platforms.system import SystemSfx
from openstk.gfx import IOpenGfxApi, IOpenGfxSprite, IOpenGfxModel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureManager, TextureBuilderBase
from openstk.client import IClientHost
from openstk.gfx.egin import Game, GraphicsDeviceManager


#region Client

# EginXClientHost
class EginXClientHost(Game, IClientHost):
    def __init__(self, client: callable):
        super().__init__()
        # self.deviceManager = GraphicsDeviceManager(self)
        self.client: ClientBase = client()
        self.scene: SceneBase = None
        self.pluginHost: IPluginHost = None

    def getScene[T](self) -> T: return self.scene

    def setScene(self, scene: SceneBase) -> None:
        if self.scene: self.scene.dispose()
        self.scene = scene
        self.scene.load() 

    def loadContent(self) -> None:
        super().loadContent()
        self.client.loadContent()

    def unloadContent(self) -> None:
        self.client.unloadContent()
        super().unloadContent()

#endregion

#region Platform

# EginXObjectBuilder
# MISSING

# EginXGfxApi
class EginXGfxApi(IOpenGfxApi):
    def __init__(self): pass
    def attach(self, method: GfxAttach, src: object, args: list[object]) -> object: raise NotImplementedError()

# EginXGfxSprite2D
class EginXGfxSprite2D(IOpenGfxSprite):
    def __init__(self):
        # self.spriteManager: SpriteManager = SpriteManager(OpenGLTextureBuilder())
        # self.objectManager: ObjectSpriteManager = ObjectManager(OpenGLObjectModelBuilder())
        pass
    # def preloadObject(self, source: ISource, path: object) -> None: raise NotImplementedError()
    def preloadSprite(self, source: ISource, path: object) -> None: self.textureManager.spriteManager(path)
    # def createObject(self, source: ISource, path: object, parent: object) -> tuple[object, dict[str, object]]: raise NotImplementedError()
    def createSprite(self, source: ISource, path: object, level: range = None) -> int: return self.spriteManager.createSprite(path)[0]

# EginXPlatform
class EginXPlatform(Platform):
    def __init__(self):
        super().__init__('EX', 'EginX')
        self.gfxFactory = staticmethod(lambda: [EginXGfxApi(), EginXGfxSprite2D(), None, None, None, None])
        self.sfxFactory = staticmethod(lambda: [SystemSfx()])
EginXPlatform.this = EginXPlatform()

#endregion