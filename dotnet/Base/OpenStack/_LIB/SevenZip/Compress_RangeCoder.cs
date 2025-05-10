using System;

namespace SevenZip.Compression.RangeCoder;

#region RangeCoder

internal class Decoder {
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

internal struct BitDecoder {
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

internal struct BitTreeDecoder {
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