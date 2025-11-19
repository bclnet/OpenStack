using System.Collections.Generic;
using System.Linq;

namespace System.Reflection;

public static class Reflect {
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

    public static (string, List<string>) SplitGenericName(string name) {
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
                else {
                    if (nesting > 0) nesting--;
                    else break;
                }
            }
            // extract the type name argument.
            genericArguments.Add(name[pos..end].Trim());
            // skip past the type name, plus any subsequent "," goo.
            pos = end;
            if (pos < name.Length && name[pos] == ',') pos++;
        }
        return (genericName, genericArguments);
    }

    public static (string, List<string>) SplitGenericTypeName(string name) {
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
        return (genericName, genericArguments);
    }

    public static ConstructorInfo GetDefaultConstructor(Type type) => type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, [], null);
    public static (PropertyInfo[], FieldInfo[]) GetAllPropertiesFields(Type type) => (GetAllProperties(type), GetAllFields(type));
    public static PropertyInfo[] GetAllProperties(Type type) => [.. type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().FindAll(p => p.GetGetMethod(true) != null && p.GetGetMethod(true) == p.GetGetMethod(true).GetBaseDefinition())];
    public static FieldInfo[] GetAllFields(Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}