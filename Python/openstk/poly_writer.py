import os, numpy as np
from struct import unpack
from io import BytesIO
from openstk.util import _throw

# Writer
class Writer:
    def __init__(self, f): self.f = f; self.__update()
    def __enter__(self): return self
    def __exit__(self, type, value, traceback): self.f.close()
    def __update(self):
        f = self.f
        pos = f.tell()
        self.length = f.seek(0, os.SEEK_END)
        f.seek(pos, os.SEEK_SET)

    # base
    def write(self, bytes: bytearray): return self.f.write(size)
    def length(self): return self.length
    def copyTo(self, destination: BytesIO, resetAfter: bool = False): raise NotImplementedError()
    # def writeLine(self, value) -> str: return self.f.writeline(value.decode('utf-8'))

    # primatives : normal
    def writeDouble(self, value: float): self.f.write(value.to_bytes(8, 'little'))
    def writeSByte(self, value: int): self.f.write(value.to_bytes(1, 'little', signed=True))
    def writeInt16(self, value: int): self.f.write(value.to_bytes(2, 'little', signed=True))
    def writeInt32(self, value: int): self.f.write(value.to_bytes(4, 'little', signed=True))
    def writeInt64(self, value: int): self.f.write(value.to_bytes(8, 'little', signed=True))
    def writeSingle(self, value: float): self.f.write(value.to_bytes(4, 'little'))
    def writeByte(self, value: int): self.f.write(value.to_bytes(1, 'little', signed=False))
    def writeUInt16(self, value: int): self.f.write(value.to_bytes(2, 'little', signed=False))
    def writeUInt32(self, value: int): self.f.write(value.to_bytes(4, 'little', signed=False))
    def writeUInt64(self, value: int): self.f.write(value.to_bytes(8, 'little', signed=False))

    # primatives : endian
    def writeDoubleE(self, value: float): self.f.write(value.to_bytes(8, 'big'))
    def writeInt16E(self, value: int): self.f.write(value.to_bytes(2, 'big', signed=True))
    def writeInt32E(self, value: int): self.f.write(value.to_bytes(4, 'big', signed=True))
    def writeInt64E(self, value: int): self.f.write(value.to_bytes(8, 'big', signed=True))
    def writeSingleE(self, value: float): self.f.write(value.to_bytes(4, 'big'))
    def writeUInt16E(self, value: int): self.f.write(value.to_bytes(2, 'big', signed=False))
    def writeUInt32E(self, value: int): self.f.write(value.to_bytes(4, 'big', signed=False))
    def writeUInt64E(self, value: int): self.f.write(value.to_bytes(8, 'big', signed=False))

    # primatives : endianX
    def writeDoubleX(self, value: float, endian: bool = True): self.f.write(value.to_bytes(8, 'big' if endian else 'little'))
    def writeInt16X(self, value: int, endian: bool = True): self.f.write(value.to_bytes(2, 'big' if endian else 'little', signed=True))
    def writeInt32X(self, value: int, endian: bool = True): self.f.write(value.to_bytes(4, 'big' if endian else 'little', signed=True))
    def writeInt64X(self, value: int, endian: bool = True): self.f.write(value.to_bytes(8, 'big' if endian else 'little', signed=True))
    def writeSingleX(self, value: float, endian: bool = True): self.f.write(value.to_bytes(4, 'big' if endian else 'little'))
    def writeUInt16X(self, value: int, endian: bool = True): self.f.write(value.to_bytes(2, 'big' if endian else 'little', signed=False))
    def writeUInt32X(self, value: int, endian: bool = True): self.f.write(value.to_bytes(4, 'big' if endian else 'little', signed=False))
    def writeUInt64X(self, value: int, endian: bool = True): self.f.write(value.to_bytes(8, 'big' if endian else 'little', signed=False))

    # primatives : specialized
    def writeBool32(self, value: bool): elf.f.write((1 if value else 0).to_bytes(4, 'little', signed=False))
    def writeGuid(self, value: bytearray): return self.f.write(value)
    def writeCInt32(self, value: int): raise NotImplementedError()


    # position
    def align(self, align: int = 4): align -= 1; self.f.seek((self.f.tell() + align) & ~align, os.SEEK_SET); return self
    def tell(self): return self.f.tell()
    def seek(self, offset: int): self.f.seek(offset, os.SEEK_SET); return self
    def seekAndAlign(self, offset: int, align: int = 4): self.f.seek(offset + align - (offset % align) if offset % align else offset, os.SEEK_SET); return self
    def skip(self, count: int): self.f.seek(count, os.SEEK_CUR); return self
    def skipAndAlign(self, count: int, align: int = 4): offset = self.f.tell() + count; self.f.seek(offset + align - (offset % align) if offset % align else offset, os.SEEK_CUR); return self
    def end(self, offset: int): self.f.seek(offset, os.SEEK_END); return self


    # string : chars
    def writeCString(self, value: str) -> None: pass
    def writeFString(self, value: str, length: int, zstring: bool = False) -> None: pass
    def writeZAString(self, value: str, length: int = 65535) -> None: pass
    # string : encoding
    # def writeL8Encoding(self, encoding: str = None): return self.f.write(int.from_bytes(self.f.write(1), 'little')).decode('ascii' if not encoding else encoding)
    # def writeL16Encoding(self, encoding: str = None): return self.f.write(int.from_bytes(self.f.write(2), 'little')).decode('ascii' if not encoding else encoding)
    # def writeL32Encoding(self, encoding: str = None): return self.f.write(int.from_bytes(self.f.write(4), 'little')).decode('ascii' if not encoding else encoding)

    
    # struct : single  - https://docs.python.org/3/library/struct.html 
    def writeF(self, cls: object, value: object, factory: callable) -> object: self.f.write(factory(self))
    def writeS(self, cls: object, value: object) -> object: pattern, size = cls._struct; cls(unpack(pattern, self.f.write(size)))
    def writeSAndVerify(self, cls: object, value: object, sizeOf: int) -> object: pattern, size = cls._struct; cls(unpack(pattern, self.f.write(size)))
    def writeT(self, cls: object, value: object, sizeOf: int) -> object: unpack(cls, self.f.write(sizeOf))[0]

    # struct : array - factory
    def writeL8FArray(self, cls: object, value: object, factory: callable, endian: bool = False) -> list[object]: return self.writeFArray(cls, factory, source.writeByte())
    def writeL16FArray(self, cls: object, value: object, factory: callable, endian: bool = False) -> list[object]: return self.writeFArray(cls, factory, source.writeUInt16E(endian))
    def writeL32FArray(self, cls: object, value: object, factory: callable, endian: bool = False) -> list[object]: return self.writeFArray(cls, factory, source.writeUInt32E(endian))
    def writeC32FArray(self, cls: object, value: object, factory: callable, endian: bool = False) -> list[object]: return self.writeFArray(cls, factory, source.writeCInt32E(endian))
    def writeFArray(self, cls: object, value: object, factory: callable, count: int) -> list[object]: return [self.writeF(cls, factory) for x in range(count)] if count else []

    # struct : array - struct
    def writeL8SArray(self, cls: object, value: object, endian: bool = False) -> list[object]: return self.writeSArray(cls, source.writeByte())
    def writeL16SArray(self, cls: object, value: object, endian: bool = False) -> list[object]: return self.writeSArray(cls, source.writeUInt16E(endian))
    def writeL32SArray(self, cls: object, value: object, endian: bool = False) -> list[object]: return self.writeSArray(cls, source.writeUInt32E(endian))
    def writeC32SArray(self, cls: object, value: object, endian: bool = False) -> list[object]: return self.writeSArray(cls, source.writeCInt32E(endian))
    def writeSArray(self, cls: object, value: object, count: int) -> list[object]: return [self.writeS(cls) for x in range(count)] if count else []

    # struct : array - type
    def writeL8TArray(self, cls: object, value: object, sizeOf: int, endian: bool = False) -> list[object]: return self.writeTArray(cls, sizeOf, source.writeByte())
    def writeL16TArray(self, cls: object, value: object, sizeOf: int, endian: bool = False) -> list[object]: return self.writeTArray(cls, sizeOf, source.writeUInt16(endian))
    def writeL32TArray(self, cls: object, value: object, sizeOf: int, endian: bool = False) -> list[object]: return self.writeTArray(cls, sizeOf, source.writeUInt32(endian))
    def writeC32TArray(self, cls: object, value: object, sizeOf: int, endian: bool = False) -> list[object]: return self.writeTArray(cls, sizeOf, source.writeCInt32(endian))
    def writeTArray(self, cls: object, value: object, sizeOf: int, count: int) -> list[object]: return [self.writeT(cls, sizeOf) for x in range(count)] if count else []

    # struct : each
    # def writeSEach(self, cls: object, value: object, count: int) -> list[object]: return [self.writeS(cls) for x in range(count)] if count else []
    # def writeTEach(self, cls: object, value: object, sizeOf: int, count: int) -> list[object]: return [self.writeT(cls, sizeOf) for x in range(count)] if count else []
    
    # struct : many - factory
    # def writeL8FMany(self, clsKey: object, value: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeFMany(clsKey, keyFactory, valueFactory, source.writeByte())
    # def writeL16FMany(self, clsKey: object, value: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeFMany(clsKey, keyFactory, valueFactory, source.writeUInt16(endian))
    # def writeL32FMany(self, clsKey: object, value: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeFMany(clsKey, keyFactory, valueFactory, source.writeUInt32(endian))
    # def writeC32FMany(self, clsKey: object, value: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeFMany(clsKey, keyFactory, valueFactory, source.writeCInt32(endian))
    # def writeFMany(self, clsKey: object, value: object, keyFactory: callable, valueFactory: callable) -> dict[object, object]: return {keyFactory(self):valueFactory(self) for x in range(count)} if count else {}

    # struct : many - struct
    # def writeL8SMany(self, clsKey: object, value: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeSMany(clsKey, valueFactory, source.writeByte())
    # def writeL16SMany(self, clsKey: object, value: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeSMany(clsKey, valueFactory, source.writeUInt16(endian))
    # def writeL32SMany(self, clsKey: object, value: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeSMany(clsKey, valueFactory, source.writeUInt32(endian))
    # def writeC32SMany(self, clsKey: object, value: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeSMany(clsKey, valueFactory, source.writeCInt32(endian))
    # def writeSMany(self, clsKey: object, value: object, valueFactory: callable) -> dict[object, object]: return {self.writeS(clsKey):valueFactory(self) for x in range(count)} if count else {}
    
    # struct : many - type
    # def writeL8TMany(self, clsKey: object, value: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeTMany(clsKey, sizeOf, valueFactory, source.writeByte())
    # def writeL16TMany(self, clsKey: object, value: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeTMany(clsKey, sizeOf, valueFactory, source.writeUInt16(endian))
    # def writeL32TMany(self, clsKey: object, value: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeTMany(clsKey, sizeOf, valueFactory, source.writeUInt32(endian))
    # def writeC32TMany(self, clsKey: object, value: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.writeTMany(clsKey, sizeOf, valueFactory, source.writeCInt32(endian))
    # def writeTMany(self, clsKey: object, value: object, sizeOf: int, valueFactory: callable) -> dict[object, object]: return {self.writeT(clsKey, sizeOf):valueFactory(self) for x in range(count)} if count else {}

    # numerics
    # def writeHalf(self, value: float) -> None: raise NotImplementedError()
    # def writeHalf16(self, value: float) -> None: raise NotImplementedError()
    # def writeVector2(self, value: np.ndarray) -> None: self.writeSingle(value[0]); self.writeSingle(value[1])
    # def writeHalfVector2(self, value: np.ndarray) -> None: self.writeHalf(value[0]); self.writeHalf(value[1])
    # def writeVector3(self, value: np.ndarray) -> None: return np.array([self.writeSingle(value[3]), self.writeSingle(value[3]), self.writeSingle(value[3])])
    # def writeHalfVector3(self, value: np.ndarray) -> None: return np.array([self.writeHalf(value[3]), self.writeHalf(value[3]), self.writeHalf(value[3])])
    # def writeHalf16Vector3(self, value: np.ndarray) -> None: return np.array([self.writeHalf16(value[3]), self.writeHalf16(value[3]), self.writeHalf16(value[3])])
    # def writeVector4(self, value: np.ndarray) -> None: return np.array([self.writeSingle(value[3]), self.writeSingle(value[3]), self.writeSingle(value[3]), self.writeSingle(value[3])])
    # def writeHalfVector4(self, value: np.ndarray) -> None: return np.array([self.writeHalf(value[3]), self.writeHalf(value[3]), self.writeHalf(value[3]), self.writeHalf(value[3])])
    # def writeMatrix3x3(self, value: np.ndarray) -> None: return np.array([
    #     [self.writeSingle(value[3]), self.writeSingle(value[3]), self.writeSingle(value[3])],
    #     [self.writeSingle(value[3]), self.writeSingle(value[3]), self.writeSingle(value[3])],
    #     [self.writeSingle(value[3]), self.writeSingle(value[3]), self.writeSingle(value[3])]
    #     ])
    