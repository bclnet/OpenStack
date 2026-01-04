class Int2:
    def __repl__(self): return f'{self.x},{self.y}'
    def __init__(self, *args):
        match len(args):
            case 1: r = args[0]; self.x: int = r[0]; self.y: int = r[1]
            case 2: self.x: int = args[0]; self.y: int = args[1]

class Byte3:
    def __repl__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 1: r = args[0]; self.x: int = r[0]; self.y: int = r[1]; self.z: int = r[2]
            case 3: self.x: int = args[0]; self.y: int = args[1]; self.z: int = args[2]

class Int3:
    def __repl__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 1: r = args[0]; self.x: int = r[0]; self.y: int = r[1]; self.z: int = r[2]
            case 3: self.x: int = args[0]; self.y: int = args[1]; self.z: int = args[2]

class Float3:
    def __repl__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x: float = 0; self.y: float = 0; self.z: float = 0
            case 1: r = args[0]; self.x: float = r[0]; self.y: float = r[1]; self.z: float = r[2]
            case 3: self.x: float = args[0]; self.y: float = args[1]; self.z: float = args[2]

