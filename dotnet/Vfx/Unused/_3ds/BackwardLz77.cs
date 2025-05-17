using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenStack.Rom.Nintendo._3ds;

public unsafe class BackwardLz77 {

    #region Headers

    [StructLayout(LayoutKind.Sequential)]
    struct CompFooter {
        public uint BufferTopAndBottom;
        public uint OriginalBottom;
    }

    #endregion

    public static bool GetUncompressedSize(byte[] compressed, uint compressedSize, out uint uncompressedSize) {
        if (compressedSize >= sizeof(CompFooter)) {
            fixed (byte* _compressed = compressed) {
                var compFooter = Marshal.PtrToStructure<CompFooter>((IntPtr)(_compressed + compressedSize - sizeof(CompFooter)));
                uncompressedSize = compressedSize + compFooter.OriginalBottom;
                return true;
            }
        }
        uncompressedSize = default;
        return false;
    }

    public static bool Uncompress(byte[] compressed, uint compressedSize, byte[] uncompressed, ref uint uncompressedSize) {
        bool result = true;
        fixed (byte* _compressed = compressed)
        fixed (byte* _uncompressed = uncompressed) {
            if (compressedSize >= sizeof(CompFooter)) {
                var compFooter = Marshal.PtrToStructure<CompFooter>((IntPtr)(_compressed + compressedSize - sizeof(CompFooter)));
                uint top = compFooter.BufferTopAndBottom & 0xFFFFFF, bottom = compFooter.BufferTopAndBottom >> 24 & 0xFF;
                if (bottom >= sizeof(CompFooter) && bottom <= sizeof(CompFooter) + 3 && top >= bottom && top <= compressedSize && uncompressedSize >= compressedSize + compFooter.OriginalBottom) {
                    uncompressedSize = compressedSize + compFooter.OriginalBottom;
                    Marshal.Copy(compressed, 0, (IntPtr)_uncompressed, (int)compressedSize); //: memcpy(_uncompressed, compressed, compressedSize);
                    byte* _dest = _uncompressed + uncompressedSize, _src = _uncompressed + compressedSize - bottom, _end = _uncompressed + compressedSize - top;
                    while (_src - _end > 0) {
                        byte flag = *--_src;
                        for (var i = 0; i < 8; i++) {
                            if ((flag << i & 0x80) == 0) {
                                if (_dest - _end < 1 || _src - _end < 1) { result = false; break; }
                                *--_dest = *--_src;
                            }
                            else {
                                if (_src - _end < 2) { result = false; break; }
                                int size = *--_src, offset = (((size & 0x0F) << 8) | *--_src) + 3;
                                size = (size >> 4 & 0x0F) + 3;
                                if (size > _dest - _end) { result = false; break; }
                                byte* data = _dest + offset;
                                if (data > _uncompressed + uncompressedSize) { result = false; break; }
                                for (var j = 0; j < size; j++) { *--_dest = *--data; }
                            }
                            if (_src - _end <= 0) break;
                        }
                        if (!result) break;
                    }
                }
                else result = false;
            }
            else result = false;
            return result;
        }
    }

    struct CompressInfo {
        public ushort WindowPos;
        public ushort WindowLen;
        public ushort* OffsetTable;
        public ushort* ReversedOffsetTable;
        public ushort* ByteTable;
        public ushort* EndTable;
    }

    public static bool Compress(byte[] uncompressed, uint uncompressedSize, byte[] compressed, ref uint compressedSize) {
        throw new NotImplementedException();
#if false
        bool bResult = true;
        if (uncompressedSize > sizeof(CompFooter) && compressedSize >= uncompressedSize) {
            u8* pWork = new byte[compressWorkSize];
            do {
                CompressInfo info = new();
                InitTable(info, pWork);
                const int nMaxSize = 0xF + 3;
                const u8* pSrc = a_pUncompressed + a_uUncompressedSize;
                u8* pDest = a_pCompressed + a_uUncompressedSize;
                while (pSrc - a_pUncompressed > 0 && pDest - a_pCompressed > 0) {
                    u8* pFlag = --pDest;
                    *pFlag = 0;
                    for (int i = 0; i < 8; i++) {
                        int nOffset = 0;
                        int nSize = search(&info, pSrc, nOffset, static_cast<int>(min<n64>(min<n64>(nMaxSize, pSrc - a_pUncompressed), a_pUncompressed + a_uUncompressedSize - pSrc)));
                        if (nSize < 3) {
                            if (pDest - a_pCompressed < 1) {
                                bResult = false;
                                break;
                            }
                            slide(&info, pSrc, 1);
                            *--pDest = *--pSrc;
                        }
                        else {
                            if (pDest - a_pCompressed < 2) {
                                bResult = false;
                                break;
                            }
                            *pFlag |= 0x80 >> i;
                            slide(&info, pSrc, nSize);
                            pSrc -= nSize;
                            nSize -= 3;
                            *--pDest = (nSize << 4 & 0xF0) | ((nOffset - 3) >> 8 & 0x0F);
                            *--pDest = (nOffset - 3) & 0xFF;
                        }
                        if (pSrc - a_pUncompressed <= 0) {
                            break;
                        }
                    }
                    if (!bResult) {
                        break;
                    }
                }
                if (!bResult) {
                    break;
                }
                a_uCompressedSize = static_cast<u32>(a_pCompressed + a_uUncompressedSize - pDest);
            } while (false);
            delete[] pWork;
        }
        else {
            bResult = false;
        }
        if (bResult) {
            u32 uOrigSize = a_uUncompressedSize;
            u8* pCompressBuffer = a_pCompressed + a_uUncompressedSize - a_uCompressedSize;
            u32 uCompressBufferSize = a_uCompressedSize;
            u32 uOrigSafe = 0;
            u32 uCompressSafe = 0;
            bool bOver = false;
            while (uOrigSize > 0) {
                u8 uFlag = pCompressBuffer[--uCompressBufferSize];
                for (int i = 0; i < 8; i++) {
                    if ((uFlag << i & 0x80) == 0) {
                        uCompressBufferSize--;
                        uOrigSize--;
                    }
                    else {
                        int nSize = (pCompressBuffer[--uCompressBufferSize] >> 4 & 0x0F) + 3;
                        uCompressBufferSize--;
                        uOrigSize -= nSize;
                        if (uOrigSize < uCompressBufferSize) {
                            uOrigSafe = uOrigSize;
                            uCompressSafe = uCompressBufferSize;
                            bOver = true;
                            break;
                        }
                    }
                    if (uOrigSize <= 0) {
                        break;
                    }
                }
                if (bOver) {
                    break;
                }
            }
            u32 uCompressedSize = a_uCompressedSize - uCompressSafe;
            u32 uPadOffset = uOrigSafe + uCompressedSize;
            u32 uCompFooterOffset = static_cast<u32>(Align(uPadOffset, 4));
            a_uCompressedSize = uCompFooterOffset + sizeof(CompFooter);
            u32 uTop = a_uCompressedSize - uOrigSafe;
            u32 uBottom = a_uCompressedSize - uPadOffset;
            if (a_uCompressedSize >= a_uUncompressedSize || uTop > 0xFFFFFF) {
                bResult = false;
            }
            else {
                memcpy(a_pCompressed, a_pUncompressed, uOrigSafe);
                memmove(a_pCompressed + uOrigSafe, pCompressBuffer + uCompressSafe, uCompressedSize);
                memset(a_pCompressed + uPadOffset, 0xFF, uCompFooterOffset - uPadOffset);
                CompFooter* pCompFooter = reinterpret_cast<CompFooter*>(a_pCompressed + uCompFooterOffset);
                pCompFooter.bufferTopAndBottom = uTop | (uBottom << 24);
                pCompFooter.originalBottom = a_uUncompressedSize - a_uCompressedSize;
            }
        }
        return bResult;
#endif
    }

    static void InitTable(CompressInfo info, byte* _work) {
        info.WindowPos = 0;
        info.WindowLen = 0;
        info.OffsetTable = (ushort*)_work;
        info.ReversedOffsetTable = ((ushort*)_work) + 4098;
        info.ByteTable = ((ushort*)_work) + 4098 + 4098;
        info.EndTable = ((ushort*)_work) + 4098 + 4098 + 256;
        for (var i = 0; i < 256; i++) {
            info.ByteTable[i] = NEG1;
            info.EndTable[i] = NEG1;
        }
    }

    static int Search(CompressInfo info, byte* _src, ref int offset, int maxSize) {
        if (maxSize < 3) return 0;
        byte* _search = null;
        int size = 2;
        ushort windowPos = info.WindowPos;
        ushort windowLen = info.WindowLen;
        ushort* reversedOffsetTable = info.ReversedOffsetTable;
        for (var nOffset = info.EndTable[*(_src - 1)]; nOffset != NEG1; nOffset = reversedOffsetTable[nOffset]) {
            _search = nOffset < windowPos
                ? _src + windowPos - nOffset
                : _src + windowLen + windowPos - nOffset;
            if (_search - _src < 3) continue;
            if (*(_search - 2) != *(_src - 2) || *(_search - 3) != *(_src - 3)) continue;
            int maxSize2 = (int)Math.Min(maxSize, _search - _src);
            int currentSize = 3;
            while (currentSize < maxSize2 && *(_search - currentSize - 1) == *(_src - currentSize - 1)) currentSize++;
            if (currentSize > size) {
                size = currentSize;
                offset = (int)(_search - _src);
                if (size == maxSize) break;
            }
        }
        return size < 3 ? 0 : size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Slide(CompressInfo info, byte* _src, int size) {
        for (var i = 0; i < size; i++) {
            SlideByte(info, _src--);
        }
    }

    const ushort NEG1 = ushort.MaxValue;
    static void SlideByte(CompressInfo info, byte* _src) {
        byte inData = *(_src - 1);
        ushort insertOffset = 0;
        ushort windowPos = info.WindowPos;
        ushort windowLen = info.WindowLen;
        ushort* offsetTable = info.OffsetTable;
        ushort* reversedOffsetTable = info.ReversedOffsetTable;
        ushort* byteTable = info.ByteTable;
        ushort* endTable = info.EndTable;
        if (windowLen == 4098) {
            byte outData = *(_src + 4097);
            if ((byteTable[outData] = offsetTable[byteTable[outData]]) == NEG1) endTable[outData] = NEG1;
            else reversedOffsetTable[byteTable[outData]] = NEG1;
            insertOffset = windowPos;
        }
        else insertOffset = windowLen;
        ushort offset = endTable[inData];
        if (offset == NEG1) byteTable[inData] = insertOffset;
        else offsetTable[offset] = insertOffset;
        endTable[inData] = insertOffset;
        offsetTable[insertOffset] = NEG1;
        reversedOffsetTable[insertOffset] = offset;
        if (windowLen == 4098) info.WindowPos = (ushort)((windowPos + 1) % 4098);
        else info.WindowLen++;
    }
    static readonly int CompressWorkSize = (4098 + 4098 + 256 + 256) * sizeof(ushort);
}
