using System;
using System.Collections.Generic;
using System.IO;

namespace OpenStack.Rom;

#region _3dsFileSystem

/// <summary>
/// _3dsFileSystem
/// </summary>
public class _3dsFileSystem : IFileSystem {
    public _3dsFileSystem(string root, string path) {
    }

    public bool FileExists(string path) => throw new NotImplementedException();
    public (string path, long length) FileInfo(string path) => throw new NotImplementedException();
    public IEnumerable<string> Glob(string path, string searchPattern) => throw new NotImplementedException();
    public BinaryReader OpenReader(string path) => throw new NotImplementedException();
    public BinaryWriter OpenWriter(string path) => throw new NotImplementedException();
}

#endregion