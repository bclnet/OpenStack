from __future__ import annotations
import os, sys
from enum import Enum
from openstk import ISource, decodePath, YamlDict
from openstk.platforms.platform_test import TestPlatform
from openstk.platforms.platform_unknown import UnknownPlatform

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
    def decodePath(path: str, rootPath: str = None) -> str: return decodePath(PlatformX.applicationPath, path, rootPath)

    platformOS: OS = OS.Windows if sys.platform == 'win32' else \
        OS.OSX if sys.platform == 'darwin' else \
        OS.Linux if sys.platform.startswith('linux') else \
        OS.Unknown
    platforms: set[object] = { UnknownPlatform.This }
    inTestHost: bool = 'unittest' in sys.modules.keys()
    applicationPath = os.getcwd()
    options = YamlDict('~/.gamex.yaml')
    current: Platform = None

#endregion

PlatformX.current = PlatformX.activate(TestPlatform.This if PlatformX.inTestHost else UnknownPlatform.This)
