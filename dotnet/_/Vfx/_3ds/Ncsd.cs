using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Console;

namespace OpenStack.Rom.Nintendo._3ds;
using static OpenStack.Rom.Nintendo._3ds.Ncch;
using static Util;

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
            if (NotPad && Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD2) { NotPad = false; if (Verbose) WriteLine("INFO: not support --not-pad with CARD2 type"); }
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
            if (Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD2) { if (Verbose) WriteLine("INFO: not support --trim with CARD2 type"); return false; }
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
            if (Header.Ncsd.Flags[MEDIA_TYPE_INDEX] == (byte)MediaType.CARD2) { if (Verbose) WriteLine("INFO: not support --pad with CARD2 type"); return false; }
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
                    if (Verbose) WriteLine($"save: {fileName}");
                    if (mediaUnitSize) {
                        offset *= MediaUnitSize;
                        size *= MediaUnitSize;
                    }
                    w.CopyFile(r, offset, size);
                }
                catch (IOException) { result = false; }
            }
            else if (Verbose) {
                if (typeId < 0 || typeId >= 8) WriteLine($"INFO: {type} is not exists, {fileName} will not be create");
                else WriteLine($"INFO: {type} {typeId} is not exists, {fileName} will not be create");
            }
        }
        else if ((offset != 0 || size != 0) && Verbose) {
            if (typeId < 0 || typeId >= 8) WriteLine($"INFO: {type} is not extract");
            else WriteLine($"INFO: {type} {typeId} is not extract");
        }
        return result;
    }

    bool CreateHeader(Stream w) {
        try {
            using (var r = File.Open(HeaderFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                r.Seek(0, SeekOrigin.End);
                var fileSize = r.Position;
                if (fileSize < sizeof(SNcsdHeader) + sizeof(CardInfoHeader)) { WriteLine("ERROR: ncsd header is too short\n"); return false; }
                if (Verbose) WriteLine($"load: {HeaderFileName}");
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
            if (Verbose) WriteLine($"load: {NcchFileName[index]}");
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
