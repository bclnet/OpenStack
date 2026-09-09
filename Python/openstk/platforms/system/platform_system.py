from __future__ import annotations
import os, io, pathlib
from openstk.core import ISource, BinaryReader, PlatformX
from openstk.sfx import IOpenSfx2, AudioBuilder, AudioManager

#region Platform

# SystemAudioBuilder
class SystemAudioBuilder(AudioBuilder):
    def create(self, path: object) -> object: raise NotImplementedError()
    def delete(self, audio: object) -> None: raise NotImplementedError()

# SystemSfx
class SystemSfx(IOpenSfx2):
    def __init__(self):
        self.audioManager: AudioManager = AudioManager(SystemAudioBuilder())
    def create(self, source: ISource, path: object) -> tuple[int, object]: return self.audioManager.create(source, path)

#endregion
