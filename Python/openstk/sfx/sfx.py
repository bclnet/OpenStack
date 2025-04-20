import numpy as np
from openstk.poly import ISource

# typedefs
class Audio: pass

# AudioBuilderBase
class AudioBuilderBase:
    def createAudio(self, path: object) -> Audio: pass
    def deleteAudio(self, audio: Audio) -> None: pass

# IAudioManager
class IAudioManager:
    def createAudio(self, path: object) -> (Audio, object): pass
    def preloadAudio(self, path: object) -> None: pass
    def deleteAudio(self, path: object) -> None: pass

# AudioManager
class AudioManager(IAudioManager):
    _source: ISource
    _builder: AudioBuilderBase
    _cachedAudios: dict[object, (Audio, object)] = {}
    _preloadTasks: dict[object, object] = {}
    def __init__(self, source: ISource, builder: AudioBuilderBase):
        self._source = source
        self._builder = builder

    def createAudio(self, path: object) -> (Audio, object):
        if path in self._cachedAudios: return self._cachedAudios[path]
        # load & cache the audio.
        tag = self._loadAudio(path)
        obj = self._builder.createAudio(tag) if tag else None
        self._cachedAudios[path] = (obj, tag)
        return (obj, tag)

    def preloadAudio(self, path: object) -> None:
        if path in self._cachedAudios: return
        # start loading the audio file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._pakFile.loadFileObject(object, path)

    def deleteAudio(self, path: object) -> None:
        if not path in self._cachedAudios: return
        self._builder.deleteAudio(self._cachedAudios[0])
        self._cachedAudios.remove(path)

    async def _loadAudio(self, path: object) -> object:
        assert(not path in self._cachedAudios)
        self.preloadAudio(s)
        obj = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return obj

# IOpenGfx:
class IOpenSfx: pass

# IOpenSfx2
class IOpenSfx2(IOpenSfx):
    audioManager: IAudioManager
    def createAudio(self, path: object) -> Audio: pass
