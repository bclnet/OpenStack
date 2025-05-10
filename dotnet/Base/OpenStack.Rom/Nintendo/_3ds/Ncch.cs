using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Console;

namespace OpenStack.Rom.Nintendo._3ds;
using static _3dsCrypt;
using static ExeFs;
using static ExtHeader;
using static Ncch.Flag;
using static RomFs;
using static Util;

public enum XFileType {
    Unknown,
    Cfa
}

public unsafe class Ncch {

    #region Headers

    [StructLayout(LayoutKind.Sequential)]
    public struct NcchCommonHeader {
        public uint Signature;
        public uint ContentSize;
        public ulong PartitionId;
        public ushort MakerCode;
        public ushort NcchVersion;
        public fixed byte Reserved0[4];
        public ulong ProgramId;
        public fixed byte Reserved1[16];
        public fixed byte LogoRegionHash[32];
        public fixed byte ProductCode[16];
        public fixed byte ExtendedHeaderHash[32];
        public uint ExtendedHeaderSize;
        public fixed byte Reserved2[4];
        public fixed byte Flags[8];
        public uint PlainRegionOffset;
        public uint PlainRegionSize;
        public uint LogoRegionOffset;
        public uint LogoRegionSize;
        public uint ExeFsOffset;
        public uint ExeFsSize;
        public uint ExeFsHashRegionSize;
        public fixed byte Reserved4[4];
        public uint RomFsOffset;
        public uint RomFsSize;
        public uint RomFsHashRegionSize;
        public fixed byte Reserved5[4];
        public fixed byte ExeFsSuperBlockHash[32];
        public fixed byte RomFsSuperBlockHash[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcchHeader {
        public fixed byte RSASignature[256];
        public NcchCommonHeader Ncch;
    }

    #endregion

    #region Enums

    const int kEncrypt7x = 3;
    const int kContentPlatform = 4;
    const int kContentType = 5;
    const int kSizeType = 6;
    const int kFlag = 7;

    [Flags]
    public enum Flag : uint {
        FixedCryptoKey = 1 << 0,
        NoMountRomFs = 1 << 1,
        NoEncrypto = 1 << 2,
        FlagExtKey = 1 << 5
    }

    public enum kFormType { NotAssign, SimpleContent, ExecutableContentWithoutRomFs, ExecutableContent }

    public enum kEncryptMode { None, NotEncrypt, FixedKey, Auto, AesCtr, Xor }

    const int kEncryptKeyOld = 0;
    const int kEncryptKeyNew = 1;

    public enum kAesCtrType { ExtendedHeader = 1, ExeFs, RomFs }

    const int kOffsetSizeExtendedHeader = 0;
    const int kOffsetSizeLogoRegion = 1;
    const int kOffsetSizePlainRegion = 2;
    const int kOffsetSizeExeFs = 3;
    const int kOffsetSizeRomFs = 4;
    const int kOffsetSizeCount = 5;

    #endregion

    static readonly BigInteger DevSlot0x18KeyX = new(ToBytes("304BF1468372EE64115EBD4093D84276"));
    static readonly BigInteger DevSlot0x1BKeyX = new(ToBytes("6C8B2944A0726035F941DFC018524FB6"));
    static readonly BigInteger DevSlot0x25KeyX = new(ToBytes("81907A4B6F1B47323A677974CE4AD71B"));
    static readonly BigInteger DevSlot0x2CKeyX = new(ToBytes("510207515507CBB18E243DCB85E23A1D"));
    static readonly BigInteger RetailSlot0x18KeyX = new(ToBytes("82E9C9BEBFB8BDB875ECC0A07D474374"));
    static readonly BigInteger RetailSlot0x1BKeyX = new(ToBytes("45AD04953992C7C893724A9A7BCE6182"));
    static readonly BigInteger RetailSlot0x25KeyX = new(ToBytes("CEE7D8AB30C00DAE850EF5E382AC5AF3"));
    static readonly BigInteger RetailSlot0x2CKeyX = new(ToBytes("B98E95CECA3E4D171F76A94DE934C053"));
    static readonly BigInteger SystemFixedKey = new(ToBytes("527CE630A9CA305F3696F3CDE954194B"));
    static readonly BigInteger NormalFixedKey = new(ToBytes("00000000000000000000000000000000"));
    public XFileType FileType = XFileType.Unknown;
    //public string FileName;
    //public Stream S = null;
    public bool Verbose = false;
    public string HeaderFileName;
    public kEncryptMode EncryptMode = kEncryptMode.None;
    public bool RemoveExtKey = true;
    public bool Dev = false;
    readonly BigInteger[] Key = new BigInteger[2];
    public int DownloadBegin = -1;
    public int DownloadEnd = -1;
    public string ExtendedHeaderFileName;
    public string LogoRegionFileName;
    public string PlainRegionFileName;
    public string ExeFsFileName;
    public string RomFsFileName;
    public long Offset = 0;
    public NcchHeader Header = new();
    long MediaUnitSize = 1 << 9;
    public readonly long[] OffsetAndSize = new long[kOffsetSizeCount * 2];
    bool AlignToBlockSize = false;
    Dictionary<string, byte[]> ExtKey;
    int KeyIndex = 0;
    BigInteger Counter;

    //public void SetFileType(EFileType a_eFileType) => FileType = a_eFileType;
    //public void SetFileName(string a_sFileName) => FileName = a_sFileName;
    //public void SetVerbose(bool a_bVerbose) => Verbose = a_bVerbose;
    //public void SetHeaderFileName(string a_sHeaderFileName) => HeaderFileName = a_sHeaderFileName;
    //public void SetEncryptMode(EncryptMode_ a_nEncryptMode) => EncryptMode = a_nEncryptMode;
    //public void SetRemoveExtKey(bool a_bRemoveExtKey) => RemoveExtKey = a_bRemoveExtKey;
    //public void SetDev(bool a_bDev) => Dev = a_bDev;
    //public void SetDownloadBegin(int a_nDownloadBegin) => DownloadBegin = a_nDownloadBegin;
    //public void SetDownloadEnd(int a_nDownloadEnd) => DownloadEnd = a_nDownloadEnd;
    //public void SetExtendedHeaderFileName(string a_sExtendedHeaderFileName) => ExtendedHeaderFileName = a_sExtendedHeaderFileName;
    //public void SetLogoRegionFileName(string a_sLogoRegionFileName) => LogoRegionFileName = a_sLogoRegionFileName;
    //public void SetPlainRegionFileName(string a_sPlainRegionFileName) => PlainRegionFileName = a_sPlainRegionFileName;
    //public void SetExeFsFileName(string a_sExeFsFileName) => ExeFsFileName = a_sExeFsFileName;
    //public void SetRomFsFileName(string a_sRomFsFileName) => RomFsFileName = a_sRomFsFileName;
    //public void SetFilePtr(Stream a_fpNcch) => FilePtr = a_fpNcch;
    //public void SetOffset(long a_nOffset) => Offset = a_nOffset;
    //public NcchHeader GetNcchHeader() => Header;
    //public long[] GetOffsetAndSize() => OffsetAndSize;

    public bool ExtractFile(Stream r) {
        try {
            var result = true;
            r.Read(ref Header, 0, sizeof(NcchHeader));
            CalculateMediaUnitSize();
            CalculateOffsetSize();
            CalculateKey();
            if (!ExtractFile(r, HeaderFileName, 0, sizeof(NcchHeader), true, "ncch header")) result = false;
            KeyIndex = kEncryptKeyOld;
            CalculateCounter(kAesCtrType.ExtendedHeader);
            if (!ExtractFile(r, ExtendedHeaderFileName, OffsetAndSize[kOffsetSizeExtendedHeader * 2], OffsetAndSize[kOffsetSizeExtendedHeader * 2 + 1], false, "extendedheader")) result = false;
            if (!ExtractFile(r, LogoRegionFileName, OffsetAndSize[kOffsetSizeLogoRegion * 2], OffsetAndSize[kOffsetSizeLogoRegion * 2 + 1], true, "logoregion")) result = false;
            if (!ExtractFile(r, PlainRegionFileName, OffsetAndSize[kOffsetSizePlainRegion * 2], OffsetAndSize[kOffsetSizePlainRegion * 2 + 1], true, "plainregion")) result = false;
            if (!string.IsNullOrEmpty(ExeFsFileName)) {
                if (OffsetAndSize[kOffsetSizeExeFs * 2 + 1] != 0) {
                    r.Seek(OffsetAndSize[kOffsetSizeExeFs * 2], SeekOrigin.Begin);
                    ExeFsSuperBlock superBlock = new();
                    r.Read(ref superBlock, 0, sizeof(ExeFsSuperBlock));
                    CalculateCounter(kAesCtrType.ExeFs);
                    if (EncryptMode == kEncryptMode.FixedKey || EncryptMode == kEncryptMode.Auto)
                        FEncryptAesCtrData((byte*)&superBlock, 0, Key[kEncryptKeyOld], Counter, sizeof(ExeFsSuperBlock), 0);
                    if (ExeFs.IsExeFsSuperBlock(ref superBlock)) {
                        r.Seek(OffsetAndSize[kOffsetSizeExeFs * 2], SeekOrigin.Begin);
                        var exeFs = new byte[OffsetAndSize[kOffsetSizeExeFs * 2 + 1]];
                        r.Read(exeFs, 0, (int)OffsetAndSize[kOffsetSizeExeFs * 2 + 1]);
                        if (EncryptMode == kEncryptMode.FixedKey || EncryptMode == kEncryptMode.Auto) {
                            var xorOffset = 0L;
                            FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, sizeof(ExeFsSuperBlock), xorOffset);
                            xorOffset += sizeof(ExeFsSuperBlock);
                            FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyNew], Counter, superBlock.Header0.Size, xorOffset);
                            xorOffset += superBlock.Header0.Size;
                            FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, OffsetAndSize[kOffsetSizeExeFs * 2 + 1] - xorOffset, xorOffset);
                        }
                        try {
                            using var w = File.Open(ExeFsFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
                            if (Verbose) WriteLine($"save: {ExeFsFileName}");
                            w.Write(exeFs, 0, (int)OffsetAndSize[kOffsetSizeExeFs * 2 + 1]);
                        }
                        catch (IOException) { result = false; }
                    }
                    else {
                        result = false;
                        ExtractFile(r, ExeFsFileName, OffsetAndSize[kOffsetSizeExeFs * 2], OffsetAndSize[kOffsetSizeExeFs * 2 + 1], true, "exefs");
                    }
                }
                else if (Verbose) WriteLine($"INFO: exefs is not exists, {ExeFsFileName} will not be create");
            }
            else if (OffsetAndSize[kOffsetSizeExeFs * 2 + 1] != 0 && Verbose) WriteLine("INFO: exefs is not extract");
            KeyIndex = kEncryptKeyNew;
            CalculateCounter(kAesCtrType.RomFs);
            if (!ExtractFile(r, RomFsFileName, OffsetAndSize[kOffsetSizeRomFs * 2], OffsetAndSize[kOffsetSizeRomFs * 2 + 1], false, "romfs")) result = false;
            return result;
        }
        catch (IOException) { return false; }
    }

    public bool CreateFile(Stream w) {
        try {
            var result = true;
            if (!CreateHeader(w)) return false;
            CalculateMediaUnitSize();
            CalculateAlignment();
            CalculateKey();
            if (!CreateExtendedHeader(w)) result = false;
            AlignFileSize(w, MediaUnitSize);
            if (!CreateLogoRegion(w)) result = false;
            AlignFileSize(w, MediaUnitSize);
            if (!CreatePlainRegion(w)) result = false;
            AlignFileSize(w, MediaUnitSize);
            if (!CreateExeFs(w)) result = false;
            AlignFileSize(w, AlignToBlockSize ? BlockSize : MediaUnitSize);
            if (!CreateRomFs(w)) result = false;
            AlignFileSize(w, AlignToBlockSize ? BlockSize : MediaUnitSize);
            w.Seek(0, SeekOrigin.Begin);
            w.Write(ref Header, 0, sizeof(NcchHeader));
            return result;
        }
        catch (IOException) { return false; }
    }

    public bool Download(bool readExtKey = true) {
        if (readExtKey) ReadExtKey();
        //    CUrlManager urlManager;
        //    var count = DownloadEnd - DownloadBegin + 1;
        //    var downloadCount = 0;
        //    var totalLoadCount = 0;
        //    var loadCount = 0;
        //    while (downloadCount != count) {
        //        while (totalLoadCount != count && loadCount < 256) {
        //            size_t uUserData = DownloadBegin + totalLoadCount;
        //            CUrl* pUrl = urlManager.HttpsGet(Format($"https://kagiya-ctr.cdn.nintendo.net/title/0x000400000%05X00/ext_key?country=JP", DownloadBegin + totalLoadCount), *this, &CNcch::onHttpsGetExtKey, reinterpret_cast<void*>(uUserData));
        //            if (pUrl == nullptr) {
        //                urlManager.Cleanup();
        //                return false;
        //            }
        //            totalLoadCount++;
        //            loadCount++;
        //        }
        //        while (urlManager.GetCount() != 0) {
        //            u32 uCount0 = urlManager.GetCount();
        //            urlManager.Perform();
        //            u32 uCount1 = urlManager.GetCount();
        //            if (uCount1 != uCount0) {
        //                downloadCount += uCount0 - uCount1;
        //                loadCount -= uCount0 - uCount1;
        //                if (Verbose) {
        //                    UPrintf(USTR("download: %u/%u/%u\n"), downloadCount, totalLoadCount, count);
        //                }
        //                if (totalLoadCount != count) {
        //                    break;
        //                }
        //            }
        //        }
        //    }
        return WriteExtKey();
    }

    public void Analyze(Stream r) {
        if (r == null) return;
        var filePos = r.Position;
        r.Seek(Offset, SeekOrigin.Begin);
        r.Read(ref Header, 0, sizeof(NcchHeader));
        CalculateMediaUnitSize();
        CalculateOffsetSize();
        if (FileType == XFileType.Cfa)
            for (var i = kOffsetSizeExtendedHeader; i < kOffsetSizeRomFs; i++) {
                OffsetAndSize[i * 2] = 0;
                OffsetAndSize[i * 2 + 1] = 0;
            }
        for (var i = kOffsetSizeRomFs - 1; i >= kOffsetSizeExtendedHeader; i--)
            if (OffsetAndSize[i * 2] == 0 && OffsetAndSize[i * 2 + 1] == 0)
                OffsetAndSize[i * 2] = OffsetAndSize[(i + 1) * 2];
        for (var i = kOffsetSizeExtendedHeader + 1; i < kOffsetSizeCount; i++)
            if (OffsetAndSize[i * 2] == 0 && OffsetAndSize[i * 2 + 1] == 0)
                OffsetAndSize[i * 2] = OffsetAndSize[(i - 1) * 2] + OffsetAndSize[(i - 1) * 2 + 1];
        r.Seek(filePos, SeekOrigin.Begin);
    }

    public static bool IsCxiFile(Stream s) {
        try {
            NcchHeader ncchHeader = new();
            s.Read(ref ncchHeader, 0, sizeof(NcchHeader));
            s.Seek(0, SeekOrigin.Begin);
            var result = ncchHeader.Ncch.Signature == Signature;
            if (result)
                switch ((kFormType)(ncchHeader.Ncch.Flags[kContentType] & 3)) {
                    case kFormType.ExecutableContentWithoutRomFs:
                    case kFormType.ExecutableContent: break;
                    default: result = false; break;
                }
            return result;
        }
        catch (IOException) { return false; }
    }

    public static bool IsCfaFile(Stream s) {
        try {
            NcchHeader ncchHeader = new();
            s.Read(ref ncchHeader, 0, sizeof(NcchHeader));
            s.Seek(0, SeekOrigin.Begin);
            var result = ncchHeader.Ncch.Signature == Signature;
            if (result)
                switch ((kFormType)(ncchHeader.Ncch.Flags[kContentType] & 3)) {
                    case kFormType.SimpleContent: break;
                    default: result = false; break;
                }
            return result;
        }
        catch (IOException) { return false; }
    }

    public static readonly uint Signature = 0x4843434e; //: NCCH
    public static readonly int BlockSize = 0x1000;

    void CalculateMediaUnitSize() => MediaUnitSize = 1L << (Header.Ncch.Flags[kSizeType] + 9);

    void CalculateOffsetSize() {
        OffsetAndSize[kOffsetSizeExtendedHeader * 2] = sizeof(NcchHeader);
        OffsetAndSize[kOffsetSizeExtendedHeader * 2 + 1] = Header.Ncch.ExtendedHeaderSize == sizeof(NcchExtendedHeader) ? sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended) : 0;
        OffsetAndSize[kOffsetSizeLogoRegion * 2] = Header.Ncch.LogoRegionOffset * MediaUnitSize;
        OffsetAndSize[kOffsetSizeLogoRegion * 2 + 1] = Header.Ncch.LogoRegionSize * MediaUnitSize;
        OffsetAndSize[kOffsetSizePlainRegion * 2] = Header.Ncch.PlainRegionOffset * MediaUnitSize;
        OffsetAndSize[kOffsetSizePlainRegion * 2 + 1] = Header.Ncch.PlainRegionSize * MediaUnitSize;
        OffsetAndSize[kOffsetSizeExeFs * 2] = Header.Ncch.ExeFsOffset * MediaUnitSize;
        OffsetAndSize[kOffsetSizeExeFs * 2 + 1] = Header.Ncch.ExeFsSize * MediaUnitSize;
        OffsetAndSize[kOffsetSizeRomFs * 2] = Header.Ncch.RomFsOffset * MediaUnitSize;
        OffsetAndSize[kOffsetSizeRomFs * 2 + 1] = Header.Ncch.RomFsSize * MediaUnitSize;
    }

    void CalculateAlignment() => AlignToBlockSize = Header.Ncch.ContentSize % 8 == 0 && Header.Ncch.RomFsOffset % 8 == 0 && Header.Ncch.RomFsSize % 8 == 0;

    void CalculateKey() {
        if (EncryptMode == kEncryptMode.Auto) {
            if ((Header.Ncch.Flags[kFlag] & (int)NoEncrypto) != 0) EncryptMode = kEncryptMode.NotEncrypt;
            else if ((Header.Ncch.Flags[kFlag] & (int)FixedCryptoKey) != 0) EncryptMode = kEncryptMode.FixedKey;
        }
        if (EncryptMode == kEncryptMode.NotEncrypt) return;
        else if (EncryptMode == kEncryptMode.FixedKey) {
            var programIdHigh = (uint)(Header.Ncch.ProgramId >> 32);
            Key[kEncryptKeyOld] = (programIdHigh >> 14) == 0x10 && (programIdHigh & 0x10) != 0 ? SystemFixedKey : NormalFixedKey;
            Key[kEncryptKeyNew] = Key[kEncryptKeyOld];
            return;
        }
        var keyX = new[] { Dev ? DevSlot0x2CKeyX : RetailSlot0x2CKeyX, Dev ? DevSlot0x2CKeyX : RetailSlot0x2CKeyX };
        switch (Header.Ncch.Flags[kEncrypt7x]) {
            case 0x01: keyX[kEncryptKeyNew] = Dev ? DevSlot0x25KeyX : RetailSlot0x25KeyX; break;
            case 0x0A: keyX[kEncryptKeyNew] = Dev ? DevSlot0x18KeyX : RetailSlot0x18KeyX; break;
            case 0x0B: keyX[kEncryptKeyNew] = Dev ? DevSlot0x1BKeyX : RetailSlot0x1BKeyX; break;
            default: break;
        }
        var keyYb = new byte[16]; fixed (byte* _ = Header.RSASignature) Unsafe.Copy(ref keyYb, _);
        var keyY = new BigInteger[] { new(keyYb), new() };
        while ((Header.Ncch.Flags[kFlag] & (int)FlagExtKey) != 0) {
            ReadExtKey();
            var programId = BitConverter.GetBytes(Header.Ncch.ProgramId);
            Array.Reverse(programId);
            var sProgramId = FromBytes(programId);
            if (!ExtKey.TryGetValue(sProgramId, out var extKey)) {
                DownloadBegin = DownloadEnd = int.Parse(sProgramId.Substring(9, 5), NumberStyles.HexNumber);
                if (!Download(false)) WriteLine("INFO: download failed");
                if (!ExtKey.TryGetValue(sProgramId, out extKey)) { WriteLine($"ERROR: can not find ext key for {sProgramId}\n"); break; }
            }
            if (extKey.Length != 16) { WriteLine($"ERROR: can not find ext key for {sProgramId}\n"); break; }
            //if (extKey.Length != 32 || extKey.Any(x => "0123456789ABCDEFabcdef".IndexOf(x) != -1)) { WriteLine($"ERROR: can not find ext key for {sProgramId}\n"); break; }
            Array.Reverse(programId);
            var bigNum = new BigInteger([.. extKey, .. programId]);
            //u8 uBytes[32] = { };
            //bigNum.Tr
            //bigNum.GetBytes(uBytes, 24);
            //u8 uSHA256[32] = { };
            //SHA256(uBytes, 24, uSHA256);
            //fixed (byte* _ = Header.Ncch.Reserved0) Unsafe.Copy(ref buf, _);
            //u8* pReserved0 = Header.Ncch.Reserved0;
            //for (int i = 0; i < static_cast<int>(SDW_ARRAY_COUNT(Header.Ncch.Reserved0)); i++) {
            //    if (pReserved0[i] != uSHA256[i]) {
            //        UPrintf(USTR("ERROR: ext key verification failed\n\n"));
            //        return;
            //    }
            //}
            bigNum = new BigInteger([.. keyYb, .. extKey]);
            //bigNum.GetBytes(uBytes, 32);
            //SHA256(uBytes, 32, uSHA256);
            //sKeyY.clear();
            //for (int i = 0; i < 16; i++) {
            //    sKeyY += Format("%02X", uSHA256[i]);
            //}
            break;
        }
        //keyY[kEncryptKeyNew] = keyY;
        //Key[kEncryptKeyOld] = ((keyX[kEncryptKeyOld].Crol(2, 128) ^ keyY[kEncryptKeyOld]) + "1FF9E9AAC5FE0408024591DC5D52768A").Crol(87, 128);
        //Key[kEncryptKeyNew] = ((keyX[kEncryptKeyNew].Crol(2, 128) ^ keyY[kEncryptKeyNew]) + "1FF9E9AAC5FE0408024591DC5D52768A").Crol(87, 128);
    }

    void ReadExtKey() {
        string txt = null;
        try {
            var extKeyPath = "MODULE/ext_key.txt";
            using var s = File.Open(extKeyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            s.Seek(0, SeekOrigin.End);
            var size = (int)s.Position;
            s.Seek(0, SeekOrigin.Begin);
            var buf = new byte[size + 1];
            s.Read(buf, 0, size);
            buf[size] = (byte)'\0';
            txt = new string(MemoryMarshal.Cast<byte, char>(buf));
        }
        catch (IOException) { return; }
        foreach (var z in txt.Split("\r\n")) {
            var line = z.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;
            var vals = line.Split(" ");
            if (vals.Length != 2) { WriteLine($"INFO: unknown ext key record {line}"); continue; }
            else if (!ExtKey.TryAdd(vals[0], ToBytes(vals[1]))) WriteLine($"INFO: multiple ext key for {vals[0]}");
        }
    }

    bool WriteExtKey() {
        try {
            var extKeyPath = "MODULE/ext_key.txt";
            using var s = File.Open(extKeyPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            foreach (var z in ExtKey)
                s.WriteBytes(Encoding.ASCII.GetBytes($"{z.Key} {FromBytes(z.Value)}\r\n"));
            return true;
        }
        catch (IOException) { return false; }
    }

    void CalculateCounter(kAesCtrType type) {
        Counter = 0;
        var partitionId = BitConverter.GetBytes(Header.Ncch.PartitionId);
        if (Header.Ncch.NcchVersion == 2 || Header.Ncch.NcchVersion == 0) {
            Array.Reverse(partitionId);
            Counter = new BigInteger(partitionId);
            Counter = (Counter << 8 | (int)type) << 56;
        }
        else if (Header.Ncch.NcchVersion == 1) {
            Counter = new BigInteger(partitionId);
            Counter <<= 64;
            var offset = 0L;
            switch (type) {
                case kAesCtrType.ExtendedHeader: offset = sizeof(NcchHeader); break;
                case kAesCtrType.ExeFs: offset = Header.Ncch.ExeFsOffset * MediaUnitSize; break;
                case kAesCtrType.RomFs: offset = Header.Ncch.RomFsOffset * MediaUnitSize; break;
                default: break;
            }
            Counter += (uint)offset;
        }
    }

    bool ExtractFile(Stream s, string fileName, long offset, long size, bool plainData, string type) {
        var result = true;
        if (!string.IsNullOrEmpty(fileName)) {
            if (size != 0)
                try {
                    using var w = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Write);
                    if (Verbose) WriteLine($"save: {fileName}");
                    if (plainData || EncryptMode == kEncryptMode.NotEncrypt) w.CopyFile(s, offset, size);
                    else FEncryptAesCtrCopyFile(w, s, Key[KeyIndex], Counter, offset, size);
                }
                catch (IOException) { result = false; }
            else if (Verbose) WriteLine($"INFO: {type} does not exist, {fileName} will not be create");
        }
        else if (size != 0 && Verbose) WriteLine($"INFO: {type} is not extract");
        return result;
    }

    bool CreateHeader(Stream w) {
        try {
            using var r = File.Open(HeaderFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) WriteLine($"load: {HeaderFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            if (fileSize < sizeof(NcchHeader)) { WriteLine("ERROR: ncch header is too short\n"); return false; }
            r.Seek(0, SeekOrigin.Begin);
            r.Read(ref Header, 0, sizeof(NcchHeader));
            unchecked {
                if (EncryptMode == kEncryptMode.NotEncrypt) {
                    Header.Ncch.Flags[kFlag] |= (int)NoEncrypto;
                }
                else if (EncryptMode == kEncryptMode.FixedKey) {
                    Header.Ncch.Flags[kFlag] &= (byte)~(int)NoEncrypto;
                    Header.Ncch.Flags[kFlag] |= (int)FixedCryptoKey;
                }
                else {
                    Header.Ncch.Flags[kFlag] &= (byte)~(int)NoEncrypto;
                    Header.Ncch.Flags[kFlag] &= (byte)~(int)FixedCryptoKey;
                    if (RemoveExtKey) Header.Ncch.Flags[kFlag] &= (byte)~(int)FlagExtKey;
                }
            }
            w.Write(ref Header, 0, sizeof(NcchHeader));
            return true;
        }
        catch (IOException) { return false; }
    }

    bool CreateExtendedHeader(Stream w) {
        if (string.IsNullOrEmpty(ExtendedHeaderFileName)) { ClearExtendedHeader(); return true; }
        try {
            using var r = File.Open(ExtendedHeaderFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) WriteLine($"load: {ExtendedHeaderFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            if (fileSize < sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended)) { ClearExtendedHeader(); WriteLine("ERROR: extendedheader is too short\n"); return false; }
            Header.Ncch.ExtendedHeaderSize = (uint)sizeof(NcchExtendedHeader);
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended)];
            r.Read(buf, 0, sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended));
            fixed (byte* _ = Header.Ncch.ExtendedHeaderHash) ToSha256(buf, (int)Header.Ncch.ExtendedHeaderSize, _);
            if (EncryptMode == kEncryptMode.NotEncrypt) w.CopyFile(r, 0, sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended));
            else {
                CalculateCounter(kAesCtrType.ExtendedHeader);
                FEncryptAesCtrCopyFile(w, r, Key[kEncryptKeyOld], Counter, 0, sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended));
            }
            return true;
        }
        catch (IOException) { ClearExtendedHeader(); return false; }
    }

    bool CreateLogoRegion(Stream w) {
        if (string.IsNullOrEmpty(LogoRegionFileName)) { ClearLogoRegion(); return true; }
        try {
            using var r = File.Open(LogoRegionFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) WriteLine($"load: {LogoRegionFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            var logoRegionSize = Align(r.Position, MediaUnitSize);
            Header.Ncch.LogoRegionOffset = Header.Ncch.ContentSize;
            Header.Ncch.LogoRegionSize = (uint)(logoRegionSize / MediaUnitSize);
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[logoRegionSize];
            Unsafe.InitBlock(ref buf[0], 0, (uint)logoRegionSize);
            r.Read(buf, 0, fileSize);
            w.Write(buf, 0, (int)logoRegionSize);
            fixed (byte* _ = Header.Ncch.LogoRegionHash) ToSha256(buf, (int)logoRegionSize, _);
            return true;
        }
        catch (IOException) { ClearLogoRegion(); return false; }
    }

    bool CreatePlainRegion(Stream w) {
        if (string.IsNullOrEmpty(PlainRegionFileName)) { ClearPlainRegion(); return true; }
        try {
            using var r = File.Open(PlainRegionFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) WriteLine($"load: {PlainRegionFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            Header.Ncch.PlainRegionOffset = Header.Ncch.ContentSize;
            Header.Ncch.PlainRegionSize = (uint)(Align(r.Position, MediaUnitSize) / MediaUnitSize);
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[fileSize];
            r.Read(buf, 0, fileSize);
            w.Write(buf, 0, fileSize);
            return true;
        }
        catch (IOException) { ClearPlainRegion(); return false; }
    }

    bool CreateExeFs(Stream w) {
        if (string.IsNullOrEmpty(ExeFsFileName)) { ClearExeFs(); return true; }
        try {
            using var r = File.Open(ExeFsFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) WriteLine($"load: {ExeFsFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            var superBlockSize = (int)Align(sizeof(ExeFsSuperBlock), MediaUnitSize);
            if (fileSize < superBlockSize) { ClearExeFs(); WriteLine("ERROR: exefs is too short\n"); return false; }
            Header.Ncch.ExeFsOffset = Header.Ncch.ContentSize;
            Header.Ncch.ExeFsSize = (uint)(Align(fileSize, MediaUnitSize) / MediaUnitSize);
            Header.Ncch.ExeFsHashRegionSize = (uint)(superBlockSize / MediaUnitSize);
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[superBlockSize];
            r.Read(buf, 0, superBlockSize);
            ExeFsSuperBlock superBlock = ToStruct<ExeFsSuperBlock>(buf);
            if (ExeFs.IsExeFsSuperBlock(ref superBlock)) { ClearExeFs(); WriteLine("INFO: exefs is encrypted"); return false; }
            fixed (byte* _ = Header.Ncch.ExeFsSuperBlockHash) ToSha256(buf, superBlockSize, _);
            r.Seek(0, SeekOrigin.Begin);
            var exeFs = new byte[fileSize];
            r.Read(exeFs, 0, fileSize);
            if (EncryptMode == kEncryptMode.NotEncrypt) w.CopyFile(r, 0, fileSize);
            else {
                CalculateCounter(kAesCtrType.ExeFs);
                var xorOffset = 0L;
                FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, sizeof(ExeFsSuperBlock), xorOffset);
                xorOffset += sizeof(ExeFsSuperBlock);
                FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyNew], Counter, superBlock.Header0.Size, xorOffset);
                xorOffset += superBlock.Header0.Size;
                FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, fileSize - xorOffset, xorOffset);
                w.Write(exeFs, 0, fileSize);
            }
            return true;
        }
        catch (IOException) { ClearExeFs(); return false; }
    }

    bool CreateRomFs(Stream w) {
        if (string.IsNullOrEmpty(RomFsFileName)) { ClearRomFs(); return true; }
        try {
            using var r = File.Open(RomFsFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var encrypted = !RomFs.IsRomFsFile(r);
            if (encrypted) { ClearRomFs(); WriteLine("INFO: romfs is encrypted"); return false; }
            if (Verbose) WriteLine($"load: {RomFsFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            var superBlockSize = Align(sizeof(SRomFsHeader), RomFs.SHA256BlockSize);
            if (fileSize < superBlockSize) { ClearRomFs(); WriteLine("ERROR: romfs is too short\n"); return false; }
            r.Seek(0, SeekOrigin.Begin);
            SRomFsHeader header = new();
            r.Read(ref header, 0, sizeof(SRomFsHeader));
            superBlockSize = Align(Align(sizeof(SRomFsHeader), RomFs.SHA256BlockSize) + header.Level0Size, MediaUnitSize);
            if (fileSize < superBlockSize) { ClearRomFs(); WriteLine("ERROR: romfs is too short\n"); return false; }
            Header.Ncch.RomFsHashRegionSize = (uint)(superBlockSize / MediaUnitSize);
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[superBlockSize];
            r.Read(buf, 0, (int)superBlockSize);
            fixed (byte* _ = Header.Ncch.RomFsSuperBlockHash) ToSha256(buf, (int)superBlockSize, _);
            Header.Ncch.RomFsOffset = Header.Ncch.ContentSize;
            Header.Ncch.RomFsSize = (uint)(Align(fileSize, MediaUnitSize) / MediaUnitSize);
            if (EncryptMode == kEncryptMode.NotEncrypt) w.CopyFile(r, 0, fileSize);
            else {
                CalculateCounter(kAesCtrType.RomFs);
                FEncryptAesCtrCopyFile(w, r, Key[kEncryptKeyNew], Counter, 0, fileSize);
            }
        }
        catch (IOException) { ClearRomFs(); return false; }
        return true;
    }

    void ClearExtendedHeader() {
        Unsafe.InitBlock(ref Header.Ncch.ExtendedHeaderHash[0], 0, 32);
        Header.Ncch.ExtendedHeaderSize = 0;
    }

    void ClearLogoRegion() {
        Header.Ncch.LogoRegionOffset = 0;
        Header.Ncch.LogoRegionSize = 0;
        Unsafe.InitBlock(ref Header.Ncch.LogoRegionHash[0], 0, 32);
    }
    void ClearPlainRegion() {
        Header.Ncch.PlainRegionOffset = 0;
        Header.Ncch.PlainRegionSize = 0;
    }
    void ClearExeFs() {
        Header.Ncch.ExeFsOffset = 0;
        Header.Ncch.ExeFsSize = 0;
        Header.Ncch.ExeFsHashRegionSize = 0;
        Unsafe.InitBlock(ref Header.Ncch.ExeFsSuperBlockHash[0], 0, 32);
    }
    void ClearRomFs() {
        Header.Ncch.RomFsOffset = 0;
        Header.Ncch.RomFsSize = 0;
        Header.Ncch.RomFsHashRegionSize = 0;
        Unsafe.InitBlock(ref Header.Ncch.RomFsSuperBlockHash[0], 0, 32);
    }

    void AlignFileSize(Stream s, long alignment) {
        s.Seek(0, SeekOrigin.End);
        var fileSize = Align(s.Position, alignment);
        s.Seek2(fileSize);
        Header.Ncch.ContentSize = (uint)(fileSize / MediaUnitSize);
    }

    //void onHttpsGetExtKey(Uri a_pUrl, byte[] a_pUserData) {
    //    size_t uUserData = reinterpret_cast<size_t>(a_pUserData);
    //    u32 uUniqueId = static_cast<u32>(uUserData);
    //    if (a_pUrl != nullptr) {
    //        const string&sData = a_pUrl->GetData();
    //        if (!sData.empty()) {
    //            string sExtKey;
    //            for (int i = 0; i < static_cast<int>(sData.size()); i++) {
    //                sExtKey += Format("%02X", static_cast<u8>(sData[i]));
    //            }
    //            string sProgramId = Format("000400000%05X00", uUniqueId);
    //            ExtKey.insert(make_pair(sProgramId, sExtKey));
    //            if (Verbose) {
    //                UPrintf(USTR("download: %") PRIUS USTR(" %") PRIUS USTR("\n"), AToU(sProgramId).c_str(), AToU(sExtKey).c_str());
    //            }
    //        }
    //    }
    //}
}