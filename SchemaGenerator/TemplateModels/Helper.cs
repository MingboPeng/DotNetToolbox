using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TemplateModels
{
    public enum TargetLanguage
    {
        CSharp,
        TypeScript
    }

    public static class Helper
    {
        public static bool IsAnyOf(this Type type)
        {
            return type?.BaseType?.Name == "AnyOf";
        }

        public static bool IsArray(this Type type) {

            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
            var IsArray = isEnumerable && type != typeof(string);
            return IsArray;
        }

        /// <summary>
        /// Get all related type, this is useful for Array or List with its ElementType and GenericArguments
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Type> GetTypes(Type type)
        {
            var tps = new List<Type>();
            if (!type.IsGenericType) // non-List
            {
                // array
                if (type.IsArray)
                {
                    var elemType = type.GetElementType();
                    tps.Add(elemType);
                }
                else
                {
                    tps.Add(type);
                }

            }

            tps.AddRange(type.GetGenericArguments().SelectMany(_ => GetTypes(_)));
            tps = tps?.Distinct()?.ToList();
            return tps;

        }

        public static string GetCheckTypeName(Type type)
        {
            if (!type.IsGenericType) // non-List
            {
                // array
                if (type.IsArray)
                {
                    var elemType = type.GetElementType();
                    return $"{CheckTypeName(elemType.Name)}[]";
                }
                else
                {
                    return CheckTypeName(type.Name);
                }

            }

            string typeName = type.GetGenericTypeDefinition().Name;
            // Remove the generic arity from the type name
            typeName = typeName.Substring(0, typeName.IndexOf('`'));
            typeName = CheckTypeName(typeName);

            string[] typeArguments = type.GetGenericArguments()
                                          .Select(_ => GetCheckTypeName(_))
                                          .Select(_ => CheckTypeName(_))
                                          .ToArray();

            // AnyOf in TypeScript: (T1 | T2)
            if (Language == TargetLanguage.TypeScript && typeName == "AnyOf")
            {
                return $"({string.Join(" | ", typeArguments)})";
            }

            return $"{typeName}<{string.Join(", ", typeArguments)}>";
        }

        private static string CheckTypeName(string typeName)
        {
            if (TypeMapper.TryGetValue(typeName, out var t))
                return t;
            return typeName;
        }

        public static TargetLanguage Language { get; set; } = TargetLanguage.CSharp;

        private static Dictionary<string, string> TypeMapper
        {
            get
            {
                switch (Language)
                {
                    case TargetLanguage.CSharp:
                        return CsTypeMapper;
                    case TargetLanguage.TypeScript:
                        return TsTypeMapper;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private static Dictionary<string, string> CsTypeMapper
            = new Dictionary<string, string> {
                { "Boolean", "bool" },
                { "Char", "char" },
                { "Decimal", "decimal" },
                { "Double", "double" },
                { "Int32", "int" },
                { "Int64", "long" },
                { "Object", "object" },
                { "String", "string" },
                { "Single", "float" },
                { "Void", "void" }
            };

        private static Dictionary<string, string> TsTypeMapper
            = new Dictionary<string, string> {
                { "Boolean", "boolean" },
                { "Char", "string" },
                { "Decimal", "number" },
                { "Double", "number" },
                { "Int32", "number" },
                { "Int64", "number" },
                { "Object", "any" },
                { "String", "string" },
                { "Single", "number" },
                { "Void", "void" },
                { "Task", "Promise" },
                { "Dictionary", "Map" },
                { "List", "Array" },
                { "DateTime", "Date" }
            };



        public static string GetMethodSummary(string assemblyPath, string typeName, string methodName)
        {
            // Load the assembly
            Assembly assembly = Assembly.LoadFrom(assemblyPath);

            // Get the type
            Type type = assembly.GetType(typeName);

            // Get the method info
            //MethodInfo methodInfo = type.GetMethod(methodName);

            // Construct the member name to search in the XML file
            string memberName = $"M:{type.FullName}.{methodName}";

            // Load the XML documentation
            string xmlPath = assemblyPath.Substring(0, assemblyPath.LastIndexOf(".")) + ".xml";
            XDocument xmlDoc = XDocument.Load(xmlPath);

            // Query the XML for the summary of the method
            var summary = xmlDoc.Descendants("member")
                                .FirstOrDefault(m => m.Attribute("name").Value.Equals(memberName))
                                ?.Element("summary")?.Value.Trim();

            return summary;
        }

        public static string CheckTypeScriptTypeName(string typeName)
        {
            if (TsTypeMapper.TryGetValue(typeName, out var t))
                return t;
            return typeName;
        }

        public static string CleanName(string input, bool allowUnderscore = false)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove invalid characters
            var patten = allowUnderscore ? @"[^a-zA-Z0-9_]" : @"[^a-zA-Z0-9]";
            string cleaned = Regex.Replace(input, patten, "");

            // Remove first underscore if any: _OpenAPIBase
            cleaned = cleaned.StartsWith("_") ? cleaned.Substring(1, cleaned.Length - 1) : cleaned;

            // Ensure the name starts with a letter or underscore
            if (!Regex.IsMatch(cleaned, @"^[a-zA-Z]"))
            {
                cleaned = "_" + cleaned;
            }

            return cleaned;
        }

        public static string ToPascalCase(string input, bool allowUnderscore = false)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var words = input.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return allowUnderscore ? string.Join("_", words) : string.Join(string.Empty, words);
        }

        /// <summary>
        /// camelCase
        /// </summary>
        /// <param name="input"></param>
        /// <param name="allowUnderscore"></param>
        /// <returns></returns>
        public static string ToCamelCase(string input, bool allowUnderscore = false)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var words = input.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length ==1)
            {
                // CamelCase => camelCase
                return char.ToLower(words[0][0]) + words[0].Substring(1);
            }

            for (int i = 0; i < words.Length; i++)
            {
                if (i == 0)
                {
                    words[i] = words[i].ToLower();
                }
                else
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            return allowUnderscore ? string.Join("_", words) : string.Join(string.Empty, words);

        }


        public static string CleanMethodName(string input)
        {
            return ToPascalCase(CleanName(ToPascalCase(input)));
        }

        public static string CleanPropertyName(string input) => CleanMethodName(input);

        public static string CleanParameterName(string input)
        {
            return CleanName(ToCamelCase(input));
        }

    }
}
