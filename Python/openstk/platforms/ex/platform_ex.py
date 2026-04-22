from __future__ import annotations
import os, io, numpy as np
from openstk.core import Platform
from openstk.platforms.system import SystemSfx
from openstk.gfx import IOpenGfxSprite, IOpenGfxModel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureManager, TextureBuilderBase
from openstk.client import IClientHost
from openstk.gfx.egin import Game, GraphicsDeviceManager

# typedefs
class ISource: pass

#region Client

# ExClientHost
class ExClientHost(Game, IClientHost):
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

# ExObjectBuilder
# MISSING

# ExGfxApi
class ExGfxApi(IOpenGfxApi):
    def __init__(self, source: ISource):
        self.source: ISource = source
    def attachObject(self, method: AttachObjectMethod, source: object, args: list[object]) -> object: raise NotImplementedError()

# ExGfxSprite2D
class ExGfxSprite2D(IOpenGfxSprite):
    def __init__(self, source: ISource):
        self.source: ISource = source
        # self.spriteManager: SpriteManager = SpriteManager(source, OpenGLTextureBuilder())
        # self.objectManager: ObjectSpriteManager = ObjectManager(source, OpenGLObjectModelBuilder())

    def createSprite(self, path: object, level: range = None) -> int: return self.spriteManager.createSprite(path)[0]
    def preloadSprite(self, path: object) -> None: self.textureManager.spriteManager(path)
    def createObject(self, path: object, parent: object) -> (object, dict[str, object]): raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()
    def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)

# ExPlatform
class ExPlatform(Platform):
    def __init__(self):
        super().__init__('EX', 'EnginX')
        self.gfxFactory = staticmethod(lambda source: [ExGfxApi(source), ExGfxSprite2D(source), None, None, None])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
ExPlatform.This = ExPlatform()

#endregion