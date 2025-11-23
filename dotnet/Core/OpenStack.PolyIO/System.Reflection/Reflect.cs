using System.Collections.Generic;
using System.Linq;

namespace System.Reflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public class RTypeAttribute(string name = null) : Attribute {
    public string Name = name;
    internal string GetName(Type cls) => Name ?? cls.FullName[(cls.FullName.IndexOf("Formats.") + 8)..].Replace("+", ".");
    //internal string GetNameX(Type cls) => cls.FullName[(cls.FullName.IndexOf("Formats.") + 8)..];
}

public static class Reflect {
    //static Dictionary<Assembly, Dictionary<string, Type>> TypesByNameX = [];
    static Dictionary<Assembly, Dictionary<string, Type>> TypesByName = [];
    static Dictionary<Assembly, Dictionary<Type, string>> TypeByType = [];

    public static void Scan(Type obj) {
        var assembly = obj.Assembly;
        if (TypesByName.ContainsKey(assembly)) return;
        var attributes = assembly.GetTypes().SelectMany(s => Attribute.GetCustomAttributes(s, typeof(RTypeAttribute)).Select(t => (Name: ((RTypeAttribute)t).GetName(s), /*NameX: ((RTypeAttribute)t).GetNameX(s),*/ Type: s)));
        //TypesByNameX[assembly] = attributes.ToDictionary(s => s.NameX, s => s.Type);
        TypesByName[assembly] = attributes.ToDictionary(s => s.Name, s => s.Type);
        TypeByType[assembly] = attributes.ToDictionary(s => s.Type, s => s.Name);
    }

    public static Type GetTypeByName(string name) {
        foreach (var s in TypesByName.Values) if (s.TryGetValue(name, out var type)) return type;
        //foreach (var s in TypesByNameX.Values) if (s.TryGetValue(name, out type)) return type;
        return null;
    }

    public static string GetNameByType(Type wanted) => TypeByType.TryGetValue(wanted.Assembly, out var z) && z.TryGetValue(wanted, out var name) ? name : null;

    public static string StripAssemblyVersion(string name) {
        var commaIndex = 0;
        while ((commaIndex = name.IndexOf(',', commaIndex)) != -1) {
            if (commaIndex + 1 < name.Length && name[commaIndex + 1] == '[') commaIndex++;
            else {
                var closeBracket = name.IndexOf(']', commaIndex);
                if (closeBracket != -1) name = name.Remove(commaIndex, closeBracket - commaIndex);
                else name = name[..commaIndex];
            }
        }
        return name;
    }

    public static (string, string[]) SplitGenericName(string name) {
        // look for the < generic marker character.
        var pos = name.IndexOf('<');
        if (pos == -1) return default;
        // everything to the left of < is the generic type name.
        var genericName = name[..pos]; var genericArguments = new List<string>();
        // advance to the start of the generic argument list.
        pos++;
        // split up the list of generic type arguments.
        while (pos < name.Length && name[pos] != '>') {
            // locate the end of the current type name argument.
            int nesting = 0, end;
            for (end = pos; end < name.Length; end++) {
                // handle nested types in case we have eg. "List<List<Int>>".
                if (name[end] == '<') nesting++;
                else if (name[end] == '>') {
                    if (nesting > 0) nesting--;
                    else break;
                }
                else if (nesting == 0 && name[end] == ',') break;
            }
            // extract the type name argument.
            genericArguments.Add(name[pos..end].Trim());
            // skip past the type name, plus any subsequent "," goo.
            pos = end;
            if (pos < name.Length && name[pos] == ',') pos++;
        }
        return (genericName, genericArguments.ToArray());
    }

    public static (string, Type[]) SplitGenericType(Type type) {
        if (!type.IsGenericType) return default;
        return (type.FullName[..type.FullName.IndexOf('`')], type.GetGenericArguments());
    }

    public static (string, string[]) SplitGenericTypeName(string name) {
        // look for the ` generic marker character.
        var pos = name.IndexOf('`');
        if (pos == -1) return default;
        // everything to the left of ` is the generic type name.
        var genericName = name[..pos]; var genericArguments = new List<string>();
        // advance to the start of the generic argument list.
        pos++;
        while (pos < name.Length && char.IsDigit(name[pos])) pos++;
        while (pos < name.Length && name[pos] == '[') pos++;
        // split up the list of generic type arguments.
        while (pos < name.Length && name[pos] != ']') {
            // locate the end of the current type name argument.
            int nesting = 0, end;
            for (end = pos; end < name.Length; end++) {
                // handle nested types in case we have eg. "List`1[[List`1[[Int]]]]".
                if (name[end] == '[') nesting++;
                else if (name[end] == ']') {
                    if (nesting > 0) nesting--;
                    else break;
                }
            }
            // extract the type name argument.
            genericArguments.Add(name[pos..end]);
            // skip past the type name, plus any subsequent "],[" goo.
            pos = end;
            if (pos < name.Length && name[pos] == ']') pos++;
            if (pos < name.Length && name[pos] == ',') pos++;
            if (pos < name.Length && name[pos] == '[') pos++;
        }
        return (genericName, genericArguments.ToArray());
    }

    public static ConstructorInfo GetDefaultConstructor(Type type) => type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, [], null);
    public static (PropertyInfo[], FieldInfo[]) GetAllPropertiesFields(Type type) => (GetAllProperties(type), GetAllFields(type));
    public static PropertyInfo[] GetAllProperties(Type type) => [.. type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().FindAll(p => p.GetGetMethod(true) != null && p.GetGetMethod(true) == p.GetGetMethod(true).GetBaseDefinition())];
    public static FieldInfo[] GetAllFields(Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}