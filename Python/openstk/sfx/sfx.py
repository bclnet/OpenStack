from openstk.core import ISource

# typedefs
class Audio: pass

# AudioBuilder
class AudioBuilder:
    def create(self, path: object) -> Audio: pass
    def delete(self, audio: Audio) -> None: pass

# AudioManager
class AudioManager:
    _builder: AudioBuilder
    _cached: dict[object, (Audio, object)] = {}
    _tasks: dict[object, object] = {}
    def __init__(self, builder: AudioBuilder):
        self._builder = builder

    def create(self, source: ISource, path: object) -> (Audio, object):
        key = (source, path)
        if key in self._cached: return self._cached[key]
        tag = self._load(source, path)
        obj = self._builder.create(tag) if tag else None
        self._cached[key] = (obj, tag)
        return (obj, tag)

    def preload(self, source: ISource, path: object) -> None:
        key = (source, path)
        if key in self._cached: return
        if not key in self._tasks: self._tasks[key] = source.getAsset(object, path)

    def delete(self, source: ISource, path: object) -> None:
        key = (source, path)
        if not key in self._cached: return
        self._builder.delete(self._cached[0])
        self._cached.pop(key)

    async def _load(self, source: ISource, path: object) -> object:
        key = (source, path)
        assert(not key in self._cached)
        self.preload(source, s)
        obj = await self._tasks[key]
        self._tasks.pop(key)
        return obj

# IOpenGfx:
class IOpenSfx: pass

# IOpenSfx2
class IOpenSfx2(IOpenSfx):
    audioManager: AudioManager
    def create(self, source: ISource, path: object) -> Audio: pass
