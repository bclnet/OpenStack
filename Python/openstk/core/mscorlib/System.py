from __future__ import annotations
import sys

#region Types

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

class Vector2: valueType: bool = True
class Vector3: valueType: bool = True
class Vector4: valueType: bool = True
class Matrix4x4: valueType: bool = True
class Quaternion: valueType: bool = True

class IDisposable:
    def dispose(self) -> None: pass

class Attribute:
    def __init__(self, type: type, kind: str, name: str): self.type = type; self.kind = kind; self.name = name
    def __getitem__(self, key: str): return None
    @staticmethod
    def getCustomAttribute(member: MemberInfo, name: str) -> Attribute: return next(iter([s for s in member.customAttributes if s.name == name]), None)

class Collections:
    class Generic:
        class List: valueType: bool = False
        class Dictionary: valueType: bool = False

class Reflection:
    class AssemblyName:
        def __init__(self, fullName):
            self.name = fullName
            self.fullName = fullName
        @staticmethod
        def parse(name: str) -> tuple[AssemblyName, str]: idx = name.find(','); return (Reflection.AssemblyName(name[idx+1:].strip()), name[:idx]) if idx != -1 else (None, name)

    class Assembly:
        @staticmethod
        def load(assemblyString: str) -> module:
            m = modulex(sys.modules[assemblyString])
            return m
        @staticmethod
        def getAssembly(type: type) -> module:
            m = modulex(sys.modules[type.__module__])
            return m

    class MemberInfo:
        def __init__(self, name: str, customAttributes: list[Attribute]):
            self.name = name
            self.customAttributes = customAttributes
        def init(self) -> None: pass

    class PropertyInfo(MemberInfo):
        def __init__(self, name: str, typeFunc: callable, defaultValue: object, customAttributes: list[Attribute]):
            super().__init__(name, customAttributes)
            self.typeFunc = typeFunc; self.propertyType = None
            self.defaultValue = defaultValue
            self.canWrite = True
            self.canRead = True
        def init(self) -> None: self.propertyType = self.typeFunc()
        def getValue(self, obj: object) -> object: return getAttr(obj, f'_{self.name}', self.defaultValue)
        def setValue(self, obj: object, value: object) -> None: setattr(obj, f'_{self.name}', value)
        def getIndexParameters(self) -> list[object]: return None

    class FieldInfo(MemberInfo):
        def __init__(self, name: str, typeFunc: callable, defaultValue: object, customAttributes: list[Attribute]):
            super().__init__(name, customAttributes)
            self.typeFunc = typeFunc; self.fieldType = None
            self.defaultValue = defaultValue
            self.isPublic = True
            self.isInitOnly = False
        def init(self) -> None: self.fieldType = self.typeFunc()
        def getValue(self, obj: object) -> object: return getAttr(obj, self.name, self.defaultValue)
        def setValue(self, obj: object, value: object) -> None: setattr(obj, self.name, value)

#endregion

#region Internal

def _rtype(name: str) -> str:
    match name:
        case 'Enum': return Enum
        case 'Nullable': return Nullable
        case 'Array': return Array
        case 'List': return Collections.Generic.List
        case 'Dictionary': return Collections.Generic.Dictionary
        case 'byte': return Byte
        case 'sbyte': return SByte
        case 'short': return Int16
        case 'ushort': return UInt16
        case 'int': return Int32
        case 'uint': return UInt32
        case 'int': return Int64
        case 'uint': return UInt64
        case 'float': return Single
        case 'double': return Double
        case 'bool': return Boolean
        case 'char': return Char
        case 'string': return String
        case 'object': return Object
        case 'TimeSpan': return TimeSpan
        case 'DateTime': return DateTime
        case _: return None

def _splitRTypeName(name: str) -> tuple[str, list[str]]:
    # look for the < generic marker character.
    pos = name.find('<')
    if pos == -1: return (name, None)
    # everything to the left of < is the generic type name.
    newName = name[:pos]; args = []
    # advance to the start of the generic argument list.
    pos+=1
    # split up the list of generic type arguments.
    while pos < len(name) and name[pos] != '>':
        # locate the end of the current type name argument.
        nesting = 0; end = 0
        for end in range(pos, len(name)):
            # handle nested types in case we have eg. "List<List<Int>>".
            if name[end] == '<': nesting+=1
            elif name[end] == '>':
                if nesting > 0: nesting-=1
                else: break
            elif nesting == 0 and name[end] == ',': break
        # if pos == end: break
        # extract the type name argument.
        args.append(name[pos:end].strip())
        # skip past the type name, plus any subsequent "," goo.
        pos = end
        if pos < len(name) and name[pos] == ',': pos+=1
    return (newName, args)

def _splitTypeName(name: str) -> tuple[str, list[str]]:
    # look for the ` generic marker character.
    pos = name.find('`')
    if pos == -1: return (name, None)
    # everything to the left of ` is the generic type name.
    newName = name[:pos]; args = []
    # advance to the start of the generic argument list.
    pos+=1
    while pos < len(name) and name[pos].isdigit(): pos+=1
    while pos < len(name) and name[pos] == '[': pos+=1
    # split up the list of generic type arguments.
    while pos < len(name) and name[pos] != ']':
        # locate the end of the current type name argument.
        nesting = 0; end = 0
        for end in range(pos, len(name)):
            # handle nested types in case we have eg. "List`1[[List`1[[Int]]]]".
            if name[end] == '[': nesting+=1
            elif name[end] == ']':
                if nesting > 0: nesting-=1
                else: break
        # extract the type name argument.
        args.append(name[pos:end])
        # skip past the type name, plus any subsequent "],[" goo.
        pos = end
        if pos < len(name) and name[pos] == ']': pos+=1
        if pos < len(name) and name[pos] == ',': pos+=1
        if pos < len(name) and name[pos] == '[': pos+=1
    return (newName, args)

#endregion

class Type:
    @staticmethod
    def invoke(obj: type, parameters: list[object]) -> bool: return obj(obj) if not parameters else _throw('Not Implemented')
    @staticmethod
    def isArray(obj: type) -> bool: return False #TODO
    @staticmethod
    def isClass(obj: type) -> bool: return True #TODO
    @staticmethod
    def baseType(obj: type) -> type: return next(iter(obj.__bases__), None) if hasattr(obj, '__bases__') else None
    @staticmethod
    def getType(typeName: str, assemblyResolver: callback, typeResolver: callback, throwOnError: bool = True, rtype: bool = False, obj: object = None) -> type:
        name, args = _splitRTypeName(typeName) if rtype else _splitTypeName(typeName)
        assemblyName, name = Reflection.AssemblyName.parse(name)
        type = None
        if rtype:
            if name.endswith('?'): return getType(f'Nullable<{name[:-1]}>', assemblyResolver, typeResolver, throwOnError, rtype, obj)
            elif name.startswith('^'):
                name = name[1:]
                name = f'{obj.__qualname__}.{name}'
            elif name.startswith('#'):
                name = name[1:]
                scan = obj.__qualname__.split('.')
                print(f'#: {name} - {scan}')
                exit(1)
                # while len(scan) > 1:
                #     scan.pop()
                #     newName = f'{'.'.join(scan)}.{name}'
                #     raise Exception(f'here: {newName}')
                #     # v = getTypeByName(newName)
                #     # if v: return newName
            else: type = _rtype(name)
        if not type: type = typeResolver(assemblyResolver(assemblyName) if assemblyName else None, name, throwOnError)
        if args: type.args = [Type.getType(s, assemblyResolver, typeResolver, throwOnError, rtype, obj) for s in args]; return typex(type, type.args)
        return type

def hasattrx(obj: type, name: str) -> bool: return hasattr(object, name)
    # for s in name.split('.'): if obj = getattr(obj, s) return False
    # return True

def getattrx(obj: type, name: str) -> type: return getattr(obj, s)
    # for s in name.split('.'): obj = getattr(obj, s)
    # return obj

def modulex(obj: module | type | str) -> module:
    if not obj: return obj
    if isinstance(obj, type): obj = sys.modules[obj.__module__]
    elif isinstance(obj, str): obj = sys.modules[obj]
    if hasattr(obj, '_x'): return obj
    def getType(typeName: str, throwOnError: bool = False) -> type:
        nonlocal obj
        if typeName.startswith('System.'): typeName = typeName[7:]
        type = getattr(obj, typeName)
        if not type and throwOnError: raise Exception(f'type {typeName} not found.')
        return type
    obj.getType = getType
    obj._x = True
    return obj

typexCache = {}
def typex(obj: type, args: object = None) -> type:
    if not obj or hasattr(obj, '_x'): return obj
    if isinstance(args, list):
        name = f'{obj.__qualname__}`{len(args)}[[{'],['.join([s.__qualname__ for s in args])}]]'
        if name in typexCache: return typexCache[name]
        obj = typexCache[name] = type(name, (obj, ), {})
        obj.args = args
    obj._x = True
    return obj

def attributex(obj: type, kind: str, name: str, action: callable) -> type:
    module = modulex(obj)
    if not hasattr(module, '_attribs_'): module._attribs_ = []
    self = Attribute(obj, kind, name); module._attribs_.append(self)
    if kind == 'RType':
        if hasattr(obj, '_fields_'):
            inits, properties, fields = [], [], []
            for s in obj._fields_:
                customAttributes = []
                name = s[0]; typeFunc = lambda: action(module, s[1], rtype=True, obj=obj); defaultValue = s[2] if len(s) > 2 else None
                if name.startswith('['): idx = name.find(']'); customAttributes = [Attribute(None, 'Field', x.strip()) for x in name[1:idx].split(',')]; name = name[idx + 2:].strip()
                if name.startswith('#'): x = Reflection.PropertyInfo(name[1:], typeFunc, defaultValue, customAttributes); inits.append(x); properties.append(x)
                else: x = Reflection.FieldInfo(name, typeFunc, defaultValue, customAttributes); inits.append(x); fields.append(x)
            del obj._fields_
            # obj.valueType = valueType
            obj._inits = inits
            obj._properties = properties
            obj._fields = fields
    elif action: action(self, obj)

def newx(obj: type) -> object: return obj()

def memberx(obj: type) -> type:
    if hasattr(obj, '_inits'):
        for s in obj._inits: s.init()
        del obj._inits
    return obj