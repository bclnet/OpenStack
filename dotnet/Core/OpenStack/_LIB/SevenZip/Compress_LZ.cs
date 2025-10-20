using System;

namespace SevenZip.Compression.LZ;

#region IMatchFinder

interface IInWindowStream {
    void SetStream(System.IO.Stream inStream);
    void Init();
    void ReleaseStream();
    Byte GetIndexByte(Int32 index);
    UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit);
    UInt32 GetNumAvailableBytes();
}

interface IMatchFinder : IInWindowStream {
    void Create(UInt32 historySize, UInt32 keepAddBufferBefore,
            UInt32 matchMaxLen, UInt32 keepAddBufferAfter);
    UInt32 GetMatches(UInt32[] distances);
    void Skip(UInt32 num);
}

#endregion

#region LzBinTree

public class BinTree : InWindow, IMatchFinder {
    UInt32 _cyclicBufferPos;
    UInt32 _cyclicBufferSize = 0;
    UInt32 _matchMaxLen;

    UInt32[] _son;
    UInt32[] _hash;

    UInt32 _cutValue = 0xFF;
    UInt32 _hashMask;
    UInt32 _hashSizeSum = 0;

    bool HASH_ARRAY = true;

    const UInt32 kHash2Size = 1 << 10;
    const UInt32 kHash3Size = 1 << 16;
    const UInt32 kBT2HashSize = 1 << 16;
    const UInt32 kStartMaxLen = 1;
    const UInt32 kHash3Offset = kHash2Size;
    const UInt32 kEmptyHashValue = 0;
    const UInt32 kMaxValForNormalize = ((UInt32)1 << 31) - 1;

    UInt32 kNumHashDirectBytes = 0;
    UInt32 kMinMatchCheck = 4;
    UInt32 kFixHashSize = kHash2Size + kHash3Size;

    public void SetType(int numHashBytes) {
        HASH_ARRAY = (numHashBytes > 2);
        if (HASH_ARRAY) {
            kNumHashDirectBytes = 0;
            kMinMatchCheck = 4;
            kFixHashSize = kHash2Size + kHash3Size;
        }
        else {
            kNumHashDirectBytes = 2;
            kMinMatchCheck = 2 + 1;
            kFixHashSize = 0;
        }
    }

    public new void SetStream(System.IO.Stream stream) { base.SetStream(stream); }
    public new void ReleaseStream() { base.ReleaseStream(); }

    public new void Init() {
        base.Init();
        for (UInt32 i = 0; i < _hashSizeSum; i++)
            _hash[i] = kEmptyHashValue;
        _cyclicBufferPos = 0;
        ReduceOffsets(-1);
    }

    public new void MovePos() {
        if (++_cyclicBufferPos >= _cyclicBufferSize)
            _cyclicBufferPos = 0;
        base.MovePos();
        if (_pos == kMaxValForNormalize)
            Normalize();
    }

    public new Byte GetIndexByte(Int32 index) { return base.GetIndexByte(index); }

    public new UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit) { return base.GetMatchLen(index, distance, limit); }

    public new UInt32 GetNumAvailableBytes() { return base.GetNumAvailableBytes(); }

    public void Create(UInt32 historySize, UInt32 keepAddBufferBefore,
            UInt32 matchMaxLen, UInt32 keepAddBufferAfter) {
        if (historySize > kMaxValForNormalize - 256)
            throw new Exception();
        _cutValue = 16 + (matchMaxLen >> 1);

        UInt32 windowReservSize = (historySize + keepAddBufferBefore +
                matchMaxLen + keepAddBufferAfter) / 2 + 256;

        base.Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, windowReservSize);

        _matchMaxLen = matchMaxLen;

        UInt32 cyclicBufferSize = historySize + 1;
        if (_cyclicBufferSize != cyclicBufferSize)
            _son = new UInt32[(_cyclicBufferSize = cyclicBufferSize) * 2];

        UInt32 hs = kBT2HashSize;

        if (HASH_ARRAY) {
            hs = historySize - 1;
            hs |= (hs >> 1);
            hs |= (hs >> 2);
            hs |= (hs >> 4);
            hs |= (hs >> 8);
            hs >>= 1;
            hs |= 0xFFFF;
            if (hs > (1 << 24))
                hs >>= 1;
            _hashMask = hs;
            hs++;
            hs += kFixHashSize;
        }
        if (hs != _hashSizeSum)
            _hash = new UInt32[_hashSizeSum = hs];
    }

    public UInt32 GetMatches(UInt32[] distances) {
        UInt32 lenLimit;
        if (_pos + _matchMaxLen <= _streamPos)
            lenLimit = _matchMaxLen;
        else {
            lenLimit = _streamPos - _pos;
            if (lenLimit < kMinMatchCheck) {
                MovePos();
                return 0;
            }
        }

        UInt32 offset = 0;
        UInt32 matchMinPos = (_pos > _cyclicBufferSize) ? (_pos - _cyclicBufferSize) : 0;
        UInt32 cur = _bufferOffset + _pos;
        UInt32 maxLen = kStartMaxLen; // to avoid items for len < hashSize;
        UInt32 hashValue, hash2Value = 0, hash3Value = 0;

        if (HASH_ARRAY) {
            UInt32 temp = CRC.Table[_bufferBase[cur]] ^ _bufferBase[cur + 1];
            hash2Value = temp & (kHash2Size - 1);
            temp ^= ((UInt32)(_bufferBase[cur + 2]) << 8);
            hash3Value = temp & (kHash3Size - 1);
            hashValue = (temp ^ (CRC.Table[_bufferBase[cur + 3]] << 5)) & _hashMask;
        }
        else
            hashValue = _bufferBase[cur] ^ ((UInt32)(_bufferBase[cur + 1]) << 8);

        UInt32 curMatch = _hash[kFixHashSize + hashValue];
        if (HASH_ARRAY) {
            UInt32 curMatch2 = _hash[hash2Value];
            UInt32 curMatch3 = _hash[kHash3Offset + hash3Value];
            _hash[hash2Value] = _pos;
            _hash[kHash3Offset + hash3Value] = _pos;
            if (curMatch2 > matchMinPos)
                if (_bufferBase[_bufferOffset + curMatch2] == _bufferBase[cur]) {
                    distances[offset++] = maxLen = 2;
                    distances[offset++] = _pos - curMatch2 - 1;
                }
            if (curMatch3 > matchMinPos)
                if (_bufferBase[_bufferOffset + curMatch3] == _bufferBase[cur]) {
                    if (curMatch3 == curMatch2)
                        offset -= 2;
                    distances[offset++] = maxLen = 3;
                    distances[offset++] = _pos - curMatch3 - 1;
                    curMatch2 = curMatch3;
                }
            if (offset != 0 && curMatch2 == curMatch) {
                offset -= 2;
                maxLen = kStartMaxLen;
            }
        }

        _hash[kFixHashSize + hashValue] = _pos;

        UInt32 ptr0 = (_cyclicBufferPos << 1) + 1;
        UInt32 ptr1 = (_cyclicBufferPos << 1);

        UInt32 len0, len1;
        len0 = len1 = kNumHashDirectBytes;

        if (kNumHashDirectBytes != 0) {
            if (curMatch > matchMinPos) {
                if (_bufferBase[_bufferOffset + curMatch + kNumHashDirectBytes] !=
                        _bufferBase[cur + kNumHashDirectBytes]) {
                    distances[offset++] = maxLen = kNumHashDirectBytes;
                    distances[offset++] = _pos - curMatch - 1;
                }
            }
        }

        UInt32 count = _cutValue;

        while (true) {
            if (curMatch <= matchMinPos || count-- == 0) {
                _son[ptr0] = _son[ptr1] = kEmptyHashValue;
                break;
            }
            UInt32 delta = _pos - curMatch;
            UInt32 cyclicPos = ((delta <= _cyclicBufferPos) ?
                        (_cyclicBufferPos - delta) :
                        (_cyclicBufferPos - delta + _cyclicBufferSize)) << 1;

            UInt32 pby1 = _bufferOffset + curMatch;
            UInt32 len = Math.Min(len0, len1);
            if (_bufferBase[pby1 + len] == _bufferBase[cur + len]) {
                while (++len != lenLimit)
                    if (_bufferBase[pby1 + len] != _bufferBase[cur + len])
                        break;
                if (maxLen < len) {
                    distances[offset++] = maxLen = len;
                    distances[offset++] = delta - 1;
                    if (len == lenLimit) {
                        _son[ptr1] = _son[cyclicPos];
                        _son[ptr0] = _son[cyclicPos + 1];
                        break;
                    }
                }
            }
            if (_bufferBase[pby1 + len] < _bufferBase[cur + len]) {
                _son[ptr1] = curMatch;
                ptr1 = cyclicPos + 1;
                curMatch = _son[ptr1];
                len1 = len;
            }
            else {
                _son[ptr0] = curMatch;
                ptr0 = cyclicPos;
                curMatch = _son[ptr0];
                len0 = len;
            }
        }
        MovePos();
        return offset;
    }

    public void Skip(UInt32 num) {
        do {
            UInt32 lenLimit;
            if (_pos + _matchMaxLen <= _streamPos)
                lenLimit = _matchMaxLen;
            else {
                lenLimit = _streamPos - _pos;
                if (lenLimit < kMinMatchCheck) {
                    MovePos();
                    continue;
                }
            }

            UInt32 matchMinPos = (_pos > _cyclicBufferSize) ? (_pos - _cyclicBufferSize) : 0;
            UInt32 cur = _bufferOffset + _pos;

            UInt32 hashValue;

            if (HASH_ARRAY) {
                UInt32 temp = CRC.Table[_bufferBase[cur]] ^ _bufferBase[cur + 1];
                UInt32 hash2Value = temp & (kHash2Size - 1);
                _hash[hash2Value] = _pos;
                temp ^= ((UInt32)(_bufferBase[cur + 2]) << 8);
                UInt32 hash3Value = temp & (kHash3Size - 1);
                _hash[kHash3Offset + hash3Value] = _pos;
                hashValue = (temp ^ (CRC.Table[_bufferBase[cur + 3]] << 5)) & _hashMask;
            }
            else
                hashValue = _bufferBase[cur] ^ ((UInt32)(_bufferBase[cur + 1]) << 8);

            UInt32 curMatch = _hash[kFixHashSize + hashValue];
            _hash[kFixHashSize + hashValue] = _pos;

            UInt32 ptr0 = (_cyclicBufferPos << 1) + 1;
            UInt32 ptr1 = (_cyclicBufferPos << 1);

            UInt32 len0, len1;
            len0 = len1 = kNumHashDirectBytes;

            UInt32 count = _cutValue;
            while (true) {
                if (curMatch <= matchMinPos || count-- == 0) {
                    _son[ptr0] = _son[ptr1] = kEmptyHashValue;
                    break;
                }

                UInt32 delta = _pos - curMatch;
                UInt32 cyclicPos = ((delta <= _cyclicBufferPos) ?
                            (_cyclicBufferPos - delta) :
                            (_cyclicBufferPos - delta + _cyclicBufferSize)) << 1;

                UInt32 pby1 = _bufferOffset + curMatch;
                UInt32 len = Math.Min(len0, len1);
                if (_bufferBase[pby1 + len] == _bufferBase[cur + len]) {
                    while (++len != lenLimit)
                        if (_bufferBase[pby1 + len] != _bufferBase[cur + len])
                            break;
                    if (len == lenLimit) {
                        _son[ptr1] = _son[cyclicPos];
                        _son[ptr0] = _son[cyclicPos + 1];
                        break;
                    }
                }
                if (_bufferBase[pby1 + len] < _bufferBase[cur + len]) {
                    _son[ptr1] = curMatch;
                    ptr1 = cyclicPos + 1;
                    curMatch = _son[ptr1];
                    len1 = len;
                }
                else {
                    _son[ptr0] = curMatch;
                    ptr0 = cyclicPos;
                    curMatch = _son[ptr0];
                    len0 = len;
                }
            }
            MovePos();
        }
        while (--num != 0);
    }

    void NormalizeLinks(UInt32[] items, UInt32 numItems, UInt32 subValue) {
        for (UInt32 i = 0; i < numItems; i++) {
            UInt32 value = items[i];
            if (value <= subValue)
                value = kEmptyHashValue;
            else
                value -= subValue;
            items[i] = value;
        }
    }

    void Normalize() {
        UInt32 subValue = _pos - _cyclicBufferSize;
        NormalizeLinks(_son, _cyclicBufferSize * 2, subValue);
        NormalizeLinks(_hash, _hashSizeSum, subValue);
        ReduceOffsets((Int32)subValue);
    }

    public void SetCutValue(UInt32 cutValue) { _cutValue = cutValue; }
}

#endregion

#region LzInWindow

public class InWindow {
    public Byte[] _bufferBase = null; // pointer to buffer with data
    System.IO.Stream _stream;
    UInt32 _posLimit; // offset (from _buffer) of first byte when new block reading must be done
    bool _streamEndWasReached; // if (true) then _streamPos shows real end of stream

    UInt32 _pointerToLastSafePosition;

    public UInt32 _bufferOffset;

    public UInt32 _blockSize; // Size of Allocated memory block
    public UInt32 _pos; // offset (from _buffer) of curent byte
    UInt32 _keepSizeBefore; // how many BYTEs must be kept in buffer before _pos
    UInt32 _keepSizeAfter; // how many BYTEs must be kept buffer after _pos
    public UInt32 _streamPos; // offset (from _buffer) of first not read byte from Stream

    public void MoveBlock() {
        UInt32 offset = (UInt32)(_bufferOffset) + _pos - _keepSizeBefore;
        // we need one additional byte, since MovePos moves on 1 byte.
        if (offset > 0)
            offset--;

        UInt32 numBytes = (UInt32)(_bufferOffset) + _streamPos - offset;

        // check negative offset ????
        for (UInt32 i = 0; i < numBytes; i++)
            _bufferBase[i] = _bufferBase[offset + i];
        _bufferOffset -= offset;
    }

    public virtual void ReadBlock() {
        if (_streamEndWasReached)
            return;
        while (true) {
            int size = (int)((0 - _bufferOffset) + _blockSize - _streamPos);
            if (size == 0)
                return;
            int numReadBytes = _stream.Read(_bufferBase, (int)(_bufferOffset + _streamPos), size);
            if (numReadBytes == 0) {
                _posLimit = _streamPos;
                UInt32 pointerToPostion = _bufferOffset + _posLimit;
                if (pointerToPostion > _pointerToLastSafePosition)
                    _posLimit = (UInt32)(_pointerToLastSafePosition - _bufferOffset);

                _streamEndWasReached = true;
                return;
            }
            _streamPos += (UInt32)numReadBytes;
            if (_streamPos >= _pos + _keepSizeAfter)
                _posLimit = _streamPos - _keepSizeAfter;
        }
    }

    void Free() { _bufferBase = null; }

    public void Create(UInt32 keepSizeBefore, UInt32 keepSizeAfter, UInt32 keepSizeReserv) {
        _keepSizeBefore = keepSizeBefore;
        _keepSizeAfter = keepSizeAfter;
        UInt32 blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
        if (_bufferBase == null || _blockSize != blockSize) {
            Free();
            _blockSize = blockSize;
            _bufferBase = new Byte[_blockSize];
        }
        _pointerToLastSafePosition = _blockSize - keepSizeAfter;
    }

    public void SetStream(System.IO.Stream stream) { _stream = stream; }
    public void ReleaseStream() { _stream = null; }

    public void Init() {
        _bufferOffset = 0;
        _pos = 0;
        _streamPos = 0;
        _streamEndWasReached = false;
        ReadBlock();
    }

    public void MovePos() {
        _pos++;
        if (_pos > _posLimit) {
            UInt32 pointerToPostion = _bufferOffset + _pos;
            if (pointerToPostion > _pointerToLastSafePosition)
                MoveBlock();
            ReadBlock();
        }
    }

    public Byte GetIndexByte(Int32 index) { return _bufferBase[_bufferOffset + _pos + index]; }

    // index + limit have not to exceed _keepSizeAfter;
    public UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit) {
        if (_streamEndWasReached)
            if ((_pos + index) + limit > _streamPos)
                limit = _streamPos - (UInt32)(_pos + index);
        distance++;
        // Byte *pby = _buffer + (size_t)_pos + index;
        UInt32 pby = _bufferOffset + _pos + (UInt32)index;

        UInt32 i;
        for (i = 0; i < limit && _bufferBase[pby + i] == _bufferBase[pby + i - distance]; i++) ;
        return i;
    }

    public UInt32 GetNumAvailableBytes() { return _streamPos - _pos; }

    public void ReduceOffsets(Int32 subValue) {
        _bufferOffset += (UInt32)subValue;
        _posLimit -= (UInt32)subValue;
        _pos -= (UInt32)subValue;
        _streamPos -= (UInt32)subValue;
    }
}

#endregion

#region LzOutWindow

public class OutWindow {
    byte[] _buffer = null;
    uint _pos;
    uint _windowSize = 0;
    uint _streamPos;
    System.IO.Stream _stream;

    public uint TrainSize = 0;

    public void Create(uint windowSize) {
        if (_windowSize != windowSize) {
            // System.GC.Collect();
            _buffer = new byte[windowSize];
        }
        _windowSize = windowSize;
        _pos = 0;
        _streamPos = 0;
    }

    public void Init(System.IO.Stream stream, bool solid) {
        ReleaseStream();
        _stream = stream;
        if (!solid) {
            _streamPos = 0;
            _pos = 0;
            TrainSize = 0;
        }
    }

    public bool Train(System.IO.Stream stream) {
        long len = stream.Length;
        uint size = (len < _windowSize) ? (uint)len : _windowSize;
        TrainSize = size;
        stream.Position = len - size;
        _streamPos = _pos = 0;
        while (size > 0) {
            uint curSize = _windowSize - _pos;
            if (size < curSize)
                curSize = size;
            int numReadBytes = stream.Read(_buffer, (int)_pos, (int)curSize);
            if (numReadBytes == 0)
                return false;
            size -= (uint)numReadBytes;
            _pos += (uint)numReadBytes;
            _streamPos += (uint)numReadBytes;
            if (_pos == _windowSize)
                _streamPos = _pos = 0;
        }
        return true;
    }

    public void ReleaseStream() {
        Flush();
        _stream = null;
    }

    public void Flush() {
        uint size = _pos - _streamPos;
        if (size == 0)
            return;
        _stream.Write(_buffer, (int)_streamPos, (int)size);
        if (_pos >= _windowSize)
            _pos = 0;
        _streamPos = _pos;
    }

    public void CopyBlock(uint distance, uint len) {
        uint pos = _pos - distance - 1;
        if (pos >= _windowSize)
            pos += _windowSize;
        for (; len > 0; len--) {
            if (pos >= _windowSize)
                pos = 0;
            _buffer[_pos++] = _buffer[pos++];
            if (_pos >= _windowSize)
                Flush();
        }
    }

    public void PutByte(byte b) {
        _buffer[_pos++] = b;
        if (_pos >= _windowSize)
            Flush();
    }

    public byte GetByte(uint distance) {
        uint pos = _pos - distance - 1;
        if (pos >= _windowSize)
            pos += _windowSize;
        return _buffer[pos];
    }
}

#endregion