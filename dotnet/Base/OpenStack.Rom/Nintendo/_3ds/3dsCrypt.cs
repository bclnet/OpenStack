using System;
using System.IO;
using System.Numerics;

namespace OpenStack.Rom.Nintendo._3ds;

public unsafe class _3dsCrypt {
    public static void FEncryptAesCtrCopyFile(Stream dest, Stream src, BigInteger key, BigInteger counter, long srcOffset, long size) => throw new NotImplementedException();
    public static bool FEncryptAesCtrFile(string dataFileName, BigInteger key, BigInteger counter, long dataOffset, long dataSize, bool dataFileAll, long xorOffset) => throw new NotImplementedException();
    public static bool FEncryptXorFile(string dataFileName, string xorFileName) => throw new NotImplementedException();
    public static void FEncryptAesCtrData(byte[] data, long offset, BigInteger key, BigInteger counter, long dataSize, long xorOffset) => throw new NotImplementedException();
    public static void FEncryptAesCtrData(byte* data, long offset, BigInteger key, BigInteger counter, long dataSize, long xorOffset) => throw new NotImplementedException();
}

