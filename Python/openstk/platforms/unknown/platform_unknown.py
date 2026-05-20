from __future__ import annotations
from openstk.core.platform import Platform

#region Platform

# UnknownPlatform
class UnknownPlatform(Platform):
    # buildersByType: dict[type, callable] = {}
    def __init__(self):
        super().__init__('UK', 'Unknown')
        self.gfxFactory = staticmethod(lambda: [None, None, None, None, None, None])
        self.sfxFactory = staticmethod(lambda: [None])
UnknownPlatform.this = UnknownPlatform()

#endregion
