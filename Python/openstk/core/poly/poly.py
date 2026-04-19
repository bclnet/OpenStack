class Byte2:
    def __repr__(self): return f'{self.x},{self.y}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y = (0, 0)
            case 1: r = args[0]; self.x, self.y = r
            case 2: self.x, self.y = args
            case _: raise NotImplementedError('Byte2')
    def __eq__(self, other): return isinstance(other, Int2) and self.x == other.x and self.y == other.y
    def __hash__(self): return hash((self.x, self.y))

class Int2:
    def __repr__(self): return f'{self.x},{self.y}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y = (0, 0)
            case 1: r = args[0]; self.x, self.y = r
            case 2: self.x, self.y = args
            case _: raise NotImplementedError('Int2')
    def __eq__(self, other): return isinstance(other, Int2) and self.x == other.x and self.y == other.y
    def __hash__(self): return hash((self.x, self.y))

class Byte3:
    def __repr__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y, self.z = (0, 0, 0)
            case 1: r = args[0]; self.x, self.y, self.z = r
            case 3: self.x, self.y, self.z = args
            case _: raise NotImplementedError('Byte3')
    def __eq__(self, other): return isinstance(other, Byte3) and self.x == other.x and self.y == other.y and self.z == other.z
    def __hash__(self): return hash((self.x, self.y, self.z))

class Int3:
    def __repr__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y, self.z = (0, 0, 0)
            case 1: r = args[0]; self.x, self.y, self.z = r
            case 3: self.x, self.y, self.z = args
            case _: raise NotImplementedError('Int3')
    def __eq__(self, other): return isinstance(other, Int3) and self.x == other.x and self.y == other.y and self.z == other.z
    def __hash__(self): return hash((self.x, self.y, self.z))

class Float3:
    def __repr__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y, self.z = (0., 0., 0.)
            case 1: r = args[0]; self.x, self.y, self.z = r
            case 3: self.x, self.y, self.z = args
            case _: raise NotImplementedError('Float3')
    def __eq__(self, other): return isinstance(other, Float3) and self.x == other.x and self.y == other.y and self.z == other.z
    def __hash__(self): return hash((self.x, self.y, self.z))

