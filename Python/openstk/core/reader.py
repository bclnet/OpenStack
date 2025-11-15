import os
from numpy import ndarray, array
from quaternion import quaternion
from struct import calcsize, unpack, iter_unpack
from io import BytesIO
from openstk.core.util import _throw

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
    def read(self, data: bytearray, offset: int, size: int) -> bytearray: return self.f.readinto(data[offset:offset+size]) #data[offset:offset+size] = self.f.read(size)
    def readBytes(self, size: int) -> bytearray: return self.f.read(size)
    def readChar(self) -> chr:
        result = self.f.read(1)[0]
        if (result & 0x80):
            byteCount = 1
            while result & (0x80 >> byteCount): byteCount += 1
            result &= (1 << (8 - byteCount)) - 1
            while byteCount > 0: byteCount -= 1; result <<= 6; result |= self.f.read(1)[0] & 0x3F
        return chr(result)
    def readChars(self, count: int) -> list[chr]: return [self.readChar() for x in range(count)]
    def length(self) -> int: return self.length
    def readBoolean(self) -> bool: return self.readByte() != 0 
    def copyTo(self, destination: BytesIO, resetAfter: bool = False) -> None: raise NotImplementedError()
    def readToEnd(self) -> bytearray: length = self.length - self.f.tell(); return self.f.read(length)
    def readLine(self) -> str: return self.f.readline().decode('utf-8')
    def readToValue(self, value: int = b'\x00', length: int = 65535, ms: BytesIO = None) -> bytearray:
        if not ms: ms = BytesIO()
        else: ms.seek(0); ms.truncate(0)
        f = self.f; length = min(length, self.length - self.f.tell())
        while length > 0 and (c := f.read(1)) != value: length -= 1; ms.write(c)
        ms.seek(0)
        return ms.read()
    
    # primatives : bytes
    def readL8Bytes(self, maxLength: int = 0, endian: bool = False) -> bytearray:
        length = self.readByte()
        if maxLength > 0 and length > maxLength: raise Exception('byte length exceeds maximum length')
        return self.f.read(length) if length > 0 else None
    def readL16Bytes(self, maxLength: int = 0, endian: bool = False) -> bytearray:
        length = self.readUInt16X(endian)
        if maxLength > 0 and length > maxLength: raise Exception('byte length exceeds maximum length')
        return self.f.read(length) if length > 0 else None
    def readL32Bytes(self, maxLength: int = 0, endian: bool = False) -> bytearray:
        length = self.readUInt32X(endian)
        if maxLength > 0 and length > maxLength: raise Exception('byte length exceeds maximum length')
        return self.f.read(length) if length > 0 else None

    # primatives : normal
    def readDouble(self) -> float: return unpack('<d', self.f.read(8))[0]
    def readSByte(self) -> int: return int.from_bytes(self.f.read(1), 'little', signed=True)
    def readInt16(self) -> int: return int.from_bytes(self.f.read(2), 'little', signed=True)
    def readInt32(self) -> int: return int.from_bytes(self.f.read(4), 'little', signed=True)
    def readInt64(self) -> int: return int.from_bytes(self.f.read(8), 'little', signed=True)
    def readSingle(self) -> float: return unpack('<f', self.f.read(4))[0]
    def readByte(self) -> int: return int.from_bytes(self.f.read(1), 'little', signed=False)
    def readUInt16(self) -> int: return int.from_bytes(self.f.read(2), 'little', signed=False)
    def readUInt32(self) -> int: return int.from_bytes(self.f.read(4), 'little', signed=False)
    def readUInt64(self) -> int: return int.from_bytes(self.f.read(8), 'little', signed=False)

    # primatives : endian
    def readDoubleE(self) -> float: return unpack('>d', self.f.read(8))[0]
    def readInt16E(self) -> int: return int.from_bytes(self.f.read(2), 'big', signed=True)
    def readInt32E(self) -> int: return int.from_bytes(self.f.read(4), 'big', signed=True)
    def readInt64E(self) -> int: return int.from_bytes(self.f.read(8), 'big', signed=True)
    def readSingleE(self) -> float: return unpack('>f', self.f.read(4))[0]
    def readUInt16E(self) -> int: return int.from_bytes(self.f.read(2), 'big', signed=False)
    def readUInt32E(self) -> int: return int.from_bytes(self.f.read(4), 'big', signed=False)
    def readUInt64E(self) -> int: return int.from_bytes(self.f.read(8), 'big', signed=False)

    # primatives : endianX
    def readDoubleX(self, endian: bool) -> float: return unpack('>d' if endian else '<d', self.f.read(8))[0]
    def readInt16X(self, endian: bool) -> int: return int.from_bytes(self.f.read(2), 'big' if endian else 'little', signed=True)
    def readInt32X(self, endian: bool) -> int: return int.from_bytes(self.f.read(4), 'big' if endian else 'little', signed=True)
    def readInt64X(self, endian: bool) -> int: return int.from_bytes(self.f.read(8), 'big' if endian else 'little', signed=True)
    def readSingleX(self, endian: bool) -> float: return unpack('>f' if endian else '<f', self.f.read(4))[0]
    def readUInt16X(self, endian: bool) -> int: return int.from_bytes(self.f.read(2), 'big' if endian else 'little', signed=False)
    def readUInt32X(self, endian: bool) -> int: return int.from_bytes(self.f.read(4), 'big' if endian else 'little', signed=False)
    def readUInt64X(self, endian: bool) -> int: return int.from_bytes(self.f.read(8), 'big' if endian else 'little', signed=False)

    # primatives : specialized
    #
    def readVInt7(self) -> int:
        r = 0; v = 0; b = 0
        while True:
            v = self.f.read(1)[0]; r |= (v & 0x7f) << b; b += 7
            if (v & 0x80) == 0: break
        return r
    def readVInt7X(self, endian: bool) -> int: return self.readVInt7() if not endian else _throw('NotImplementedError')
    def readVInt8(self) -> int:
        b0 = self.f.read(1)[0]
        if (b0 & 0x80) == 0: return b0
        b1 = self.f.read(1)[0]
        if (b0 & 0x40) == 0: return ((b0 & 0x7F) << 8) | b1
        return ((((b0 & 0x3F) << 8) | b1) << 16) | int.from_bytes(self.f.read(2), 'little', signed=False)
    def readVInt8X(self, endian: bool) -> int: return self.readVInt8() if not endian else _throw('NotImplementedError')
    def readBool32(self) -> bool: return int.from_bytes(self.f.read(4), 'little', signed=False) != 0
    def readGuid(self) -> bytes: return self.f.read(16)

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
    def atEnd(self) -> bool: return self.f.tell() == self.length
    def ensureAtEnd(self, end: int = -1):
        if (self.f.tell() if end == -1 else end) != self.length: raise Exception('Not at end')

    # string : special
    def readL16OString(self, codepage: int = 1252) -> str: raise Exception('not implemented')
    # string : length
    def readL8UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readByte(); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readL16UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt16X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readL32UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt32X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readLV7UString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readVInt7(); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-8').rstrip('\00') if length != 0 else None
    def readL8AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readByte(); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    def readL16AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt16X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    def readL32AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readUInt32X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    def readLV7AString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readVInt7X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('ascii').rstrip('\00') if length != 0 else None
    def readLV8WString(self, maxLength: int = 0, endian: bool = False) -> str: length = self.readVInt8X(endian); return _throw('string length exceeds maximum length') if maxLength > 0 and length > maxLength else self.f.read(length)[:length].decode('utf-16').rstrip('\00') if length != 0 else None
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
    def readS(self, cls: object, sizeOf: int = -1) -> object: pat, size = cls._struct; return cls(unpack(pat, self.f.read(size)))

    # struct : array - factory
    def readL8FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readByte())
    def readL16FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readUInt16X(endian))
    def readL32FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readUInt32X(endian))
    def readLV7FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readVInt7X(endian))
    def readLV8FArray(self, factory: callable, endian: bool = False) -> list[object]: return self.readFArray(factory, self.readVInt8X(endian))
    def readFArray(self, factory: callable, count: int) -> list[object]: return [self.readF(factory) for x in range(count)] if count else []

    # struct : array - pattern / primative
    def readL8PArray(self, cls: callable, pat: str) -> list[object]: return self.readPArray(cls, pat, self.readByte())
    def readL16PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readUInt16X(endian))
    def readL32PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readUInt32X(endian))
    def readLV7PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readVInt7X(endian))
    def readLV8PArray(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPArray(cls, pat, self.readVInt8X(endian))
    def readPArray(self, cls: callable, pat: str, count: int) -> list[object]: cls = cls or (lambda x: x[0]); return [cls(x) for x in iter_unpack(pat, self.f.read(calcsize(pat) * count))] if count else []

    # struct : array - struct
    def readL8SArray(self, cls: object, sizeOf: int = 0) -> list[object]: return self.readSArray(cls, self.readByte(), sizeOf)
    def readL16SArray(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSArray(cls, self.readUInt16X(endian), sizeOf)
    def readL32SArray(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSArray(cls, self.readUInt32X(endian), sizeOf)
    def readLV7SArray(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSArray(cls, self.readVInt7X(endian), sizeOf)
    def readLV8SArray(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSArray(cls, self.readVInt8X(endian), sizeOf)
    def readSArray(self, cls: object, count: int, sizeOf: int = 0) -> list[object]: return [self.readS(cls) for x in range(count)] if count else []

    # struct : array - each
    def readSEach(self, cls: object, count: int) -> list[object]: return [self.readS(cls) for x in range(count)] if count else []
    def readTEach(self, cls: object, sizeOf: int, count: int) -> list[object]: return [self.readT(cls, sizeOf) for x in range(count)] if count else []

    # struct : array - type
    # def readL8TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readByte())
    # def readL16TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readUInt16X(endian))
    # def readL32TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readUInt32X(endian))
    # def readLV8TArray(self, cls: object, sizeOf: int, endian: bool = False) -> list[object]: return self.readTArray(cls, sizeOf, self.readCInt32X(endian))
    # def readTArray(self, cls: object, sizeOf: int, count: int) -> list[object]: return [self.readT(cls, sizeOf) for x in range(count)] if count else []
    
    # struct : list - factory
    def readL8FList(self, factory: callable) -> list[object]: return self.readFList(factory, self.readByte())
    def readL16FList(self, factory: callable, endian: bool = False) -> list[object]: return self.readFList(factory, self.readUInt16X(endian))
    def readL32FList(self, factory: callable, endian: bool = False) -> list[object]: return self.readFList(factory, self.readUInt32X(endian))
    def readLV7FList(self, factory: callable, endian: bool = False) -> list[object]: return self.readFList(factory, self.readVInt7X(endian))
    def readLV8FList(self, factory: callable, endian: bool = False) -> list[object]: return self.readFList(factory, self.readVInt8X(endian))
    def readFList(self, factory: callable, count: int) -> list[object]: return [self.readF(factory) for x in range(count)] if count else []

    # struct : list - pattern
    def readL8PList(self, cls: callable, pat: str) -> list[object]: return self.readPList(cls, pat, self.readByte())
    def readL16PList(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPList(cls, pat, self.readUInt16X(endian))
    def readL32PList(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPList(cls, pat, self.readUInt32X(endian))
    def readLV7PList(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPList(cls, pat, self.readVInt7X(endian))
    def readLV8PList(self, cls: callable, pat: str, endian: bool = False) -> list[object]: return self.readPList(cls, pat, self.readVInt8X(endian))
    def readPList(self, cls: callable, pat: str, count: int) -> list[object]: cls = cls or (lambda x: x[0]); return [cls(x) for x in iter_unpack(pat, self.f.read(calcsize(pat) * count))] if count else []

    # struct : list - struct
    def readL8SList(self, cls: object, sizeOf: int = 0) -> list[object]: return self.readSList(cls, self.readByte(), sizeOf)
    def readL16SList(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSList(cls, self.readUInt16X(endian), sizeOf)
    def readL32SList(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSList(cls, self.readUInt32X(endian), sizeOf)
    def readLV7SList(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSList(cls, self.readVInt7X(endian), sizeOf)
    def readLV8SList(self, cls: object, sizeOf: int = 0, endian: bool = False) -> list[object]: return self.readSList(cls, self.readVInt8X(endian), sizeOf)
    def readSList(self, cls: object, count: int, sizeOf: int = 0) -> list[object]: return [self.readS(cls) for x in range(count)] if count else []

    # struct : many - factory
    def readL8FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readByte())
    def readL16FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readUInt16X(endian))
    def readL32FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readUInt32X(endian))
    def readC32FMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, endian: bool = False) -> list[object]: return self.readFMany(clsKey, keyFactory, valueFactory, self.readCInt32X(endian))
    def readFMany(self, clsKey: object, keyFactory: callable, valueFactory: callable, count: int) -> dict[object, object]: return {keyFactory(self):valueFactory(self) for x in range(count)} if count else {}

    # struct : many - pattern
    def readL8PMany(self, clsKey: object, pat: str, valueFactory: callable, endian: bool = False) -> list[object]: return self.readPMany(clsKey, pat, valueFactory, self.readByte())
    def readL16PMany(self, clsKey: object, pat: str, valueFactory: callable, endian: bool = False) -> list[object]: return self.readPMany(clsKey, pat, valueFactory, self.readUInt16X(endian))
    def readL32PMany(self, clsKey: object, pat: str, valueFactory: callable, endian: bool = False) -> list[object]: return self.readPMany(clsKey, pat, valueFactory, self.readUInt32X(endian))
    def readC32PMany(self, clsKey: object, pat: str, valueFactory: callable, endian: bool = False) -> list[object]: return self.readPMany(clsKey, pat, valueFactory, self.readCInt32X(endian))
    def readPMany(self, clsKey: object, pat: str, valueFactory: callable, count: int) -> dict[object, object]: return {self.readP(clsKey, pat):valueFactory(self) for x in range(count)} if count else {}

    # struct : many - struct
    def readL8SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readByte())
    def readL16SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readUInt16X(endian))
    def readL32SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readUInt32X(endian))
    def readC32SMany(self, clsKey: object, valueFactory: callable, endian: bool = False) -> list[object]: return self.readSMany(clsKey, valueFactory, self.readCInt32X(endian))
    def readSMany(self, clsKey: object, valueFactory: callable, count: int) -> dict[object, object]: return {self.readS(clsKey):valueFactory(self) for x in range(count)} if count else {}
    
    # struct : many - type
    # def readL8TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readByte())
    # def readL16TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readUInt16X(endian))
    # def readL32TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readUInt32X(endian))
    # def readC32TMany(self, clsKey: object, sizeOf: int, valueFactory: callable, endian: bool = False) -> list[object]: return self.readTMany(clsKey, sizeOf, valueFactory, self.readCInt32X(endian))
    # def readTMany(self, clsKey: object, sizeOf: int, valueFactory: callable, count: int) -> dict[object, object]: return {self.readT(clsKey, sizeOf):valueFactory(self) for x in range(count)} if count else {}

    # numerics
    def readHalf(self) -> float: raise NotImplementedError()
    def readHalf16(self) -> float: raise NotImplementedError()
    def readVector2(self) -> ndarray: return array([self.readSingle(), self.readSingle()])
    def readHalfVector2(self) -> ndarray: return array([self.readHalf(), self.readHalf()])
    def readVector3(self) -> ndarray: return array(unpack('<3f', self.f.read(12)))
    def readHalfVector3(self) -> ndarray: return array([self.readHalf(), self.readHalf(), self.readHalf()])
    def readHalf16Vector3(self) -> ndarray: return array([self.readHalf16(), self.readHalf16(), self.readHalf16()])
    def readVector4(self) -> ndarray: return array(unpack('<4f', self.f.read(16)))
    def readHalfVector4(self) -> ndarray: return array([self.readHalf(), self.readHalf(), self.readHalf(), self.readHalf()])
    def readMatrix2x2(self) -> ndarray: return array([
        [self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle()]
        ])
    def readMatrix3x3(self) -> ndarray: return array([
        [self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle()]
        ])
    def readMatrix3x4(self) -> ndarray: return array([
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()]
        ])
    def readMatrix3x3As4x4(self) -> ndarray: return array([
        [self.readSingle(), self.readSingle(), self.readSingle(), 1.],
        [self.readSingle(), self.readSingle(), self.readSingle(), 1.],
        [self.readSingle(), self.readSingle(), self.readSingle(), 1.],
        [0., 0., 0., 1.]
        ])
    def readMatrix4x4(self) -> ndarray: return array([
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()],
        [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()]
        ])
    def readQuaternion(self) -> quaternion: v = [self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle()]; return quaternion(v[3], v[0], v[1], v[2])
    def readQuaternionWFirst(self) -> quaternion: return quaternion(self.readSingle(), self.readSingle(), self.readSingle(), self.readSingle())
    def readHalfQuaternion(self) -> quaternion: v = [self.readHalf(), self.readHalf(), self.readHalf(), self.readHalf()]; return quaternion(v[3], v[0], v[1], v[2])
