from __future__ import annotations
import os, io, numpy as np
from openstk.core import Platform, SystemSfx
from openstk.gfx import IOpenGfxModel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureManager, TextureBuilderBase
from openstk.client import IClientHost
from .enginx.eng import Game, GraphicsDeviceManager

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

# ExGfxSprite2D
class ExGfxSprite2D(IOpenGfxSprite):
    source: ISource
    spriteManager: SpriteManager
    objectManager: ObjectSpriteManager
    def __init__(self, source: ISource):
        self.source = source
        # self.spriteManager = SpriteManager(source, OpenGLTextureBuilder())
        # self.objectManager = ObjectManager(source, OpenGLObjectModelBuilder())

    def createSprite(self, path: object, level: range = None) -> int: return self.spriteManager.createSprite(path)[0]
    def preloadSprite(self, path: object) -> None: self.textureManager.spriteManager(path)
    def createObject(self, path: object, parent: object) -> (object, dict[str, object]): raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()
    def getAsset(self, t: type, path: object) -> object: return self.source.getAsset(t, path)
    def attachObject(self, method: AttachObjectMethod, source: object, args: list[object]) -> object: raise NotImplementedError()

# ExPlatform
class ExPlatform(Platform):
    def __init__(self):
        super().__init__('EX', 'EnginX')
        self.gfxFactory = staticmethod(lambda source: [ExGfxSprite2D(source), None, None])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
ExPlatform.This = ExPlatform()

#endregion