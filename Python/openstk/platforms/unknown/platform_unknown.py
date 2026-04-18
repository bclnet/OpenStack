from __future__ import annotations
import os, io, pathlib
from openstk.core.platform import Platform

#region Platform

# UnknownPlatform
class UnknownPlatform(Platform):
    def __init__(self): super().__init__('UK', 'Unknown')
UnknownPlatform.This = UnknownPlatform()

#endregion
