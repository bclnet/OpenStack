using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public class RAssemblyAttribute(string name = null) : Attribute {
    public string Name = name;
    internal Dictionary<string, Dictionary<string, Type>> GetLiteralTypes(Type cls) => (Dictionary<string, Dictionary<string, Type>>)cls.GetField("LiteralTypes")?.GetValue(null);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public class RTypeAttribute(string name = null) : Attribute {
    public string Name = name;
    internal string GetName(Type cls) => Name == null || Name == "+"
        ? cls.FullName[(cls.FullName.IndexOf("Formats.") + 8)..].Replace(Name != "+" ? "+" : "", ".")
        : Name;
}

// AssemblyTag
class AssemblyTag {
    public Dictionary<string, Dictionary<string, Type>> LiteralTypes;
    public Dictionary<string, Type> RTypes;
}

// TypeX
public static class TypeX {
    #region Indirect

    static readonly Dictionary<string, Assembly> Assemblys = [];
    static Dictionary<string, Assembly> AssemblyRedirects = [];
    static readonly Dictionary<Assembly, AssemblyTag> Tags = [];

    public static void ScanTypes(this Type obj) {
        if (Tags.ContainsKey(obj.Assembly)) return;
        var assembly = obj.Assembly;
        var rassembiles = assembly.GetTypes().SelectMany(s => Attribute.GetCustomAttributes(s, typeof(RAssemblyAttribute)).Cast<RAssemblyAttribute>().Select(t => (t.Name, LiteralTypes: t.GetLiteralTypes(s))));
        var rtypes = assembly.GetTypes().SelectMany(s => Attribute.GetCustomAttributes(s, typeof(RTypeAttribute)).Cast<RTypeAttribute>().Select(t => (Type: s, Name: t.GetName(s))));
        var assemblyRedirects = rassembiles.Where(s => s.Name != null).ToDictionary(s => s.Name, s => assembly);
        if (assemblyRedirects != null) AssemblyRedirects = AssemblyRedirects.Concat(assemblyRedirects).ToDictionary(s => s.Key, s => s.Value);
        Tags[assembly] = new AssemblyTag {
            LiteralTypes = rassembiles.Where(s => s.LiteralTypes != null).SelectMany(s => s.LiteralTypes).GroupBy(s => s.Key).ToDictionary(s => s.Key, s => s.First().Value),
            RTypes = rtypes.ToDictionary(s => s.Name, s => s.Type),
        };
    }

    public static Type GetRType(this Assembly source, string type) => Type.GetType(type,
        assemblyResolver: assembly => Assemblys.TryGetValue(assembly.FullName, out var a) ? a : Assemblys[assembly.FullName] = AssemblyRedirects.TryGetValue(assembly.FullName, out var s) ? s : Assembly.Load(assembly.FullName),
        typeResolver: (assembly, name, throwOnError) => {
            var a = assembly ?? source;
            if (Tags.TryGetValue(a, out var tag)) {
                // r-types
                if (tag.RTypes.TryGetValue(name, out var type)) return type;
                // l-types
                var idx = name.LastIndexOf('.');
                var (ns, na) = idx != -1 ? (name[..idx], name[(idx + 1)..]) : default;
                if (ns != null && tag.LiteralTypes.TryGetValue(ns, out var b) && b.TryGetValue(na, out type)) return type;
            }
            return a.GetType(name, throwOnError);
        });

    //public static string StripAssemblyVersion(string name) {
    //    var commaIndex = 0;
    //    while ((commaIndex = name.IndexOf(',', commaIndex)) != -1) {
    //        if (commaIndex + 1 < name.Length && name[commaIndex + 1] == '[') commaIndex++;
    //        else {
    //            var closeBracket = name.IndexOf(']', commaIndex);
    //            if (closeBracket != -1) name = name.Remove(commaIndex, closeBracket - commaIndex);
    //            else name = name[..commaIndex];
    //        }
    //    }
    //    return name;
    //}

    //public static (string, string[]) SplitGenericName(string name) {
    //    // look for the < generic marker character.
    //    var pos = name.IndexOf('<');
    //    if (pos == -1) return default;
    //    // everything to the left of < is the generic type name.
    //    var genericName = name[..pos]; var genericArguments = new List<string>();
    //    // advance to the start of the generic argument list.
    //    pos++;
    //    // split up the list of generic type arguments.
    //    while (pos < name.Length && name[pos] != '>') {
    //        // locate the end of the current type name argument.
    //        int nesting = 0, end;
    //        for (end = pos; end < name.Length; end++) {
    //            // handle nested types in case we have eg. "List<List<Int>>".
    //            if (name[end] == '<') nesting++;
    //            else if (name[end] == '>') {
    //                if (nesting > 0) nesting--;
    //                else break;
    //            }
    //            else if (nesting == 0 && name[end] == ',') break;
    //        }
    //        // extract the type name argument.
    //        genericArguments.Add(name[pos..end].Trim());
    //        // skip past the type name, plus any subsequent "," goo.
    //        pos = end;
    //        if (pos < name.Length && name[pos] == ',') pos++;
    //    }
    //    return (genericName, genericArguments.ToArray());
    //}

    //public static (string, Type[]) SplitGenericType(Type type) {
    //    if (!type.IsGenericType) return default;
    //    return (type.FullName[..type.FullName.IndexOf('`')], type.GetGenericArguments());
    //}

    //public static (string, string[]) SplitGenericTypeName(string name) {
    //    // look for the ` generic marker character.
    //    var pos = name.IndexOf('`');
    //    if (pos == -1) return default;
    //    // everything to the left of ` is the generic type name.
    //    var genericName = name[..pos]; var genericArguments = new List<string>();
    //    // advance to the start of the generic argument list.
    //    pos++;
    //    while (pos < name.Length && char.IsDigit(name[pos])) pos++;
    //    while (pos < name.Length && name[pos] == '[') pos++;
    //    // split up the list of generic type arguments.
    //    while (pos < name.Length && name[pos] != ']') {
    //        // locate the end of the current type name argument.
    //        int nesting = 0, end;
    //        for (end = pos; end < name.Length; end++) {
    //            // handle nested types in case we have eg. "List`1[[List`1[[Int]]]]".
    //            if (name[end] == '[') nesting++;
    //            else if (name[end] == ']') {
    //                if (nesting > 0) nesting--;
    //                else break;
    //            }
    //        }
    //        // extract the type name argument.
    //        genericArguments.Add(name[pos..end]);
    //        // skip past the type name, plus any subsequent "],[" goo.
    //        pos = end;
    //        if (pos < name.Length && name[pos] == ']') pos++;
    //        if (pos < name.Length && name[pos] == ',') pos++;
    //        if (pos < name.Length && name[pos] == '[') pos++;
    //    }
    //    return (genericName, genericArguments.ToArray());
    //}

    #endregion

    #region Helpers

    public static ConstructorInfo GetDefaultConstructor(this Type type) => type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, [], null);
    //public static (PropertyInfo[], FieldInfo[]) GetAllPropertiesFields(Type type) => (GetAllProperties(type), GetAllFields(type));
    public static PropertyInfo[] GetAllProperties(this Type type) => [.. type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().FindAll(p => p.GetGetMethod(true) != null && p.GetGetMethod(true) == p.GetGetMethod(true).GetBaseDefinition())];
    public static FieldInfo[] GetAllFields(this Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    #endregion
}