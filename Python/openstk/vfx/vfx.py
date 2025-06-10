
from __future__ import annotations
import os, io, re, pathlib
from zipfile import ZipFile

#region FileSystem

# FileSystem
class FileSystem:
    def glob(self, path: str, searchPattern: str) -> list[str]: pass
    def fileExists(self, path: str) -> bool: pass
    def fileInfo(self, path: str) -> tuple[str, int]: pass
    def open(self, path: str, mode: str = None) -> object: pass
    def next(self) -> object: return self

    def findPaths(self, path: str, searchPattern: str) -> str:
        if (expandStartIdx := searchPattern.find('(')) != -1 and \
            (expandMidIdx := searchPattern.find(':', expandStartIdx)) != -1 and \
            (expandEndIdx := searchPattern.find(')', expandMidIdx)) != -1 and \
            expandStartIdx < expandEndIdx:
            for expand in searchPattern[expandStartIdx + 1: expandEndIdx].split(':'):
                for found in self.findPaths(path, searchPattern[:expandStartIdx] + expand + searchPattern[expandEndIdx+1:]): yield found
            return
        for path in self.glob(path, searchPattern): yield path

    def advance(self, basePath: str, path: str) -> object:
        match os.path.splitext(path)[1].lower():
            case '.zip': return ZipFileSystem(self, path, basePath)
            # case '.7z': return SevenZipFileSystem(self, path, basePath)
            # case '.iso' | '.bin' | '.cue': return DiscFileSystem(self, path, basePath)
            # case '.n64' | '.v64' | '.z64': return N64FileSystem(self, path, basePath)
            # case '.3ds': return X3dsFileSystem(self, path, basePath)
            case _: return None

    def next2(self, basePath: str, count: int, firstFunc: callable, elseFunc: callable) -> object:
        if count == 0: return self
        first = firstFunc(); firstLower = first.lower()
        if count == 1 or firstLower.endswith('.bin') or firstLower.endswith('.cue'): return self.advance(basePath, first) or self
        return elseFunc()

    @staticmethod
    def createMatcher(searchPattern: str) -> callable:
        if not searchPattern: return lambda x: True
        wildcardCount = searchPattern.count('*')
        if wildcardCount <= 0: return lambda x: x.casefold() == searchPattern.casefold()
        elif wildcardCount == 1:
            newPattern = searchPattern.replace('*', '')
            if searchPattern.startswith('*'): return lambda x: x.casefold().endswith(newPattern)
            elif searchPattern.endswith('*'): return lambda x: x.casefold().startswith(newPattern)
        regexPattern = f'^{re.escape(searchPattern).replace('\\*', '.*')}$'
        def lambdaX(x: str):
            try: return re.match(x, regexPattern)
            except: return False
        return lambdaX
    
# tag::AggregateFileSystem[]
class AggregateFileSystem(FileSystem):
    def __init__(self, aggreate: list[FileSystem]): self.aggreate = aggreate
    def glob(self, path: str, searchPattern: str) -> list[str]: return [y for z in [x.glob(path, searchPattern) for x in self.aggreate] for y in z]
    def fileExists(self, path: str) -> bool: return any(x.fileExists(path) for x in self.aggreate)
    def fileInfo(self, path: str) -> tuple[str, int]: return next(iter([x.fileInfo(path) for x in self.aggreate if x]))
    def open(self, path: str, mode: str = None) -> object: return next(iter([x.open(path, mode) for x in self.aggreate if x]))
# end::AggregateFileSystem[]

# tag::VirtualFileSystem[]
class VirtualFileSystem(FileSystem):
    def __init__(self, virtuals: dict[str, object]): self.virtuals = virtuals
    def glob(self, path: str, searchPattern: str) -> list[str]:
        matcher = FileSystem.createMatcher(searchPattern)
        return [x for x in self.virtuals.keys() if matcher(x)]
    def fileExists(self, path: str) -> bool: return path in self.virtuals
    def fileInfo(self, path: str) -> tuple[str, int]: return (path, len(self.virtuals[path]) if self.virtuals[path] else 0) if path in self.virtuals else (None, 0)
    def open(self, path: str, mode: str = None) -> object: return self.virtuals[path] or io.BytesIO() if path in self.virtuals else None
# end::VirtualFileSystem[]

# tag::DirectoryFileSystem[]
class DirectoryFileSystem(FileSystem):
    def __init__(self, baseRoot: str, basePath: str): self.baseRoot = baseRoot; self.basePath = basePath; self.root = baseRoot; self.skip = len(baseRoot) + 1
    def glob(self, path: str, searchPattern: str) -> list[str]:
        g = pathlib.Path(os.path.join(self.root, path)).glob(searchPattern if searchPattern else '**/*')
        return [str(x)[self.skip:] for x in g if x.is_file()]
    def fileExists(self, path: str) -> bool: return os.path.exists(os.path.join(self.root, path))
    def fileInfo(self, path: str) -> tuple[str, int]: return (path, os.stat(path).st_size) if os.path.exists(path := os.path.join(self.root, path)) else (None, 0)
    def open(self, path: str, mode: str = None) -> object: return open(os.path.join(self.root, path), mode or 'rb')
    def next(self) -> FileSystem:
        if os.path.isfile(self.root) or '*' in os.path.basename(self.root):
            self.root = os.path.dirname(self.root); self.skip = len(self.root) + 1
            return self #self.advance(self.basePath, ).next() or self
        @staticmethod
        def _lambdaX(self):
            if self.basePath: self.root = os.path.join(self.baseRoot, self.basePath); self.skip = len(self.root) + 1
            return self
        return self.next2(self.basePath, -1, lambda: next(iter(os.listdir(self.root)), None), lambda: _lambdaX(self))
# end::DirectoryFileSystem[]

#endregion

#region FileSystem : Network

# tag::NetworkFileSystem[]
class NetworkFileSystem(FileSystem):
    def __init__(self, uri: str): self.uri = uri
    def glob(self, path: str, searchPattern: str) -> list[str]: raise NotImplementedError()
    def fileExists(self, path: str) -> bool: raise NotImplementedError()
    def fileInfo(self, path: str) -> tuple[str, int]: raise NotImplementedError()
    def open(self, path: str, mode: str = None) -> object: raise NotImplementedError()
# end::NetworkFileSystem[]

#endregion

#region FileSystem : Archive

# tag::ZipFileSystem[]
class ZipFileSystem(FileSystem):
    def __init__(self, vfx: FileSystem, path: str, basePath: str): self.basePath = basePath; self.root = ''; self.zip = ZipFile(path)
    def glob(self, path: str, searchPattern: str) -> list[str]:
        root = os.path.join(self.root, path); skip = len(root); return []
    def fileExists(self, path: str) -> bool: return self.zip.read(os.path.join(self.root, path)) != None
    def fileInfo(self, path: str) -> tuple[str, int]: x = self.zip.read(os.path.join(self.root, path)); return (x.name, x.length) if x else (None, 0)
    def open(self, path: str, mode: str = None) -> object: return self.zip.read(os.path.join(self.root, path))
    def _lambdaX(self):
        if self.basePath: self.root = f'{path}/'
        return self
    def next(self) -> object: return self.next2(self.basePath, self.zip.count, lambda: self.zip.one, _lambdaX)
# end::ZipFileSystem[]

#endregion
