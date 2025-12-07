from __future__ import annotations
import os, io, numpy as np
from openstk.gfx import IOpenGfxModel, ObjectModelBuilderBase, ObjectModelManager, MaterialBuilderBase, MaterialManager, ShaderBuilderBase, ShaderManager, TextureManager, TextureBuilderBase
from openstk.platforms import Platform, SystemSfx

# typedefs
class ISource: pass

#region OpenGfx

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
    def createObject(self, path: object) -> (object, dict[str, object]): raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()
    def loadFileObject(self, type: type, path: object) -> object: return self.source.loadFileObject(type, path)


# ExPlatform
class ExPlatform(Platform):
    def __init__(self):
        super().__init__('EX', 'EnginX')
        self.gfxFactory = staticmethod(lambda source: [ExGfxSprite2D(source), None, None])
        self.sfxFactory = staticmethod(lambda source: [SystemSfx(source)])
ExPlatform.This = ExPlatform()

#endregion