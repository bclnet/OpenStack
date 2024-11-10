import openstk.poly_unsafe as unsafe
from openstk.poly_find import findType
from openstk.poly_genericpool import IGenericPool, GenericPool, SinglePool, StaticPool
from openstk.poly_reader import Reader
from openstk.poly_writer import Writer
__all__ = ['unsafe', 'findType', 'IGenericPool', 'GenericPool', 'SinglePool', 'StaticPool', 'Reader', 'Writer']

# lumps
class X_LumpON:
    offset: int
    num: int

class X_LumpNO:
    num: int
    offset: int

class X_LumpNO2:
    num: int
    offset: int
    offset2: int

class X_Lump2NO:
    offset2: int
    num: int
    offset: int
