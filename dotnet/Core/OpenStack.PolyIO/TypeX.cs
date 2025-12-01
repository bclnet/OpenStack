using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public class RAssemblyAttribute(string name = null) : Attribute {
    public string Name = name;
    internal Dictionary<string, Dictionary<string, Type>> GetLTypes(Type cls) => (Dictionary<string, Dictionary<string, Type>>)cls.GetField("LTypes")?.GetValue(null);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public class RTypeAttribute(string name = null) : Attribute {
    public string Name = name;
    internal string GetName(Type cls) => Name == null || Name == "+"
        ? cls.FullName[(cls.FullName.IndexOf("Formats.") + 8)..].Replace(Name != "+" ? "+" : "\x00", ".")
        : Name;
}

// AssemblyTag
class AssemblyTag {
    public Dictionary<string, Dictionary<string, Type>> LTypes;
    public Dictionary<string, Type> RTypes;
}

// TypeX
public static class TypeX {
    static readonly Dictionary<string, Assembly> Assemblys = [];
    static Dictionary<string, Assembly> AssemblyRedirects = [];
    static readonly Dictionary<Assembly, AssemblyTag> Tags = [];

    public static void ScanTypes(Type[] types) {
        foreach (var type in types) {
            if (Tags.ContainsKey(type.Assembly)) continue;
            var assembly = type.Assembly;
            var attribs = assembly.GetTypes();
            var rassembiles = attribs.SelectMany(s => Attribute.GetCustomAttributes(s, typeof(RAssemblyAttribute)).Cast<RAssemblyAttribute>().Select(t => (t.Name, LTypes: t.GetLTypes(s))));
            var rtypes = attribs.SelectMany(s => Attribute.GetCustomAttributes(s, typeof(RTypeAttribute)).Cast<RTypeAttribute>().Select(t => (Type: s, Name: t.GetName(s))));
            var assemblyRedirects = rassembiles.Where(s => s.Name != null).ToDictionary(s => s.Name, s => assembly);
            if (assemblyRedirects.Count > 0) AssemblyRedirects = AssemblyRedirects.Concat(assemblyRedirects).ToDictionary(s => s.Key, s => s.Value);
            Tags[assembly] = new AssemblyTag {
                LTypes = rassembiles.Where(s => s.LTypes != null).SelectMany(s => s.LTypes).GroupBy(s => s.Key).ToDictionary(s => s.Key, s => s.First().Value),
                RTypes = rtypes.ToDictionary(s => s.Name, s => s.Type),
            };
        }
    }

    public static Type GetRType(this Assembly source, string typeName, bool throwOnError = true) => Type.GetType(typeName,
        assemblyResolver: assembly =>
            Assemblys.TryGetValue(assembly.Name, out var a) ? a
            : Assemblys[assembly.FullName] = AssemblyRedirects.TryGetValue(assembly.Name, out var s) ? s : Assembly.Load(assembly.FullName),
        typeResolver: (assembly, name, throwOnError) => {
            var a = assembly ?? source;
            if (Tags.TryGetValue(a, out var tag)) {
                // r-types
                if (tag.RTypes.TryGetValue(name, out var type)) return type;
                // l-types
                var idx = name.LastIndexOf('.');
                var (ns, na) = idx != -1 ? (name[..idx], name[(idx + 1)..]) : default;
                if (ns != null && tag.LTypes.TryGetValue(ns, out var b) && b.TryGetValue(na, out type)) return type;
            }
            return a.GetType(name, throwOnError);
        }, throwOnError);

    public static ConstructorInfo GetDefaultConstructor(this Type type) => type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, [], null);
    public static PropertyInfo[] GetAllProperties(this Type type) => [.. type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().FindAll(p => p.GetGetMethod(true) != null && p.GetGetMethod(true) == p.GetGetMethod(true).GetBaseDefinition())];
    public static FieldInfo[] GetAllFields(this Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}