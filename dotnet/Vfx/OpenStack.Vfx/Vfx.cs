using Microsoft.Extensions.FileSystemGlobbing;
using OpenStack.Vfx.Disc;
using OpenStack.Vfx.N64;
using OpenStack.Vfx.X3ds;
using SharpCompress.Archives.SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("OpenStack.Vfx.Program")]

namespace OpenStack.Vfx;

/// <summary>
/// VfxExtensions
/// </summary>
public static class VfxExtensions {
}

#region FileSystem

/// <summary>
/// FileSystem
/// </summary>
public abstract class FileSystem {
    public abstract IEnumerable<string> Glob(string path, string searchPattern);
    public abstract bool FileExists(string path);
    public abstract (string path, long length) FileInfo(string path);
    public abstract Stream Open(string path, string mode = null);
    public virtual FileSystem Next() => this;
    public IEnumerable<string> FindPaths(string path, string searchPattern) {
        // expand
        int expandStartIdx, expandMidIdx, expandEndIdx;
        if ((expandStartIdx = searchPattern.IndexOf('(')) != -1 &&
            (expandMidIdx = searchPattern.IndexOf(':', expandStartIdx)) != -1 &&
            (expandEndIdx = searchPattern.IndexOf(')', expandMidIdx)) != -1 &&
            expandStartIdx < expandEndIdx) {
            foreach (var expand in searchPattern.Substring(expandStartIdx + 1, expandEndIdx - expandStartIdx - 1).Split(':'))
                foreach (var found in FindPaths(path, searchPattern.Remove(expandStartIdx, expandEndIdx - expandStartIdx + 1).Insert(expandStartIdx, expand)))
                    yield return found;
            yield break;
        }
        foreach (var file in Glob(path, searchPattern)) yield return file;
    }

    /// <summary>
    /// TryAdvance
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public FileSystem Advance(string basePath, string path)
         => Path.GetExtension(path).ToLowerInvariant() switch {
             ".zip" => new ZipFileSystem(this, path, basePath),
             ".7z" => new SevenZipFileSystem(this, path, basePath),
             //".iso" => new IsoFileSystem(this, path, basePath),
             ".bin" or ".cue" => new DiscFileSystem(this, Glob("", "*.cue").Single(), basePath),
             ".n64" or ".v64" or ".z64" => new N64FileSystem(this, path, basePath),
             ".3ds" => new X3dsFileSystem(this, path, basePath),
             _ => null
         };

    protected FileSystem Next2(string basePath, int count, Func<string> firstFunc, Func<FileSystem> elseFunc) {
        if (count == 0) return this;
        var first = firstFunc() ?? "";
        return count == 1 || first.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) || first.EndsWith(".cue", StringComparison.OrdinalIgnoreCase)
            ? Advance(basePath, first) ?? this
            : elseFunc();
    }

    /// <summary>
    /// Creates the matcher.
    /// </summary>
    /// <param name="searchPattern">The searchPattern.</param>
    /// <returns></returns>
    public static Func<string, bool> CreateMatcher(string searchPattern) {
        if (string.IsNullOrEmpty(searchPattern)) return x => true;
        var wildcardCount = searchPattern.Count(x => x.Equals('*'));
        if (wildcardCount <= 0) return x => x.Equals(searchPattern, StringComparison.CurrentCultureIgnoreCase);
        else if (wildcardCount == 1) {
            var newPattern = searchPattern.Replace("*", "");
            if (searchPattern.StartsWith("*")) return x => x.EndsWith(newPattern, StringComparison.CurrentCultureIgnoreCase);
            else if (searchPattern.EndsWith("*")) return x => x.StartsWith(newPattern, StringComparison.CurrentCultureIgnoreCase);
        }
        var regexPattern = $"^{Regex.Escape(searchPattern).Replace("\\*", ".*")}$";
        return x => {
            try { return Regex.IsMatch(x, regexPattern); }
            catch { return false; }
        };
    }
}

/// <summary>
/// AggregateFileSystem
/// </summary>
public class AggregateFileSystem(FileSystem[] aggreate) : FileSystem {
    readonly FileSystem[] aggreate = aggreate;

    public override IEnumerable<string> Glob(string path, string searchPattern) => aggreate.SelectMany(x => x.Glob(path, searchPattern));
    public override bool FileExists(string path) => aggreate.Any(x => x.FileExists(path));
    public override (string path, long length) FileInfo(string path) => aggreate.Select(x => x.FileInfo(path)).FirstOrDefault(x => x.path != null);
    public override Stream Open(string path, string mode) => aggreate.Select(x => x.Open(path, mode)).FirstOrDefault(x => x != null);
}

/// <summary>
/// VirtualFileSystem
/// </summary>
public class VirtualFileSystem(Dictionary<string, byte[]> virtuals) : FileSystem {
    readonly Dictionary<string, byte[]> Virtuals = virtuals;

    public override IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = CreateMatcher(searchPattern);
        return Virtuals.Keys.Where(matcher);
    }
    public override bool FileExists(string path) => Virtuals.ContainsKey(path);
    public override (string path, long length) FileInfo(string path) => Virtuals.TryGetValue(path, out var z) ? (path, z != null ? z.Length : 0) : (null, 0);
    public override Stream Open(string path, string mode) => Virtuals.TryGetValue(path, out var z) ? z != null ? new MemoryStream(z) : new MemoryStream() : null;
}

/// <summary>
/// DirectoryFileSystem
/// </summary>
public class DirectoryFileSystem(string baseRoot, string basePath) : FileSystem {
    readonly string BaseRoot = baseRoot; readonly string BasePath = basePath; string Root = baseRoot; int Skip = baseRoot.Length + 1;

    public override IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = new Matcher();
        matcher.AddIncludePatterns([string.IsNullOrEmpty(searchPattern) ? "**/*" : searchPattern]);
        return [.. matcher.GetResultsInFullPath(Path.Combine(Root, path)).Select(x => x[Skip..])];
    }
    public override bool FileExists(string path) => File.Exists(Path.Combine(Root, path));
    public override (string path, long length) FileInfo(string path) => File.Exists(path = Path.Combine(Root, path)) ? (path[Skip..], new FileInfo(Path.Combine(Root, path)).Length) : (null, 0);
    public override Stream Open(string path, string mode) => mode != "Height"
        ? File.Open(Path.Combine(Root, path), FileMode.Open, FileAccess.Read, FileShare.Read)
        : File.Open(Path.Combine(Root, path), FileMode.Open, FileAccess.Write, FileShare.Write);
    public override FileSystem Next() {
        if (File.Exists(Root) || Path.GetFileName(Root).Contains('*')) {
            Root = Path.GetDirectoryName(Root); Skip = Root.Length + 1;
            return Advance(BasePath, Path.GetFileName(BaseRoot))?.Next() ?? this;
        }
        return Next2(BasePath, -1, () => Directory.EnumerateFiles(Root).FirstOrDefault(), () => {
            if (!string.IsNullOrEmpty(BasePath)) { Root = Path.Combine(BaseRoot, BasePath); Skip = Root.Length + 1; }
            return this;
        });
    }
}

#endregion

#region FileSystem : Network

/// <summary>
/// NetworkFileSystem
/// </summary>
public class NetworkFileSystem : FileSystem {
    public NetworkFileSystem(Uri uri) {
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
    public override IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = new Matcher();
        matcher.AddIncludePatterns([searchPattern]);
        return matcher.GetResultsInFullPath(searchPattern).ToList();
    }
    public override bool FileExists(string path) => File.Exists(path);
    public override (string path, long length) FileInfo(string path) => File.Exists(path) ? (path, 0) : (null, 0);
    public override Stream Open(string path, string mode) => null;
}

#endregion

#region FileSystem : Archive

/// <summary>
/// ZipFileSystem
/// </summary>
public class ZipFileSystem(FileSystem vfx, string path, string basePath) : FileSystem {
    readonly ZipArchive Arc = new(vfx.Open(path), ZipArchiveMode.Read);
    string Root = string.Empty;

    public override IEnumerable<string> Glob(string path, string searchPattern) {
        var root = Path.Combine(Root, path); var skip = root.Length;
        var matcher = CreateMatcher(searchPattern);
        return [.. Arc.Entries.Where(x =>
        {
            var fn = x.FullName;
            return !fn.EndsWith("/") && fn.Length > skip && fn.StartsWith(root) && matcher(fn[skip..]);
        }).Select(x => x.FullName[skip..])];
    }
    public override bool FileExists(string path) => Arc.GetEntry(Path.Combine(Root, path)) != null;
    public override (string path, long length) FileInfo(string path) { var x = Arc.GetEntry(Path.Combine(Root, path)); return x != null ? (x.Name, x.Length) : (null, 0); }
    public override Stream Open(string path, string mode) => Arc.GetEntry(Path.Combine(Root, path)).Open();
    public override FileSystem Next() => Next2(basePath, Arc.Entries.Count, () => Arc.Entries[0].Name, () => {
        if ($"{Path.GetFileNameWithoutExtension(path)}/" == Arc.Entries[0].FullName) basePath = $"{Arc.Entries[0].FullName}{basePath}";
        if (!string.IsNullOrEmpty(basePath)) Root = $"{basePath}{(basePath.EndsWith("/") ? "" : "/")}";
        return this;
    });
}

/// <summary>
/// SevenZipFileSystem
/// </summary>
public class SevenZipFileSystem(FileSystem vfx, string path, string basePath) : FileSystem {
    readonly SevenZipArchive Arc = SevenZipArchive.Open(vfx.Open(path));
    string Root = string.Empty;

    public override IEnumerable<string> Glob(string path, string searchPattern) {
        var root = Path.Combine(Root, path); var skip = root.Length;
        var matcher = CreateMatcher(searchPattern);
        return [.. Arc.Entries.Where(x =>
        {
            var fn = x.Key;
            return fn.Length > skip && fn.StartsWith(root) && matcher(fn[skip..]);
        }).Select(x => x.Key[skip..])];
    }
    public override bool FileExists(string path) { path = Path.Combine(Root, path); return Arc.Entries.Any(x => x.Key == path); }
    public override (string path, long length) FileInfo(string path) { path = Path.Combine(Root, path); var x = Arc.Entries.FirstOrDefault(x => x.Key == path); return x != null ? (x.Key, x.Size) : (null, 0); }
    public override Stream Open(string path, string mode) { path = Path.Combine(Root, path); return Arc.Entries.First(x => x.Key == path).OpenEntryStream(); }
    public override FileSystem Next() => Next2(basePath, Arc.Entries.Count, () => Arc.Entries.First().Key, () => {
        if (!string.IsNullOrEmpty(basePath)) Root = $"{basePath}{Path.AltDirectorySeparatorChar}";
        return this;
    });
}

#endregion
