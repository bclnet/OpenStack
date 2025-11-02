using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenStack;

// YamlDict
public class YamlDict : Dictionary<string, object> {
    string path;

    public YamlDict(string file) {
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), file);
        if (!File.Exists(path)) return;
        var items = (Dictionary<object, object>)new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance).Build()
            .Deserialize(File.ReadAllText(path));
        foreach (var s in items) Add((string)s.Key, s.Value);
    }
}
