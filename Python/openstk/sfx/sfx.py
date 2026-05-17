from openstk.core import ISource

# typedefs
class Audio: pass

# AudioBuilderBase
class AudioBuilderBase:
    def createAudio(self, path: object) -> Audio: pass
    def deleteAudio(self, audio: Audio) -> None: pass

# AudioManager
class AudioManager:
    _builder: AudioBuilderBase
    _cachedAudios: dict[object, (Audio, object)] = {}
    _preloadTasks: dict[object, object] = {}
    def __init__(self, builder: AudioBuilderBase):
        self._builder = builder

    def createAudio(self, source: ISource, path: object) -> (Audio, object):
        key = (source, path)
        if key in self._cachedAudios: return self._cachedAudios[key]
        tag = self._loadAudio(source, path)
        obj = self._builder.createAudio(tag) if tag else None
        self._cachedAudios[key] = (obj, tag)
        return (obj, tag)

    def preloadAudio(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cachedAudios: return
        if not key in self._preloadTasks: self._preloadTasks[key] = source.getAsset(object, path)

    def deleteAudio(self, source: ISource, path: object) -> None:
        key = (source, path)
        if not key in self._cachedAudios: return
        self._builder.deleteAudio(self._cachedAudios[0])
        self._cachedAudios.pop(key)

    async def _loadAudio(self, source: ISource, path: object) -> object:
        key = (source, path)
        assert(not key in self._cachedAudios)
        self.preloadAudio(source, s)
        obj = await self._preloadTasks[key]
        self._preloadTasks.pop(key)
        return obj

# IOpenGfx:
class IOpenSfx: pass

# IOpenSfx2
class IOpenSfx2(IOpenSfx):
    audioManager: AudioManager
    def createAudio(self, source: ISource, path: object) -> Audio: pass
