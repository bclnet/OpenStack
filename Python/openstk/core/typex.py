from __future__ import annotations
import os, functools

#region type helper

# def stripAssemblyVersion(name: str) -> str:
#     commaIndex = 0
#     while (commaIndex := name.find(',', commaIndex)) != -1:
#         if commaIndex + 1 < len(name) and name[commaIndex + 1] == '[': commaIndex+=1
#         else:
#             closeBracket = name.find(']', commaIndex)
#             if closeBracket != -1: name = name[:commaIndex] + name[closeBracket:]
#             else: name = name[:commaIndex]
#     return name

def splitGenericName(name: str) -> tuple[str, list[str]]:
    # look for the < generic marker character.
    pos = name.find('<')
    if pos == -1: return (None, None)
    # everything to the left of < is the generic type name.
    genericName = name[:pos]; args = []
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
    return (genericName, args)

def splitGenericTypeName(name: str) -> tuple[str, list[str]]:
    # look for the ` generic marker character.
    pos = name.find('`')
    if pos == -1: return (None, None)
    # everything to the left of ` is the generic type name.
    genericName = name[:pos]; args = []
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
    return (genericName, args)

def mapType(parent: type, name: str) -> str:
    if '<' in name:
        genericName, args = splitGenericName(name)
        # print(f'{name}: {genericName}, {genericArguments}')
        args = [f'[{mapType(parent, x)}]' for x in args]
        suffix = f'`{len(args)}[{','.join(args)}]'
        match genericName:
            case 'Enum': return 'Enum' + suffix
            case 'Nullable': return 'Nullable' + suffix
            case 'Array': return 'Array' + suffix
            case 'List': return 'Collections.Generic.List' + suffix
            case 'Dictionary': return 'Collections.Generic.Dictionary' + suffix
            case _: raise Exception('Unknown generic: {genericName}')
    if name.endswith('?'): return mapType(parent, f'Nullable<{name[:-1]}>')
    match name:
        case 'byte': name = 'Byte'
        case 'sbyte': name = 'SByte'
        case 'short': name = 'Int16'
        case 'ushort': name = 'UInt16'
        case 'int': name = 'Int32'
        case 'uint': name = 'UInt32'
        case 'int': name = 'Int64'
        case 'uint': name = 'UInt64'
        case 'float': name = 'Single'
        case 'double': name = 'Double'
        case 'bool': name = 'Boolean'
        case 'char': name = 'Char'
        case 'string': name = 'String'
        case 'object': name = 'Object'
        # case 'TimeSpan': name = 'TimeSpan'
        # case 'DateTime': name = 'DateTime'
    if name.startswith('#'):
        name = name[1:]
        baseNames = parent.name.split('.')
        while len(baseNames) > 1:
            baseNames.pop()
            wanted = f'{'.'.join(baseNames)}.{name}'
            v = getTypeByName(wanted)
            if v: return wanted
    return name

@staticmethod
def getType(type: str) -> type:
    if '<' in name:
        genericName, args = splitGenericName(name)

#endregion

#region Types

class Assembly:
    def __init__(self):
        pass
    @staticmethod
    def getAssembly(type: type) -> Assembly: return None
    @staticmethod
    def getRType(type: str) -> type: return None

    @staticmethod
    def getType(type: str, assemblyResolver: callback, typeResolver: callback) -> type:
        print('HERE')


# allTypes: dict[object, dict[str, type]] = {}
# class type:
#     def __init__(self, func: obj, name: str):
#         m = func.__module__
#         if not name: name = f'{m[m.index('formats.')+8:]}.{func.__qualname__}'
#         if m not in allTypes: allTypes[m] = {}
#         allTypes[m][name] = self
#         self.func = func
#         self.name = name
#         self.members = func._fields_ if hasattr(func, '_fields_') else None
#     def new(self) -> object: return self.func()

class Attribute:
    def __init__(self, name: str): self.name = name
    def __getitem__(self, key: str): return None
    @staticmethod
    def getCustomAttribute(member: MemberInfo, name: str) -> Attribute: return next(iter([x for x in member.customAttributes if x.name == name]), None)

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

def RAssembly(name: str = None):
    def clscall(cls):
        def funccall(func):
            def wrapper(*args, **kwargs): print(f'wrapper: {func.__name__} for {cls.__name__}'); return func(*args, **kwargs)
            return wrapper
        return cls
    return clscall

def RType(name: str = None):
    def clscall(cls):
        cls._name_ = name
        if hasattr(cls, '_fields_'):
            properties, fields = [], []
            for s in cls._fields_:
                customAttributes = []
                #Reflect.mapType(cls, s[1])
                name = s[0]; typex = s[1]; defaultValue = s[2] if len(s) > 2 else None
                if name.startswith('['): idx = name.find(']'); customAttributes = [Attribute(x.strip()) for x in name[1:idx].split(',')]; name = name[idx + 2:].strip()
                if name.startswith('#'): properties.append(PropertyInfo(name[1:], typex, defaultValue, customAttributes))
                else: fields.append(FieldInfo(name, typex, defaultValue, customAttributes))
            del cls._fields_
            cls._properties_ = properties
            cls._fields_ = fields
        def funccall(func):
            def wrapper(*args, **kwargs): print(f'wrapper: {func.__name__} for {cls.__name__}'); return func(*args, **kwargs)
            return wrapper
        return cls
    return clscall

# AssemblyTag
class AssemblyTag:
    literalTypes: dict[str, dict[str, type]]
    rtypes: dict[str, type]

# TypeX
class TypeX:
    @staticmethod
    def getRType(type: str) -> type:
        print('HERE')
        exit(0)

    #region Indirect

    assemblys: dict[str, Assembly] = {}
    assemblyRedirects: dict[str, Assembly] = {}
    tags: dict[Assembly, AssemblyTag] = {}

    @staticmethod
    def scanTypes(type: type) -> None:
        pass

    #endregion

    #region Helpers

    @staticmethod
    def isClass(type: type) -> bool: return type.isClass

    @staticmethod
    def baseType(type: type) -> None: return next(iter(type.__bases__), None) if hasattr(type, '__bases__') else None

    @staticmethod
    def getDefaultConstructor(type: type) -> callable: return type

    @staticmethod
    def getAllProperties(type: type) -> list: return type.properties

    @staticmethod
    def getAllFields(type: type) -> list: return type.fields

    #endregion

    