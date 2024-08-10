import numpy as np

# typedefs
class Audio: pass

# IAudioManager
class IAudioManager:
    def createAudio(self, path: object) -> (Audio, object): pass
    def preloadAudio(self, path: object) -> None: pass
    def deleteAudio(self, path: object) -> None: pass

# IOpenGfx:
class IOpenSfx:
    pass

# IOpenSfxAny
class IOpenSfxAny(IOpenSfx):
    audioManager: IAudioManager
    def createAudio(self, path: object) -> Audio: pass
