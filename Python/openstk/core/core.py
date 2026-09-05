
# ISource
class ISource:
    def getAsset(self, path: object, option: object = None, throwOnError: bool = True) -> object: pass
    def findPath(self, path: object) -> object: pass

# IHaveSource
class IHaveSource:
    source: ISource

# IStream
class IStream:
    def getStream(self) -> object: pass

# IWriteToStream
class IWriteToStream:
    def writeToStream(self, stream: object) -> None: pass
