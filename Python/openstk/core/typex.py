from __future__ import annotations
import sys, os, functools, itertools
from .mscorlib.System import Byte

#region Internal

def _rtypeName(name: str) -> str:
    match name:
        case 'Enum': return 'System.Enum, mscorlib'
        case 'Nullable': return 'System.Nullable, mscorlib'
        case 'Array': return 'System.Array, mscorlib'
        case 'List': return 'System.Collections.Generic.List, mscorlib'
        case 'Dictionary': return 'System.Collections.Generic.Dictionary, mscorlib'
        case 'byte': return 'System.Byte, mscorlib'
        case 'sbyte': return 'System.SByte, mscorlib'
        case 'short': return 'System.Int16, mscorlib'
        case 'ushort': return 'System.UInt16, mscorlib'
        case 'int': return 'System.Int3, mscorlib'
        case 'uint': return 'System.UInt32, mscorlib'
        case 'int': return 'System.Int64, mscorlib'
        case 'uint': return 'System.UInt64, mscorlib'
        case 'float': return 'System.Single, mscorlib'
        case 'double': return 'System.Double, mscorlib'
        case 'bool': return 'System.Boolean, mscorlib'
        case 'char': return 'System.Char, mscorlib'
        case 'string': return 'System.String, mscorlib'
        case 'object': return 'System.Object, mscorlib'
        case 'TimeSpan': return 'System.TimeSpan, mscorlib'
        case 'DateTime': return 'System.DateTime, mscorlib'
        case _: return name

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

#region Types

def modulex(source: module | type | str) -> module:
    if not source: return source
    if isinstance(source, type): source = sys.modules[source.__module__]
    elif isinstance(source, str): source = sys.modules[source]
    if hasattr(source, '_x'): return source
    def getType(typeName: str, throwOnError: bool = False) -> type:
        nonlocal source
        if typeName.startswith('System.'): typeName = typeName[7:]
        type = getattr(source, typeName)
        if not type and throwOnError: raise Exception(f'type {typeName} not found.')
        return type
    source.getType = getType
    source._x = True
    return source

def typex(source: type, t: type = None) -> type:
    if not source or hasattr(source, '_x'): return source
    if t and hasattr(t, 'args'):
        source.args = t.args
        return source
    source._x = True
    return source

class IDisposable:
    def dispose(self) -> None: pass

class AssemblyName:
    def __init__(self, fullName):
        self.name = fullName
        self.fullName = fullName
    @staticmethod
    def parse(name: str) -> tuple[AssemblyName, str]: idx = name.find(','); return (AssemblyName(name[idx+1:].strip()), name[:idx]) if idx != -1 else (None, name)

class Assembly:
    @staticmethod
    def load(assemblyString: str) -> module:
        m = modulex(sys.modules[assemblyString])
        return m
    @staticmethod
    def getAssembly(type: type) -> module:
        m = modulex(sys.modules[type.__module__])
        return m

class Type:
    @staticmethod
    def invoke(source: type, parameters: list[object]) -> bool: return source(source) if not parameters else _throw('Not Implemented')
    @staticmethod
    def isArray(source: type) -> bool: return False #TODO
    @staticmethod
    def isClass(source: type) -> bool: return True #TODO
    @staticmethod
    def baseType(source: type) -> type: return next(iter(source.__bases__), None) if hasattr(source, '__bases__') else None
    @staticmethod
    def getType(typeName: str, assemblyResolver: callback, typeResolver: callback, throwOnError: bool = True, rtype: bool = False) -> type:
        name, args = _splitRTypeName(typeName) if rtype else _splitTypeName(typeName)
        if rtype:
            name = _rtypeName(name)
            print(f'rtype: {name}')
            exit(0)
            # if name.endswith('?'): return _getType(f'Nullable<{name[:-1]}>', source, rtype)
            if name.startswith('#'):
                raise Exception(f'here: {name}')
                # name = name[1:]
                # scan = source.name.split('.')
                # while len(scan) > 1:
                #     scan.pop()
                #     newName = f'{'.'.join(scan)}.{name}'
                #     raise Exception(f'here: {newName}')
                #     # v = getTypeByName(newName)
                #     # if v: return newName
        assemblyName, name = AssemblyName.parse(name)
        type = typeResolver(assemblyResolver(assemblyName) if assemblyName else None, name, throwOnError)
        if args: type.args = [Type.getType(s, assemblyResolver, typeResolver, throwOnError) for s in args]
        return type

class Attribute:
    def __init__(self, type: type, kind: str, name: str): self.type = type; self.kind = kind; self.name = name
    def __getitem__(self, key: str): return None
    @staticmethod
    def getCustomAttribute(member: MemberInfo, name: str) -> Attribute: return next(iter([s for s in member.customAttributes if s.name == name]), None)

class MemberInfo:
    def __init__(self, name: str, customAttributes: list[Attribute]):
        self.name = name
        self.customAttributes = customAttributes

class PropertyInfo(MemberInfo):
    def __init__(self, name: str, type: str, defaultValue: object, customAttributes: list[Attribute]):
        super().__init__(name, customAttributes)
        self.propertyType = type
        self.defaultValue = defaultValue
        self.canWrite = True
        self.canRead = True
    def getValue(self, obj: object) -> object: return getAttr(obj, f'_{self.name}', self.defaultValue)
    def setValue(self, obj: object, value: object) -> None: setattr(obj, f'_{self.name}', value)
    def getIndexParameters(self) -> list[object]: return None

class FieldInfo(MemberInfo):
    def __init__(self, name: str, type: str, defaultValue: object, customAttributes: list[Attribute]):
        super().__init__(name, customAttributes)
        self.fieldType = type
        self.defaultValue = defaultValue
        self.isPublic = True
        self.isInitOnly = False
    def getValue(self, obj: object) -> object: return getAttr(obj, self.name, self.defaultValue)
    def setValue(self, obj: object, value: object) -> None: setattr(obj, self.name, value)

#endregion

#region TypeX

def RAssembly(name: str = None):
    def clscall(cls):
        module = sys.modules[cls.__module__]
        if not hasattr(module, '_attribs_'): module._attribs_ = []
        nonlocal name
        a = Attribute(cls, 'RAssembly', name)
        a.ltypes = cls.ltypes if hasattr(cls, 'ltypes') else None
        module._attribs_.append(a)
        def funccall(func):
            def wrapper(*args, **kwargs): print(f'wrapper: {func.__name__} for {cls.__name__}'); return func(*args, **kwargs)
            return wrapper
        return cls
    return clscall

def RType(name: str = None, valueType: bool = False):
    def clscall(cls):
        module = sys.modules[cls.__module__]
        if not hasattr(module, '_attribs_'): module._attribs_ = []
        nonlocal name
        a = Attribute(cls, 'RType', f'{cls.__module__[cls.__module__.index('formats.')+8:]}.{cls.__qualname__.replace('+' if name != '+' else '\00', '.')}' if name == None or name == '+' else name)
        if hasattr(cls, '_fields_'):
            properties, fields = [], []
            for s in cls._fields_:
                customAttributes = []
                # type = TypeX.getRType(module, s[1], rtype=True)
                name = s[0]; type = s[1]; defaultValue = s[2] if len(s) > 2 else None
                if name.startswith('['): idx = name.find(']'); customAttributes = [Attribute(None, 'Field', x.strip()) for x in name[1:idx].split(',')]; name = name[idx + 2:].strip()
                if name.startswith('#'): properties.append(PropertyInfo(name[1:], type, defaultValue, customAttributes))
                else: fields.append(FieldInfo(name, type, defaultValue, customAttributes))
            del cls._fields_
            cls.valueType = valueType
            cls.properties = properties
            cls.fields = fields
        module._attribs_.append(a)
        def funccall(func):
            def wrapper(*args, **kwargs): print(f'wrapper: {func.__name__} for {cls.__name__}'); return func(*args, **kwargs)
            return wrapper
        return cls
    return clscall

# AssemblyTag
class AssemblyTag:
    def __init__(self, ltypes: dict[str, dict[str, type]], rtypes: dict[str, type]):
        self.ltypes: dict[str, dict[str, type]] = ltypes
        self.rtypes: dict[str, type] = rtypes

# TypeX
class TypeX:
    assemblys: dict[str, module] = { 'mscorlib': Assembly.getAssembly(Byte) }
    assemblyRedirects: dict[str, module] = {}
    tags: dict[str, AssemblyTag] = {}

    @staticmethod
    def getRType(source: module, typeName: str, throwOnError: bool = True, rtype: bool = False) -> type:
        # print(f'getRType{"R" if rtype else ""}: {source.__name__ if source else ""} - {typeName}')
        @staticmethod
        def _assemblyResolver(assembly):
            # print(f'_assemblyResolver: {assembly.name}')
            if assembly.name in TypeX.assemblys and (a := TypeX.assemblys[assembly.name]): return a
            a = TypeX.assemblys[assembly.name] = s if assembly.name in TypeX.assemblyRedirects and (s := TypeX.assemblyRedirects[assembly.name]) else Assembly.load(assembly.fullName)
            return a
        @staticmethod
        def _typeResolver(assembly, name, throwOnError):
            nonlocal source
            a = assembly or source
            # print(f'_typeResolver: {a.__name__} - {name}')
            if a.__name__ in TypeX.tags and (tag := TypeX.tags[a.__name__]):
                # r-types
                if name in tag.rtypes and (type := tag.rtypes[name]): return type
                # l-types
                idx = name.rfind('.')
                ns, na = (name[:idx], name[(idx + 1):]) if idx != -1 else (None, None)
                if ns and ns in tag.ltypes and (b := tag.ltypes[ns]) and na in b and (type := b[na]): return type
            return a.getType(name, throwOnError)
        return Type.getType(typeName, assemblyResolver=_assemblyResolver, typeResolver=_typeResolver, throwOnError=throwOnError, rtype=rtype)

    @staticmethod
    def scanTypes(types: list[type]) -> None:
        for type in types:
            if type.__module__ in TypeX.tags: continue
            assembly = Assembly.getAssembly(type)
            attribs = assembly._attribs_ if hasattr(assembly, '_attribs_') else []
            rassembiles = [(s.name, s.ltypes) for s in attribs if s.kind == 'RAssembly']
            rtypes = [(s.type, s.name) for s in attribs if s.kind == 'RType']
            assemblyRedirects = {s[0]:assembly for s in rassembiles if s[0]}
            if len(assemblyRedirects) > 0: TypeX.assemblyRedirects |= assemblyRedirects
            ltypes_ = {i:ss[i] for ss in [s[1] for s in rassembiles if s[1]] for i in ss} # ..GroupBy(s => s.Key).ToDictionary(s => s.Key, s => s.First().Value),
            # for k, g in itertools.groupby(sorted(ltypes_, key=lambda x: x.type), key=lambda x: x.type): v = next(g); readersByType[v.type] = v
            TypeX.tags[assembly.__name__] = AssemblyTag(
                ltypes=ltypes_,
                rtypes={s[1]:s[0] for s in rtypes})

    #endregion

    #region Helpers

    @staticmethod
    def getDefaultConstructor(type: type) -> callable: return type

    @staticmethod
    def getAllProperties(type: type) -> list: return type.properties if hasattr(type, 'properties') else []

    @staticmethod
    def getAllFields(type: type) -> list: return type.fields if hasattr(type, 'fields') else []

    #endregion
