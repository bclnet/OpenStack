using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenStack;

public static class Util {
    public static string DecodePath(string ApplicationPath, string path, string rootPath = null) =>
        path.StartsWith("~", StringComparison.OrdinalIgnoreCase) ? $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{path[1..]}"
        : path.StartsWith("%Path%", StringComparison.OrdinalIgnoreCase) ? $"{rootPath}{path[6..]}"
        : path.StartsWith("%AppPath%", StringComparison.OrdinalIgnoreCase) ? $"{ApplicationPath}{path[9..]}"
        : path.StartsWith("%AppData%", StringComparison.OrdinalIgnoreCase) ? $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{path[9..]}"
        : path.StartsWith("%LocalAppData%", StringComparison.OrdinalIgnoreCase) ? $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{path[14..]}"
        : path;
}

// YamlDict
public class YamlDict : Dictionary<string, object> {
    static IDeserializer Deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
    static ISerializer Serializer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
    string path;

    public YamlDict(string file) {
        path = Util.DecodePath(null, file);
        if (!File.Exists(path)) return;
        var items = (Dictionary<object, object>)Deserializer.Deserialize(File.ReadAllText(path));
        foreach (var s in items) Add((string)s.Key, s.Value);
    }

    public void Flush() => File.WriteAllText(path, Serializer.Serialize(this));
}