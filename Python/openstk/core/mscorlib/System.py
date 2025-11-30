class Enum: valueType: bool = True
class Nullable: valueType: bool = True
class Array: valueType: bool = False
class Byte: valueType: bool = True
class SByte: valueType: bool = True
class Int16: valueType: bool = True
class UInt16: valueType: bool = True
class Int32: valueType: bool = True
class UInt32: valueType: bool = True
class Int64: valueType: bool = True
class UInt64: valueType: bool = True
class Single: valueType: bool = True
class Double: valueType: bool = True
class Boolean: valueType: bool = True
class Char: pasvalueType: bool = True
class String: valueType: bool = False
class Object: valueType: bool = False
class TimeSpan: valueType: bool = True
class DateTime: valueType: bool = True

class Collections:
    class Generic:
        class List: valueType: bool = False
        class Dictionary: valueType: bool = False