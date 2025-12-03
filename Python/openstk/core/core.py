
# ISource
class ISource:
    def getAsset(self, path: object, option: object = None, throwOnError: bool = True) -> object: pass
    def findPath(self, path: object) -> object: pass

# IStream
class IStream:
    def getStream(self) -> object: pass

# IWriteToStream
class IWriteToStream:
    def writeToStream(self, stream: object) -> None: pass

# lumps
# class X_LumpON:
#     offset: int
#     num: int

# class X_LumpNO:
#     num: int
#     offset: int

# class X_LumpNO2:
#     num: int
#     offset: int
#     offset2: int

# class X_Lump2NO:
#     offset2: int
#     num: int
#     offset: int
