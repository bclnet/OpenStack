using System;

namespace SevenZip;

#region CRC

class CRC {
    public static readonly uint[] Table;

    static CRC() {
        Table = new uint[256];
        const uint kPoly = 0xEDB88320;
        for (uint i = 0; i < 256; i++) {
            uint r = i;
            for (int j = 0; j < 8; j++)
                if ((r & 1) != 0)
                    r = (r >> 1) ^ kPoly;
                else
                    r >>= 1;
            Table[i] = r;
        }
    }

    uint _value = 0xFFFFFFFF;

    public void Init() { _value = 0xFFFFFFFF; }

    public void UpdateByte(byte b) {
        _value = Table[(((byte)(_value)) ^ b)] ^ (_value >> 8);
    }

    public void Update(byte[] data, uint offset, uint size) {
        for (uint i = 0; i < size; i++)
            _value = Table[(((byte)(_value)) ^ data[offset + i])] ^ (_value >> 8);
    }

    public uint GetDigest() { return _value ^ 0xFFFFFFFF; }

    static uint CalculateDigest(byte[] data, uint offset, uint size) {
        CRC crc = new CRC();
        // crc.Init();
        crc.Update(data, offset, size);
        return crc.GetDigest();
    }

    static bool VerifyDigest(uint digest, byte[] data, uint offset, uint size) {
        return (CalculateDigest(data, offset, size) == digest);
    }
}

#endregion

#region ICoder

/// <summary>
/// The exception that is thrown when an error in input stream occurs during decoding.
/// </summary>
class DataErrorException : ApplicationException {
    public DataErrorException() : base("Data Error") { }
}

/// <summary>
/// The exception that is thrown when the value of an argument is outside the allowable range.
/// </summary>
class InvalidParamException : ApplicationException {
    public InvalidParamException() : base("Invalid Parameter") { }
}

public interface ICodeProgress {
    /// <summary>
    /// Callback progress.
    /// </summary>
    /// <param name="inSize">
    /// input size. -1 if unknown.
    /// </param>
    /// <param name="outSize">
    /// output size. -1 if unknown.
    /// </param>
    void SetProgress(Int64 inSize, Int64 outSize);
};

public interface ICoder {
    /// <summary>
    /// Codes streams.
    /// </summary>
    /// <param name="inStream">
    /// input Stream.
    /// </param>
    /// <param name="outStream">
    /// output Stream.
    /// </param>
    /// <param name="inSize">
    /// input Size. -1 if unknown.
    /// </param>
    /// <param name="outSize">
    /// output Size. -1 if unknown.
    /// </param>
    /// <param name="progress">
    /// callback progress reference.
    /// </param>
    /// <exception cref="SevenZip.DataErrorException">
    /// if input stream is not valid
    /// </exception>
    void Code(System.IO.Stream inStream, System.IO.Stream outStream,
        Int64 inSize, Int64 outSize, ICodeProgress progress);
};

/*
public interface ICoder2
{
     void Code(ISequentialInStream []inStreams,
            const UInt64 []inSizes, 
            ISequentialOutStream []outStreams, 
            UInt64 []outSizes,
            ICodeProgress progress);
};
*/

/// <summary>
/// Provides the fields that represent properties idenitifiers for compressing.
/// </summary>
public enum CoderPropID {
    /// <summary>
    /// Specifies default property.
    /// </summary>
    DefaultProp = 0,
    /// <summary>
    /// Specifies size of dictionary.
    /// </summary>
    DictionarySize,
    /// <summary>
    /// Specifies size of memory for PPM*.
    /// </summary>
    UsedMemorySize,
    /// <summary>
    /// Specifies order for PPM methods.
    /// </summary>
    Order,
    /// <summary>
    /// Specifies Block Size.
    /// </summary>
    BlockSize,
    /// <summary>
    /// Specifies number of postion state bits for LZMA (0 <= x <= 4).
    /// </summary>
    PosStateBits,
    /// <summary>
    /// Specifies number of literal context bits for LZMA (0 <= x <= 8).
    /// </summary>
    LitContextBits,
    /// <summary>
    /// Specifies number of literal position bits for LZMA (0 <= x <= 4).
    /// </summary>
    LitPosBits,
    /// <summary>
    /// Specifies number of fast bytes for LZ*.
    /// </summary>
    NumFastBytes,
    /// <summary>
    /// Specifies match finder. LZMA: "BT2", "BT4" or "BT4B".
    /// </summary>
    MatchFinder,
    /// <summary>
    /// Specifies the number of match finder cyckes.
    /// </summary>
    MatchFinderCycles,
    /// <summary>
    /// Specifies number of passes.
    /// </summary>
    NumPasses,
    /// <summary>
    /// Specifies number of algorithm.
    /// </summary>
    Algorithm,
    /// <summary>
    /// Specifies the number of threads.
    /// </summary>
    NumThreads,
    /// <summary>
    /// Specifies mode with end marker.
    /// </summary>
    EndMarker
};


public interface ISetCoderProperties {
    void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
};

public interface IWriteCoderProperties {
    void WriteCoderProperties(System.IO.Stream outStream);
}

public interface ISetDecoderProperties {
    void SetDecoderProperties(byte[] properties);
}

#endregion