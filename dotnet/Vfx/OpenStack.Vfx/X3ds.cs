using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static Gee.External.Capstone.Arm.ArmOperandType;
using static Gee.External.Capstone.Arm.ArmRegisterId;
using static OpenStack.Vfx.Util;
using static OpenStack.Vfx.X3ds.Crypt;
using static OpenStack.Vfx.X3ds.Ncch;
#pragma warning disable CS9084, CS0649

namespace OpenStack.Vfx.X3ds;

#region FileSystem : 3ds

/// <summary>
/// X3dsFileSystem
/// </summary>
public class X3dsFileSystem : FileSystem {
    public X3dsFileSystem(FileSystem vfx, string path, string basePath) {
        Log.Info("X3dsFileSystem");
    }

    public override bool FileExists(string path) => throw new NotImplementedException();
    public override (string path, long length) FileInfo(string path) => throw new NotImplementedException();
    public override IEnumerable<string> Glob(string path, string searchPattern) => throw new NotImplementedException();
    public override Stream Open(string path, string mode) => throw new NotImplementedException();
}

#endregion

#region Ncch

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

    #region ExtHeaders

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemInfoFlagStruct {
        public fixed byte Reserved[5];
        public byte Flag;
        public ushort RemasterVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CodeSegmentInfo {
        public uint Address;
        public uint NumMaxPages;
        public uint CodeSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CodeSetInfo {
        public ulong Name;
        public SystemInfoFlagStruct Flags;
        public CodeSegmentInfo TextSectionInfo;
        public uint StackSize;
        public CodeSegmentInfo ReadOnlySectionInfo;
        public fixed byte Reserved1[4];
        public CodeSegmentInfo DataSectionInfo;
        public uint BssSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CoreInfo {
        public CodeSetInfo CodeSetInfo;
        public fixed ulong DepedencyList[48];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemInfoStruct {
        public ulong SaveDataSize;
        public ulong JumpId;
        public fixed byte Reserved2[48];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemControlInfo {
        public CoreInfo CoreInfo;
        public SystemInfoStruct SystemInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ARM11SystemLocalCapabilityFlags {
        public uint CoreVersion;
        public fixed byte Reserved[2];
        public byte mixed;
        public readonly byte IdealProcessor => (byte)(mixed & 0x2);
        public readonly byte AffinityMask => (byte)(mixed & 0x2);
        public readonly byte SystemMode => (byte)(mixed & 0x4);
        public byte MainThreadPriority;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StorageInfoFlags {
        public fixed byte StorageAccessInfo[7];
        public byte OtherAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StorageInfoStruct {
        public ulong ExtSaveDataId;
        public ulong SystemSaveDataId;
        public ulong StorageAccessableUniqueIds;
        public StorageInfoFlags InfoFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ARM11SystemLocalCapabilities {
        public ulong ProgramId;
        public ARM11SystemLocalCapabilityFlags Flags;
        public byte MaxCpu;
        public byte Reserved0;
        public fixed byte ResourceLimits[15 * 2];
        public StorageInfoStruct StorageInfo;
        public fixed ulong ServiceAccessControl[32];
        public fixed byte Reserved[31];
        public byte ResourceLimitCategory;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ARM11KernelCapabilities {
        public fixed uint Descriptor[28];
        public fixed byte Reserved[16];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AccessControlInfo {
        public ARM11SystemLocalCapabilities m_ARM11SystemLocalCapabilities;
        public ARM11KernelCapabilities m_ARM11KernelCapabilities;
        public fixed byte m_ARM9AccessControlInfo[16];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcchExtendedHeader {
        public SystemControlInfo m_SystemControlInfo;
        public AccessControlInfo m_AccessControlInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcchAccessControlExtended {
        public fixed byte m_RsaSignature[256];
        public fixed byte m_NcchHeaderPublicKey[256];
        public AccessControlInfo m_AccessControlInfoDescriptor;
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
        ExtKey = 1 << 5
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

    static readonly BigInteger DevSlot0x18KeyX = new(FromHexString("304BF1468372EE64115EBD4093D84276"));
    static readonly BigInteger DevSlot0x1BKeyX = new(FromHexString("6C8B2944A0726035F941DFC018524FB6"));
    static readonly BigInteger DevSlot0x25KeyX = new(FromHexString("81907A4B6F1B47323A677974CE4AD71B"));
    static readonly BigInteger DevSlot0x2CKeyX = new(FromHexString("510207515507CBB18E243DCB85E23A1D"));
    static readonly BigInteger RetailSlot0x18KeyX = new(FromHexString("82E9C9BEBFB8BDB875ECC0A07D474374"));
    static readonly BigInteger RetailSlot0x1BKeyX = new(FromHexString("45AD04953992C7C893724A9A7BCE6182"));
    static readonly BigInteger RetailSlot0x25KeyX = new(FromHexString("CEE7D8AB30C00DAE850EF5E382AC5AF3"));
    static readonly BigInteger RetailSlot0x2CKeyX = new(FromHexString("B98E95CECA3E4D171F76A94DE934C053"));
    static readonly BigInteger SystemFixedKey = new(FromHexString("527CE630A9CA305F3696F3CDE954194B"));
    static readonly BigInteger NormalFixedKey = new(FromHexString("00000000000000000000000000000000"));
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
                    ExeFs.ExeFsSuperBlock superBlock = new();
                    r.Read(ref superBlock, 0, sizeof(ExeFs.ExeFsSuperBlock));
                    CalculateCounter(kAesCtrType.ExeFs);
                    if (EncryptMode == kEncryptMode.FixedKey || EncryptMode == kEncryptMode.Auto)
                        FEncryptAesCtrData((byte*)&superBlock, 0, Key[kEncryptKeyOld], Counter, sizeof(ExeFs.ExeFsSuperBlock), 0);
                    if (ExeFs.IsExeFsSuperBlock(ref superBlock)) {
                        r.Seek(OffsetAndSize[kOffsetSizeExeFs * 2], SeekOrigin.Begin);
                        var exeFs = new byte[OffsetAndSize[kOffsetSizeExeFs * 2 + 1]];
                        r.Read(exeFs, 0, (int)OffsetAndSize[kOffsetSizeExeFs * 2 + 1]);
                        if (EncryptMode == kEncryptMode.FixedKey || EncryptMode == kEncryptMode.Auto) {
                            var xorOffset = 0L;
                            FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, sizeof(ExeFs.ExeFsSuperBlock), xorOffset);
                            xorOffset += sizeof(ExeFs.ExeFsSuperBlock);
                            FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyNew], Counter, superBlock.Header0.Size, xorOffset);
                            xorOffset += superBlock.Header0.Size;
                            FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, OffsetAndSize[kOffsetSizeExeFs * 2 + 1] - xorOffset, xorOffset);
                        }
                        try {
                            using var w = File.Open(ExeFsFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
                            if (Verbose) Log.Info($"save: {ExeFsFileName}");
                            w.Write(exeFs, 0, (int)OffsetAndSize[kOffsetSizeExeFs * 2 + 1]);
                        }
                        catch (IOException) { result = false; }
                    }
                    else {
                        result = false;
                        ExtractFile(r, ExeFsFileName, OffsetAndSize[kOffsetSizeExeFs * 2], OffsetAndSize[kOffsetSizeExeFs * 2 + 1], true, "exefs");
                    }
                }
                else if (Verbose) Log.Info($"INFO: exefs is not exists, {ExeFsFileName} will not be create");
            }
            else if (OffsetAndSize[kOffsetSizeExeFs * 2 + 1] != 0 && Verbose) Log.Info("INFO: exefs is not extract");
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
            if ((Header.Ncch.Flags[kFlag] & (int)Flag.NoEncrypto) != 0) EncryptMode = kEncryptMode.NotEncrypt;
            else if ((Header.Ncch.Flags[kFlag] & (int)Flag.FixedCryptoKey) != 0) EncryptMode = kEncryptMode.FixedKey;
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
        while ((Header.Ncch.Flags[kFlag] & (int)Flag.ExtKey) != 0) {
            ReadExtKey();
            var programId = BitConverter.GetBytes(Header.Ncch.ProgramId);
            Array.Reverse(programId);
            var sProgramId = ToHexString(programId);
            if (!ExtKey.TryGetValue(sProgramId, out var extKey)) {
                DownloadBegin = DownloadEnd = int.Parse(sProgramId.Substring(9, 5), NumberStyles.HexNumber);
                if (!Download(false)) Log.Info("INFO: download failed");
                if (!ExtKey.TryGetValue(sProgramId, out extKey)) { Log.Info($"ERROR: can not find ext key for {sProgramId}\n"); break; }
            }
            if (extKey.Length != 16) { Log.Info($"ERROR: can not find ext key for {sProgramId}\n"); break; }
            //if (extKey.Length != 32 || extKey.Any(x => "0123456789ABCDEFabcdef".IndexOf(x) != -1)) { Log.Info($"ERROR: can not find ext key for {sProgramId}\n"); break; }
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
            if (vals.Length != 2) { Log.Info($"INFO: unknown ext key record {line}"); continue; }
            else if (!ExtKey.TryAdd(vals[0], FromHexString(vals[1]))) Log.Info($"INFO: multiple ext key for {vals[0]}");
        }
    }

    bool WriteExtKey() {
        try {
            var extKeyPath = "MODULE/ext_key.txt";
            using var s = File.Open(extKeyPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            foreach (var z in ExtKey)
                s.WriteBytes(Encoding.ASCII.GetBytes($"{z.Key} {ToHexString(z.Value)}\r\n"));
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
                    if (Verbose) Log.Info($"save: {fileName}");
                    if (plainData || EncryptMode == kEncryptMode.NotEncrypt) w.CopyFile(s, offset, size);
                    else FEncryptAesCtrCopyFile(w, s, Key[KeyIndex], Counter, offset, size);
                }
                catch (IOException) { result = false; }
            else if (Verbose) Log.Info($"INFO: {type} does not exist, {fileName} will not be create");
        }
        else if (size != 0 && Verbose) Log.Info($"INFO: {type} is not extract");
        return result;
    }

    bool CreateHeader(Stream w) {
        try {
            using var r = File.Open(HeaderFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) Log.Info($"load: {HeaderFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            if (fileSize < sizeof(NcchHeader)) { Log.Info("ERROR: ncch header is too short\n"); return false; }
            r.Seek(0, SeekOrigin.Begin);
            r.Read(ref Header, 0, sizeof(NcchHeader));
            unchecked {
                if (EncryptMode == kEncryptMode.NotEncrypt) {
                    Header.Ncch.Flags[kFlag] |= (int)Flag.NoEncrypto;
                }
                else if (EncryptMode == kEncryptMode.FixedKey) {
                    Header.Ncch.Flags[kFlag] &= (byte)~(int)Flag.NoEncrypto;
                    Header.Ncch.Flags[kFlag] |= (int)Flag.FixedCryptoKey;
                }
                else {
                    Header.Ncch.Flags[kFlag] &= (byte)~(int)Flag.NoEncrypto;
                    Header.Ncch.Flags[kFlag] &= (byte)~(int)Flag.FixedCryptoKey;
                    if (RemoveExtKey) Header.Ncch.Flags[kFlag] &= (byte)~(int)Flag.ExtKey;
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
            if (Verbose) Log.Info($"load: {ExtendedHeaderFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            if (fileSize < sizeof(NcchExtendedHeader) + sizeof(NcchAccessControlExtended)) { ClearExtendedHeader(); Log.Info("ERROR: extendedheader is too short\n"); return false; }
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
            if (Verbose) Log.Info($"load: {LogoRegionFileName}");
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
            if (Verbose) Log.Info($"load: {PlainRegionFileName}");
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
            if (Verbose) Log.Info($"load: {ExeFsFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            var superBlockSize = (int)Align(sizeof(ExeFs.ExeFsSuperBlock), MediaUnitSize);
            if (fileSize < superBlockSize) { ClearExeFs(); Log.Info("ERROR: exefs is too short\n"); return false; }
            Header.Ncch.ExeFsOffset = Header.Ncch.ContentSize;
            Header.Ncch.ExeFsSize = (uint)(Align(fileSize, MediaUnitSize) / MediaUnitSize);
            Header.Ncch.ExeFsHashRegionSize = (uint)(superBlockSize / MediaUnitSize);
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[superBlockSize];
            r.Read(buf, 0, superBlockSize);
            ExeFs.ExeFsSuperBlock superBlock = ToStruct<ExeFs.ExeFsSuperBlock>(buf);
            if (ExeFs.IsExeFsSuperBlock(ref superBlock)) { ClearExeFs(); Log.Info("INFO: exefs is encrypted"); return false; }
            fixed (byte* _ = Header.Ncch.ExeFsSuperBlockHash) ToSha256(buf, superBlockSize, _);
            r.Seek(0, SeekOrigin.Begin);
            var exeFs = new byte[fileSize];
            r.Read(exeFs, 0, fileSize);
            if (EncryptMode == kEncryptMode.NotEncrypt) w.CopyFile(r, 0, fileSize);
            else {
                CalculateCounter(kAesCtrType.ExeFs);
                var xorOffset = 0L;
                FEncryptAesCtrData(exeFs, xorOffset, Key[kEncryptKeyOld], Counter, sizeof(ExeFs.ExeFsSuperBlock), xorOffset);
                xorOffset += sizeof(ExeFs.ExeFsSuperBlock);
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
            if (encrypted) { ClearRomFs(); Log.Info("INFO: romfs is encrypted"); return false; }
            if (Verbose) Log.Info($"load: {RomFsFileName}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = (int)r.Position;
            var superBlockSize = Align(sizeof(RomFs.SRomFsHeader), RomFs.SHA256BlockSize);
            if (fileSize < superBlockSize) { ClearRomFs(); Log.Info("ERROR: romfs is too short\n"); return false; }
            r.Seek(0, SeekOrigin.Begin);
            RomFs.SRomFsHeader header = new();
            r.Read(ref header, 0, sizeof(RomFs.SRomFsHeader));
            superBlockSize = Align(Align(sizeof(RomFs.SRomFsHeader), RomFs.SHA256BlockSize) + header.Level0Size, MediaUnitSize);
            if (fileSize < superBlockSize) { ClearRomFs(); Log.Info("ERROR: romfs is too short\n"); return false; }
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

#endregion

#region Ncch : Ncsd


public unsafe class Ncsd {

    #region Headers

    [StructLayout(LayoutKind.Sequential)]
    public struct NcsdCommonHeaderStruct {
        public uint Signature;
        public uint MediaSize;
        public ulong MediaId;
        public fixed byte PartitionFsType[8];
        public fixed byte PartitionCryptType[8];
        public fixed uint ParitionOffsetAndSize[16];
        public fixed byte ExtendedHeaderHash[32];
        public uint AdditionalHeaderSize;
        public uint SectorZeroOffset;
        public fixed byte Flags[8];
        public fixed ulong PartitionId[8];
        public fixed byte Reserved[48];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SNcsdHeader {
        public fixed byte RSASignature[256];
        public NcsdCommonHeaderStruct Ncsd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CardInfoHeader {
        public ulong CardInfo;
        public fixed byte Reserved1[3576];
        public ulong MediaId;
        public ulong Reserved2;
        public fixed byte InitialData[48];
        public fixed byte Reserved3[192];
        public NcchCommonHeader NcchHeader;
        public fixed byte CardDeviceReserved1[512];
        public fixed byte TitleKey[16];
        public fixed byte CardDeviceReserved2[240];
    }

    #endregion

    #region Records

    //public enum PartitionFsType { FS_TYPE_DEFAULT }
    //public enum PartitionEncryptoType { ENCRYPTO_TYPE_DEFAULT }

    public enum MediaType { INNER_DEVICE, CARD1, CARD2, EXTENDED_DEVICE }

    #endregion

    public const int MEDIA_CARD_DEVICE = 3;
    public const int MEDIA_PLATFORM_INDEX = 4;
    public const int MEDIA_TYPE_INDEX = 5;
    public const int MEDIA_UNIT_SIZE = 6;
    public static readonly uint Signature = 0x4453434e; //: NCSD;
    public static readonly long OffsetFirstNcch = 0x4000;
    public static readonly int BlockSize = 0x1000;
    public bool Verbose = false;
    public string HeaderFileName;
    public Dictionary<int, string> NcchFileName = [];
    public bool NotPad = false;
    public const int LastPartitionIndex = 7;
    public SNcsdHeader Header = new();
    CardInfoHeader CardInfo = new();
    long MediaUnitSize = 1 << 9;
    bool AlignToBlockSize = false;
    long ValidSize = 0;
    public long[] OffsetAndSize = new long[8 * 2];

    //public void SetFileName(string a_sFileName) => m_sFileName = a_sFileName;
    //public void SetVerbose(bool a_bVerbose) => m_bVerbose = a_bVerbose;
    //public void SetHeaderFileName(string a_sHeaderFileName) => m_sHeaderFileName = a_sHeaderFileName;
    //public void SetNcchFileName(Dictionary<int, string> a_mNcchFileName) => m_mNcchFileName.insert(a_mNcchFileName.begin(), a_mNcchFileName.end());
    //public void SetNotPad(bool a_bNotPad) => m_bNotPad = a_bNotPad;
    //public void SetLastPartitionIndex(int a_nLastPartitionIndex) => m_nLastPartitionIndex = a_nLastPartitionIndex;
    //public void SetFilePtr(FILE* a_fpNcsd) => m_fpNcsd = a_fpNcsd;
    //public SNcsdHeader GetNcsdHeader() => m_NcsdHeader;
    //public long[] GetOffsetAndSize() => m_nOffsetAndSize;

    public bool ExtractFile(Stream r) {
        try {
            var result = true;
            r.Read(ref Header, 0, sizeof(SNcsdHeader));
            CalculateMediaUnitSize();
            if (!ExtractFile(r, HeaderFileName, 0, OffsetFirstNcch, "ncsd header", -1, false)) result = false;
            for (var i = 0; i < 8; i++)
                if (!ExtractFile(r, NcchFileName.TryGetValue(i, out var z) ? z : default, Header.Ncsd.ParitionOffsetAndSize[i * 2], Header.Ncsd.ParitionOffsetAndSize[i * 2 + 1], "partition", i, true)) result = false;
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
            for (var i = 0; i < 8; i++)
                if (!CreateNcch(w, i)) result = false;
            var fileSize = w.Position;
            fixed (byte* _ = &CardInfo.Reserved1[248]) *(long*)_ = fileSize;
            if (NotPad && Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD2) { NotPad = false; if (Verbose) Log.Info("INFO: not support --not-pad with CARD2 type"); }
            if (NotPad) Header.Ncsd.MediaSize = (uint)(fileSize / MediaUnitSize);
            else {
                var minPower = Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD1 ? 27 : 29;
                for (var i = minPower; i < 64; i++)
                    if (fileSize <= 1L << i) { Header.Ncsd.MediaSize = (uint)((1L << i) / MediaUnitSize); break; }
                w.PadFile(Header.Ncsd.MediaSize * MediaUnitSize - fileSize, 0xFF);
            }
            w.Seek(0, SeekOrigin.Begin);
            w.Write(ref Header, 0, sizeof(SNcsdHeader));
            w.Write(ref CardInfo, 0, sizeof(CardInfoHeader));
            return result;
        }
        catch (IOException) { return false; }
    }

    public bool TrimFile(Stream s) {
        try {
            s.Read(ref Header, 0, sizeof(SNcsdHeader));
            if (Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD2) { if (Verbose) Log.Info("INFO: not support --trim with CARD2 type"); return false; }
            s.Read(ref CardInfo, 0, sizeof(CardInfoHeader));
            CalculateMediaUnitSize();
            for (var i = LastPartitionIndex + 1; i < 8; i++)
                ClearNcch(i);
            CalculateValidSize();
            fixed (byte* _ = &CardInfo.Reserved1[248]) *(long*)_ = ValidSize;
            Header.Ncsd.MediaSize = (uint)(ValidSize / MediaUnitSize);
            s.Seek(0, SeekOrigin.Begin);
            s.Write(ref Header, 0, sizeof(SNcsdHeader));
            s.Write(ref CardInfo, 0, sizeof(CardInfoHeader));
            ((FileStream)s).SetLength(ValidSize);
            return true;
        }
        catch (IOException) { return false; }
    }

    public bool PadFile(Stream s) {
        try {
            s.Read(ref Header, 0, sizeof(SNcsdHeader));
            if (Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD2) { if (Verbose) Log.Info("INFO: not support --pad with CARD2 type"); return false; }
            s.Read(ref CardInfo, 0, sizeof(CardInfoHeader));
            CalculateMediaUnitSize();
            CalculateValidSize();
            fixed (byte* _ = &CardInfo.Reserved1[248]) *(long*)_ = ValidSize;
            for (var i = 27; i < 64; i++)
                if (ValidSize <= 1L << i) { Header.Ncsd.MediaSize = (uint)((1L << i) / MediaUnitSize); break; }
            s.Seek(ValidSize, SeekOrigin.Begin);
            s.PadFile(Header.Ncsd.MediaSize * MediaUnitSize - ValidSize, 0xFF);
            s.Seek(0, SeekOrigin.Begin);
            s.Write(ref Header, 0, sizeof(SNcsdHeader));
            s.Write(ref CardInfo, 0, sizeof(CardInfoHeader));
            ((FileStream)s).SetLength(Header.Ncsd.MediaSize * MediaUnitSize);
            return true;
        }
        catch (IOException) { return false; }
    }

    public void Analyze(Stream r) {
        if (r != null) return;
        var filePos = r.Position;
        r.Seek(0, SeekOrigin.Begin);
        r.Read(ref Header, 0, sizeof(SNcsdHeader));
        CalculateMediaUnitSize();
        for (var i = 0; i < 8; i++) {
            OffsetAndSize[i * 2] = Header.Ncsd.ParitionOffsetAndSize[i * 2] * MediaUnitSize;
            OffsetAndSize[i * 2 + 1] = Header.Ncsd.ParitionOffsetAndSize[i * 2 + 1] * MediaUnitSize;
        }
        for (var i = 6; i >= 0; i--)
            if (OffsetAndSize[i * 2] == 0 && OffsetAndSize[i * 2 + 1] == 0) OffsetAndSize[i * 2] = OffsetAndSize[(i + 1) * 2];
        for (var i = 1; i < 8; i++)
            if (OffsetAndSize[i * 2] == 0 && OffsetAndSize[i * 2 + 1] == 0) OffsetAndSize[i * 2] = OffsetAndSize[(i - 1) * 2] + OffsetAndSize[(i - 1) * 2 + 1];
        r.Seek(filePos, SeekOrigin.Begin);
    }

    public static bool IsNcsdFile(Stream r) {
        try {
            SNcsdHeader header = new();
            r.Read(ref header, 0, sizeof(SNcsdHeader));
            r.Seek(0, SeekOrigin.Begin);
            return header.Ncsd.Signature == Signature;
        }
        catch (IOException) { return false; }
    }

    void CalculateMediaUnitSize() => MediaUnitSize = 1L << (Header.Ncsd.Flags[MEDIA_UNIT_SIZE] + 9);

    void CalculateAlignment() {
        AlignToBlockSize = true;
        for (var i = 0; AlignToBlockSize && i < 8; i++)
            AlignToBlockSize = Header.Ncsd.ParitionOffsetAndSize[i * 2] % 8 == 0 && Header.Ncsd.ParitionOffsetAndSize[i * 2 + 1] % 8 == 0;
    }

    void CalculateValidSize() {
        ValidSize = Header.Ncsd.ParitionOffsetAndSize[0] + Header.Ncsd.ParitionOffsetAndSize[1];
        if (ValidSize < OffsetFirstNcch / MediaUnitSize) ValidSize = OffsetFirstNcch / MediaUnitSize;
        for (var i = 1; i < 8; i++) {
            var size = Header.Ncsd.ParitionOffsetAndSize[i * 2] + Header.Ncsd.ParitionOffsetAndSize[i * 2 + 1];
            if (size > ValidSize) ValidSize = size;
        }
        ValidSize *= MediaUnitSize;
    }

    bool ExtractFile(Stream r, string fileName, long offset, long size, string type, int typeId, bool mediaUnitSize) {
        var result = true;
        if (!string.IsNullOrEmpty(fileName)) {
            if (offset != 0 || size != 0) {
                try {
                    using var w = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Write);
                    if (Verbose) Log.Info($"save: {fileName}");
                    if (mediaUnitSize) {
                        offset *= MediaUnitSize;
                        size *= MediaUnitSize;
                    }
                    w.CopyFile(r, offset, size);
                }
                catch (IOException) { result = false; }
            }
            else if (Verbose) {
                if (typeId < 0 || typeId >= 8) Log.Info($"INFO: {type} is not exists, {fileName} will not be create");
                else Log.Info($"INFO: {type} {typeId} is not exists, {fileName} will not be create");
            }
        }
        else if ((offset != 0 || size != 0) && Verbose) {
            if (typeId < 0 || typeId >= 8) Log.Info($"INFO: {type} is not extract");
            else Log.Info($"INFO: {type} {typeId} is not extract");
        }
        return result;
    }

    bool CreateHeader(Stream w) {
        try {
            using (var r = File.Open(HeaderFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                r.Seek(0, SeekOrigin.End);
                var fileSize = r.Position;
                if (fileSize < sizeof(SNcsdHeader) + sizeof(CardInfoHeader)) { Log.Info("ERROR: ncsd header is too short\n"); return false; }
                if (Verbose) Log.Info($"load: {HeaderFileName}");
                r.Seek(0, SeekOrigin.Begin);
                r.Read(ref Header, 0, sizeof(SNcsdHeader));
                r.Read(ref CardInfo, 0, sizeof(CardInfoHeader));
            }
            w.Write(ref Header, 0, sizeof(SNcsdHeader));
            w.Write(ref CardInfo, 0, sizeof(CardInfoHeader));
            w.PadFile(OffsetFirstNcch - w.Position, 0xFF);
            return true;
        }
        catch (IOException) { return false; }
    }

    bool CreateNcch(Stream w, int index) {
        if (string.IsNullOrEmpty(NcchFileName[index])) { ClearNcch(index); return true; }
        try {
            using var r = File.Open(NcchFileName[index], FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) Log.Info($"load: {NcchFileName[index]}");
            r.Seek(0, SeekOrigin.End);
            var fileSize = r.Position;
            if (index == 0) {
                if (fileSize < sizeof(NcchHeader)) { ClearNcch(index); return false; }
                r.Seek(sizeof(NcchHeader) - sizeof(NcchCommonHeader), SeekOrigin.Begin);
                r.Read(ref CardInfo.NcchHeader, 0, sizeof(NcchHeader));
            }
            r.Seek(0, SeekOrigin.Begin);
            Header.Ncsd.ParitionOffsetAndSize[index * 2] = (uint)(w.Position / MediaUnitSize);
            Header.Ncsd.ParitionOffsetAndSize[index * 2 + 1] = (uint)(Align(fileSize, AlignToBlockSize ? BlockSize : MediaUnitSize) / MediaUnitSize);
            w.CopyFile(r, 0, fileSize);
            w.PadFile(Align(w.Position, AlignToBlockSize ? BlockSize : MediaUnitSize) - r.Position, 0);
            return true;
        }
        catch (IOException) { ClearNcch(index); return false; }
    }

    void ClearNcch(int index) {
        Header.Ncsd.PartitionFsType[index] = 0;
        Header.Ncsd.PartitionCryptType[index] = 0;
        Header.Ncsd.ParitionOffsetAndSize[index * 2] = 0;
        Header.Ncsd.ParitionOffsetAndSize[index * 2 + 1] = 0;
        fixed (ulong* _ = &Header.Ncsd.PartitionId[index]) Unsafe.InitBlock(_, 0, 8 * 8);
        if (index == 0) fixed (NcchCommonHeader* _ = &CardInfo.NcchHeader) Unsafe.InitBlock(_, 0, (uint)sizeof(NcchHeader));
    }
}

#endregion

#region Ncsd : ExeFs

public unsafe class ExeFs {

    #region Headers

    [StructLayout(LayoutKind.Sequential)]
    public struct ExeSectionHeader {
        public fixed byte Name[8];
        public uint Offset;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ExeFsSuperBlock {
        public ExeSectionHeader Header0;
        public ExeSectionHeader Header1;
        public ExeSectionHeader Header2;
        public ExeSectionHeader Header3;
        public ExeSectionHeader Header4;
        public ExeSectionHeader Header5;
        public ExeSectionHeader Header6;
        public ExeSectionHeader Header7;
        public ref ExeSectionHeader Header(int index) {
            switch (index) {
                case 0: return ref Header0;
                case 1: return ref Header1;
                case 2: return ref Header2;
                case 3: return ref Header3;
                case 4: return ref Header4;
                case 6: return ref Header6;
                case 5: return ref Header5;
                case 7: return ref Header7;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        public fixed byte Reserved[128];
        public fixed byte Hash0[32];
        public fixed byte Hash1[32];
        public fixed byte Hash2[32];
        public fixed byte Hash3[32];
        public fixed byte Hash4[32];
        public fixed byte Hash5[32];
        public fixed byte Hash6[32];
        public fixed byte Hash7[32];
    }

    #endregion

    public string FileName;
    public bool Verbose;
    public string HeaderFileName;
    public string ExeFsDirName;
    public bool Uncompress;
    public bool Compress;
    public Stream S;
    ExeFsSuperBlock SuperBlock = new();
    readonly Dictionary<string, string> Path = new() {
        { "banner", "banner.bnr" },
        { "icon", "icon.icn" },
        { "logo", "logo.darc.lz" }
    };

    //public void SetFileName(string fileName) => throw new NotImplementedException();
    //public void SetVerbose(bool verbose) => throw new NotImplementedException();
    //public void SetHeaderFileName(string headerFileName) => throw new NotImplementedException();
    //public void SetExeFsDirName(string exeFsDirName) => throw new NotImplementedException();
    //public void SetUncompress(bool uncompress) => throw new NotImplementedException();
    //public void SetCompress(bool compress) => throw new NotImplementedException();

    public bool ExtractFile() {
        try {
            var result = true;
            using (S = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                S.Read(ref SuperBlock, 0, sizeof(ExeFsSuperBlock));
                if (!string.IsNullOrEmpty(ExeFsDirName)) Directory.CreateDirectory(ExeFsDirName);
                if (!ExtractHeader()) result = false;
                if (!string.IsNullOrEmpty(ExeFsDirName))
                    for (var i = 0; i < 8; i++)
                        if (!ExtractSection(i)) result = false;
            }
            return result;
        }
        catch (IOException) { return false; }
    }

    public bool CreateFile() {
        try {
            var result = true;
            using (S = File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.Write)) {
                if (!CreateHeader()) return false;
                for (var i = 0; i < 8; i++)
                    if (!CreateSection(i)) { result = false; i--; }
                S.Seek(0, SeekOrigin.Begin);
                S.Write(ref SuperBlock, 0, sizeof(ExeFsSuperBlock));
                return result;
            }
        }
        catch (IOException) { return false; }
    }

    public static bool IsExeFsFile(string fileName, long offset) {
        try {
            using var s = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            ExeFsSuperBlock superBlock = new();
            s.Read(ref superBlock, 0, sizeof(ExeFsSuperBlock));
            return IsExeFsSuperBlock(ref superBlock);
        }
        catch (IOException) { return false; }
    }

    public static bool IsExeFsSuperBlock(ref ExeFsSuperBlock superBlock) {
        // TODO@sky
        return true;
        //static const u8 c_uReserved[sizeof(a_ExeFsSuperBlock.m_Reserved)] = { };
        //return superBlock.Header0.Offset == 0 && memcmp(a_ExeFsSuperBlock.m_Reserved, c_uReserved, sizeof(a_ExeFsSuperBlock.m_Reserved)) == 0;
    }

    public static readonly int BlockSize;

    bool ExtractHeader() {
        var result = true;
        if (string.IsNullOrEmpty(HeaderFileName)) { if (Verbose) Log.Info("INFO: exefs header is not extract\n"); return result; }
        try {
            using var s = File.Open(HeaderFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            if (Verbose) Log.Info($"save: {HeaderFileName}\n");
            s.Write(ref SuperBlock, 0, sizeof(ExeFsSuperBlock));
            return result;
        }
        catch (IOException) { return false; }
    }

    bool ExtractSection(int index) {
        var result = true;
        ref ExeSectionHeader header = ref SuperBlock.Header(index);
        string path, name; fixed (byte* _name = header.Name) name = Encoding.ASCII.GetString(_name, 8);
        if (string.IsNullOrEmpty(name)) return result;
        var topSection = false;
        if (!Path.TryGetValue(name, out var z)) {
            if (index == 0) { path = $"{ExeFsDirName}/code.bin"; topSection = true; }
            else { if (Verbose) Log.Info($"INFO: unknown entry Name {name}\n"); path = $"{ExeFsDirName}/{name}.bin"; }
        }
        else path = $"{ExeFsDirName}/{z}";
        try {
            using var s = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
            if (Verbose) Log.Info($"save: {Path}\n");
            if (topSection && Uncompress) {
                var compressedSize = header.Size;
                s.Seek(sizeof(ExeFsSuperBlock) + header.Offset, SeekOrigin.Begin);
                var compressed = new byte[compressedSize];
                S.Read(compressed, 0, (int)compressedSize);
                result = BackwardLz77.GetUncompressedSize(compressed, compressedSize, out var uncompressedSize);
                if (result) {
                    var uncompressed = new byte[uncompressedSize];
                    result = BackwardLz77.Uncompress(compressed, compressedSize, uncompressed, ref uncompressedSize);
                    if (result) s.Write(uncompressed, 0, (int)uncompressedSize);
                    else Log.Info($"ERROR: uncompress error\n\n");
                }
                else Log.Info($"ERROR: get uncompressed RomSize error\n\n");
            }
            if (!topSection || !Uncompress || !result) s.CopyFile(S, sizeof(ExeFsSuperBlock) + header.Offset, header.Size);
            return result;
        }
        catch (IOException) { return false; }
    }

    bool CreateHeader() {
        try {
            using var s = File.Open(HeaderFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            s.Seek(0, SeekOrigin.End);
            var fileSize = s.Position;
            if (fileSize < sizeof(ExeFsSuperBlock)) { Log.Info("ERROR: exefs header is too short\n\n"); return false; }
            if (Verbose) Log.Info($"load: {HeaderFileName}\n");
            s.Seek(0, SeekOrigin.Begin);
            s.Read(ref SuperBlock, 0, sizeof(ExeFsSuperBlock));
            S.Write(ref SuperBlock, 0, sizeof(ExeFsSuperBlock));
            return true;
        }
        catch (IOException) { return false; }
    }

    bool CreateSection(int index) {
        var result = true;
        ref ExeSectionHeader header = ref SuperBlock.Header(index);
        string path, name; fixed (byte* _name = header.Name) name = Encoding.ASCII.GetString(_name, 8);
        if (string.IsNullOrEmpty(name)) return result;
        var topSection = false;
        if (!Path.TryGetValue(name, out var z)) {
            if (index == 0) { path = $"{ExeFsDirName}/code.bin"; topSection = true; }
            else { if (Verbose) Log.Info($"INFO: unknown entry Name {name}\n"); path = $"{ExeFsDirName}/{name}.bin"; }
        }
        else path = $"{ExeFsDirName}/{z}";
        try {
            uint fileSize;
            byte[] data;
            using (var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                header.Offset = (uint)(S.Position - sizeof(ExeFsSuperBlock));
                if (Verbose) Log.Info($"load: {path}\n");
                s.Seek(0, SeekOrigin.End);
                fileSize = (uint)s.Position;
                s.Seek(0, SeekOrigin.Begin);
                data = new byte[fileSize];
                S.Read(data, 0, (int)fileSize);
            }
            var compressResult = false;
            if (topSection && Compress) {
                var compressedSize = fileSize;
                var compressed = new byte[compressedSize];
                compressResult = BackwardLz77.Compress(data, fileSize, compressed, ref compressedSize);
                if (compressResult) {
                    fixed (ExeFsSuperBlock* _ = &SuperBlock) ToSha256(compressed, (int)compressedSize, _->Hash0 + ((7 - index) * 32));
                    S.Write(compressed, 0, (int)compressedSize);
                    header.Size = compressedSize;
                }
            }
            if (!topSection || !Compress || !compressResult) {
                fixed (ExeFsSuperBlock* _ = &SuperBlock) ToSha256(data, (int)fileSize, _->Hash0 + ((7 - index) * 32));
                S.Write(data, 0, (int)fileSize);
                header.Size = fileSize;
            }
            S.PadFile(Align(S.Position, BlockSize) - S.Position, 0);
            return result;
        }
        catch (IOException) { ClearSection(index); return false; }
    }

    void ClearSection(int index) {
        int size;
        fixed (ExeFsSuperBlock* _ = &SuperBlock) {
            if (index != 7) Buffer.MemoryCopy(&_->Header1 + index, &_->Header0 + index, size = sizeof(ExeSectionHeader) * (7 - index), size);
            Unsafe.InitBlock(&_->Header7, 0, (uint)sizeof(ExeSectionHeader));
            Buffer.MemoryCopy(_->Hash0, _->Hash1, size = 32 * (7 - index), size);
            Unsafe.InitBlock(_->Hash0, 0, 32);
        }
    }
}

#endregion

#region Ncsd : RomFs

public unsafe class RomFs {

    #region Headers

    [StructLayout(LayoutKind.Sequential)]
    public struct SRomFsLevel {
        public long LogicOffset;
        public long Size;
        public uint BlockSize;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SRomFsHeader {
        public uint Signature;
        public uint Id;
        public uint Level0Size;
        public SRomFsLevel Level1;
        public SRomFsLevel Level2;
        public SRomFsLevel Level3;
        public uint Size;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SRomFsMetaInfoSection {
        public uint Offset;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SRomFsMetaInfo {
        public uint Size;
        public SRomFsMetaInfoSection DirHash;
        public SRomFsMetaInfoSection Dir;
        public SRomFsMetaInfoSection FileHash;
        public SRomFsMetaInfoSection File;
        public uint DataOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CommonDirEntry {
        public int ParentDirOffset;
        public int SiblingDirOffset;
        public int ChildDirOffset;
        public int ChildFileOffset;
        public int PrevDirOffset;
        public int NameSize;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CommonFileEntry {
        [FieldOffset(0)] public int ParentDirOffset;
        [FieldOffset(4)] public int SiblingFileOffset;
        [FieldOffset(8)] public long FileOffset; //union
        [FieldOffset(8)] public ulong RemapIgnoreLevel; //union
        [FieldOffset(16)] public long FileSize;
        [FieldOffset(20)] public int PrevFileOffset;
        [FieldOffset(24)] public int NameSize;
    }

    public class CommonEntry {
        public CommonDirEntry Dir;
        public CommonFileEntry File;
    }

    #endregion

    #region Records

    public enum kExtractState { Begin, ChildDir, SiblingDir, End }

    public class ExtractStackElement {
        public bool IsDir;
        public int EntryOffset;
        public CommonEntry Entry;
        public string EntryName;
        public string Prefix;
        public kExtractState ExtractState;
    }

    public class SEntry {
        public string Path;
        public string EntryName;
        public int EntryNameSize;
        public int EntryOffset;
        public int BucketIndex;
        public CommonEntry Entry;
    }

    public class CreateStackElement {
        public int EntryOffset;
        public List<int> ChildOffset;
        public int ChildIndex;
    }

    public class SLevelBuffer {
        public byte[] Data;
        public int DataPos;
        public long FilePos;
    }

    #endregion

    public static readonly uint Signature = 0; // SDW_CONVERT_ENDIAN32('IVFC');
    public static readonly int BlockSizePower = 0xC;
    public static readonly int BlockSize = 1 << BlockSizePower;
    public static readonly int SHA256BlockSize = 0x20;
    public static readonly int InvalidOffset = -1;
    public static readonly int EntryNameAlignment = 4;
    public static readonly long FileSizeAlignment = 0x10;
    //public string FileName;
    //Stream S = null;
    public bool Verbose = false;
    public string RomFsDirName;
    public string RomFsFileName;
    SRomFsHeader RomFsHeader = new();
    long Level3Offset = BlockSize;
    SRomFsMetaInfo RomFsMetaInfo = new();
    Stack<ExtractStackElement> ExtractStack = [];
    List<Regex> Ignores = [];
    List<Regex> RemapIgnores = [];
    List<SEntry> CreateDirs = [];
    List<SEntry> CreateFiles = [];
    Stack<CreateStackElement> CreateStack = [];
    List<int> DirBuckets = [];
    List<int> FileBuckets = [];
    Dictionary<string, CommonFileEntry> TravelInfo;
    bool Remapped = false;
    SLevelBuffer[] LevelBuffer = new SLevelBuffer[4];

    static int RemapIgnoreLevelCompare(CommonFileEntry lhs, CommonFileEntry rhs) => lhs.RemapIgnoreLevel < rhs.RemapIgnoreLevel ? -1 : +1;

    //public void SetFileName(string a_sFileName) => m_sFileName = a_sFileName;
    //public void SetVerbose(bool a_bVerbose) => m_bVerbose = a_bVerbose;
    //public void SetRomFsDirName(string a_sRomFsDirName) => m_sRomFsDirName = a_sRomFsDirName;
    //public void SetRomFsFileName(string a_sRomFsFileName) => m_sRomFsFileName = a_sRomFsFileName;

    public bool ExtractFile(Stream r) {
        try {
            var result = true;
            r.Read(ref RomFsHeader, 0, sizeof(SRomFsHeader));
            Level3Offset = Align(Align(RomFsHeader.Size, SHA256BlockSize) + RomFsHeader.Level0Size, BlockSize);
            r.Seek(Level3Offset, SeekOrigin.Begin);
            r.Read(ref RomFsMetaInfo, 0, sizeof(SRomFsMetaInfo));
            PushExtractStackElement(true, 0, "/");
            while (ExtractStack.Count != 0) {
                var s = ExtractStack.Peek();
                if (s.IsDir) {
                    if (!ExtractDirEntry(r)) result = false;
                }
                else if (!ExtractFileEntry(r)) result = false;
            }
            return result;
        }
        catch (IOException) { return false; }
    }

    public bool CreateFile(Stream r, Stream w) {
        var result = true;
        SetupCreate();
        BuildIgnoreList();
        PushDirEntry("", 0);
        PushCreateStackElement(0);
        while (CreateStack.Count == 0)
            if (!CreateEntryList()) result = false;
        RemoveEmptyDirEntry();
        CreateHash();
        RedirectOffset();
        CreateMetaInfo();
        Remap(w);
        CreateHeader();
        InitLevelBuffer();
        var fileSize = Align(LevelBuffer[2].FilePos + RomFsHeader.Level2.Size, BlockSize);
        try {
            //using (S = File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.Write)) {
            w.Seek2(fileSize);
            if (!UpdateLevelBuffer(w)) result = false;
            return result;
        }
        catch (IOException) { return false; }
    }

    public static bool IsRomFsFile(Stream r) {
        try {
            SRomFsHeader header = new();
            r.Read(ref header, 0, sizeof(SRomFsHeader));
            r.Seek(0, SeekOrigin.Begin);
            return header.Signature == Signature;
        }
        catch (IOException) { return false; }
    }

    void PushExtractStackElement(bool isDir, int entryOffset, string prefix) {
        if (entryOffset == InvalidOffset) return;
        ExtractStack.Push(new ExtractStackElement());
        var s = ExtractStack.Peek();
        s.IsDir = isDir;
        s.EntryOffset = entryOffset;
        s.Prefix = prefix;
        s.ExtractState = kExtractState.Begin;
    }

    bool ExtractDirEntry(Stream r) {
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(r, s);
            var prefix = s.Prefix;
            var dirName = RomFsDirName + prefix;
            if (s.Entry.Dir.NameSize != 0) {
                prefix += s.EntryName + "/";
                dirName += s.EntryName;
            }
            else dirName = dirName[..^1]; //was: sDirName.erase(sDirName.end() - 1);
            if (Verbose) Log.Info($"save: {dirName}");
            if (Directory.CreateDirectory(dirName) == null) { ExtractStack.Pop(); return false; }
            PushExtractStackElement(false, s.Entry.Dir.ChildFileOffset, prefix);
            s.ExtractState = kExtractState.ChildDir;
        }
        else if (s.ExtractState == kExtractState.ChildDir) {
            var prefix = s.Prefix;
            if (s.Entry.Dir.NameSize != 0) prefix += s.EntryName + "/";
            PushExtractStackElement(true, s.Entry.Dir.ChildDirOffset, prefix);
            s.ExtractState = kExtractState.SiblingDir;
        }
        else if (s.ExtractState == kExtractState.SiblingDir) {
            PushExtractStackElement(true, s.Entry.Dir.SiblingDirOffset, s.Prefix);
            s.ExtractState = kExtractState.End;
        }
        else if (s.ExtractState == kExtractState.End) ExtractStack.Pop();
        return true;
    }

    bool ExtractFileEntry(Stream r) {
        var result = true;
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(r, s);
            var path = RomFsDirName + s.Prefix + s.EntryName;
            try {
                using var w = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
                if (Verbose) Log.Info($"save: {path}");
                w.CopyFile(r, Level3Offset + RomFsMetaInfo.DataOffset + s.Entry.File.FileOffset, s.Entry.File.FileSize);
            }
            catch (IOException) { result = false; }
            PushExtractStackElement(false, s.Entry.File.SiblingFileOffset, s.Prefix);
            s.ExtractState = kExtractState.End;
        }
        else if (s.ExtractState == kExtractState.End) ExtractStack.Pop();
        return result;
    }

    void ReadEntry(Stream r, ExtractStackElement element) {
        if (element.IsDir) {
            r.Seek(Level3Offset + RomFsMetaInfo.Dir.Offset + element.EntryOffset, SeekOrigin.Begin);
            r.Read(ref element.Entry.Dir, 0, sizeof(CommonDirEntry));
            var entryName = new byte[element.Entry.Dir.NameSize];
            r.Read(entryName, 0, 2 * element.Entry.Dir.NameSize);
            element.EntryName = Encoding.Unicode.GetString(entryName);
        }
        else {
            r.Seek(Level3Offset + RomFsMetaInfo.File.Offset + element.EntryOffset, SeekOrigin.Begin);
            r.Read(ref element.Entry.File, 0, sizeof(CommonFileEntry));
            var entryName = new byte[element.Entry.File.NameSize];
            r.Read(entryName, 0, 2 * element.Entry.File.NameSize);
            element.EntryName = Encoding.Unicode.GetString(entryName);
        }
    }

    void SetupCreate() {
        RomFsHeader.Signature = Signature;
        RomFsHeader.Id = 0x10000;
        RomFsHeader.Level1.BlockSize = (uint)BlockSizePower;
        RomFsHeader.Level2.BlockSize = (uint)BlockSizePower;
        RomFsHeader.Level3.BlockSize = (uint)BlockSizePower;
        RomFsHeader.Size = (uint)sizeof(SRomFsHeader);
        RomFsMetaInfo.Size = (uint)sizeof(SRomFsMetaInfo);
        RomFsMetaInfo.DirHash.Offset = (uint)(Align(RomFsMetaInfo.Size, EntryNameAlignment));
    }

    void BuildIgnoreList() {
        Ignores.Clear();
        RemapIgnores.Clear();
        string txt = null;
        try {
            var ignorePath = "MODULE/ignore_3dstool.txt";
            using var r = File.Open(ignorePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            r.Seek(0, SeekOrigin.End);
            var size = (int)r.Position;
            r.Seek(0, SeekOrigin.Begin);
            var buf = new byte[size + 1];
            r.Read(buf, 0, size);
            buf[size] = (byte)'\0';
            txt = new string(MemoryMarshal.Cast<byte, char>(buf));
        }
        catch (IOException) { return; }
        var ignores = Ignores;
        foreach (var z in txt.Split("\r\n")) {
            var line = z.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("//")) {
                var tags = line[2..].Split(":");
                if (tags.Length == 2 && string.IsNullOrEmpty(tags[1])) {
                    tags[0] = tags[0].Trim();
                    if (tags[0] == "ignore") ignores = Ignores;
                    else if (tags[0] == "Remap ignore") ignores = RemapIgnores;
                }
            }
            else
                try {
                    ignores.Add(new Regex(line, RegexOptions.ECMAScript | RegexOptions.IgnoreCase));
                }
                catch (ArgumentException e) { Log.Info($"ERROR: {e.Message}\n"); }
        }
    }

    void PushDirEntry(string entryName, int parentDirOffset) {
        SEntry s;
        CreateDirs.Add(s = new SEntry());
        var entry = CreateDirs[parentDirOffset];
        s.Path = CreateDirs.Count == 1 ? RomFsDirName : $"{entry.Path}/{entryName}";
        s.EntryName = entryName;
        s.Entry.Dir.ParentDirOffset = parentDirOffset;
        s.Entry.Dir.SiblingDirOffset = InvalidOffset;
        s.Entry.Dir.ChildDirOffset = InvalidOffset;
        s.Entry.Dir.ChildFileOffset = InvalidOffset;
        s.Entry.Dir.PrevDirOffset = InvalidOffset;
        s.Entry.Dir.NameSize = s.EntryName.Length * 2;
        s.EntryNameSize = (int)Align(s.Entry.Dir.NameSize, EntryNameAlignment);
        if (entry.Entry.Dir.ChildDirOffset != InvalidOffset && CreateDirs.Count - 1 != entry.Entry.Dir.ChildDirOffset)
            CreateDirs[^2].Entry.Dir.SiblingDirOffset = CreateDirs.Count - 1;
    }

    bool PushFileEntry(string entryName, int parentDirOffset) {
        var result = true;
        SEntry s;
        CreateFiles.Add(s = new SEntry());
        var entry = CreateDirs[parentDirOffset];
        s.Path = $"{entry.Path}/{entryName}";
        s.EntryName = entryName;
        s.EntryOffset = (int)Align(RomFsMetaInfo.File.Size, EntryNameAlignment);
        s.Entry.File.ParentDirOffset = parentDirOffset;
        s.Entry.File.SiblingFileOffset = InvalidOffset;
        s.Entry.File.FileOffset = Align(RomFsHeader.Level3.Size, FileSizeAlignment);
        if (s.Path.Length != s.Entry.File.FileSize >> 1) { result = false; Log.Info($"ERROR: {s.Path} stat error\n"); }
        s.Entry.File.PrevFileOffset = InvalidOffset;
        s.Entry.File.NameSize = s.EntryName.Length * 2;
        s.EntryNameSize = (int)Align(s.Entry.File.NameSize, EntryNameAlignment);
        if (entry.Entry.Dir.ChildFileOffset != InvalidOffset && CreateFiles.Count - 1 != entry.Entry.Dir.ChildFileOffset)
            CreateFiles[^2].Entry.File.SiblingFileOffset = CreateFiles.Count - 1;
        RomFsMetaInfo.File.Size = (uint)(s.EntryOffset + sizeof(CommonFileEntry) + s.EntryNameSize);
        RomFsHeader.Level3.Size = s.Entry.File.FileOffset + s.Entry.File.FileSize;
        return result;
    }

    void PushCreateStackElement(int entryOffset) {
        CreateStackElement s;
        CreateStack.Push(s = new CreateStackElement());
        s.EntryOffset = entryOffset;
        s.ChildIndex = -1;
    }

    bool CreateEntryList() {
        var result = true;
        var s = CreateStack.Peek();
        if (s.ChildIndex == -1) {
            var entry = CreateDirs[s.EntryOffset];
            var path = entry.Path; ref CommonDirEntry entryDir = ref entry.Entry.Dir;
            var basePath = path[RomFsDirName.Length..];
            foreach (var name in Directory.EnumerateDirectories(path).Select(Path.GetFileName)) {
                if (MatchInIgnoreList($"{basePath}/{name}")) continue;
                if (entryDir.ChildDirOffset == InvalidOffset) entryDir.ChildDirOffset = CreateDirs.Count();
                s.ChildOffset.Add(CreateDirs.Count());
                PushDirEntry(name, s.EntryOffset);
            }
            foreach (var name in Directory.EnumerateFiles(path).Select(Path.GetFileName)) {
                if (MatchInIgnoreList($"{basePath}/{name}")) continue;
                if (entryDir.ChildFileOffset == InvalidOffset) entryDir.ChildFileOffset = CreateFiles.Count;
                if (!PushFileEntry(name, s.EntryOffset)) result = false;
            }
            s.ChildIndex = 0;
        }
        else if (s.ChildIndex != s.ChildOffset.Count) PushCreateStackElement(s.ChildOffset[s.ChildIndex++]);
        else CreateStack.Pop();
        return result;
    }

    bool MatchInIgnoreList(string path) {
        foreach (var rgx in Ignores)
            if (rgx.IsMatch(path)) return true;
        return false;
    }

    uint GetRemapIgnoreLevel(string path) {
        for (var i = 0; i < RemapIgnores.Count; i++) {
            var rgx = RemapIgnores[i];
            if (rgx.IsMatch(path)) return (uint)i;
        }
        return uint.MaxValue;
    }

    void RemoveEmptyDirEntry() {
        int emptyDirIndex;
        do {
            emptyDirIndex = 0;
            for (var i = CreateDirs.Count - 1; i > 0; i--) {
                var s = CreateDirs[i];
                if (s.Entry.Dir.ChildDirOffset == InvalidOffset && s.Entry.Dir.ChildFileOffset == InvalidOffset) { emptyDirIndex = i; break; }
            }
            if (emptyDirIndex > 0) RemoveDirEntry(emptyDirIndex);
        } while (emptyDirIndex > 0);
        for (var i = 0; i < CreateDirs.Count; i++) {
            var s = CreateDirs[i];
            s.EntryOffset = (int)Align(RomFsMetaInfo.Dir.Size, EntryNameAlignment);
            RomFsMetaInfo.Dir.Size = (uint)(s.EntryOffset + sizeof(CommonDirEntry) + s.EntryNameSize);
        }
    }

    void RemoveDirEntry(int index) {
        var removedEntry = CreateDirs[index];
        var siblingEntry = CreateDirs[index - 1];
        var parentEntry = CreateDirs[removedEntry.Entry.Dir.ParentDirOffset];
        if (siblingEntry.Entry.Dir.SiblingDirOffset == index)
            siblingEntry.Entry.Dir.SiblingDirOffset = removedEntry.Entry.Dir.SiblingDirOffset;
        else if (parentEntry.Entry.Dir.ChildDirOffset == index)
            parentEntry.Entry.Dir.ChildDirOffset = removedEntry.Entry.Dir.SiblingDirOffset;
        for (var i = 0; i < CreateDirs.Count; i++) {
            var s = CreateDirs[i];
            SubDirOffset(ref s.Entry.Dir.ParentDirOffset, index);
            SubDirOffset(ref s.Entry.Dir.SiblingDirOffset, index);
            SubDirOffset(ref s.Entry.Dir.ChildDirOffset, index);
        }
        for (var i = 0; i < CreateFiles.Count; i++) {
            var s = CreateFiles[i];
            SubDirOffset(ref s.Entry.File.ParentDirOffset, index);
        }
        CreateDirs.RemoveAt(index);
    }

    void SubDirOffset(ref int offset, int index) {
        if (offset > index) offset--;
    }

    void CreateHash() {
        DirBuckets.Resize((int)ComputeBucketCount((uint)CreateDirs.Count), InvalidOffset);
        FileBuckets.Resize((int)ComputeBucketCount((uint)CreateFiles.Count), InvalidOffset);
        for (var i = 0; i < CreateDirs.Count; i++) {
            var s = CreateDirs[i];
            s.BucketIndex = (int)Hash(CreateDirs[s.Entry.Dir.ParentDirOffset].EntryOffset, s.EntryName) % DirBuckets.Count;
            if (DirBuckets[s.BucketIndex] != InvalidOffset) s.Entry.Dir.PrevDirOffset = DirBuckets[s.BucketIndex];
            DirBuckets[s.BucketIndex] = i;
        }
        for (var i = 0; i < CreateFiles.Count; i++) {
            var s = CreateFiles[i];
            s.BucketIndex = (int)Hash(CreateDirs[s.Entry.File.ParentDirOffset].EntryOffset, s.EntryName) % FileBuckets.Count;
            if (FileBuckets[s.BucketIndex] != InvalidOffset) s.Entry.File.PrevFileOffset = FileBuckets[s.BucketIndex];
            FileBuckets[s.BucketIndex] = i;
        }
    }

    uint ComputeBucketCount(uint entries) {
        var bucket = entries;
        if (bucket < 3) bucket = 3;
        else if (bucket <= 19) bucket |= 1;
        else while (bucket % 2 == 0 || bucket % 3 == 0 || bucket % 5 == 0 || bucket % 7 == 0 || bucket % 11 == 0 || bucket % 13 == 0 || bucket % 17 == 0) bucket += 1;
        return bucket;
    }

    void RedirectOffset() {
        var _ = 0;
        for (var i = 0; i < DirBuckets.Count; i++)
            DirBuckets[i] = RedirectOffset(ref _, true);
        for (var i = 0; i < FileBuckets.Count; i++)
            FileBuckets[i] = RedirectOffset(ref _, false);
        for (var i = 0; i < CreateDirs.Count; i++) {
            var s = CreateDirs[i];
            RedirectOffset(ref s.Entry.Dir.ParentDirOffset, true);
            RedirectOffset(ref s.Entry.Dir.SiblingDirOffset, true);
            RedirectOffset(ref s.Entry.Dir.ChildDirOffset, true);
            RedirectOffset(ref s.Entry.Dir.ChildFileOffset, false);
            RedirectOffset(ref s.Entry.Dir.PrevDirOffset, true);
        }
        for (var i = 0; i < CreateFiles.Count; i++) {
            var s = CreateFiles[i];
            RedirectOffset(ref s.Entry.File.ParentDirOffset, true);
            RedirectOffset(ref s.Entry.File.SiblingFileOffset, false);
            RedirectOffset(ref s.Entry.File.PrevFileOffset, false);
        }
    }

    int RedirectOffset(ref int offset, bool isDir) {
        if (offset != InvalidOffset)
            return offset = isDir
            ? CreateDirs[offset].EntryOffset
            : CreateFiles[offset].EntryOffset;
        else return offset;
    }

    void CreateMetaInfo() {
        RomFsMetaInfo.DirHash.Size = (uint)(DirBuckets.Count * 4);
        RomFsMetaInfo.Dir.Offset = (uint)(Align(RomFsMetaInfo.DirHash.Offset + RomFsMetaInfo.DirHash.Size, EntryNameAlignment));
        RomFsMetaInfo.FileHash.Offset = (uint)(Align(RomFsMetaInfo.Dir.Offset + RomFsMetaInfo.Dir.Size, EntryNameAlignment));
        RomFsMetaInfo.FileHash.Size = (uint)(FileBuckets.Count * 4);
        RomFsMetaInfo.File.Offset = (uint)(Align(RomFsMetaInfo.FileHash.Offset + RomFsMetaInfo.FileHash.Size, EntryNameAlignment));
        RomFsMetaInfo.DataOffset = (uint)(Align(RomFsMetaInfo.File.Offset + RomFsMetaInfo.File.Size, FileSizeAlignment));
    }

    void Remap(Stream r) {
        if (string.IsNullOrEmpty(RomFsFileName)) { }
        var romFs = new RomFs {
            RomFsFileName = RomFsFileName,
            RomFsDirName = RomFsDirName
        };
        if (!romFs.TravelFile(r)) return;
        foreach (var s in CreateFiles) {
            s.Entry.File.RemapIgnoreLevel = GetRemapIgnoreLevel(s.Path);
            TravelInfo[s.Path] = s.Entry.File;
        }
        Space space = new();
        if (Level3Offset + RomFsMetaInfo.DataOffset > romFs.Level3Offset + romFs.RomFsMetaInfo.DataOffset) {
            foreach (var it in romFs.TravelInfo) {
                var s = it.Value;
                var delta = s.FileOffset - (Level3Offset + RomFsMetaInfo.DataOffset);
                if (delta < 0) {
                    s.FileOffset = Level3Offset + RomFsMetaInfo.DataOffset;
                    s.FileSize += delta;
                    if (s.FileSize < 0) s.FileSize = 0;
                }
            }
        }
        else if (Level3Offset + RomFsMetaInfo.DataOffset < romFs.Level3Offset + romFs.RomFsMetaInfo.DataOffset)
            space.AddSpace(Level3Offset + RomFsMetaInfo.DataOffset, romFs.Level3Offset + romFs.RomFsMetaInfo.DataOffset - Level3Offset - RomFsMetaInfo.DataOffset);
        RomFsHeader.Level3.Size = 0;
        List<CommonFileEntry> remapIgnore = [];
        foreach (var it in romFs.TravelInfo) {
            var s = it.Value;
            if (!TravelInfo.TryGetValue(it.Key, out var t)) {
                space.AddSpace(s.FileOffset, Align(s.FileSize, FileSizeAlignment));
                s.FileSize = 0;
                continue;
            }
            if (Align(t.FileSize, FileSizeAlignment) > Align(s.FileSize, FileSizeAlignment) || t.RemapIgnoreLevel != uint.MaxValue) {
                space.AddSpace(s.FileOffset, Align(s.FileSize, FileSizeAlignment));
                s.FileSize = 0;
                remapIgnore.Add(t);
            }
            else {
                t.FileOffset = s.FileOffset - Level3Offset - RomFsMetaInfo.DataOffset;
                space.AddSpace(Align(s.FileOffset + t.FileSize, FileSizeAlignment), Align(s.FileSize, FileSizeAlignment) - Align(t.FileSize, FileSizeAlignment));
                if (t.FileOffset + t.FileSize > RomFsHeader.Level3.Size) RomFsHeader.Level3.Size = t.FileOffset + t.FileSize;
            }
        }
        if (RomFsHeader.Level3.Size == 0) space.Clear();
        else space.SubSpace(Align(Level3Offset + RomFsMetaInfo.DataOffset + RomFsHeader.Level3.Size, FileSizeAlignment), Align(romFs.Level3Offset + romFs.RomFsHeader.Level3.Size, FileSizeAlignment) - Align(Level3Offset + RomFsMetaInfo.DataOffset + RomFsHeader.Level3.Size, FileSizeAlignment));
        foreach (var it in TravelInfo) {
            var s = it.Value;
            if (!romFs.TravelInfo.ContainsKey(it.Key))
                remapIgnore.Add(s);
        }

        remapIgnore.Sort(RemapIgnoreLevelCompare);
        for (var i = 0; i < remapIgnore.Count; i++) {
            var s = remapIgnore[i];
            var offset = space.GetSpace(Align(s.FileSize, FileSizeAlignment));
            if (offset < 0) {
                s.FileOffset = Align(RomFsHeader.Level3.Size, FileSizeAlignment);
                RomFsHeader.Level3.Size = s.FileOffset + s.FileSize;
            }
            else {
                s.FileOffset = offset - Level3Offset - RomFsMetaInfo.DataOffset;
                space.SubSpace(offset, Align(s.FileSize, FileSizeAlignment));
            }
        }
        foreach (var s in CreateFiles)
            s.Entry.File.FileOffset = TravelInfo[s.Path].FileOffset;
        Remapped = true;
    }

    bool TravelFile(Stream r) {
        try {
            //using (S = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            r.Read(ref RomFsHeader, 0, sizeof(SRomFsHeader));
            Level3Offset = Align(Align(RomFsHeader.Size, SHA256BlockSize) + RomFsHeader.Level0Size, BlockSize);
            r.Seek(Level3Offset, SeekOrigin.Begin);
            r.Read(ref RomFsMetaInfo, 0, sizeof(SRomFsMetaInfo));
            PushExtractStackElement(true, 0, "/");
            while (ExtractStack.Count != 0) {
                var s = ExtractStack.Peek();
                if (s.IsDir) TravelDirEntry(r);
                else TravelFileEntry(r);
            }
            return true;
        }
        catch (IOException) { return false; }
    }

    void TravelDirEntry(Stream r) {
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(r, s);
            var prefix = s.Prefix;
            //var dirName = RomFsDirName + prefix;
            //if (s.Entry.Dir.NameSize != 0) {
            //    prefix += s.EntryName + "/";
            //    dirName += s.EntryName;
            //}
            //else dirName = dirName[..^1]; //was: sDirName.erase(sDirName.end() - 1);
            PushExtractStackElement(false, s.Entry.Dir.ChildFileOffset, prefix);
            s.ExtractState = kExtractState.ChildDir;
        }
        else if (s.ExtractState == kExtractState.ChildDir) {
            var prefix = s.Prefix;
            if (s.Entry.Dir.NameSize != 0) prefix += s.EntryName + "/";
            PushExtractStackElement(true, s.Entry.Dir.ChildDirOffset, prefix);
            s.ExtractState = kExtractState.SiblingDir;
        }
        else if (s.ExtractState == kExtractState.SiblingDir) {
            PushExtractStackElement(true, s.Entry.Dir.SiblingDirOffset, s.Prefix);
            s.ExtractState = kExtractState.End;
        }
        else if (s.ExtractState == kExtractState.End) ExtractStack.Pop();
    }

    void TravelFileEntry(Stream r) {
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(r, s);
            var path = RomFsDirName + s.Prefix + s.EntryName;
            s.Entry.File.FileOffset += Level3Offset + RomFsMetaInfo.DataOffset;
            TravelInfo[path] = s.Entry.File;
            PushExtractStackElement(false, s.Entry.File.SiblingFileOffset, s.Prefix);
            s.ExtractState = kExtractState.End;
        }
        else if (s.ExtractState == kExtractState.End) ExtractStack.Pop();
    }

    void CreateHeader() {
        RomFsHeader.Level3.Size += RomFsMetaInfo.DataOffset;
        RomFsHeader.Level2.Size = Align(RomFsHeader.Level3.Size, BlockSize) / BlockSize * SHA256BlockSize;
        RomFsHeader.Level1.Size = Align(RomFsHeader.Level2.Size, BlockSize) / BlockSize * SHA256BlockSize;
        RomFsHeader.Level0Size = (uint)(Align(RomFsHeader.Level1.Size, BlockSize) / BlockSize * SHA256BlockSize);
        RomFsHeader.Level2.LogicOffset = Align(RomFsHeader.Level1.Size, BlockSize);
        RomFsHeader.Level3.LogicOffset = Align(RomFsHeader.Level2.LogicOffset + RomFsHeader.Level2.Size, BlockSize);
    }

    void InitLevelBuffer() {
        // level 0
        var fileSize = 0L;
        var buffer = LevelBuffer[0];
        Array.Resize(ref buffer.Data, BlockSize); buffer.DataPos = 0; buffer.FilePos = fileSize;
        // level 3
        fileSize = Level3Offset = Align(Align(sizeof(SRomFsHeader), SHA256BlockSize) + RomFsHeader.Level0Size, BlockSize);
        buffer = LevelBuffer[3];
        Array.Resize(ref buffer.Data, BlockSize); buffer.DataPos = 0; buffer.FilePos = fileSize;
        // level 1
        fileSize += Align(RomFsHeader.Level3.Size, BlockSize);
        buffer = LevelBuffer[1];
        Array.Resize(ref buffer.Data, BlockSize); buffer.DataPos = 0; buffer.FilePos = fileSize;
        // level 2
        fileSize += Align(RomFsHeader.Level1.Size, BlockSize);
        buffer = LevelBuffer[2];
        Array.Resize(ref buffer.Data, BlockSize); buffer.DataPos = 0; buffer.FilePos = fileSize;
    }

    bool UpdateLevelBuffer(Stream w) {
        var result = true;
        WriteBuffer(w, 0, ref RomFsHeader, sizeof(SRomFsHeader));
        AlignBuffer(w, 0, SHA256BlockSize);
        WriteBuffer(w, 3, ref RomFsMetaInfo, sizeof(SRomFsMetaInfo));
        WriteBuffer(w, 3, ref DirBuckets, RomFsMetaInfo.DirHash.Size);
        for (var i = 0; i < CreateDirs.Count; i++) {
            var s = CreateDirs[i];
            WriteBuffer(w, 3, ref s.Entry.Dir, sizeof(CommonDirEntry));
            WriteBuffer(w, 3, Encoding.Unicode.GetBytes(s.EntryName), s.EntryNameSize);
        }
        WriteBuffer(w, 3, ref FileBuckets, RomFsMetaInfo.FileHash.Size);
        for (var i = 0; i < CreateFiles.Count; i++) {
            var s = CreateFiles[i];
            WriteBuffer(w, 3, ref s.Entry.File, sizeof(CommonFileEntry));
            WriteBuffer(w, 3, ref s.EntryName, s.EntryNameSize);
        }
        if (!Remapped) {
            for (var i = 0; i < CreateFiles.Count; i++) {
                AlignBuffer(w, 3, (int)FileSizeAlignment);
                var s = CreateFiles[i];
                if (!WriteBufferFromFile(w, 3, s.Path, s.Entry.File.FileSize)) result = false;
            }
        }
        else {
            Dictionary<long, SEntry> createFiles = [];
            for (var i = 0; i < CreateFiles.Count; i++)
                if (CreateFiles[i].Entry.File.FileSize != 0)
                    createFiles.Add(CreateFiles[i].Entry.File.FileOffset, CreateFiles[i]);
            var buffer3 = LevelBuffer[3];
            foreach (var s in createFiles.Values) {
                WriteBuffer(w, 3, null, Level3Offset + RomFsMetaInfo.DataOffset + s.Entry.File.FileOffset - (buffer3.FilePos + buffer3.DataPos));
                if (!WriteBufferFromFile(w, 3, s.Path, s.Entry.File.FileSize)) result = false;
            }
        }
        AlignBuffer(w, 3, BlockSize);
        AlignBuffer(w, 2, BlockSize);
        AlignBuffer(w, 1, BlockSize);
        AlignBuffer(w, 0, BlockSize);
        return result;
    }

    void WriteBuffer<T>(Stream w, int level, ref T src, long size) {
        throw new NotImplementedException();
        //WriteBuffer(w, level, null, size);
    }
    void WriteBuffer(Stream w, int level, byte[] src, long size) {
        var buffer = LevelBuffer[level];
        fixed (byte* _data = buffer.Data)
        fixed (byte* _src = src) {
            var psrc = _src;
            do {
                var remainSize = BlockSize - buffer.DataPos;
                var size2 = size > remainSize ? remainSize : size;
                if (size2 > 0) {
                    if (src != null) {
                        Unsafe.CopyBlock(_data + buffer.DataPos, psrc, (uint)size2);
                        psrc += size2;
                    }
                    buffer.DataPos += (int)size2;
                }
                if (buffer.DataPos == BlockSize) {
                    if (level != 0) WriteBuffer(w, level - 1, ToSha256(buffer.Data, BlockSize, null), SHA256BlockSize);
                    w.Seek(buffer.FilePos, SeekOrigin.Begin);
                    w.Write(buffer.Data, 0, BlockSize);
                    Unsafe.InitBlock(ref buffer.Data[0], 0, (uint)BlockSize);
                    buffer.DataPos = 0;
                    buffer.FilePos += BlockSize;
                }
                size -= size2;
            } while (size > 0);
        }
    }

    const long _write_bufferSize = 0x100000;
    static byte[] _write_buf = new byte[_write_bufferSize];
    bool WriteBufferFromFile(Stream w, int level, string path, long size) {
        try {
            using var r = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Verbose) Log.Info($"load: {path}");
            while (size > 0) {
                var size2 = size > _write_bufferSize ? _write_bufferSize : size;
                r.Read(_write_buf, 0, (int)size2);
                WriteBuffer(w, level, ref _write_buf, size2);
                size -= size2;
            }
            return true;
        }
        catch (IOException) { return false; }
    }

    void AlignBuffer(Stream w, int level, int alignment) {
        var buffer = LevelBuffer[level];
        buffer.DataPos = (int)Align(buffer.DataPos, alignment);
        WriteBuffer(w, level, null, 0);
    }

    static uint Hash(int parentOffset, string entryName) {
        var hash = (uint)(parentOffset ^ 123456789);
        for (var i = 0; i < entryName.Length; i++)
            hash = ((hash >> 5) | (hash << 27)) ^ entryName[i];
        return hash;
    }
}

#endregion

#region ExeFs : Banner

//public unsafe class Banner {
//    struct CbmdHeader {
//        public uint Signature;
//        public uint CbmdOffset;
//        public fixed uint CgfxOffset[31];
//        public uint CwavOffset;
//    }
//    public Banner() => throw new NotImplementedException();
//    public void Dispose() => throw new NotImplementedException();
//    public void SetFileName(string fileName) => throw new NotImplementedException();
//    public void SetVerbose(bool verbose) => throw new NotImplementedException();
//    public void SetBannerDirName(string bannerDirName) => throw new NotImplementedException();
//    public bool ExtractFile() => throw new NotImplementedException();
//    public bool CreateFile() => throw new NotImplementedException();
//    public static bool IsBannerFile(string fileName) => throw new NotImplementedException();
//    public static readonly uint Signature;
//    public static readonly int CbmdSizeAlignment;
//    public static readonly string CbmdHeaderFileName;
//    public static readonly string CbmdBodyFileName;
//    public static readonly string BcwavFileName;
//    bool ExtractCbmdHeader() => throw new NotImplementedException();
//    bool ExtractCbmdBody() => throw new NotImplementedException();
//    bool ExtractBcwav() => throw new NotImplementedException();
//    bool CreateCbmdHeader() => throw new NotImplementedException();
//    bool CreateCbmdBody() => throw new NotImplementedException();
//    bool CreateBcwav() => throw new NotImplementedException();
//    string FileName;
//    bool Verbose;
//    string BannerDirName;
//    Stream Banner_;
//    CbmdHeader CbmdHeader_;
//}

#endregion

#region ExeFs : Code


public unsafe class Code {

    public struct SFunction(int first, int last) {
        public int First = first;
        public int Last = last;
    }

    public string FileName;
    public bool Verbose = false;
    public int RegionCode = -1;
    public int LanguageCode = -1;
    uint[] Arm;
    byte[] Thumb;
    CapstoneArmDisassembler Handle;
    ArmInstruction[] Disasm = null;

    //public void SetFileName(string fileName) => throw new NotImplementedException();
    //public void SetVerbose(bool verbose) => throw new NotImplementedException();
    //public void SetRegionCode(int regionCode) => throw new NotImplementedException();
    //public void SetLanguageCode(int languageCode) => throw new NotImplementedException();

    public bool Lock() {
        try {
            byte[] code = [];
            using (var s = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                s.Seek(0, SeekOrigin.End);
                var codeSize = (int)s.Position;
                if (codeSize < 4) { Log.Info("ERROR: code is too short\n\n"); return false; }
                Array.Resize(ref code, codeSize);
                s.Seek(0, SeekOrigin.Begin);
                s.Read(code, 0, code.Length);
            }
            Arm = MemoryMarshal.Cast<byte, uint>(code).ToArray();
            Thumb = code;
            bool resultArm = LockArm(), resultThumb = resultArm && LockThumb();
            using (var s = File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.Write))
                s.Write(code, 0, code.Length);
            if (!resultArm && !resultThumb) Log.Info("ERROR: lock failed\n\n");
            return resultArm || resultThumb;
        }
        catch (IOException) { return false; }
    }

    bool LockArm() {
        if (Verbose) Log.Info("INFO: lock arm\n");
        try {
            var result = true;
            using (Handle = new CapstoneArmDisassembler(ArmDisassembleMode.Arm) { EnableInstructionDetails = true }) {
                if (RegionCode >= 0) {
                    if (Verbose) Log.Info("INFO: lock region arm\n");
                    var resultRegion = LockRegionArm();
                    if (!resultRegion) Log.Info("ERROR: lock region arm failed\n\n");
                    result = result && resultRegion;
                }
                if (LanguageCode >= 0) {
                    if (Verbose) Log.Info("INFO: lock language arm\n");
                    var resultLanguage = LockLanguageArm();
                    if (!resultLanguage) Log.Info("ERROR: lock language arm failed\n\n");
                    result = result && resultLanguage;
                }
                if (!result) Log.Info("ERROR: lock arm failed\n\n");
                return result;
            }
        }
        catch (CapstoneException) { Log.Info("ERROR: open arm handlefailed\n\n"); return false; }
    }

    bool LockThumb() {
        if (Verbose) Log.Info("INFO: lock thumb\n");
        try {
            var result = true;
            using (Handle = new CapstoneArmDisassembler(ArmDisassembleMode.Thumb) { EnableInstructionDetails = true }) {
                if (RegionCode >= 0) {
                    if (Verbose) Log.Info("INFO: lock region thumb\n");
                    var resultRegion = LockRegionThumb();
                    if (!resultRegion) Log.Info("ERROR: lock region thumb failed\n\n");
                    result = result && resultRegion;
                }
                if (LanguageCode >= 0) {
                    if (Verbose) Log.Info("INFO: lock language thumb\n");
                    var resultLanguage = LockLanguageThumb();
                    if (!resultLanguage) Log.Info("ERROR: lock language thumb failed\n\n");
                    result = result && resultLanguage;
                }
            }
            if (!result) Log.Info("ERROR: lock thumb failed\n\n");
            return result;
        }
        catch (CapstoneException) { Log.Info("ERROR: open thumb handle failed\n\n"); return false; }
    }

    bool LockRegionArm() {
        if (Verbose) Log.Info("INFO: find arm nn::cfg::CTR::detail::IpcUser::GetRegion\n");
        SFunction functionGetRegion = new();
        FindGetRegionFunctionArm(ref functionGetRegion);
        if (functionGetRegion.First == functionGetRegion.Last || functionGetRegion.Last == 0) return false;
        if (Verbose) {
            Log.Info("INFO: nn::cfg::CTR::detail::IpcUser::GetRegion\n");
            Log.Info("INFO:   func:\n");
            Log.Info($"INFO:     first: {functionGetRegion.First * 4:08X}\n");
            Log.Info($"INFO:     last:  {functionGetRegion.Last * 4:08X}\n");
        }
        return PatchGetRegionFunctionArm(functionGetRegion);
    }

    bool LockRegionThumb() {
        if (Verbose) Log.Info("INFO: find thumb nn::cfg::CTR::detail::IpcUser::GetRegion\n");
        SFunction functionGetRegion = new();
        FindGetRegionFunctionThumb(ref functionGetRegion);
        if (functionGetRegion.First == functionGetRegion.Last || functionGetRegion.Last == 0) return false;
        if (Verbose) {
            Log.Info("INFO: nn::cfg::CTR::detail::IpcUser::GetRegion\n");
            Log.Info("INFO:   func:\n");
            Log.Info($"INFO:     first: {functionGetRegion.First:08X}\n");
            Log.Info($"INFO:     last:  {functionGetRegion.Last:8X}\n");
        }
        return PatchGetRegionFunctionThumb(functionGetRegion);
    }

    bool LockLanguageArm() {
        if (Verbose) Log.Info("INFO: find arm nn::cfg::CTR::GetLanguage\n");
        SFunction functionGetLanguage = new();
        FindGetLanguageFunctionArm(functionGetLanguage);
        if (functionGetLanguage.First == functionGetLanguage.Last || functionGetLanguage.Last == 0) return false;
        if (Verbose) {
            Log.Info("INFO: nn::cfg::CTR::GetLanguage\n");
            Log.Info("INFO:   func:\n");
            Log.Info($"INFO:     first: {functionGetLanguage.First * 4:08X}\n");
            Log.Info($"INFO:     last:  {functionGetLanguage.Last * 4:08X}\n");
        }
        return PatchGetLanguageFunctionArm(functionGetLanguage);
    }

    bool LockLanguageThumb() {
        if (Verbose) Log.Info("INFO: find thumb nn::cfg::CTR::GetLanguageRaw\n");
        SFunction functionGetLanguage = new();
        FindGetLanguageFunctionThumb(functionGetLanguage);
        if (functionGetLanguage.First == functionGetLanguage.Last || functionGetLanguage.Last == 0) return false;
        if (Verbose) {
            Log.Info("INFO: nn::cfg::CTR::GetLanguageRaw\n");
            Log.Info("INFO:   func:\n");
            Log.Info($"INFO:     first: {functionGetLanguage.First:08X}\n");
            Log.Info($"INFO:     last:  {functionGetLanguage.Last:08X}\n");
        }
        return PatchGetLanguageFunctionThumb(functionGetLanguage);
    }

    void FindGetRegionFunctionArm(ref SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        var functions = new List<SFunction>();
        for (var i = 0; i < Arm.Length; i++) {
            // mov r0, #0x20000
            if (Arm[i] == 0xE3A00802) {
                SFunction func = new(i, i);
                FindFunctionArm(ref func);
                for (var j = i + 1; j < func.Last; j++)
                    // svc 0x32
                    if (Arm[j] == 0xEF000032) { functions.Add(func); break; }
            }
        }
        // nn::cfg::CTR::detail::Initialize
        for (var i = 0; i < Arm.Length; i++) {
            // nn::srv::Initialize
            // nn::Result
            // Level	-5 LEVEL_PERMANENT
            // Summary	5 SUMMARY_INVALID_STATE
            // Module	64 MODULE_NN_CFG
            if (Arm[i] == 0xD8A103F9)
                for (var j = i - 4; j < i + 4; j++)
                    if (j >= 0 && j < Arm.Length)
                        foreach (var func in functions)
                            // nn::cfg::CTR::detail::IpcUser::s_Session
                            if (func.Last + 1 < Arm.Length && Arm[j] == Arm[func.Last + 1]) { function.First = func.First; function.Last = func.Last; return; }
        }
        for (var i = 0; i < Arm.Length; i++) {
            // mov r0, #0x20000
            if (Arm[i] == 0xE3A00802) {
                SFunction func = new(i, i);
                FindFunctionArm(ref func);
                for (var j = i + 1; j < func.Last; j++) {
                    // nn::svc::SendSyncRequest
                    Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(j)).ToArray(), 4, 0x100000 + j * 4);
                    if (Disasm.Length > 1 && (insn = Disasm[0]) != null)
                        if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                            var armOp0 = detail.Operands[0];
                            if (armOp0.Type == Immediate && armOp0.Immediate >= 0x100000 && armOp0.Immediate + 8 <= 0x100000 + Arm.Length * 4 && armOp0.Immediate % 4 == 0) {
                                var kFunction = (armOp0.Immediate - 0x100000) / 4;
                                // svc 0x32
                                // bx lr
                                if (Arm[kFunction] == 0xEF000032 && Arm[kFunction + 1] == 0xE12FFF1E) { functions.Add(func); break; }
                            }
                        }
                }
            }
        }
        // nn::cfg::CTR::detail::Initialize
        for (var i = 0; i < Arm.Length; i++) {
            // nn::srv::Initialize
            // nn::Result
            // Level	-5 LEVEL_PERMANENT
            // Summary	5 SUMMARY_INVALID_STATE
            // Module	64 MODULE_NN_CFG
            if (Arm[i] == 0xD8A103F9)
                for (var j = i - 4; j < i + 4; j++)
                    if (j >= 0 && j < Arm.Length)
                        foreach (var func in functions)
                            // nn::cfg::CTR::detail::IpcUser::s_Session
                            if (func.Last + 1 < Arm.Length && Arm[j] == Arm[func.Last + 1]) { function.First = func.First; function.Last = func.Last; return; }
        }
    }

    void FindGetRegionFunctionThumb(ref SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        SFunction functionGetLanguage = new();
        FindGetLanguageFunctionThumb(functionGetLanguage);
        if (functionGetLanguage.First == functionGetLanguage.Last || functionGetLanguage.Last == 0) return;
        // nn::cfg::CTR::GetLanguage
        // nn::cfg::CTR::GetLanguageRaw()
        int getLanguage = -1, codeSizeMax = 4;
        for (var i = 0; i < Thumb.Length; i += 2) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length != 0 && (insn = Disasm[0]) != null) //!cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Immediate && armOp0.Immediate == 0x100000 + functionGetLanguage.First - 2) over = true;
                }
            //cs_free(Disasm, Disasm.Length);
            if (over) { getLanguage = i; break; }
        }
        if (getLanguage < 0) return;
        int getRegion = -1, codeSize = 4;
        for (var i = getLanguage + 4; i < Thumb.Length; i += codeSize) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            //Handle.GetInstructionGroupName(ArmInstructionGroupId.ARM_GRP_THUMB2);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) { //!cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Immediate && armOp0.Immediate >= 0x100000 && armOp0.Immediate < 0x100000 + Thumb.Length) {
                        getRegion = armOp0.Immediate - 0x100000;
                        over = true;
                    }
                }
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) break;
        }
        SFunction functionGetRegion = new(getRegion, getRegion);
        FindFunctionThumb(ref functionGetRegion);
        for (var i = functionGetRegion.First + 2; i < functionGetRegion.Last; i += codeSize) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {  //&& !cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Immediate && armOp0.Immediate >= 0x100000 && armOp0.Immediate < 0x100000 + Thumb.Length) {
                        getRegion = armOp0.Immediate - 0x100000;
                        over = true;
                    }
                }
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) break;
        }
        function.Last = function.First = getRegion + 4;
        FindFunctionThumb(ref function);
    }

    void FindGetLanguageFunctionArm(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = 0; i < Arm.Length; i++) {
            // nn::cfg::CTR::detail::GetConfig
            // key
            if (Arm[i] == 0xA0002) {
                var index = i - 4;
                if (index < 0) index = 0;
                SFunction func = new(index, index);
                FindFunctionArm(ref func);
                for (var j = func.First + 1; j < func.Last; j++) {
                    var over = false;
                    Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(j)).ToArray(), 4, 0x100000 + j * 4);
                    if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                        if (insn.Mnemonic == "ldr" && (detail = insn.Details) != null && detail.Operands.Length > 1) {
                            var armOp0 = detail.Operands[0];
                            var armOp1 = detail.Operands[1];
                            // ldr rm, =0xA0002
                            if (armOp0.Type == Register && armOp1.Type == Memory && armOp1.Register.Id == ARM_REG_PC && armOp1.Memory.Displacement == (i - j - 2) * 4) over = true;
                        }
                    }
                    //cs_free(m_pInsn, m_uDisasmCount);
                    if (over) { function.First = func.First; function.Last = func.Last; return; }
                }
            }
        }
    }

    void FindGetLanguageFunctionThumb(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = 0; i < Thumb.Length; i += 4) {
            // nn::cfg::CTR::detail::GetConfig
            // key
            if (MemoryMarshal.Cast<byte, uint>(Thumb.AsSpan(i, 2))[0] == 0xA0002) {
                var offset = i - 16;
                if (offset < 0) offset = 0;
                SFunction func = new(offset, offset);
                FindFunctionThumb(ref func);
                var codeSize = 4;
                for (var j = func.First; j < func.Last; j += codeSize) {
                    var over = false;
                    var codeSizeMax = func.Last - j;
                    if (codeSizeMax > 4) codeSizeMax = 4;
                    codeSize = 2;
                    Disasm = Handle.Disassemble(Thumb.AsSpan(j).ToArray(), codeSizeMax, 0x100000 + j);
                    if (Disasm.Length > 0 && (insn = Disasm[0]) != null) { // !cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                        codeSize = insn.Bytes.Length;
                        if (insn.Mnemonic == "ldr" && (detail = insn.Details) != null && detail.Operands.Length > 1) {
                            var armOp0 = detail.Operands[0];
                            var armOp1 = detail.Operands[1];
                            // ldr rm, =0xA0002
                            if (armOp0.Type == Register && armOp1.Type == Memory && armOp1.Register.Id == ARM_REG_PC && armOp1.Memory.Displacement == i - j - 4) over = true;
                        }
                    }
                    //cs_free(m_pInsn, m_uDisasmCount);
                    if (over) { function.First = func.First; function.Last = func.Last; return; }
                }
            }
        }
    }

    void FindFunctionArm(ref SFunction function) {
        ArmInstruction insn;
        for (var i = function.Last; i < Arm.Length; i++) {
            var over = false;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null)
                if (insn.Mnemonic == "pop" || insn.Mnemonic == "bx" || insn.Mnemonic == "lr") over = true;
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.Last = i; break; }
        }
        for (var i = function.First; i >= 0; i--) {
            var over = false;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null)
                if (insn.Mnemonic == "push") over = true;
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.First = i; break; }
        }
    }

    void FindFunctionThumb(ref SFunction function) {
        ArmInstruction insn;
        int codeSize = 4, codeSizeMax = 4;
        for (var i = function.Last; i < Thumb.Length; i += codeSize) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) { // !cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "pop" || insn.Mnemonic == "bx" && insn.Operand == "lr") over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.Last = i; break; }
        }
        int codeSizeCache = 0;
        for (var i = function.First; i >= 0; i -= codeSize) {
            var over = false;
            if (codeSizeCache == 0) {
                codeSizeMax = Thumb.Length - i;
                if (codeSizeMax > 4) codeSizeMax = 4;
            }
            else {
                codeSizeMax = codeSizeCache;
                codeSizeCache = 0;
            }
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length == 0 && i >= 2) { //|| cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                i -= 2;
                codeSizeMax += 2;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length == 2 && (insn = Disasm[0]) != null) {
                codeSizeCache = insn.Bytes.Length;
                i += codeSizeCache;
                codeSizeMax -= codeSizeCache;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                if (codeSizeCache == 0) {
                    codeSize = 4;
                    if (i < 4) codeSize = 2;
                }
                else codeSize = codeSizeCache;
                if (insn.Mnemonic == "push") over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.First = i; break; }
        }
    }

    bool PatchGetRegionFunctionArm(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = function.First + 1; i < function.Last; i++) {
            var over = false;
            var rt = -1;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register)
                        switch (armOp0.Register.Id) {
                            case ARM_REG_R0: rt = 0; break;
                            case ARM_REG_R1: rt = 1; break;
                            case ARM_REG_R2: rt = 2; break;
                            case ARM_REG_R3: rt = 3; break;
                            case ARM_REG_R4: rt = 4; break;
                            case ARM_REG_R5: rt = 5; break;
                            case ARM_REG_R6: rt = 6; break;
                            case ARM_REG_R7: rt = 7; break;
                            case ARM_REG_R8: rt = 8; break;
                            case ARM_REG_R9: rt = 9; break;
                            case ARM_REG_R10: rt = 10; break;
                            case ARM_REG_R11: rt = 11; break;
                            case ARM_REG_R12: rt = 12; break;
                            case ARM_REG_R13: rt = 13; break; // ARM_REG_SP
                            case ARM_REG_R14: rt = 14; break; // ARM_REG_LR
                            case ARM_REG_R15: rt = 15; break; // ARM_REG_PC
                        }
                }
                if (rt >= 0) over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                Arm[i] = 0xE3A00000U | (uint)(rt << 12) | (uint)RegionCode;
                Log.Info($"INFO:   modify:  {i * 4:08X}  mov r{rt}, #0x{RegionCode:x} ; {Arm[i] & 0xFF:02X} {Arm[i] >> 8 & 0xFF:02X} {Arm[i] >> 16 & 0xFF:02X} {Arm[i] >> 24 & 0xFF:02X}\n");
                return true;
            }
        }
        return false;
    }

    bool PatchGetRegionFunctionThumb(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        var codeSize = 4;
        for (var i = function.First + 2; i < function.Last; i += codeSize) {
            var over = false;
            var rt = -1;
            var codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register)
                        switch (armOp0.Register.Id) {
                            case ARM_REG_R0: rt = 0; break;
                            case ARM_REG_R1: rt = 1; break;
                            case ARM_REG_R2: rt = 2; break;
                            case ARM_REG_R3: rt = 3; break;
                            case ARM_REG_R4: rt = 4; break;
                            case ARM_REG_R5: rt = 5; break;
                            case ARM_REG_R6: rt = 6; break;
                            case ARM_REG_R7: rt = 7; break;
                            case ARM_REG_R8: rt = 8; break;
                            case ARM_REG_R9: rt = 9; break;
                            case ARM_REG_R10: rt = 10; break;
                            case ARM_REG_R11: rt = 11; break;
                            case ARM_REG_R12: rt = 12; break;
                            case ARM_REG_R13: rt = 13; break;// ARM_REG_SP
                            case ARM_REG_R14: rt = 14; break; // ARM_REG_LR
                            case ARM_REG_R15: rt = 15; break; // ARM_REG_PC
                        }
                }
                if (rt >= 0) over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                fixed (byte* _ = &Thumb[i]) *(uint*)_ = 0x2000U | (uint)(rt << 8) | (uint)RegionCode;
                Log.Info($"INFO:   modify:  {i:08X}  mov r{rt}, #0x{RegionCode:x} ; {Thumb[i]:02X} {Thumb[i + 1]:02X}\n");
                return true;
            }
        }
        return false;
    }

    bool PatchGetLanguageFunctionArm(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = function.Last - 1; i > function.First; i--) {
            var over = false;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null)
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register && armOp0.Register.Id == ARM_REG_R0) over = true;
                }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                Arm[i] = 0xE3A00000U | (uint)LanguageCode;
                Log.Info($"INFO:   modify:  {i * 4:08X}  mov r0, #0x{LanguageCode:x} ; {Arm[i] & 0xFF:02X} {Arm[i] >> 8 & 0xFF:02X} {Arm[i] >> 16 & 0xFF:02X} {Arm[i] >> 24 & 0xFF:02X}\n");
                return true;
            }
        }
        return false;
    }

    bool PatchGetLanguageFunctionThumb(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        var codeSize = 4;
        var codeSizeMax = 4;
        var codeSizeCache = 0;
        for (var i = function.Last - 2; i > function.First; i -= codeSize) {
            var over = false;
            if (codeSizeCache == 0) {
                codeSizeMax = Thumb.Length - i;
                if (codeSizeMax > 4) codeSizeMax = 4;
            }
            else { codeSizeMax = codeSizeCache; codeSizeCache = 0; }
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if ((Disasm.Length == 0 && i > function.First + 2)) { // || cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                i -= 2;
                codeSizeMax += 2;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length == 2 && (insn = Disasm[0]) != null) {
                codeSizeCache = insn.Bytes.Length;
                i += codeSizeCache;
                codeSizeMax -= codeSizeCache;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                if (codeSizeCache == 0) {
                    codeSize = 4;
                    if (i <= function.Last + 4) codeSize = 2;
                }
                else codeSize = codeSizeCache;
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register && armOp0.Register.Id == ARM_REG_R0) over = true;
                }
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                fixed (byte* _ = &Thumb[i]) *(uint*)_ = 0x2000U | (uint)LanguageCode;
                Log.Info($"INFO:   modify:  {i:08X}  mov r0, #0x{LanguageCode:x} ; {Thumb[i]:02X} {Thumb[i + 1]:02X}\n");
                return true;
            }
        }
        return false;
    }
}

#endregion

#region Util : BackwardLz77

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
    //static readonly int CompressWorkSize = (4098 + 4098 + 256 + 256) * sizeof(ushort);
}

#endregion

#region Util : Crypt

public static class CryptUtil {
    public static void CopyToAsAes(this Stream dst, Stream src, byte[] key, byte[] iv, int blockSize = 128) {
        using var crypt = Aes.Create();
        crypt.Key = key;
        crypt.IV = iv;
        crypt.BlockSize = blockSize;
        using var s = new MemoryStream();
        using var w = new CryptoStream(s, crypt.CreateEncryptor(), CryptoStreamMode.Write);
        src.CopyTo(dst);
    }
}

public unsafe static class Crypt {
    public static void FEncryptAesCtrCopyFile(Stream dst, Stream src, BigInteger key, BigInteger counter, long srcOffset, long size) {
        var key2 = key.ToByteArray(); Array.Resize(ref key2, 16);
        var iv2 = counter.ToByteArray(); Array.Resize(ref iv2, 16);
        src.Seek(srcOffset, SeekOrigin.Begin);
        dst.CopyToAsAes(src, key2, iv2);
    }
    public static bool FEncryptAesCtrFile(string dataFileName, BigInteger key, BigInteger counter, long dataOffset, long dataSize, bool dataFileAll, long xorOffset) => throw new NotImplementedException();
    public static bool FEncryptXorFile(string dataFileName, string xorFileName) => throw new NotImplementedException();
    public static void FEncryptAesCtrData(byte[] data, long offset, BigInteger key, BigInteger counter, long dataSize, long xorOffset) => throw new NotImplementedException();
    public static void FEncryptAesCtrData(byte* data, long offset, BigInteger key, BigInteger counter, long dataSize, long xorOffset) => throw new NotImplementedException();
}

#endregion

#region Util : Space

public class Space {
    public struct SBuffer(long top, long bottom) {
        public long Top = top;
        public long Bottom = bottom;
    }

    readonly List<SBuffer> Buffers = [];

    public bool AddSpace(long offset, long size) {
        if (size == 0) return true;
        long top = offset, bottom = offset + size;
        for (var i = 0; i < Buffers.Count; i++) {
            var buffer = Buffers[i];
            if ((top >= buffer.Top && top < buffer.Bottom) || (bottom > buffer.Top && bottom <= buffer.Bottom)) { Log.Info($"ERROR: [0x{top:x}, 0x{bottom:x}) [0x{buffer.Top:x}, 0x{buffer.Bottom:x}) overlap\n\n"); return false; }
            if (bottom < buffer.Top) { Buffers.Insert(i, new SBuffer(top, bottom)); return true; }
            else if (bottom == buffer.Top) { buffer.Top = top; return true; }
            else if (top == buffer.Bottom) {
                var next = Buffers[++i];
                if (i == Buffers.Count || bottom < next.Top) { buffer.Bottom = bottom; return true; }
                else if (bottom == next.Top) {
                    buffer.Bottom = next.Bottom;
                    Buffers.RemoveAt(i);
                    return true;
                }
            }
        }
        Buffers.Add(new SBuffer(top, bottom));
        return true;
    }

    public bool SubSpace(long offset, long size) {
        if (size == 0) return true;
        long top = offset, bottom = offset + size;
        for (var i = 0; i < Buffers.Count; i++) {
            var buffer = Buffers[i];
            if (top == buffer.Top && bottom == buffer.Bottom) { Buffers.RemoveAt(i); return true; }
            else if (top == buffer.Top && bottom < buffer.Bottom) { buffer.Top = bottom; return true; }
            else if (top > buffer.Top && bottom == buffer.Bottom) { buffer.Bottom = top; return true; }
            else if (top > buffer.Top && bottom < buffer.Bottom) {
                ++i;
                Buffers.Insert(i, new SBuffer(bottom, buffer.Bottom));
                buffer.Bottom = top;
                return true;
            }
        }
        return false;
    }

    public void Clear() => Buffers.Clear();

    public long GetSpace(long size) {
        foreach (var buffer in Buffers)
            if (buffer.Bottom - buffer.Top >= size) return buffer.Top;
        return -1;
    }
}

#endregion