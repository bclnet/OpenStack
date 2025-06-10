import os, numpy as np
from struct import calcsize, unpack, iter_unpack
from io import BytesIO
from openstk.util import _throw

# Reader
class Reader:
    def __init__(self, f): self.f = f; self.__update()
    def __enter__(self): return self
    def __exit__(self, type, value, traceback): self.f.close()
    def __update(self):
        f = self.f
        pos = f.tell()
        self.length = f.seek(0, os.SEEK_END)
        f.seek(pos, os.SEEK_SET)

    # base
    def read(self, data: bytearray, offset: int, size: int): return self.f.readinto(data[offset:offset+size]) #data[offset:offset+size] = self.f.read(size)
    def readBytes(self, size: int): return self.f.read(size)
    def length(self): return self.length
    def copyTo(self, destination: BytesIO, resetAfter: bool = False): raise NotImplementedError()
    def readToEnd(self): length = self.length - self.f.tell(); return self.f.read(length)
    def readLine(self) -> str: return self.f.readline().decode('utf-8')
    def readToValue(self, value: int = b'\x00', length: int = 65535, ms: BytesIO = None) -> bytearray:
        if not ms: ms = BytesIO()
        else: ms.seek(0); ms.truncate(0)
        f = self.f; length = min(length, self.length - self.f.tell())
        while length > 0 and (c := f.read(1)) != value: length -= 1; ms.write(c)
        ms.seek(0)
        return ms.read()

    # primatives : normal
    def readDouble(self): return unpack('<d', self.f.read(8))[0]
    def readSByte(self): return int.from_bytes(self.f.read(1), 'little', signed=True)
    def readInt16(self): return int.from_bytes(self.f.read(2), 'little', signed=True)
    def readInt32(self): return int.from_bytes(self.f.read(4), 'little', signed=True)
    def readInt64(self): return int.from_bytes(self.f.read(8), 'little', signed=True)
    def readSingle(self): return unpack('<f', self.f.read(4))[0]
    def readByte(self): return int.from_bytes(self.f.read(1), 'little', signed=False)
    def readUInt16(self): return int.from_bytes(self.f.read(2), 'little', signed=False)
    def readUInt32(self): return int.from_bytes(self.f.read(4), 'little', signed=False)
    def readUInt64(self): return int.from_bytes(self.f.read(8), 'little', signed=False)

    # primatives : endian
    def readDoubleE(self): return unpack('>d', self.f.read(8))[0]
    def readInt16E(self): return int.from_bytes(self.f.read(2), 'big', signed=True)
    def readInt32E(self): return int.from_bytes(self.f.read(4), 'big', signed=True)
    def readInt64E(self): return int.from_bytes(self.f.read(8), 'big', signed=True)
    def readSingleE(self): return unpack('>f', self.f.read(4))[0]
    def readUInt16E(self): return int.from_bytes(self.f.read(2), 'big', signed=False)
    def readUInt32E(self): return int.from_bytes(self.f.read(4), 'big', signed=False)
    def readUInt64E(self): return int.from_bytes(self.f.read(8), 'big', signed=False)

    # primatives : endianX
    def readDoubleX(self, endian: bool = True): return unpack('>d' if endian else '<d', self.f.read(8))[0]
    def readInt16X(self, endian: bool = True): return int.from_bytes(self.f.read(2), 'big' if endian else 'little', signed=True)
    def readInt32X(self, endian: bool = True): return int.from_bytes(self.f.read(4), 'big' if endian else 'little', signed=True)
    def readInt64X(self, endian: bool = True): return int.from_bytes(self.f.read(8), 'big' if endian else 'little', signed=True)
    def readSingleX(self, endian: bool = True): return unpack('>f' if endian else '<f', self.f.read(4))[0]
    def readUInt16X(self, endian: bool = True): return int.from_bytes(self.f.read(2), 'big' if endian else 'little', signed=False)
    def readUInt32X(self, endian: bool = True): return int.from_bytes(self.f.read(4), 'big' if endian else 'little', signed=False)
    def readUInt64X(self, endian: bool = True): return int.from_bytes(self.f.read(8), 'big' if endian else 'little', signed=False)

    # primatives : specialized
    def readBool32(self): return int.from_bytes(self.f.read(4), 'little', signed=False) != 0
    def readGuid(self): return self.f.read(16)
    def readCInt32(self): raise NotImplementedError()

    # position
    def align(self, align: int = 4): align -= 1; self.f.seek((self.f.tell() + align) & ~align, os.SEEK_SET); return self
    def tell(self): return self.f.tell()
    def seek(self, offset: int): self.f.seek(offset, os.SEEK_SET); return self
    def seekAndAlign(self, offset: int, align: int = 4): self.f.seek(offset + align - (offset % align) if offset % align else offset, os.SEEK_SET); return self
    def skip(self, count: int): self.f.seek(count, os.SEEK_CUR); return self
    def skipAndAlign(self, count: int, align: int = 4): offset = self.f.tell() + count; self.f.seek(offset + align - (offset % align) if offset % align else offset, os.SEEK_CUR); return self
    def end(self, offset: int): self.f.seek(offset, os.SEEK_END); return self
    def peek(self, action, offset: int = 0, origin: int = os.SEEK_CUR):
        f = self.f
        pos = f.tell()
        f.seek(offset, origin)
        value = action(self)
        f.seek(pos)
        return value

    # string : special
    def readL16OString(self, codepage: int = 1252) -> str: raise Exception('not implemented')
    # string : length
    def readL8UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readByte(); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readL16UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt16X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readL32UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt32X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readL8AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readByte(); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    def readL16AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt16X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    def readL32AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt32X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    # string : fixed
    def readFUString(self, length: int) -> str: return self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readFAString(self, length: int) -> str: return self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    # string : variable
    def readVUString(self, length: int = 65535, stopValue: int = b'\x00', ms: BytesIO = None) -> str: return self.readToValue(stopValue, length, ms).decode('utf-8', 'ignore')
    def readVAString(self, length: int = 65535, stopValue: int = b'\x00', ms: BytesIO = None) -> str: return self.readToValue(stopValue, length, ms).decode('ascii', 'ignore')
    # string : encoding
    def readL8Encoding(self, encoding: str = None): return self.f.read(int.from_bytes(self.f.read(1), 'little')).decode('ascii' if not encoding else encoding)
    def readL16Encoding(self, encoding: str = None): return self.f.read(int.from_bytes(self.f.read(2), 'little')).decode('ascii' if not encoding else encoding)
    def readL32Encoding(self, encoding: str = None): return self.f.read(int.from_bytes(self.f.read(4), 'little')).decode('ascii' if not encoding else encoding)
    
    # struct : single  - https://docs.python.org/3/library/struct.html 
    def readF(self, factory: callable) -> object: return factory(self)
    def readP(self, cls: callable, pat: str) -> object: cls = cls or (lambda x: x[0]); return cls(unpack(pat, self.f.read(calcsize(pat))))
    def readS(self, cls: object, sizeOf: int = -1) -> object: pat, size = cls.struct; return cls(unpack(pat, self.f.read(size)))

    # struct : array - factory
    def readL8FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readByte())
    def readL16FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readUInt16X(endian))
    def readL32FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readUInt32X(endian))
    def readC32FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readCInt32X(endian))
    def readFArray(self, factory: callable, count: int) -> list[object]: return [self.readF(factory) for x in range(count)] if count else []

    # struct : array - pattern / primative
    def readL8PArray(self, cls: callable, pat: str) -> list[object]: return self.readPArray(cls, pat, self.readByte())
    def readL16PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readUInt16X(endian))
    def readL32PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readUInt32X(endian))
    def readC32PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readCInt32X(endian))
    def readPArray(self, cls: callable, pat: str, count: int) -> list[object]: cls = cls or (lambda x: x[0]); return [cls(x) for x in iter_unpack(pat, self.f.read(calcsize(pat) * count))] if count else []

    # struct : array - struct
    def readL8SArray(self, cls: object, sizeOf: int = -1) -> list[object]: return self.readSArray(cls, sizeOf, self.readByte())
    def readL16SArray(self, cls: object, sizeOf: int = -1, endian: bool = False) -> list[object]: return self.readSArray(cls, sizeOf, self.readUInt16X(endian))
    def readL32SArray(self, cls: object, sizeOf: int = -1, endian: bool = False) -> list[object]: return self.readSArray(cls, sizeOf, self.readUInt32X(endian))
    def readC32SArray(self, cls: object, sizeOf: int = -1, endian: bool = False) -> list[object]: return self.readSArray(cls, sizeOf, self.readCInt32X(endian))
    def readSArray(self, cls: object, count: int, sizeOf: int = -1) -> list[object]: return [self.readS(cls) for x in range(count)] if count else []

    # struct : array - type
    # def readL8TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readByte())
    # def readL16TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readUInt16X(endian))
    # def readL32TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readUInt32X(endian))
    # def readC32TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readCInt32X(endian))
    # def readTArray(self, cls: object, sizeOf: int, count: int) -> list[object]: return [self.readT(cls, sizeOf) for x in range(count)] if count else []

    # struct : each
    # def readSEach(self, cls: object, count: int) -> list[object]: return [self.readS(cls) for x in range(count)] if count else []
    # def readTEach(self, cls: object, sizeOf: int, count: int) -> list[object]: return [self.readT(cls, sizeOf) for x in range(count)] if count else []
    
    # struct : many - factory
    def readL8FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readByte())
    def readL16FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readUInt16X(endian))
    def readL32FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readUInt32X(endian))
    def readC32FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readCInt32X(endian))
    def readFMany(self, clsKey: object, keyFactory: callable, valueFactory: callable) -> dict[object, object]: return {keyFactory(self):valueFactory(self) for x in range(count)} if count else {}

    # struct : many - struct
    def readL8SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readByte())
    def readL16SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readUInt16X(endian))
    def readL32SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readUInt32X(endian))
    def readC32SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readCInt32X(endian))
    def readSMany(self, clsKey: object, valueFactory: callable) -> dict[object, object]: return {self.readS(clsKey):valueFactory(self) for x in range(count)} if count else {}
    
    # struct : many - type
    def readL8TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readByte())
    def readL16TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readUInt16X(endian))
    def readL32TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readUInt32X(endian))
    def readC32TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readCInt32X(endian))
    def readTMany(self, clsKey: object, sizeOf: int, valueFactory: callable) -> dict[object, object]: return {self.readT(clsKey, sizeOf):valueFactory(self) for x in range(count)} if count else {}

    # numerics
    def readHalf(self) -> float: raise NotImplementedError()
    def readHalf16(self) -> float: raise NotImplementedError()
    def readVector2(self) -> np.ndarray: return np.array([self.readSingle(), self.readSingle()])
    def readHalfVector2(self) -> np.ndarray: return np.array([self.readHalf(), self.readHalf()])
    def readVector3(self) -> np.ndarray: return np.array(unpack('<3f', self.f.read(12)))
    def readHalfVector3(self) -> np.ndarray: return np.array([self.readHalf(), self.readHalf(), self.readHalf()])
    def readHalf16Vector3(self) -> np.ndarray: return np.array([self.readHalf16(), self.readHalf16(), self.readHalf16()])
    def readVector4(self) -> np.ndarray: return np.array(unpack('<4f', self.f.read(16)))
    def readHalfVector4(self) -> np.ndarray: return np.array([self.readHalf(), self.readHalf(), self.readHalf(), self.readHalf()])
    def readMatrix3x3(self) -> np.ndarray: return np.array([
        [self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle()]
        ])
    def readMatrix3x3As4x4(self) -> np.ndarray: return np.array([
        [self.readSingle(), self.readSingle(), self.readSingle(), 1.],
        [self.readSingle(), self.readSingle(), self.readSingle(), 1.],
        [self.readSingle(), self.readSingle(), self.readSingle(), 1.],
        [0., 0., 0., 1.]
        ])

