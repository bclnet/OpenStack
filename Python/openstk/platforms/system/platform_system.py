from __future__ import annotations
import os, io, pathlib
from openstk.core import ISource, BinaryReader, PlatformX
from openstk.sfx import IOpenSfx2, AudioBuilderBase, AudioManager

#region Platform

# SystemAudioBuilder
class SystemAudioBuilder(AudioBuilderBase):
    def createAudio(self, path: object) -> object: raise NotImplementedError()
    def deleteAudio(self, audio: object) -> None: raise NotImplementedError()

# SystemSfx
class SystemSfx(IOpenSfx2):
    def __init__(self, source: ISource):
        self.source: ISource = source
        self.audioManager: AudioManager = AudioManager(source, SystemAudioBuilder())
    def createAudio(self, path: object) -> int: return self.audioManager.createAudio(path)[0]

#endregion
