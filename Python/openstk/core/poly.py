class Int2:
    def __repl__(self): return f'{self.x},{self.y}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y = (0, 0)
            case 1: r = args[0]; self.x, self.y = r
            case 2: self.x, self.y = args
            case _: raise NotImplementedError('Int2')

class Byte3:
    def __repl__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y, self.z = (0, 0, 0)
            case 1: r = args[0]; self.x, self.y, self.z = r
            case 3: self.x, self.y, self.z = args
            case _: raise NotImplementedError('Byte3')

class Int3:
    def __repl__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y, self.z = (0, 0, 0)
            case 1: r = args[0]; self.x, self.y, self.z = r
            case 3: self.x, self.y, self.z = args
            case _: raise NotImplementedError('Int3')

class Float3:
    def __repl__(self): return f'{self.x},{self.y},{self.z}'
    def __init__(self, *args):
        match len(args):
            case 0: self.x, self.y, self.z = (0., 0., 0.)
            case 1: r = args[0]; self.x, self.y, self.z = r
            case 3: self.x, self.y, self.z = args
            case _: raise NotImplementedError('Float3')

