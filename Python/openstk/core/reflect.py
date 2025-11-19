from __future__ import annotations
import os

class Attribute:
    def __init__(self, name: str):
        self.name = name
    def __getitem__(self, key: str):
        return None
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
    def getValue(self, obj: object) -> object: return getAttr(obj, self.name, self.defaultValue)
    def setValue(self, obj: object, value: object) -> None: setattr(obj, self.name, value)

# Reader
class Reflect:
    @staticmethod
    def stripAssemblyVersion(name: str) -> str:
        commaIndex = 0
        while (commaIndex := name.find(',', commaIndex)) != -1:
            if commaIndex + 1 < len(name) and name[commaIndex + 1] == '[': commaIndex+=1
            else:
                closeBracket = name.find(']', commaIndex)
                if closeBracket != -1: name = name[:commaIndex] + name[closeBracket:]
                else: name = name[:commaIndex]
        return name

    @staticmethod
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
                elif name[end] == ',':
                    if nesting > 0: nesting-=1
                    else: break
            # extract the type name argument.
            args.append(name[pos:end].strip())
            # skip past the type name, plus any subsequent "," goo.
            pos = end
            if pos < len(name) and name[pos] == ',': pos+=1
        return (genericName, args)

    @staticmethod
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

    @staticmethod
    def mapType(type: str) -> str:
        if '<' in type:
            genericName, genericArguments = Reflect.splitGenericName(type)
            genericArguments = [f'[{Reflect.mapType(x)}]' for x in genericArguments]
            suffix = f'`{len(genericArguments)}[{','.join(genericArguments)}]'
            match genericName:
                case 'Enum' | '#': return 'Enum' + suffix
                case 'Nullable': return 'Nullable' + suffix
                case 'Array': return 'Array' + suffix
                case 'List': return 'Collections.Generic.List' + suffix
                case 'Dictionary': return 'Collections.Generic.Dictionary' + suffix
                case _: raise Exception('Unknown generic: {genericName}')
        if type.endswith('?'): return Reflect.mapType(f'Nullable<{type[:-1]}>')
        match type:
            case 'byte': type = 'Byte'
            case 'sbyte': type = 'SByte'
            case 'short': type = 'Int16'
            case 'ushort': type = 'UInt16'
            case 'int': type = 'Int32'
            case 'uint': type = 'UInt32'
            case 'int': type = 'Int64'
            case 'uint': type = 'UInt64'
            case 'float': type = 'Single'
            case 'double': type = 'Double'
            case 'bool': type = 'Boolean'
            case 'char': type = 'Char'
            case 'string': type = 'String'
            case 'object': type = 'Object'
            # case 'TimeSpan': type = 'TimeSpan'
            # case 'DateTime': type = 'DateTime'
        return type

    @staticmethod
    def getAllPropertiesFields(cls: object) -> tuple[list, list]:
        properties, fields = [], []
        for s in cls._fields_:
            customAttributes = []
            name = s[0]; type = Reflect.mapType(s[1]); defaultValue = s[2] if len(s) > 2 else None
            if name.startswith('['): idx = name.find(']'); customAttributes = [Attribute(x.strip()) for x in name[1:idx].split(',')]; name = name[idx + 2:].strip()
            if name.startswith('#'): properties.append(PropertyInfo(name[1:], type, defaultValue, customAttributes))
            else: fields.append(FieldInfo(name, type, defaultValue, customAttributes))
        return (properties, fields)