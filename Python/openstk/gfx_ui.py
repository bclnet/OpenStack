from enum import Enum
from typing import Set

# Key
class Key(Enum):
    ShiftLeft = 1
    A = 83
    D = 86
    F = 88
    Q = 99
    S = 101
    W = 105
    Z = 108

# MouseState
class MouseState:
    x: int = 0
    y: int = 0
    scrollWheelValue: int = 0
    leftButton: bool = False
    rightButton: bool = False
    def __str__(self): return f'({self.leftButton},{self.scrollWheelValue},{self.rightButton})'

# MouseState
class KeyboardState:
    keys: set[int] = set()
    def isKeyDown(self, key: Key) -> bool: return key in self.keys
    def __str__(self): return f'({self.keys})'
    
