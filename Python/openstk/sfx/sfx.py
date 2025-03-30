import numpy as np

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
    _pakFile: PakFile
    _builder: AudioBuilderBase
    _cachedAudios: dict[object, (Audio, object)] = {}
    _preloadTasks: dict[object, object] = {}

    def __init__(self, pakFile: PakFile, builder: AudioBuilderBase):
        self._pakFile = pakFile
        self._builder = builder

    def createAudio(self, key: object) -> (Audio, object):
        if path in self._cachedAudios: return self._cachedAudios[path]
        # load & cache the audio.
        tag = self._loadAudio(path)
        audio = self._builder.createAudio(tag) if tag else None
        self._cachedAudios[path] = (audio, tag)
        return (audio, tag)

    def preloadAudio(self, path: object) -> None:
        if path in self._cachedAudios: return
        # start loading the audio file asynchronously if we haven't already started.
        if not path in self._preloadTasks: self._preloadTasks[path] = self._pakFile.loadFileObject(object, path)

    def deleteAudio(self, path: object) -> None:
        if not path in self._cachedAudios: return
        self._builder.deleteAudio(self._cachedAudios[0])
        self._cachedAudios.remove(path)

    async def _loadAudio(self, path: object) -> ITexture:
        assert(not path in self._cachedAudios)
        self.preloadAudio(s)
        source = await self._preloadTasks[path]
        self._preloadTasks.remove(path)
        return source

# IOpenGfx:
class IOpenSfx:
    pass

# IOpenSfxAny
class IOpenSfxAny(IOpenSfx):
    audioManager: IAudioManager
    def createAudio(self, path: object) -> Audio: pass
