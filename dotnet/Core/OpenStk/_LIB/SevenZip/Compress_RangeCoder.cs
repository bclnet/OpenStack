using System;

namespace SevenZip.Compression.RangeCoder;

#region RangeCoder

class Encoder {
    public const uint kTopValue = (1 << 24);

    System.IO.Stream Stream;

    public UInt64 Low;
    public uint Range;
    uint _cacheSize;
    byte _cache;

    long StartPosition;

    public void SetStream(System.IO.Stream stream) {
        Stream = stream;
    }

    public void ReleaseStream() {
        Stream = null;
    }

    public void Init() {
        StartPosition = Stream.Position;

        Low = 0;
        Range = 0xFFFFFFFF;
        _cacheSize = 1;
        _cache = 0;
    }

    public void FlushData() {
        for (int i = 0; i < 5; i++)
            ShiftLow();
    }

    public void FlushStream() {
        Stream.Flush();
    }

    public void CloseStream() {
        Stream.Close();
    }

    public void Encode(uint start, uint size, uint total) {
        Low += start * (Range /= total);
        Range *= size;
        while (Range < kTopValue) {
            Range <<= 8;
            ShiftLow();
        }
    }

    public void ShiftLow() {
        if ((uint)Low < (uint)0xFF000000 || (uint)(Low >> 32) == 1) {
            byte temp = _cache;
            do {
                Stream.WriteByte((byte)(temp + (Low >> 32)));
                temp = 0xFF;
            }
            while (--_cacheSize != 0);
            _cache = (byte)(((uint)Low) >> 24);
        }
        _cacheSize++;
        Low = ((uint)Low) << 8;
    }

    public void EncodeDirectBits(uint v, int numTotalBits) {
        for (int i = numTotalBits - 1; i >= 0; i--) {
            Range >>= 1;
            if (((v >> i) & 1) == 1)
                Low += Range;
            if (Range < kTopValue) {
                Range <<= 8;
                ShiftLow();
            }
        }
    }

    public void EncodeBit(uint size0, int numTotalBits, uint symbol) {
        uint newBound = (Range >> numTotalBits) * size0;
        if (symbol == 0)
            Range = newBound;
        else {
            Low += newBound;
            Range -= newBound;
        }
        while (Range < kTopValue) {
            Range <<= 8;
            ShiftLow();
        }
    }

    public long GetProcessedSizeAdd() {
        return _cacheSize +
            Stream.Position - StartPosition + 4;
        // (long)Stream.GetProcessedSize();
    }
}

class Decoder {
    public const uint kTopValue = (1 << 24);
    public uint Range;
    public uint Code;
    // public Buffer.InBuffer Stream = new Buffer.InBuffer(1 << 16);
    public System.IO.Stream Stream;

    public void Init(System.IO.Stream stream) {
        // Stream.Init(stream);
        Stream = stream;

        Code = 0;
        Range = 0xFFFFFFFF;
        for (int i = 0; i < 5; i++)
            Code = (Code << 8) | (byte)Stream.ReadByte();
    }

    public void ReleaseStream() {
        // Stream.ReleaseStream();
        Stream = null;
    }

    public void CloseStream() {
        Stream.Close();
    }

    public void Normalize() {
        while (Range < kTopValue) {
            Code = (Code << 8) | (byte)Stream.ReadByte();
            Range <<= 8;
        }
    }

    public void Normalize2() {
        if (Range < kTopValue) {
            Code = (Code << 8) | (byte)Stream.ReadByte();
            Range <<= 8;
        }
    }

    public uint GetThreshold(uint total) {
        return Code / (Range /= total);
    }

    public void Decode(uint start, uint size, uint total) {
        Code -= start * Range;
        Range *= size;
        Normalize();
    }

    public uint DecodeDirectBits(int numTotalBits) {
        uint range = Range;
        uint code = Code;
        uint result = 0;
        for (int i = numTotalBits; i > 0; i--) {
            range >>= 1;
            /*
            result <<= 1;
            if (code >= range)
            {
                code -= range;
                result |= 1;
            }
            */
            uint t = (code - range) >> 31;
            code -= range & (t - 1);
            result = (result << 1) | (1 - t);

            if (range < kTopValue) {
                code = (code << 8) | (byte)Stream.ReadByte();
                range <<= 8;
            }
        }
        Range = range;
        Code = code;
        return result;
    }

    public uint DecodeBit(uint size0, int numTotalBits) {
        uint newBound = (Range >> numTotalBits) * size0;
        uint symbol;
        if (Code < newBound) {
            symbol = 0;
            Range = newBound;
        }
        else {
            symbol = 1;
            Code -= newBound;
            Range -= newBound;
        }
        Normalize();
        return symbol;
    }

    // ulong GetProcessedSize() {return Stream.GetProcessedSize(); }
}

#endregion

#region RangeCoderBit

struct BitEncoder {
    public const int kNumBitModelTotalBits = 11;
    public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
    const int kNumMoveBits = 5;
    const int kNumMoveReducingBits = 2;
    public const int kNumBitPriceShiftBits = 6;

    uint Prob;

    public void Init() { Prob = kBitModelTotal >> 1; }

    public void UpdateModel(uint symbol) {
        if (symbol == 0)
            Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
        else
            Prob -= (Prob) >> kNumMoveBits;
    }

    public void Encode(Encoder encoder, uint symbol) {
        // encoder.EncodeBit(Prob, kNumBitModelTotalBits, symbol);
        // UpdateModel(symbol);
        uint newBound = (encoder.Range >> kNumBitModelTotalBits) * Prob;
        if (symbol == 0) {
            encoder.Range = newBound;
            Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
        }
        else {
            encoder.Low += newBound;
            encoder.Range -= newBound;
            Prob -= (Prob) >> kNumMoveBits;
        }
        if (encoder.Range < Encoder.kTopValue) {
            encoder.Range <<= 8;
            encoder.ShiftLow();
        }
    }

    private static UInt32[] ProbPrices = new UInt32[kBitModelTotal >> kNumMoveReducingBits];

    static BitEncoder() {
        const int kNumBits = (kNumBitModelTotalBits - kNumMoveReducingBits);
        for (int i = kNumBits - 1; i >= 0; i--) {
            UInt32 start = (UInt32)1 << (kNumBits - i - 1);
            UInt32 end = (UInt32)1 << (kNumBits - i);
            for (UInt32 j = start; j < end; j++)
                ProbPrices[j] = ((UInt32)i << kNumBitPriceShiftBits) +
                    (((end - j) << kNumBitPriceShiftBits) >> (kNumBits - i - 1));
        }
    }

    public uint GetPrice(uint symbol) {
        return ProbPrices[(((Prob - symbol) ^ ((-(int)symbol))) & (kBitModelTotal - 1)) >> kNumMoveReducingBits];
    }
    public uint GetPrice0() { return ProbPrices[Prob >> kNumMoveReducingBits]; }
    public uint GetPrice1() { return ProbPrices[(kBitModelTotal - Prob) >> kNumMoveReducingBits]; }
}

struct BitDecoder {
    public const int kNumBitModelTotalBits = 11;
    public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
    const int kNumMoveBits = 5;

    uint Prob;

    public void UpdateModel(int numMoveBits, uint symbol) {
        if (symbol == 0)
            Prob += (kBitModelTotal - Prob) >> numMoveBits;
        else
            Prob -= (Prob) >> numMoveBits;
    }

    public void Init() { Prob = kBitModelTotal >> 1; }

    public uint Decode(RangeCoder.Decoder rangeDecoder) {
        uint newBound = (uint)(rangeDecoder.Range >> kNumBitModelTotalBits) * (uint)Prob;
        if (rangeDecoder.Code < newBound) {
            rangeDecoder.Range = newBound;
            Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
            if (rangeDecoder.Range < Decoder.kTopValue) {
                rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                rangeDecoder.Range <<= 8;
            }
            return 0;
        }
        else {
            rangeDecoder.Range -= newBound;
            rangeDecoder.Code -= newBound;
            Prob -= (Prob) >> kNumMoveBits;
            if (rangeDecoder.Range < Decoder.kTopValue) {
                rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                rangeDecoder.Range <<= 8;
            }
            return 1;
        }
    }
}

#endregion

#region RangeCoderBitTree

struct BitTreeEncoder {
    BitEncoder[] Models;
    int NumBitLevels;

    public BitTreeEncoder(int numBitLevels) {
        NumBitLevels = numBitLevels;
        Models = new BitEncoder[1 << numBitLevels];
    }

    public void Init() {
        for (uint i = 1; i < (1 << NumBitLevels); i++)
            Models[i].Init();
    }

    public void Encode(Encoder rangeEncoder, UInt32 symbol) {
        UInt32 m = 1;
        for (int bitIndex = NumBitLevels; bitIndex > 0;) {
            bitIndex--;
            UInt32 bit = (symbol >> bitIndex) & 1;
            Models[m].Encode(rangeEncoder, bit);
            m = (m << 1) | bit;
        }
    }

    public void ReverseEncode(Encoder rangeEncoder, UInt32 symbol) {
        UInt32 m = 1;
        for (UInt32 i = 0; i < NumBitLevels; i++) {
            UInt32 bit = symbol & 1;
            Models[m].Encode(rangeEncoder, bit);
            m = (m << 1) | bit;
            symbol >>= 1;
        }
    }

    public UInt32 GetPrice(UInt32 symbol) {
        UInt32 price = 0;
        UInt32 m = 1;
        for (int bitIndex = NumBitLevels; bitIndex > 0;) {
            bitIndex--;
            UInt32 bit = (symbol >> bitIndex) & 1;
            price += Models[m].GetPrice(bit);
            m = (m << 1) + bit;
        }
        return price;
    }

    public UInt32 ReverseGetPrice(UInt32 symbol) {
        UInt32 price = 0;
        UInt32 m = 1;
        for (int i = NumBitLevels; i > 0; i--) {
            UInt32 bit = symbol & 1;
            symbol >>= 1;
            price += Models[m].GetPrice(bit);
            m = (m << 1) | bit;
        }
        return price;
    }

    public static UInt32 ReverseGetPrice(BitEncoder[] Models, UInt32 startIndex,
        int NumBitLevels, UInt32 symbol) {
        UInt32 price = 0;
        UInt32 m = 1;
        for (int i = NumBitLevels; i > 0; i--) {
            UInt32 bit = symbol & 1;
            symbol >>= 1;
            price += Models[startIndex + m].GetPrice(bit);
            m = (m << 1) | bit;
        }
        return price;
    }

    public static void ReverseEncode(BitEncoder[] Models, UInt32 startIndex,
        Encoder rangeEncoder, int NumBitLevels, UInt32 symbol) {
        UInt32 m = 1;
        for (int i = 0; i < NumBitLevels; i++) {
            UInt32 bit = symbol & 1;
            Models[startIndex + m].Encode(rangeEncoder, bit);
            m = (m << 1) | bit;
            symbol >>= 1;
        }
    }
}

struct BitTreeDecoder {
    BitDecoder[] Models;
    int NumBitLevels;

    public BitTreeDecoder(int numBitLevels) {
        NumBitLevels = numBitLevels;
        Models = new BitDecoder[1 << numBitLevels];
    }

    public void Init() {
        for (uint i = 1; i < (1 << NumBitLevels); i++)
            Models[i].Init();
    }

    public uint Decode(RangeCoder.Decoder rangeDecoder) {
        uint m = 1;
        for (int bitIndex = NumBitLevels; bitIndex > 0; bitIndex--)
            m = (m << 1) + Models[m].Decode(rangeDecoder);
        return m - ((uint)1 << NumBitLevels);
    }

    public uint ReverseDecode(RangeCoder.Decoder rangeDecoder) {
        uint m = 1;
        uint symbol = 0;
        for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++) {
            uint bit = Models[m].Decode(rangeDecoder);
            m <<= 1;
            m += bit;
            symbol |= (bit << bitIndex);
        }
        return symbol;
    }

    public static uint ReverseDecode(BitDecoder[] Models, UInt32 startIndex,
        RangeCoder.Decoder rangeDecoder, int NumBitLevels) {
        uint m = 1;
        uint symbol = 0;
        for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++) {
            uint bit = Models[startIndex + m].Decode(rangeDecoder);
            m <<= 1;
            m += bit;
            symbol |= (bit << bitIndex);
        }
        return symbol;
    }
}

#endregion