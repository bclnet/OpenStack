using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;

namespace OpenStack.Rom.Nintendo._3ds;

public unsafe class _3dsCrypt {
    public static void FEncryptAesCtrCopyFile(Stream dest, Stream src, BigInteger key, BigInteger counter, long srcOffset, long size) {
        var crypt = Aes.Create();
        var key2 = key.ToByteArray(); Array.Resize(ref key2, 16); crypt.Key = key2;
        var iv2 = counter.ToByteArray(); Array.Resize(ref iv2, 16); crypt.IV = iv2;
        crypt.BlockSize = 128;
        src.Seek(srcOffset, SeekOrigin.Begin);
        using var s = new MemoryStream();
        using var w = new CryptoStream(s, crypt.CreateEncryptor(), CryptoStreamMode.Write);
        src.CopyTo(dest);
    }

    public static bool FEncryptAesCtrFile(string dataFileName, BigInteger key, BigInteger counter, long dataOffset, long dataSize, bool dataFileAll, long xorOffset) => throw new NotImplementedException();
    public static bool FEncryptXorFile(string dataFileName, string xorFileName) => throw new NotImplementedException();
    public static void FEncryptAesCtrData(byte[] data, long offset, BigInteger key, BigInteger counter, long dataSize, long xorOffset) => throw new NotImplementedException();
    public static void FEncryptAesCtrData(byte* data, long offset, BigInteger key, BigInteger counter, long dataSize, long xorOffset) => throw new NotImplementedException();
}

