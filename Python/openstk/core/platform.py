from __future__ import annotations
import os, sys
from enum import Enum, Flag
from openstk.core.core import ISource
from openstk.core.util import decodePath, YamlDict

#region Platform

# Platform
class Platform:
    def __init__(self, id: str, name: str):
        self.enabled: bool = True
        self.caps: PlatformX.Caps = PlatformX.Caps.None_
        self.id: str = id
        self.name: str = name
        self.tag: str = None
        self.gfxFactory: callable = None
        self.sfxFactory: callable = None
        self.logFunc: callable = lambda a: print(a)
    def activate(self) -> None: pass
    def deactivate(self) -> None: pass

# PlatformX
class PlatformX:
    # The platform Caps.
    class Caps(Flag):
        None_ = 0x0
        ReadDds = 0x1

    # The platform OS.
    class OS(Enum):
        Unknown = 0
        Windows = 1
        OSX = 2
        Linux = 3
        Android = 4

    @staticmethod
    def activate(platform: Platform) -> None:
        if not platform or not platform.enabled: platform = UnknownPlatform.this
        PlatformX.platforms.add(platform)
        current = PlatformX.current
        if current != platform:
            if current: current.deactivate()
            if platform: platform.activate()
            PlatformX.gfx = platform.gfxFactory() if platform and platform.gfxFactory else None
            PlatformX.sfx = platform.sfxFactory() if platform and platform.sfxFactory else None
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
    def decodePath(path: str, rootPath: str = None) -> str: return decodePath(PlatformX.applicationPath, path, rootPath)

    platformOS: OS = OS.Windows if sys.platform == 'win32' else \
        OS.OSX if sys.platform == 'darwin' else \
        OS.Linux if sys.platform.startswith('linux') else \
        OS.Unknown
    platforms: set[object] = {}
    inTestHost: bool = 'unittest' in sys.modules.keys()
    applicationPath = os.getcwd()
    options = YamlDict('~/.gamex.yaml')
    current: Platform = None
    gfx: list[IOpenGfx] = None
    sfx: list[IOpenSfx] = None

#endregion

from openstk.platforms.test import TestPlatform
from openstk.platforms.unknown import UnknownPlatform

PlatformX.platforms = { UnknownPlatform.this }
PlatformX.current = PlatformX.activate(TestPlatform.this if PlatformX.inTestHost else UnknownPlatform.this)
