using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Console;

namespace OpenStack.Rom.Nintendo._3ds;
using static Util;

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
                S.Read(ref SuperBlock, sizeof(ExeFsSuperBlock), 1);
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
                S.Write(ref SuperBlock, sizeof(ExeFsSuperBlock), 1);
                return result;
            }
        }
        catch (IOException) { return false; }
    }

    public static bool IsExeFsFile(string fileName, long offset) {
        try {
            using var s = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            ExeFsSuperBlock superBlock = new();
            s.Read(ref superBlock, sizeof(ExeFsSuperBlock), 1);
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
        if (string.IsNullOrEmpty(HeaderFileName)) { if (Verbose) WriteLine("INFO: exefs header is not extract\n"); return result; }
        try {
            using var s = File.Open(HeaderFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            if (Verbose) WriteLine($"save: {HeaderFileName}\n");
            s.Write(ref SuperBlock, sizeof(ExeFsSuperBlock), 1);
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
            else { if (Verbose) WriteLine($"INFO: unknown entry Name {name}\n"); path = $"{ExeFsDirName}/{name}.bin"; }
        }
        else path = $"{ExeFsDirName}/{z}";
        try {
            using var s = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
            if (Verbose) WriteLine($"save: {Path}\n");
            if (topSection && Uncompress) {
                var compressedSize = header.Size;
                s.Seek(sizeof(ExeFsSuperBlock) + header.Offset, SeekOrigin.Begin);
                var compressed = new byte[compressedSize];
                S.Read(ref compressed, 1, (int)compressedSize);
                result = BackwardLz77.GetUncompressedSize(compressed, compressedSize, out var uncompressedSize);
                if (result) {
                    var uncompressed = new byte[uncompressedSize];
                    result = BackwardLz77.Uncompress(compressed, compressedSize, uncompressed, ref uncompressedSize);
                    if (result) s.Write(ref uncompressed, 1, (int)uncompressedSize);
                    else WriteLine($"ERROR: uncompress error\n\n");
                }
                else WriteLine($"ERROR: get uncompressed Size error\n\n");
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
            if (fileSize < sizeof(ExeFsSuperBlock)) { WriteLine("ERROR: exefs header is too short\n\n"); return false; }
            if (Verbose) WriteLine($"load: {HeaderFileName}\n");
            s.Seek(0, SeekOrigin.Begin);
            s.Read(ref SuperBlock, sizeof(ExeFsSuperBlock), 1);
            S.Write(ref SuperBlock, sizeof(ExeFsSuperBlock), 1);
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
            else { if (Verbose) WriteLine($"INFO: unknown entry Name {name}\n"); path = $"{ExeFsDirName}/{name}.bin"; }
        }
        else path = $"{ExeFsDirName}/{z}";
        try {
            uint fileSize;
            byte[] data;
            using (var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                header.Offset = (uint)(S.Position - sizeof(ExeFsSuperBlock));
                if (Verbose) WriteLine($"load: {path}\n");
                s.Seek(0, SeekOrigin.End);
                fileSize = (uint)s.Position;
                s.Seek(0, SeekOrigin.Begin);
                data = new byte[fileSize];
                S.Read(ref data, 1, (int)fileSize);
            }
            var compressResult = false;
            if (topSection && Compress) {
                var compressedSize = fileSize;
                var compressed = new byte[compressedSize];
                compressResult = BackwardLz77.Compress(data, fileSize, compressed, ref compressedSize);
                if (compressResult) {
                    //SHA256(compressed, compressedSize, SuperBlock.Hash[7 - index]);
                    S.Write(ref compressed, 1, (int)compressedSize);
                    header.Size = compressedSize;
                }
            }
            if (!topSection || !Compress || !compressResult) {
                //SHA256(data, fileSize, SuperBlock.Hash[7 - index]);
                S.Write(ref data, 1, (int)fileSize);
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
            Buffer.MemoryCopy(&_->Hash0, &_->Hash1, size = 32 * (7 - index), size);
            Unsafe.InitBlock(&_->Hash0, 0, 32);
        }
    }
}
