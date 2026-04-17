from __future__ import annotations
import itertools
from .mscorlib.System import Reflection, Type, attributex, newx, memberx

def RAssembly(name: str = None):
    def clscall(cls):
        nonlocal name
        def attribute_(self, obj): self.ltypes = obj.ltypes if hasattr(obj, 'ltypes') else None
        attributex(cls, 'RAssembly', name, attribute_)
        def funccall(func):
            def wrapper(*args, **kwargs): print(f'wrapper: {func.__name__} for {cls.__name__}'); return func(*args, **kwargs)
            return wrapper
        return cls
    return clscall

def RType(name: str = None, valueType: bool = False):
    def clscall(cls):
        nonlocal name
        attributex(cls, 'RType', f'{cls.__module__[cls.__module__.index('formats.')+8:]}.{cls.__qualname__.replace('+' if name != '+' else '\00', '.')}' if name == None or name == '+' else name, TypeX.getRType)
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
    assemblys: dict[str, module] = { 'mscorlib': Reflection.Assembly.getAssembly(Type) }
    assemblyRedirects: dict[str, module] = {}
    tags: dict[str, AssemblyTag] = {}

    @staticmethod
    def getRType(source: module, typeName: str, throwOnError: bool = True, rtype: bool = False, obj: object = None) -> type:
        # print(f'getRType{"R" if rtype else ""}: {source.__name__ if source else ""} - {typeName}')
        @staticmethod
        def _assemblyResolver(assembly):
            # print(f'_assemblyResolver: {assembly.name}')
            if assembly.name in TypeX.assemblys and (a := TypeX.assemblys[assembly.name]): return a
            a = TypeX.assemblys[assembly.name] = s if assembly.name in TypeX.assemblyRedirects and (s := TypeX.assemblyRedirects[assembly.name]) else Reflection.Assembly.load(assembly.fullName)
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
        return Type.getType(typeName, assemblyResolver=_assemblyResolver, typeResolver=_typeResolver, throwOnError=throwOnError, rtype=rtype, obj=obj)

    @staticmethod
    def scanTypes(types: list[type]) -> None:
        for type in types:
            if type.__module__ in TypeX.tags: continue
            assembly = Reflection.Assembly.getAssembly(type)
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

    @staticmethod
    def getDefaultConstructor(type: type) -> callable: return type #return lambda: newx(type)
    @staticmethod
    def getAllProperties(type: type) -> list: return memberx(type)._properties if hasattr(type, '_properties') else []
    @staticmethod
    def getAllFields(type: type) -> list: return memberx(type)._fields if hasattr(type, '_fields') else []
