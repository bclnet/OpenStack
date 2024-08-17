from openstk.poly_reader import Reader
from openstk.poly_writer import Writer
__all__ = ['Reader', 'Writer']

# root module
moduleRoot = 'gamex'

# finds a type
@staticmethod
def findType(klass):
    from importlib import import_module
    klass, modulePath = klass.rsplit(',', 1)
    try:
        _, className = klass.rsplit('.', 1)
        module = import_module(moduleName := f"{moduleRoot}.{modulePath.strip().replace('.', '_')}")
        return getattr(module, className)
    except (ImportError, AttributeError) as e:
        raise ImportError(klass)

# IDisposable
class IDisposable:
    def dispose(self): pass
