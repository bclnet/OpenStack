from __future__ import annotations
from openstk.core.platform import Platform

#region Platform

# UnknownPlatform
class UnknownPlatform(Platform):
    def __init__(self):
        super().__init__('UK', 'Unknown')
        self.gfxFactory = staticmethod(lambda source: [None, None, None, None, None, None])
        self.sfxFactory = staticmethod(lambda source: [None])
UnknownPlatform.This = UnknownPlatform()

#endregion
