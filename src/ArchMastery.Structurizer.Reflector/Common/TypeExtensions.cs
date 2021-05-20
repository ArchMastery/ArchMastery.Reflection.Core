#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ArchMastery.Structurizer.Reflector
{
    public static class TypeExtensions
    {
        public static string NormalizeType(this MemberInfo type)
        {
            var typeName = type.Name;
            return NormalizeType(typeName);
        }

        public static string NormalizeType(string typeName)
        {
            return typeName.Replace("System.", string.Empty) switch
                   {
                       "ValueType" => "struct",
                       "Void" => "void",
                       "Object" => "object",
                       "String" => "string",
                       "Int16" => "short",
                       "UInt16" => "ushort",
                       "Int32" => "int",
                       "UInt32" => "uint",
                       "Int64" => "long",
                       "UInt64" => "ulong",
                       "Single" => "float",
                       "Double" => "double",
                       "Byte" => "byte",
                       "SByte" => "sbyte",
                       "Decimal" => "decimal",
                       "Boolean" => "bool",
                       "Object[]" => "object[]",
                       "String[]" => "string[]",
                       "Int16[]" => "short[]",
                       "UInt16[]" => "ushort[]",
                       "Int32[]" => "int[]",
                       "UInt32[]" => "uint[]",
                       "Int64[]" => "long[]",
                       "UInt64[]" => "ulong[]",
                       "Single[]" => "float[]",
                       "Double[]" => "double[]",
                       "Byte[]" => "byte[]",
                       "SByte[]" => "sbyte[]",
                       "Decimal[]" => "decimal[]",
                       "Boolean[]" => "bool[]",
                       _ => typeName
                   };
        }

        public static string? NormalizeNameString(this string? name)
        {
            if (name is null) return null;
            name = NormalizeType(name);
            if (name.StartsWith("get_") || name.StartsWith("set_") || name.StartsWith("init_"))
            {
#if NET5_0
                name = name[..name.IndexOf("_", StringComparison.Ordinal)] + ";";
#else
                name = name.Substring(0, name.IndexOf("_", StringComparison.Ordinal)) + ";";
#endif
            }

            Regex regex = new(@"`[1-9]\[\[([^,\s]*).*\]\]");
            if (regex.IsMatch(name))
            {
                var match = regex.Match(name);
                var result = $"<{match.Groups.Cast<Group>().LastOrDefault()?.Value}>";
                name = regex.Replace(name, result);
            }
            else
            {
                regex = new Regex(@"`[1-9]");
                if (!regex.IsMatch(name)) return name;
                name = regex.Replace(name, string.Empty);
            }

            return name;
        }

        public static string? NormalizeName(this Type? type)
        {
            if (type is null) return null;

            var typeName = GetGenericName(type);

            return NormalizeNameString(typeName);
        }

        private static string? GetGenericName(Type genericType)
        {
            var typeName = genericType.FullName ?? genericType.Name;

            if (!typeName.Contains('`')) return NormalizeNameString(typeName);

            if (genericType.IsArray)
            {
            }

            if (!genericType.IsGenericType && !genericType.IsGenericTypeDefinition)
                return NormalizeNameString(typeName);

            string genericTypes = string.Join(", ", GetGenericsArguments());

            genericTypes = $"<{genericTypes}>";

            if (genericType.IsNested)
            {
                var parentName = NormalizeName(genericType.ReflectedType!);
                typeName = $"{parentName}.{genericType.Name}";
            }

#if !NET5_0
            var result = $"{typeName.Substring(0, typeName.IndexOf("`", StringComparison.Ordinal))}{genericTypes}";
#else
            var result = $"{typeName[..typeName.IndexOf("`", StringComparison.Ordinal)]}{genericTypes}";
#endif

            return result;

            IEnumerable<string?> GetGenericsArguments()
            {
                var reflectedTypes = genericType.ReflectedType?.IsGenericType ?? false
                                         ? genericType.ReflectedType.GetGenericArguments()
                                         : Array.Empty<Type>();

                var types = new List<Type>(reflectedTypes);

                types.AddRange(genericType.GetGenericArguments().Skip(types.Count));

                return types.Select(NormalizeName);
            }
        }
    }
}
