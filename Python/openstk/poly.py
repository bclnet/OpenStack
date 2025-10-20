import asyncio, openstk.poly_unsafe as unsafe
from openstk.poly_find import findType
from openstk.poly_genericpool import IGenericPool, GenericPool, SinglePool, StaticPool
from openstk.poly_reader import Reader
from openstk.poly_writer import Writer
__all__ = ['unsafe', 'findType', 'IGenericPool', 'GenericPool', 'SinglePool', 'StaticPool', 'Reader', 'Writer', 'parallelFor']

# ISource
class ISource:
    def loadFileObject(self, path: object, option: object = None, throwOnError: bool = True) -> object: pass

# IStream
class IStream:
    def getStream(self) -> object: pass

# IWriteToStream
class IWriteToStream:
    def writeToStream(self, stream: object) -> None: pass

@staticmethod
def log(s: str) -> None: print(s)
@staticmethod
async def _parallelForTask(f: int, t: int, s: int, c: callable) -> list[object]: [await c(idx) for idx in range(f, t, s)]
@staticmethod
async def parallelFor(f: int, t: int, o: set, c: callable) -> list[object]: await asyncio.gather(*[_parallelForTask(f, t, i + 1, c) for i in range(o['max'] or 1)])

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
