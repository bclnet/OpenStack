import io

class StreamIterators:
    @staticmethod
    def StreamCipher(stream, cipher, chunkSize=16): # 4096
        while True:
            chunk = stream.read(chunkSize)
            if not chunk: break
            yield cipher.decrypt(chunk)

class ForwardStream(io.BufferedIOBase):
    def __init__(self, blockIter):
        self.blockIter = blockIter
        self._buf = b''
        self._eof = False
    def readable(self): return True
    def seekable(self): return False
    def _fill(self, size):
        while len(self._buf) < size and not self._eof:
            # try: self._buf += next(self.blockIter)
            # except StopIteration: self._eof = True
            block = next(self.blockIter, None)
            if block: self._buf += block; continue
            self._eof = True
    def read(self, size=-1):
        if size is None or size < 0:
            # Consume everything remaining
            while not self._eof: self._fill(len(self._buf) + 4096)
            result, self._buf = self._buf, b''
            return result
        # Pull exact number of bytes requested
        self._fill(size); result = self._buf[:size]; self._buf = self._buf[size:]
        return result

class SeekableStream(io.BufferedIOBase):
    def __init__(self, blockIter, totalSize=None):
        self.blockIter = blockIter
        self.totalSize = totalSize
        self._buf = b''
        self._pos = 0
        self._eof = False
    def readable(self): return True
    def seekable(self): return True
    def seek(self, offset, whence=io.SEEK_SET):
        if whence == io.SEEK_SET: self._pos = max(0, offset)
        elif whence == io.SEEK_CUR: self._pos = max(0, self._pos + offset)
        elif whence == io.SEEK_END and self.totalSize is not None: self._pos = max(0, self.totalSize + offset)
        else: raise io.UnsupportedOperation("Can't seek without a known total size")
        return self._pos
    def tell(self): return self._pos
    def _fill(self, min_bytes):
        while len(self._buf) < min_bytes and not self._eof:
            # try: self._buf += next(self.blockIter)
            # except StopIteration: self._eof = True
            block = next(self.blockIter, None)
            if block: self._buf += block; continue
            self._eof = True
    def read(self, size=-1):
        if size is None or size < 0:
            # Read all remaining data
            while not self._eof: self._fill(len(self._buf) + 4096)
            result = self._buf[self._pos:]
            self._pos += len(result)
            return result
        # Read specific byte size
        self._fill(self._pos + size); end_pos = self._pos + size; result = self._buf[self._pos:end_pos]; self._pos += len(result)
        return result

class UncloseableStream:
    def __init__(self, obj): self.obj = obj
    def __getattr__(self, name): print(name); return getattr(self.obj, name)
    def close(self): pass