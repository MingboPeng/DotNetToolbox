
using TemplateModels.Base;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TemplateModels.CSharp;

public class PropertyTemplateModel : PropertyTemplateModelBase
{
    public static string NameSpaceName => ClassTemplateModel.SDKName;
    public string CsPropertyName { get; set; }
    public string CsParameterName { get; set; }
    public string CsJsonPropertyNameName { get; set; }
    public string ConstructionParameterCode { get; set; }



    public PropertyTemplateModel(string name, JsonSchemaProperty json) : this(name, json, json.IsRequired, json.IsReadOnly)
    {
    }

    public PropertyTemplateModel(string name, JsonSchema json, bool isRequired, bool isReadOnly) : base(name, json, isRequired, isReadOnly)
    {
        // get default value for property for the current client
        DefaultCodeFormat = ConvertDefaultValue(json);

        // check types
        if (IsArray)
        {
            Type = GetListTypeString(json, out var deepestItemType);
            // check List type default
            if (HasDefault)
            {
                DefaultCodeFormat = DefaultCodeFormat.Replace("List<ITEMTYPE>", $"List<{deepestItemType}>");
            }
        }
        else
        {
            Type = GetTypeString(json);
        }

        PropertyName = string.IsNullOrEmpty(PropertyName) ? this.Type : PropertyName;
        CsParameterName = Helper.CleanParameterName(PropertyName);
        CsPropertyName = Helper.CleanPropertyName(PropertyName);
        Description = String.IsNullOrEmpty(Description) ? CsPropertyName : Description;
        CsJsonPropertyNameName = PropertyName;

        Pattern = json.Pattern;
        Maximum = json.Maximum;
        Minimum = json.Minimum;
        MaxLength = json.MaxLength;
        MinLength = json.MinLength;

        IsEnumType = json.ActualSchema.IsEnumeration;
        IsValueType = CsValueType.Contains(Type) || IsEnumType;

        // check default value for constructor parameter
        ConstructionParameterCode = $"{Type} {CsParameterName}";
        if (!this.IsRequired)
        {
            var optionalValue = string.IsNullOrEmpty(DefaultCodeFormat) ? "default" : DefaultCodeFormat;
            optionalValue = IsArray ? "default" : optionalValue;
            optionalValue = Type.StartsWith("AnyOf") ? "default" : optionalValue;
            optionalValue = optionalValue.StartsWith("new ") ? "default" : optionalValue;
            ConstructionParameterCode = $"{ConstructionParameterCode} = {optionalValue}";
        }
    }


    public PropertyTemplateModel(ParameterInfo parameterInfo, string document) : base(parameterInfo)
    {
        // get default value for property for the current client
        if (this.HasDefault)
            DefaultCodeFormat = ConvertDefaultValue(parameterInfo.ParameterType, this.Default);

        CsParameterName = Helper.CleanParameterName(PropertyName);
        Description = String.IsNullOrEmpty(document) ? CsParameterName : document;

        if (parameterInfo.HasDefaultValue)
            this.Default = CheckDefaultValue(parameterInfo.DefaultValue);

        // check default value for constructor parameter
        ConstructionParameterCode = $"{Type} {CsParameterName}";
        if (!this.IsRequired)
        {
            var optionalValue = string.IsNullOrEmpty(DefaultCodeFormat) ? "default" : DefaultCodeFormat;
            optionalValue = IsArray ? "default" : optionalValue;
            optionalValue = Type.StartsWith("AnyOf") ? "default" : optionalValue;
            optionalValue = optionalValue.StartsWith("new ") ? "default" : optionalValue;
            ConstructionParameterCode = $"{ConstructionParameterCode} = {optionalValue}";
        }

        IsEnumType = parameterInfo.ParameterType.IsEnum;
        IsValueType = parameterInfo.ParameterType.IsValueType;
    }

    public PropertyTemplateModel(PropertyInfo propertyInfo, System.Xml.Linq.XDocument xmlDoc) : base(propertyInfo, xmlDoc)
    {
        // get default value for property for the current client
        if (this.HasDefault)
            DefaultCodeFormat = ConvertDefaultValue(propertyInfo.PropertyType, this.Default);


        CsParameterName = Helper.CleanParameterName(PropertyName);
        CsPropertyName = Helper.CleanPropertyName(PropertyName);
        Description = String.IsNullOrEmpty(Description) ? CsParameterName : Description;
        CsJsonPropertyNameName = CsParameterName; // make it camelCase

        // check default value for constructor parameter
        ConstructionParameterCode = $"{Type} {CsParameterName}";
        if (!this.IsRequired)
        {
            var optionalValue = string.IsNullOrEmpty(DefaultCodeFormat) ? "default" : DefaultCodeFormat;
            optionalValue = IsArray ? "default" : optionalValue;
            optionalValue = Type.StartsWith("AnyOf") ? "default" : optionalValue;
            optionalValue = optionalValue.StartsWith("new ") ? "default" : optionalValue;
            ConstructionParameterCode = $"{ConstructionParameterCode} = {optionalValue}";
        }

        IsEnumType = propertyInfo.PropertyType.IsEnum;
        IsValueType = propertyInfo.PropertyType.IsValueType;
    }



    private static object CheckDefaultValue(object dValue)
    {
        if (dValue == null)
        {
            return "null";
        }

        if (dValue is string ss)
        {
            dValue = $"\"{ss}\"";
        }

        return dValue;
    }

    public static string GetTypeString(JsonSchema json)
    {
        var type = string.Empty;
        if ((json.AnyOf?.Any()).GetValueOrDefault())
        {
            var types = GetAnyOfTypes(json);
            var tps = types.Select(_ => ConvertToType(_)).ToList();
            type = $"AnyOf<{string.Join(", ", tps)}>";
        }
        else if (json.IsArray)
        {
            throw new ArgumentException("Found Array type, use GetListTypeString() instead!");
        }
        else
        {
            var propType = json.Type.ToString();
            if (json.HasReference)
            {
                propType = HandleReferenceType(json);
            }
            type = ConvertToType(propType);
        }

        return type;
    }

    public static string GetListTypeString(JsonSchema json, out string deepestItemType)
    {
        var type = string.Empty;
        deepestItemType = string.Empty; // the most inside type, non-list, List<???>
        var itemType = string.Empty;
        if (!json.IsArray)
            throw new ArgumentException("Invalid Array type!");

        var arrayItem = json.Item;
        if (arrayItem.IsArray)
        {
            itemType = GetListTypeString(arrayItem, out deepestItemType);
        }
        else
        {
            itemType = GetTypeString(arrayItem);
            itemType = ConvertToType(itemType);
            deepestItemType = itemType;
        }

        type = $"List<{itemType}>";

        return type;
    }

    public static List<string> GetAnyOfTypes(JsonSchema json)
    {
        var types = new List<string>();
        var anyof = json.AnyOf;
        foreach (var item in anyof)
        {
            var typeName = HandleType(item);
            types.Add(typeName);
        }

        return types;
    }


    private static string HandleType(JsonSchema json)
    {
        var type = string.Empty;
        if (json.HasReference)
        {
            type = HandleReferenceType(json);
        }
        else
        {
            type = json.Type.ToString();
        }
        return type;
    }

    private static string HandleReferenceType(JsonSchema json)
    {
        var typeName = json.ActualSchema.Title;
        //collectImportType(typeName);
        return typeName;
    }


    public static string ConvertToType(string type)
    {
        return TypeMapper.TryGetValue(type, out var result) ? result : type;
    }

    public static Dictionary<string, string> TypeMapper = new Dictionary<string, string>
    {
        {"String", "string" },
        {"Integer", "int" },
        {"Number", "double" },
        {"Boolean", "bool" },
        {"Object", "object" }
    };

    public static List<string> CsValueType = new List<string>
    {
        "int", "double", "bool"
    };

    private static string ConvertDefaultValue(Type propType, object defaultV)
    {
        var defaultValue = defaultV;
        var defaultCodeFormat = string.Empty;
        if (defaultValue == null) return defaultCodeFormat;

        if (defaultValue is string && !propType.IsEnum)
        {
            defaultCodeFormat = $"\"{defaultValue}\"";
        }
        else if (propType.IsEnum) // is enum
        {
            var enumType = propType.Name;
            var cleanEnumValue = Helper.ToPascalCase(Helper.CleanName(defaultValue.ToString(), true), true);
            defaultCodeFormat = $"{enumType}.{cleanEnumValue}";
        }
        else if (propType.Name == "Boolean")
        {
            defaultCodeFormat = defaultValue.ToString().ToLower();
        }
        else if (propType.Name == "Double")
        {
            defaultCodeFormat = $"{defaultValue}D";
        }
        else
        {
            defaultCodeFormat = defaultValue?.ToString();
        }

        return defaultCodeFormat;
    }

    private static string ConvertDefaultValue(JsonSchema prop)
    {
        var defaultValue = prop.Default;
        var defaultCodeFormat = string.Empty;
        if (defaultValue == null) return defaultCodeFormat;

        if (defaultValue is string)
        {
            defaultCodeFormat = $"\"{defaultValue}\"";
            // is enum
            if (prop.ActualSchema.IsEnumeration)
            {
                var enumType = prop.ActualSchema.Title;
                var cleanEnumValue = Helper.ToPascalCase(Helper.CleanName(defaultValue.ToString(), true), true);
                defaultCodeFormat = $"{enumType}.{cleanEnumValue}";
            }

        }
        else if (defaultValue is Newtonsoft.Json.Linq.JToken jToken)
        {
            defaultCodeFormat = GetDefaultFromJson(jToken);
        }
        else if (prop.Type.ToString() == "Boolean")
        {
            defaultCodeFormat = defaultValue.ToString().ToLower();
        }
        else if (prop.Type.ToString() == "Number")
        {
            defaultCodeFormat = $"{defaultValue}D";
        }
        else
        {
            defaultCodeFormat = defaultValue?.ToString();
        }

        return defaultCodeFormat;
    }

    private static string GetDefaultFromJson(JToken jContainer)
    {
        var defaultCodeFormat = string.Empty;
        if (jContainer is Newtonsoft.Json.Linq.JObject jObj)
        {
            if (jObj.TryGetValue("type", out var vType))
            {
                var isFullJsonObj = jObj.Values().Count() > 1;
                var formateJson = isFullJsonObj ? jObj.ToString()?.Replace("\"", "\"\"") : "";
                defaultCodeFormat = isFullJsonObj ? $"(@\"{formateJson}\").To<{vType}>()" : $"new {vType}()";
            }
            else
            {
                defaultCodeFormat = jContainer.ToString();
            }
        }
        else if (jContainer is Newtonsoft.Json.Linq.JArray jArray)
        {
            var arrayCode = new List<string>();
            var separator = $", ";
            var itemTypeKey = "ITEMTYPE";
            foreach (var item in jArray)
            {
                if (item.Type == JTokenType.Array)
                {
                    itemTypeKey = $"List<{itemTypeKey}>";
                }
                arrayCode.Add(GetDefaultFromJson(item).ToString());
            }
            defaultCodeFormat = $"new List<{itemTypeKey}>{{ {string.Join(separator, arrayCode)} }}";

        }
        else
        {
            defaultCodeFormat = jContainer.ToString();
        }

        return defaultCodeFormat;
    }


}
