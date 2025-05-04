using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    #endregion

    #region Enums

    public enum kExtractState { Begin, ChildDir, SiblingDir, End }

    public struct CommonDirEntry {
        public int ParentDirOffset;
        public int SiblingDirOffset;
        public int ChildDirOffset;
        public int ChildFileOffset;
        public int PrevDirOffset;
        public int NameSize;
    }

    public struct CommonFileEntry {
        public int ParentDirOffset;
        public int SiblingFileOffset;
        //union
        //{
        public long FileOffset;
        public ulong RemapIgnoreLevel;
        //}
        public long FileSize;
        public int PrevFileOffset;
        public int NameSize;
    }

    public class CommonEntry {
        public CommonDirEntry Dir;
        public CommonFileEntry File;
    }

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
    public string FileName;
    public bool Verbose = false;
    public string RomFsDirName;
    public string RomFsFileName;
    Stream S = null;
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

    static bool RemapIgnoreLevelCompare(CommonFileEntry lhs, CommonFileEntry rhs) => lhs.RemapIgnoreLevel < rhs.RemapIgnoreLevel;

    //public void SetFileName(string a_sFileName) => m_sFileName = a_sFileName;
    //public void SetVerbose(bool a_bVerbose) => m_bVerbose = a_bVerbose;
    //public void SetRomFsDirName(string a_sRomFsDirName) => m_sRomFsDirName = a_sRomFsDirName;
    //public void SetRomFsFileName(string a_sRomFsFileName) => m_sRomFsFileName = a_sRomFsFileName;

    public bool ExtractFile() {
        try {
            var result = true;
            using (S = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                S.Read(ref RomFsHeader, sizeof(SRomFsHeader), 1);
                Level3Offset = Align(Align(RomFsHeader.Size, SHA256BlockSize) + RomFsHeader.Level0Size, BlockSize);
                S.Seek(Level3Offset, SeekOrigin.Begin);
                S.Read(ref RomFsMetaInfo, sizeof(SRomFsMetaInfo), 1);
                PushExtractStackElement(true, 0, "/");
                while (ExtractStack.Count != 0) {
                    ExtractStackElement current = ExtractStack.Peek();
                    if (current.IsDir) {
                        if (!ExtractDirEntry()) result = false;
                    }
                    else if (!ExtractFileEntry()) result = false;
                }
                return result;
            }
        }
        catch (IOException) { return false; }
    }

    public bool CreateFile() {
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
        Remap();
        CreateHeader();
        InitLevelBuffer();
        var fileSize = Align(LevelBuffer[2].FilePos + RomFsHeader.Level2.Size, BlockSize);
        try {
            using (S = File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.Write)) {
                S.Seek2(fileSize);
                if (!UpdateLevelBuffer()) result = false;
                return result;
            }
        }
        catch (IOException) { return false; }
    }

    public static bool IsRomFsFile(string fileName) {
        try {
            using var s = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            SRomFsHeader header = new();
            s.Read(ref header, sizeof(SRomFsHeader), 1);
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

    bool ExtractDirEntry() {
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(s);
            var prefix = s.Prefix;
            var dirName = RomFsDirName + prefix;
            if (s.Entry.Dir.NameSize != 0) {
                prefix += s.EntryName + "/";
                dirName += s.EntryName;
            }
            else dirName.erase(dirName.end() - 1);
            if (Verbose) WriteLine($"save: {dirName}\n");
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

    bool ExtractFileEntry() {
        var result = true;
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(s);
            var path = RomFsDirName + s.Prefix + s.EntryName;
            try {
                using var t = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
                if (Verbose) WriteLine($"save: {path}\n");
                S.CopyFile(t, Level3Offset + RomFsMetaInfo.DataOffset + s.Entry.File.FileOffset, s.Entry.File.FileSize);
            }
            catch (IOException) { result = false; }
            PushExtractStackElement(false, s.Entry.File.SiblingFileOffset, s.Prefix);
            s.ExtractState = kExtractState.End;
        }
        else if (s.ExtractState == kExtractState.End) ExtractStack.Pop();
        return result;
    }

    void ReadEntry(ExtractStackElement element) {
        if (element.IsDir) {
            S.Seek(Level3Offset + RomFsMetaInfo.Dir.Offset + element.EntryOffset, SeekOrigin.Begin);
            S.Read(ref element.Entry.Dir, sizeof(CommonDirEntry), 1);
            var entryName = new ushort[element.Entry.Dir.NameSize / 2 + 1];
            S.Read(ref entryName, 2, element.Entry.Dir.NameSize / 2);
            entryName[element.Entry.Dir.NameSize / 2] = 0;
            element.EntryName = U16ToU(pEntryName);
        }
        else {
            S.Seek(Level3Offset + RomFsMetaInfo.File.Offset + element.EntryOffset, SeekOrigin.Begin);
            S.Read(ref element.Entry.File, sizeof(CommonFileEntry), 1);
            var entryName = new ushort[element.Entry.File.NameSize / 2 + 1];
            S.Read(ref entryName, 2, element.Entry.File.NameSize / 2);
            entryName[element.Entry.File.NameSize / 2] = 0;
            element.EntryName = U16ToU(pEntryName);
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
            using var s = File.Open(ignorePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            s.Seek(0, SeekOrigin.End);
            var size = (int)s.Position;
            s.Seek(0, SeekOrigin.Begin);
            var buf = new byte[size + 1];
            s.Read(ref buf, 1, size);
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
            else {
                try {
                    URegex black(line, regex_constants.ECMAScript | regex_constants.icase);
                    ignores.Add(black);
                }
                catch (regex_error e) { WriteLine($"ERROR: {e.what()}\n\n"); }
            }
        }
    }

    void PushDirEntry(string entryName, int parentDirOffset) {
        SEntry s;
        CreateDirs.Add(s = new SEntry());
        s.Path = CreateDirs.Count == 1 ? RomFsDirName : $"{CreateDirs[parentDirOffset].Path}/{entryName}";
        s.EntryName = entryName;
        s.Entry.Dir.ParentDirOffset = parentDirOffset;
        s.Entry.Dir.SiblingDirOffset = InvalidOffset;
        s.Entry.Dir.ChildDirOffset = InvalidOffset;
        s.Entry.Dir.ChildFileOffset = InvalidOffset;
        s.Entry.Dir.PrevDirOffset = InvalidOffset;
        s.Entry.Dir.NameSize = s.EntryName.Length * 2;
        s.EntryNameSize = (int)Align(s.Entry.Dir.NameSize, EntryNameAlignment);
        if (CreateDirs[parentDirOffset].Entry.Dir.ChildDirOffset != InvalidOffset && CreateDirs.Count - 1 != CreateDirs[parentDirOffset].Entry.Dir.ChildDirOffset)
            CreateDirs[^2].Entry.Dir.SiblingDirOffset = CreateDirs.Count - 1;
    }

    bool PushFileEntry(string entryName, int parentDirOffset) {
        var result = true;
        SEntry s;
        CreateFiles.Add(s = new SEntry());
        s.Path = $"{CreateDirs[parentDirOffset].Path}/{entryName}";
        s.EntryName = entryName;
        s.EntryOffset = (int)Align(RomFsMetaInfo.File.Size, EntryNameAlignment);
        s.Entry.File.ParentDirOffset = parentDirOffset;
        s.Entry.File.SiblingFileOffset = InvalidOffset;
        s.Entry.File.FileOffset = Align(RomFsHeader.Level3.Size, FileSizeAlignment);
        if (!UGetFileSize(s.Path, s.Entry.File.FileSize)) { result = false; WriteLine($"ERROR: {s.Path} stat error\n\n"); }
        s.Entry.File.PrevFileOffset = InvalidOffset;
        s.Entry.File.NameSize = s.EntryName.Length * 2;
        s.EntryNameSize = (int)Align(s.Entry.File.NameSize, EntryNameAlignment);
        if (CreateDirs[parentDirOffset].Entry.Dir.ChildFileOffset != InvalidOffset && CreateFiles.Count - 1 != CreateDirs[parentDirOffset].Entry.Dir.ChildFileOffset)
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
#if SDW_PLATFORM == SDW_PLATFORM_WINDOWS
            WIN32_FIND_DATAW ffd;
            HANDLE hFind = INVALID_HANDLE_VALUE;
            wstring sPattern = CreateDirs[s.EntryOffset].Path + L"/*";
            hFind = FindFirstFileW(sPattern.c_str(), &ffd);
            if (hFind != INVALID_HANDLE_VALUE) {
                do {
                    if (matchInIgnoreList(CreateDirs[s.EntryOffset].Path.substr(RomFsDirName.size()) + L"/" + ffd.cFileName)) {
                        continue;
                    }
                    if ((ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0) {
                        if (CreateDirs[s.EntryOffset].Entry.Dir.ChildFileOffset == InvalidOffset) {
                            CreateDirs[s.EntryOffset].Entry.Dir.ChildFileOffset = static_cast<n32>(CreateFiles.size());
                        }
                        if (!pushFileEntry(ffd.cFileName, s.EntryOffset)) {
                            result = false;
                        }
                    }
                    else if (wcscmp(ffd.cFileName, L".") != 0 && wcscmp(ffd.cFileName, L"..") != 0) {
                        if (CreateDirs[s.EntryOffset].Entry.Dir.ChildDirOffset == InvalidOffset) {
                            CreateDirs[s.EntryOffset].Entry.Dir.ChildDirOffset = static_cast<n32>(CreateDirs.size());
                        }
                        s.ChildOffset.push_back(static_cast<int>(CreateDirs.size()));
                        pushDirEntry(ffd.cFileName, s.EntryOffset);
                    }
                } while (FindNextFileW(hFind, &ffd) != 0);
            }
#else
		DIR* pDir = opendir(m_vCreateDir[current.EntryOffset].Path.c_str());
		if (pDir != null)
		{
			map<string, string> mDir;
			map<string, string> mFile;
			dirent* pDirent = null;
			while ((pDirent = readdir(pDir)) != null)
			{
				string sName = pDirent->d_name;
#if SDW_PLATFORM == SDW_PLATFORM_MACOS
				sName = TSToS<string, string>(sName, "UTF-8-MAC", "UTF-8");
#endif
				if (matchInIgnoreList(m_vCreateDir[current.EntryOffset].Path.substr(m_sRomFsDirName.size()) + "/" + sName))
				{
					continue;
				}
				string sNameUpper = sName;
				transform(sNameUpper.begin(), sNameUpper.end(), sNameUpper.begin(), ::toupper);
				// handle cases where d_type is DT_UNKNOWN
				if (pDirent->d_type == DT_UNKNOWN)
				{
					string sPath = m_vCreateDir[current.EntryOffset].Path + "/" + sName;
					Stat st;
					if (UStat(sPath.c_str(), &st) == 0)
					{
						if (S_ISREG(st.st_mode))
						{
							pDirent->d_type = DT_REG;
						}
						else if (S_ISDIR(st.st_mode))
						{
							pDirent->d_type = DT_DIR;
						}
					}
				}
				if (pDirent->d_type == DT_REG)
				{
					mFile.insert(make_pair(sNameUpper, sName));
				}
				else if (pDirent->d_type == DT_DIR && strcmp(pDirent->d_name, ".") != 0 && strcmp(pDirent->d_name, "..") != 0)
				{
					mDir.insert(make_pair(sNameUpper, sName));
				}
			}
			closedir(pDir);
			for (map<string, string>::const_iterator it = mDir.begin(); it != mDir.end(); ++it)
			{
				if (m_vCreateDir[current.EntryOffset].Entry.Dir.ChildDirOffset == s_nInvalidOffset)
				{
					m_vCreateDir[current.EntryOffset].Entry.Dir.ChildDirOffset = static_cast<n32>(m_vCreateDir.size());
				}
				current.ChildOffset.push_back(static_cast<int>(m_vCreateDir.size()));
				pushDirEntry(it->second, current.EntryOffset);
			}
			for (map<string, string>::const_iterator it = mFile.begin(); it != mFile.end(); ++it)
			{
				if (m_vCreateDir[current.EntryOffset].Entry.Dir.ChildFileOffset == s_nInvalidOffset)
				{
					m_vCreateDir[current.EntryOffset].Entry.Dir.ChildFileOffset = static_cast<n32>(m_vCreateFile.size());
				}
				if (!pushFileEntry(it->second, current.EntryOffset))
				{
					bResult = false;
				}
			}
		}
#endif
            s.ChildIndex = 0;
        }
        else if (s.ChildIndex != s.ChildOffset.Count) PushCreateStackElement(s.ChildOffset[s.ChildIndex++]);
        else CreateStack.Pop();
        return result;
    }

    bool MatchInIgnoreList(string path) {
        foreach (var it in Ignores)
            if (regex_search(path, it)) return true;
        return false;
    }

    uint GetRemapIgnoreLevel(string path) {
        for (var i = 0; i < RemapIgnores.Count; i++) {
            var rgx = RemapIgnores[i];
            if (regex_search(path, rgx)) return (uint)i;
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
        CreateDirs.erase(CreateDirs.begin() + index);
    }

    void SubDirOffset(ref int offset, int index) {
        if (offset > index) offset--;
    }

    void CreateHash() {
        DirBuckets.Resize(ComputeBucketCount((uint)CreateDirs.Count), InvalidOffset);
        FileBuckets.Resize(ComputeBucketCount((uint)CreateFiles.Count), InvalidOffset);
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

    void Remap() {
        if (string.IsNullOrEmpty(RomFsFileName)) { }
        var romFs = new RomFs {
            FileName = RomFsFileName,
            RomFsDirName = RomFsDirName
        };
        if (!romFs.TravelFile()) return;
        foreach (var s in CreateFiles) {
            TravelInfo[s.Path] = s.Entry.File;
            TravelInfo[s.Path].RemapIgnoreLevel = GetRemapIgnoreLevel(s.Path);
        }
        Space space = new();
        if (Level3Offset + RomFsMetaInfo.DataOffset > romFs.Level3Offset + romFs.RomFsMetaInfo.DataOffset) {
            foreach (var s in romFs.TravelInfo.Values) {
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
        vector<CommonFileEntry*> vRemapIgnore;
        for (map<UString, CommonFileEntry>::iterator itRomFs = romFs.TravelInfo.begin(); itRomFs != romFs.TravelInfo.end(); ++itRomFs) {
            SCommonFileEntry & currentRomFsFileEntry = itRomFs->second;
            map<UString, CommonFileEntry>::iterator it = TravelInfo.find(itRomFs->first);
            if (it == TravelInfo.end()) {
                space.AddSpace(currentRomFsFileEntry.FileOffset, Align(currentRomFsFileEntry.FileSize, FileSizeAlignment));
                currentRomFsFileEntry.FileSize = 0;
            }
            else {
                SCommonFileEntry & currentFileEntry = it->second;
                if (Align(currentFileEntry.FileSize, FileSizeAlignment) > Align(currentRomFsFileEntry.FileSize, FileSizeAlignment) || currentFileEntry.RemapIgnoreLevel != UINT32_MAX) {
                    space.AddSpace(currentRomFsFileEntry.FileOffset, Align(currentRomFsFileEntry.FileSize, FileSizeAlignment));
                    currentRomFsFileEntry.FileSize = 0;
                    vRemapIgnore.push_back(&currentFileEntry);
                }
                else {
                    currentFileEntry.FileOffset = currentRomFsFileEntry.FileOffset - Level3Offset - RomFsMetaInfo.DataOffset;
                    space.AddSpace(Align(currentRomFsFileEntry.FileOffset + currentFileEntry.FileSize, FileSizeAlignment), Align(currentRomFsFileEntry.FileSize, FileSizeAlignment) - Align(currentFileEntry.FileSize, FileSizeAlignment));
                    if (currentFileEntry.FileOffset + currentFileEntry.FileSize > RomFsHeader.Level3.Size) {
                        RomFsHeader.Level3.Size = currentFileEntry.FileOffset + currentFileEntry.FileSize;
                    }
                }
            }
        }
        if (RomFsHeader.Level3.Size == 0) {
            space.Clear();
        }
        else {
            space.SubSpace(Align(Level3Offset + RomFsMetaInfo.DataOffset + RomFsHeader.Level3.Size, FileSizeAlignment), Align(romFs.Level3Offset + romFs.RomFsHeader.Level3.Size, FileSizeAlignment) - Align(Level3Offset + RomFsMetaInfo.DataOffset + RomFsHeader.Level3.Size, FileSizeAlignment));
        }
        for (map<UString, CommonFileEntry>::iterator it = TravelInfo.begin(); it != TravelInfo.end(); ++it) {
            SCommonFileEntry & currentFileEntry = it->second;
            map<UString, CommonFileEntry>::const_iterator itRomFs = romFs.TravelInfo.find(it->first);
            if (itRomFs == romFs.TravelInfo.end()) {
                vRemapIgnore.push_back(&currentFileEntry);
            }
        }
        stable_sort(vRemapIgnore.begin(), vRemapIgnore.end(), RemapIgnoreLevelCompare);
        for (vector<CommonFileEntry*>::iterator it = vRemapIgnore.begin(); it != vRemapIgnore.end(); ++it) {
            SCommonFileEntry & currentFileEntry = **it;
            long nOffset = space.GetSpace(Align(currentFileEntry.FileSize, FileSizeAlignment));
            if (nOffset < 0) {
                currentFileEntry.FileOffset = Align(RomFsHeader.Level3.Size, FileSizeAlignment);
                RomFsHeader.Level3.Size = currentFileEntry.FileOffset + currentFileEntry.FileSize;
            }
            else {
                currentFileEntry.FileOffset = nOffset - Level3Offset - RomFsMetaInfo.DataOffset;
                space.SubSpace(nOffset, Align(currentFileEntry.FileSize, FileSizeAlignment));
            }
        }
        for (vector<SEntry>::iterator it = CreateFiles.begin(); it != CreateFiles.end(); ++it) {
            SEntry & currentEntry = *it;
            currentEntry.Entry.File.FileOffset = TravelInfo[currentEntry.Path].FileOffset;
        }
        Remapped = true;
    }

    bool TravelFile() {
        try {
            using (S = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                S.Read(ref RomFsHeader, sizeof(SRomFsHeader), 1);
                Level3Offset = Align(Align(RomFsHeader.Size, SHA256BlockSize) + RomFsHeader.Level0Size, BlockSize);
                S.Seek(Level3Offset, SeekOrigin.Begin);
                S.Read(ref RomFsMetaInfo, sizeof(SRomFsMetaInfo), 1);
                PushExtractStackElement(true, 0, "/");
                while (ExtractStack.Count != 0) {
                    var s = ExtractStack.Peek();
                    if (s.IsDir) TravelDirEntry();
                    else TravelFileEntry();
                }
                return true;
            }
        }
        catch (IOException) { return false; }
    }

    void TravelDirEntry() {
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(s);
            var prefix = s.Prefix;
            var dirName = RomFsDirName + prefix;
            if (s.Entry.Dir.NameSize != 0) {
                prefix += s.EntryName + "/";
                dirName += s.EntryName;
            }
            else dirName.Remove(dirName.Length - 1);

            PushExtractStackElement(false, s.Entry.Dir.ChildFileOffset, prefix);
            s.ExtractState = kExtractState.ChildDir;
        }
        else if (s.ExtractState == kExtractState.ChildDir) {
            var prefix = s.Prefix;
            if (s.Entry.Dir.NameSize != 0)
                prefix += s.EntryName + "/";
            PushExtractStackElement(true, s.Entry.Dir.ChildDirOffset, prefix);
            s.ExtractState = kExtractState.SiblingDir;
        }
        else if (s.ExtractState == kExtractState.SiblingDir) {
            PushExtractStackElement(true, s.Entry.Dir.SiblingDirOffset, s.Prefix);
            s.ExtractState = kExtractState.End;
        }
        else if (s.ExtractState == kExtractState.End) ExtractStack.Pop();
    }

    void TravelFileEntry() {
        var s = ExtractStack.Peek();
        if (s.ExtractState == kExtractState.Begin) {
            ReadEntry(s);
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
        var fileSize = 0L;
        Array.Resize(ref LevelBuffer[0].Data, BlockSize);
        LevelBuffer[0].DataPos = 0;
        LevelBuffer[0].FilePos = fileSize;
        Level3Offset = Align(Align(sizeof(SRomFsHeader), SHA256BlockSize) + RomFsHeader.Level0Size, BlockSize);
        fileSize = Level3Offset;
        Array.Resize(ref LevelBuffer[3].Data, BlockSize);
        LevelBuffer[3].DataPos = 0;
        LevelBuffer[3].FilePos = fileSize;
        fileSize += Align(RomFsHeader.Level3.Size, BlockSize);
        Array.Resize(ref LevelBuffer[1].Data, BlockSize);
        LevelBuffer[1].DataPos = 0;
        LevelBuffer[1].FilePos = fileSize;
        fileSize += Align(RomFsHeader.Level1.Size, BlockSize);
        Array.Resize(ref LevelBuffer[2].Data, BlockSize);
        LevelBuffer[2].DataPos = 0;
        LevelBuffer[2].FilePos = fileSize;
    }

    bool UpdateLevelBuffer() {
        var result = true;
        WriteBuffer(0, ref RomFsHeader, sizeof(SRomFsHeader));
        AlignBuffer(0, SHA256BlockSize);
        WriteBuffer(3, ref RomFsMetaInfo, sizeof(SRomFsMetaInfo));
        WriteBuffer(3, ref DirBuckets, RomFsMetaInfo.DirHash.Size);
        for (var i = 0; i < CreateDirs.Count; i++) {
            var s = CreateDirs[i];
            WriteBuffer(3, ref s.Entry.Dir, sizeof(CommonDirEntry));
            WriteBuffer(3, s.EntryName, s.EntryNameSize);
        }
        WriteBuffer(3, ref FileBuckets, RomFsMetaInfo.FileHash.Size);
        for (var i = 0; i < CreateFiles.Count; i++) {
            var s = CreateFiles[i];
            WriteBuffer(3, ref s.Entry.File, sizeof(CommonFileEntry));
            WriteBuffer(3, ref s.EntryName, s.EntryNameSize);
        }
        if (!Remapped) {
            for (var i = 0; i < CreateFiles.Count; i++) {
                AlignBuffer(3, (int)FileSizeAlignment);
                var s = CreateFiles[i];
                if (!WriteBufferFromFile(3, s.Path, s.Entry.File.FileSize)) result = false;
            }
        }
        else {
            Dictionary<long, SEntry> createFiles = [];
            for (var i = 0; i < CreateFiles.Count; i++)
                if (CreateFiles[i].Entry.File.FileSize != 0)
                    createFiles.Add(CreateFiles[i].Entry.File.FileOffset, CreateFiles[i]);
            foreach (var currentEntry in createFiles.Values) {
                WriteBuffer(3, null, Level3Offset + RomFsMetaInfo.DataOffset + currentEntry.Entry.File.FileOffset - (LevelBuffer[3].FilePos + LevelBuffer[3].DataPos));
                if (!WriteBufferFromFile(3, currentEntry.Path, currentEntry.Entry.File.FileSize)) result = false;
            }
        }
        AlignBuffer(3, BlockSize);
        AlignBuffer(2, BlockSize);
        AlignBuffer(1, BlockSize);
        AlignBuffer(0, BlockSize);
        return result;
    }

    void WriteBuffer(int level, ref byte[] src, long size) {
        byte* _src = null; // static_cast <const byte*> (src);
        do {
            var remainSize = BlockSize - LevelBuffer[level].DataPos;
            var size2 = size > remainSize ? remainSize : size;
            if (size2 > 0) {
                if (src != null) {
                    memcpy((LevelBuffer[level].Data + LevelBuffer[level].DataPos), _src, size2);
                    _src += size2;
                }
                LevelBuffer[level].DataPos += (int)size2;
            }
            if (LevelBuffer[level].DataPos == BlockSize) {
                if (level != 0) WriteBuffer(level - 1, SHA256(ref LevelBuffer[level].Data, BlockSize, null), SHA256BlockSize);
                S.Seek(LevelBuffer[level].FilePos, SeekOrigin.Begin);
                S.Write(ref LevelBuffer[level].Data, 1, BlockSize);
                Unsafe.InitBlock(ref LevelBuffer[level].Data[0], 0, (uint)BlockSize);
                LevelBuffer[level].DataPos = 0;
                LevelBuffer[level].FilePos += BlockSize;
            }
            size -= size2;
        } while (size > 0);
    }

    const long _write_bufferSize = 0x100000;
    static byte[] _write_buf = new byte[_write_bufferSize];
    bool WriteBufferFromFile(int level, string path, long size) {
        try {
            using var s = File.Open(path, FileMode.Create, FileAccess.Read, FileShare.Read);
            if (Verbose) WriteLine($"load: {path}\n");
            while (size > 0) {
                var size2 = size > _write_bufferSize ? _write_bufferSize : size;
                s.Read(ref _write_buf, 1, (int)size2);
                WriteBuffer(level, ref _write_buf, size2);
                size -= size2;
            }
            return true;
        }
        catch (IOException) { return false; }
    }

    void AlignBuffer(int level, int alignment) {
        LevelBuffer[level].DataPos = (int)Align(LevelBuffer[level].DataPos, alignment);
        WriteBuffer(level, null, 0);
    }

    static uint Hash(int parentOffset, string entryName) {
        var hash = (uint)(parentOffset ^ 123456789);
        for (var i = 0; i < entryName.Length; i++)
            hash = ((hash >> 5) | (hash << 27)) ^ entryName[i];
        return hash;
    }
}