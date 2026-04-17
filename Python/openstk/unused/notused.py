from zipfile import ZipFile

#region FileSystem

# IFileSystem
class IFileSystem:
    def glob(self, path: str, searchPattern: str) -> list[str]: pass
    def fileExists(self, path: str) -> bool: pass
    def fileInfo(self, path: str) -> (str, int): pass
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: pass
    # def openWriter(self, path: str, mode: str = 'rb') -> Writer: pass
    def findPaths(self, path: str, searchPattern: str) -> str:
        if (expandStartIdx := searchPattern.find('(')) != -1 and \
            (expandMidIdx := searchPattern.find(':', expandStartIdx)) != -1 and \
            (expandEndIdx := searchPattern.find(')', expandMidIdx)) != -1 and \
            expandStartIdx < expandEndIdx:
            for expand in searchPattern[expandStartIdx + 1: expandEndIdx].split(':'):
                for found in self.findPaths(path, searchPattern[:expandStartIdx] + expand + searchPattern[expandEndIdx+1:]): yield found
            return
        for path in self.glob(path, searchPattern): yield path

# tag::AggregateFileSystem[]
class AggregateFileSystem(IFileSystem):
    def __init__(self, aggreate: list[IFileSystem]): self.aggreate = aggreate
    def glob(self, path: str, searchPattern: str) -> list[str]: return [y for z in [x.glob(path, searchPattern) for x in self.aggreate] for y in z]
    def fileExists(self, path: str) -> bool: return any(x.fileExists(path) for x in self.aggreate)
    def fileInfo(self, path: str) -> (str, int): return min(x.fileInfo(path) for x in self.aggreate if x[0])
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: return min(x.openReader(path) for x in self.aggreate if x)
    # def glob(self, path: str, searchPattern: str) -> list[str]:
    #     matcher = PlatformX.createMatcher(searchPattern)
    #     return [x for x in self.virtuals.keys() if matcher(x)] + self.base.glob(path, searchPattern)
    # def fileExists(self, path: str) -> bool: return path in self.virtuals or self.base.fileExists(path)
    # def fileInfo(self, path: str) -> (str, int): return (path, len(self.virtuals[path]) if (self.virtuals[path]) else 0) if path in self.virtuals else self.base.fileInfo(path)
    # def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: return BinaryReader(self.virtuals[path] or io.BytesIO()) if path in self.virtuals else self.base.openReader(path)
# end::StandardFileSystem[]

# tag::HostFileSystem[]
class HostFileSystem(IFileSystem):
    def __init__(self, uri: str): self.uri = uri
    def glob(self, path: str, searchPattern: str) -> list[str]: raise NotImplementedError()
    def fileExists(self, path: str) -> bool: raise NotImplementedError()
    def fileInfo(self, path: str) -> (str, int): raise NotImplementedError()
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: raise NotImplementedError()
# end::HostFileSystem[]

# tag::IsoFileSystem[]
class IsoFileSystem(IFileSystem):
    def __init__(self, root: str, path: str): self.pak = ZipFile(root); self.root = '' if not path else f'{path}/'
    def glob(self, path: str, searchPattern: str) -> list[str]: root = os.path.join(self.root, path); skip = len(root); return []
    def fileExists(self, path: str) -> bool: return self.pak.read(path) != None
    def fileInfo(self, path: str) -> (str, int): x = self.pak.read(path); return (x.name, x.length) if x else (None, 0)
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: return BinaryReader(self.pak.read(path))
# end::IsoFileSystem[]

# tag::StandardFileSystem[]
class StandardFileSystem(IFileSystem):
    def __init__(self, root: str): self.root = root; self.skip = len(root) + 1
    def glob(self, path: str, searchPattern: str) -> list[str]:
        g = pathlib.Path(os.path.join(self.root, path)).glob(searchPattern if searchPattern else '**/*')
        return [str(x)[self.skip:] for x in g if x.is_file()]
    def fileExists(self, path: str) -> bool: return os.path.exists(os.path.join(self.root, path))
    def fileInfo(self, path: str) -> (str, int): return (path, os.stat(path).st_size) if os.path.exists(path := os.path.join(self.root, path)) else (None, 0)
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: return BinaryReader(open(os.path.join(self.root, path), mode))
# end::StandardFileSystem[]

# tag::VirtualFileSystem[]
class VirtualFileSystem(IFileSystem):
    def __init__(self, virtuals: dict[str, object]): self.virtuals = virtuals
    def glob(self, path: str, searchPattern: str) -> list[str]:
        matcher = PlatformX.createMatcher(searchPattern)
        return [x for x in self.virtuals.keys() if matcher(x)]
    def fileExists(self, path: str) -> bool: return path in self.virtuals
    def fileInfo(self, path: str) -> (str, int): return (path, len(self.virtuals[path]) if (self.virtuals[path]) else 0) if path in self.virtuals else (None, 0)
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: return BinaryReader(self.virtuals[path] or io.BytesIO()) if path in self.virtuals else None
# end::StandardFileSystem[]

# tag::ZipFileSystem[]
class ZipFileSystem(IFileSystem):
    def __init__(self, root: str, path: str): self.zip = ZipFile(root); self.root = '' if not path else f'{path}/'
    def glob(self, path: str, searchPattern: str) -> list[str]: root = os.path.join(self.root, path); skip = len(root); return []
    def fileExists(self, path: str) -> bool: return self.zip.read(os.path.join(self.root, path)) != None
    def fileInfo(self, path: str) -> (str, int): x = self.zip.read(os.path.join(self.root, path)); return (x.name, x.length) if x else (None, 0)
    def openReader(self, path: str, mode: str = 'rb') -> BinaryReader: return BinaryReader(self.zip.read(os.path.join(self.root, path)))

#endregion