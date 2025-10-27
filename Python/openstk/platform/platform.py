from __future__ import annotations
import os, sys
from enum import Enum
from openstk.poly import ISource

#region FileSystem

# IFileSystem
class IFileSystem:
    def glob(self, path: str, searchPattern: str) -> list[str]: pass
    def fileExists(self, path: str) -> bool: pass
    def fileInfo(self, path: str) -> (str, int): pass
    def openReader(self, path: str, mode: str = 'rb') -> Reader: pass
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

#endregion

#region Platform

# Platform
class Platform:
    enabled: bool = True
    id: str = None
    name: str = None
    tag: str = None
    gfxFactory: callable = None
    sfxFactory: callable = None
    logFunc: callable = lambda a: print(a)
    def __init__(self, id: str, name: str): self.id = id; self.name = name
    def activate(self) -> None: pass
    def deactivate(self) -> None: pass

# UnknownPlatform
class UnknownPlatform(Platform):
    def __init__(self): super().__init__('UK', 'Unknown')
UnknownPlatform.This = UnknownPlatform()

# PlatformX
class PlatformX:
    class OS(Enum):
        Unknown = 0
        Windows = 1
        OSX = 2
        Linux = 3
        Android = 4

    @staticmethod
    def activate(platform: Platform) -> None:
        if not platform or not platform.enabled: platform = UnknownPlatform.This
        PlatformX.platforms.add(platform)
        current = PlatformX.current
        if current != platform:
            if current: current.deactivate()
            if platform: platform.activate()
            PlatformX.current = platform
        return platform

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
        @staticmethod
        def _lambdax(x: str):
            try: return re.match(x, regexPattern)
            except: return False
        return _lambdax

    @staticmethod
    def decodeOptions(name: str) -> object:
        path = f'{os.getenv("APPDATA")}{name}'
        return None

    @staticmethod
    def decodePath(path: str, rootPath: str = None) -> str:
        lowerPath = path.lower()
        return f'{os.getenv("PROFILE")}{path[1:]}' if lowerPath.startswith('~') else \
        f'{rootPath}{path[6:]}' if lowerPath.startswith('%path%') else \
        f'{PlatformX.applicationPath}{path[9:]}' if lowerPath.startswith('%apppath%') else \
        f'{os.getenv("APPDATA")}{path[9:]}' if lowerPath.startswith('%appdata%') else \
        f'{os.getenv("LOCALAPPDATA")}{path[14:]}' if lowerPath.startswith('%localappdata%') else \
        path

    platformOS: OS = OS.Windows if sys.platform == 'win32' else \
        OS.OSX if sys.platform == 'darwin' else \
        OS.Linux if sys.platform.startswith('linux') else \
        OS.Unknown
    platforms: set[object] = { UnknownPlatform.This }
    inTestHost: bool = 'unittest' in sys.modules.keys()
    applicationPath = os.getcwd()
    options = decodeOptions('.gamex')
    current: Platform = None

#endregion

#region Test Platform

# TestGfxSprite
class TestGfxSprite:
    source: object
    def __init__(self, source): self.source = source
    def loadFileObject(self, type: type, path: object): raise NotImplementedError()
    def preloadSprite(self, path: object) -> None: raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()

# TestGfxModel
class TestGfxModel:
    source: object
    def __init__(self, source): self.source = source
    def loadFileObject(self, type: type, path: object): raise NotImplementedError()
    def preloadTexture(self, path: object) -> None: raise NotImplementedError()
    def preloadObject(self, path: object) -> None: raise NotImplementedError()

# TestSfx
class TestSfx:
    source: object
    def __init__(self, source): self.source = source

# TestPlatform
class TestPlatform(Platform):
    def __init__(self):
        super().__init__('TT', 'Test')
        self.gfxFactory = staticmethod(lambda source: [TestGfxSprite(source), TestGfxSprite(source), TestGfxModel(source)])
        self.sfxFactory = staticmethod(lambda source: [TestSfx(source)])
TestPlatform.This = TestPlatform()

#endregion

PlatformX.current = PlatformX.activate(TestPlatform.This if PlatformX.inTestHost else UnknownPlatform.This)
