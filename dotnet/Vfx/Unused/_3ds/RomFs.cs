using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;

namespace OpenStack.Rom.Nintendo._3ds;
using static Util;

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
            if (Verbose) WriteLine($"save: {dirName}");
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
                if (Verbose) WriteLine($"save: {path}");
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
                catch (ArgumentException e) { WriteLine($"ERROR: {e.Message}\n"); }
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
        if (s.Path.Length != s.Entry.File.FileSize >> 1) { result = false; WriteLine($"ERROR: {s.Path} stat error\n"); }
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
            if (Verbose) WriteLine($"load: {path}");
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