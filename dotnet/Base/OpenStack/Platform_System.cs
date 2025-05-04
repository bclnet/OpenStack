using Microsoft.Extensions.FileSystemGlobbing;
using OpenStack.Sfx;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace OpenStack;

/// <summary>
/// SystemAudioBuilder
/// </summary>
public class SystemAudioBuilder : AudioBuilderBase<object> {
    public override object CreateAudio(object path) => throw new NotImplementedException();
    public override void DeleteAudio(object audio) => throw new NotImplementedException();
}

#region OpenSfx

/// <summary>
/// SystemSfx
/// </summary>
public class SystemSfx(ISource source) : IOpenSfx<object> {
    readonly ISource _source = source;
    readonly AudioManager<object> _audioManager = new(source, new SystemAudioBuilder());

    public ISource Source => _source;
    public AudioManager<object> AudioManager => _audioManager;
    public object CreateAudio(object path) => _audioManager.CreateAudio(path).aud;
}

#endregion

#region FileSystem

/// <summary>
/// AggregateFileSystem
/// </summary>
public class AggregateFileSystem(IFileSystem[] aggreate) : IFileSystem {
    readonly IFileSystem[] aggreate = aggreate;

    public IEnumerable<string> Glob(string path, string searchPattern) => aggreate.SelectMany(x => x.Glob(path, searchPattern));
    public bool FileExists(string path) => aggreate.Any(x => x.FileExists(path));
    public (string path, long length) FileInfo(string path) => aggreate.Select(x => x.FileInfo(path)).FirstOrDefault(x => x.path != null);
    public BinaryReader OpenReader(string path) => aggreate.Select(x => x.OpenReader(path)).FirstOrDefault(x => x != null);
    public BinaryWriter OpenWriter(string path) => aggreate.Select(x => x.OpenWriter(path)).FirstOrDefault(x => x != null);
}

/// <summary>
/// HostFileSystem
/// </summary>
public class HostFileSystem : IFileSystem {
    public HostFileSystem(Uri uri) {
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        var pathOrPattern = uri.LocalPath;
        var searchPattern = Path.GetFileName(pathOrPattern);
        //var path = Path.GetDirectoryName(pathOrPattern);
        // file
        if (!string.IsNullOrEmpty(searchPattern)) throw new ArgumentOutOfRangeException(nameof(pathOrPattern), pathOrPattern); //: Web single file access to supported.

        //options = PakOption.Stream;
        //searchPattern = Path.GetFileName(path);
        //path = Path.GetDirectoryName(path);
        //if (path.Contains('*')) throw new NotSupportedException("Web wildcard folder access");
        //host = new UriBuilder(uri) { Path = $"{path}/", Fragment = null }.Uri;
        //if (searchPattern.Contains('*'))
        //{
        //    var set = new HttpHost(host).GetSetAsync().Result ?? throw new NotSupportedException(".set not found. Web wildcard access");
        //    var pattern = $"^{Regex.Escape(searchPattern.Replace('*', '%')).Replace("_", ".").Replace("%", ".*")}$";
        //    return set.Where(x => Regex.IsMatch(x, pattern)).ToArray();
        //}
        //return new[] { searchPattern };
    }
    public IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = new Matcher();
        matcher.AddIncludePatterns([searchPattern]);
        return matcher.GetResultsInFullPath(searchPattern).ToList();
    }
    public bool FileExists(string path) => File.Exists(path);
    public (string path, long length) FileInfo(string path) => File.Exists(path) ? (path, 0) : (null, 0);
    public BinaryReader OpenReader(string path) => null;
    public BinaryWriter OpenWriter(string path) => null;
}

/// <summary>
/// IsoFileSystem
/// </summary>
public class IsoFileSystem(string root, string path) : IFileSystem {
    //readonly ZipArchive Pak = ZipFile.Open(root, ZipArchiveMode.Read);
    //readonly string Root = path;

    //public IEnumerable<string> Glob(string path, string searchPattern) {
    //    var matcher = PlatformX.CreateMatcher(searchPattern);
    //    return Pak.Entries.Where(x => matcher(x.Name)).Select(x => x.Name);
    //}
    //public bool FileExists(string path) => Pak.GetEntry(path) != null;
    //public (string path, long length) FileInfo(string path) { var x = Pak.GetEntry(path); return x != null ? (x.Name, x.Length) : (null, 0); }
    //public BinaryReader OpenReader(string path) => new(Pak.GetEntry(path).Open());
    //public BinaryWriter OpenWriter(string path) => throw new NotSupportedException();

    public bool FileExists(string path) => throw new NotImplementedException();
    public (string path, long length) FileInfo(string path) => throw new NotImplementedException();
    public IEnumerable<string> Glob(string path, string searchPattern) => throw new NotImplementedException();
    public BinaryReader OpenReader(string path) => throw new NotImplementedException();
    public BinaryWriter OpenWriter(string path) => throw new NotImplementedException();

    public IFileSystem Advance(ref string root) => throw new NotImplementedException();
}

public class SingleFileSystem(Stream stream) : IFileSystem {
    readonly Stream Stream = stream;

    public IEnumerable<string> Glob(string path, string searchPattern) => throw new NotImplementedException();
    public bool FileExists(string path) => throw new NotImplementedException();
    public (string path, long length) FileInfo(string path) => throw new NotImplementedException();
    public BinaryReader OpenReader(string path) => throw new NotImplementedException();
    public BinaryWriter OpenWriter(string path) => throw new NotImplementedException();
}

/// <summary>
/// StandardFileSystem
/// </summary>
public class StandardFileSystem(string root) : IFileSystem {
    readonly string Root = root; readonly int Skip = root.Length + 1;

    public IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = new Matcher();
        matcher.AddIncludePatterns([string.IsNullOrEmpty(searchPattern) ? "**/*" : searchPattern]);
        return [.. matcher.GetResultsInFullPath(Path.Combine(Root, path)).Select(x => x[Skip..])];
    }
    public bool FileExists(string path) => File.Exists(Path.Combine(Root, path));
    public (string path, long length) FileInfo(string path) => File.Exists(path = Path.Combine(Root, path)) ? (path[Skip..], new FileInfo(Path.Combine(Root, path)).Length) : (null, 0);
    public BinaryReader OpenReader(string path) => new(File.Open(Path.Combine(Root, path), FileMode.Open, FileAccess.Read, FileShare.Read));
    public BinaryWriter OpenWriter(string path) => new(File.Open(Path.Combine(Root, path), FileMode.Open, FileAccess.Write, FileShare.Write));
}

/// <summary>
/// VirtualFileSystem
/// </summary>
public class VirtualFileSystem(Dictionary<string, byte[]> virtuals) : IFileSystem {
    readonly Dictionary<string, byte[]> Virtuals = virtuals;

    public IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = PlatformX.CreateMatcher(searchPattern);
        return Virtuals.Keys.Where(matcher);
    }
    public bool FileExists(string path) => Virtuals.ContainsKey(path);
    public (string path, long length) FileInfo(string path) => Virtuals.TryGetValue(path, out var z) ? (path, z != null ? z.Length : 0) : (null, 0);
    public BinaryReader OpenReader(string path) => Virtuals.TryGetValue(path, out var z) ? new BinaryReader(z != null ? new MemoryStream(z) : new MemoryStream()) : null;
    public BinaryWriter OpenWriter(string path) => throw new NotSupportedException();
}

/// <summary>
/// ZipFileSystem
/// </summary>
public class ZipFileSystem(string root, string path) : IFileSystem {
    readonly ZipArchive Zip = ZipFile.Open(root, ZipArchiveMode.Read);
    readonly string Root = string.IsNullOrEmpty(path) ? string.Empty : $"{path}{Path.AltDirectorySeparatorChar}";

    public IEnumerable<string> Glob(string path, string searchPattern) {
        var root = Path.Combine(Root, path); var skip = root.Length;
        var matcher = PlatformX.CreateMatcher(searchPattern);
        return [.. Zip.Entries.Where(x =>
        {
            var fn = x.FullName;
            return fn.Length > skip && fn.StartsWith(root) && matcher(fn[skip..]);
        }).Select(x => x.FullName[skip..])];
    }
    public bool FileExists(string path) => Zip.GetEntry(Path.Combine(Root, path)) != null;
    public (string path, long length) FileInfo(string path) { var x = Zip.GetEntry(Path.Combine(Root, path)); return x != null ? (x.Name, x.Length) : (null, 0); }
    public BinaryReader OpenReader(string path) => new(Zip.GetEntry(Path.Combine(Root, path)).Open());
    public BinaryWriter OpenWriter(string path) => throw new NotSupportedException();

    public IFileSystem Advance(ref string root) {
        if (Zip.Entries.Count != 1) return this;
        var entry = Zip.Entries[0];
        root = entry.Name;
        return new SingleFileSystem(entry.Open());
    }
}

#endregion
