
using TemplateModels.Base;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data;
using Newtonsoft.Json;
using System.Collections;

namespace TemplateModels.TypeScript;

public class PropertyTemplateModel : PropertyTemplateModelBase
{
    public string TsPropertyName { get; set; }
    public string TsParameterName { get; set; }
    public string TsJsonPropertyName { get; set; } // property name used in json
    public string ConvertToJavaScriptCode { get; set; } // for TS: property value to JS object
    public string ConvertToClassCode { get; set; } // for TS: JSON object to class property



    public List<TsImport> TsImports { get; set; } = new List<TsImport>();
    public bool HasTsImports => TsImports.Any();


    public bool HasValidationDecorators => (ValidationDecorators?.Any()).GetValueOrDefault();
    public List<string> ValidationDecorators { get; set; }

    public bool HasTransformDecorator => !string.IsNullOrWhiteSpace(TransformDecorator);
    public string TransformDecorator { get; set; }


    public PropertyTemplateModel(string name, JsonSchemaProperty json) : this(name, json, json.IsRequired, json.IsReadOnly)
    {
    }

    public PropertyTemplateModel(string name, JsonSchema json, bool isRequired, bool isReadOnly) : base(name, json, isRequired, isReadOnly)
    {
        // get default value for property for the current client
        DefaultCodeFormat = ConvertTsDefaultValue(json);

        // check types
        this.Type = GetTypeScriptType(json, AddTsImportTypes);

        PropertyName = string.IsNullOrEmpty(PropertyName) ? this.Type : PropertyName;
        TsParameterName = Helper.CleanParameterName(PropertyName);
        TsPropertyName = TsParameterName; // make it camelCase
        TsJsonPropertyName = PropertyName; 

        Description = String.IsNullOrEmpty(Description) ? TsPropertyName : Description;

        ConvertToJavaScriptCode = this.HasDefault ? 
            $"data[\"{TsJsonPropertyName}\"] = this.{TsPropertyName} ?? {DefaultCodeFormat};" : 
            $"data[\"{TsJsonPropertyName}\"] = this.{TsPropertyName};";
        ConvertToClassCode = this.HasDefault ? 
            $"this.{TsPropertyName} = obj.{TsPropertyName} ?? {DefaultCodeFormat};" : 
            $"this.{TsPropertyName} = obj.{TsPropertyName};";

        //validation decorators
        ValidationDecorators = GetValidationDecorators(json, isRequired: isRequired);

        // get @Transform
        TransformDecorator = GetTransform(json, false);


    }


    public PropertyTemplateModel(ParameterInfo parameterInfo, string document) : base(parameterInfo)
    {
        // get default value for property for the current client
        if (this.HasDefault)
            DefaultCodeFormat = ConvertTsDefaultValue(parameterInfo.ParameterType, this.Default);

        TsParameterName = Helper.CleanParameterName(PropertyName);
        Description = String.IsNullOrEmpty(document) ? TsParameterName : document;

        var typeUsed = Helper.GetTypes(parameterInfo.ParameterType);
        TsImports = typeUsed.Select(_ => new TsImport(_.Name, _.Namespace)).Distinct()?
            .ToList() ?? new List<TsImport>();

        IsEnumType = parameterInfo.ParameterType.IsEnum;
        IsValueType = parameterInfo.ParameterType.IsValueType;

    }

    public PropertyTemplateModel(PropertyInfo propertyInfo, System.Xml.Linq.XDocument xmlDoc) : base(propertyInfo, xmlDoc)
    {
        //this.Type = GetTypeScriptType(propertyInfo.PropertyType, AddTsImportTypes);

        // get default value for property for the current client
        if (this.HasDefault)
            DefaultCodeFormat = ConvertTsDefaultValue(propertyInfo.PropertyType, this.Default);

        TsParameterName = Helper.CleanParameterName(PropertyName);
        TsPropertyName = TsParameterName; // make it camelCase
        TsJsonPropertyName = TsPropertyName; // keep it the same as TsPropertyName
        Description = String.IsNullOrEmpty(Description) ? TsParameterName : Description;

        var typeUsed = Helper.GetTypes(propertyInfo.PropertyType);
        TsImports = typeUsed.Select(_ => new TsImport(_.Name, _.Namespace)).Distinct()?
            .ToList() ?? new List<TsImport>();

        ConvertToJavaScriptCode = this.HasDefault ? $"data[\"{TsJsonPropertyName}\"] = this.{TsPropertyName} ?? {DefaultCodeFormat};" : $"data[\"{TsJsonPropertyName}\"] = this.{TsPropertyName};";
        ConvertToClassCode = this.HasDefault ? $"this.{TsPropertyName} = obj.{TsPropertyName} ?? {DefaultCodeFormat};" : $"this.{TsPropertyName} = obj.{TsPropertyName};";
        //validation decorators
        ValidationDecorators = GetValidationDecorators(propertyInfo.PropertyType);


        IsEnumType = propertyInfo.PropertyType.IsEnum;
        IsValueType = propertyInfo.PropertyType.IsValueType;

        // get @Transform
        TransformDecorator = GetTransform(propertyInfo.PropertyType, false);
    }

    public static string GetTransform(JsonSchema json, bool isArray)
    {
        var code = string.Empty;
        if ((json.AnyOf?.Any()).GetValueOrDefault())
        {
            var allRefs = json.AnyOf.All(_ => _.HasReference);
            if (!allRefs)
                return code;

            var refTypes = json.AnyOf.Select(r => r.ActualSchema.Title).ToList();

            var tps = refTypes.Select(_ => $"if (item?.type === '{_}') return {_}.fromJS(item);").ToList();
            tps = tps.Take(1).Concat(tps.Skip(1).Select(_ => $"else {_}")).ToList();
            tps.Add("else return item;");
            tps = tps.Select(_ => $"      {_}").ToList();

            var trans = string.Join(Environment.NewLine, tps);
            code = $@"@Transform(({{ value }}) => {{
      const item = value;
{trans}
    }})";
            return isArray ? trans : code;
        }
        else if (json.IsArray)
        {
            var arrayItem = json.Item;
            var itemCode = GetTransform(arrayItem, true);
            if (string.IsNullOrEmpty(itemCode))
                return code;
            code = $@"@Transform(({{ value }}) => value.map((item: any) => {{
{itemCode}
    }}))";
            return code;
        }
        else
        {
            return code;
        }

    }

    public static string GetTransform(Type tp, bool isArray)
    {
        var code = string.Empty;
        if (tp.IsAnyOf())
        {
            var allTps = Helper.GetTypes(tp);
            var allRefs = allTps.All(_ => !_.IsValueType && _ != typeof(string));
            if (!allRefs)
                return code;

            var refTypes = allTps.Select(r => r.Name).ToList();

            var tps = refTypes.Select(_ => $"if (item?.type === '{_}') return {_}.fromJS(item);").ToList();
            tps = tps.Take(1).Concat(tps.Skip(1).Select(_ => $"else {_}")).ToList();
            tps.Add("else return item;");
            tps = tps.Select(_ => $"      {_}").ToList();

            var trans = string.Join(Environment.NewLine, tps);
            code = $@"@Transform(({{ value }}: {{ value: any }}) => {{
      const item = value;
{trans}
    }})";
            return isArray ? trans : code;
        }
        else if (tp.IsArray())
        {
            var arrayItem = tp.GetGenericArguments()[0];
            var itemCode = GetTransform(arrayItem, true);
            if (string.IsNullOrEmpty(itemCode))
                return code;
            code = $@"@Transform(({{ value }}: {{ value: any }}) => value.map((item: any) => {{
{itemCode}
    }}))";
            return code;
        }
        else
        {
            return code;
        }

    }


    public static string GetTypeScriptType(JsonSchema json, Action<string> collectImportType)
    {
        var type = string.Empty;
        if ((json.AnyOf?.Any()).GetValueOrDefault())
        {
            var types = GetAnyOfTypes(json, collectImportType);
            var tps = types.Select(_ => ConvertToTypeScriptType(_)).ToList();
            type = $"({string.Join(" | ", tps)})";
        }
        else if (json.IsArray)
        {
            var arrayItem = json.Item;
            var itemType = GetTypeScriptType(arrayItem, collectImportType);
            type = $"{ConvertToTypeScriptType(itemType)}[]";
        }
        else
        {
            var propType = json.Type.ToString();
            if (json.HasReference)
            {
                propType = HandleReferenceType(json, collectImportType);
            }
            type = ConvertToTypeScriptType(propType);
        }

        return type;
    }

    public static List<string> GetAnyOfTypes(JsonSchema json, Action<string> collectImportType)
    {
        var types = new List<string>();
        var anyof = json.AnyOf;
        foreach (var item in anyof)
        {
            var typeName = HandleType(item, collectImportType);
            types.Add(typeName);
        }

        return types;
    }


    private static string HandleType(JsonSchema json, Action<string> collectImportType)
    {
        var type = string.Empty;
        if (json.HasReference)
        {
            type = HandleReferenceType(json, collectImportType);
        }
        else
        {
            type = json.Type.ToString();
        }
        return type;
    }

    private static string HandleReferenceType(JsonSchema json, Action<string> collectImportType)
    {
        var typeName = json.ActualSchema.Title;
        collectImportType(typeName);
        return typeName;
    }


    public static string ConvertToTypeScriptType(string type)
    {
        return TsTypeMapper.TryGetValue(type, out var result) ? result : type;
    }

    public static Dictionary<string, string> TsTypeMapper = new Dictionary<string, string>
    {
        {"String", "string" },
        {"Integer", "number" },
        {"Number", "number" },
        {"Boolean", "boolean" }
    };

    private static bool IsTypeEnumerable(Type type)
    {
        bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
        var isArray = isEnumerable && type != typeof(string);
        return isArray;
    }

    private static List<string> GetValidationDecorators(Type type, bool isArrayItem, bool isRequired)
    {
        var result = new List<string>();
        var isArray = IsTypeEnumerable(type);
        if (isArray)
        {
            var arrayItem = type.GetGenericArguments()[0];
            var decos = GetValidationDecorators(arrayItem, isArrayItem: true, isRequired: isRequired);

            result.Add("@IsArray({ each: true })"); // Ensures each item in the array is also an array.
            result.Add("@ValidateNested({each: true })");// Ensures each item in the array is validated as a nested object.
            result.Add($"@Type(() => Array)"); //Helps class-transformer understand that each item in the array should be treated as an array.

            result.AddRange(decos);

            return result;
        }


        var propType = type.Name;
        var isValueType = type.IsValueType || type == typeof(string);
        if (!isValueType)
        {
            var refPropType = propType;
            result.Add(isArrayItem ? $"@IsInstance({refPropType}, {{ each: true }})" : $"@IsInstance({refPropType})");
            result.Add($"@Type(() => {refPropType})");
            result.Add(isArrayItem ? "@ValidateNested({ each: true })" : "@ValidateNested()");
        }
        else if (type.IsEnum)
        {
            result.Add(isArrayItem ? $"@IsEnum({propType}, {{ each: true }})" : $"@IsEnum({propType})");
            result.Add($"@Type(() => String)");
        }
        else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)) // number
        {
            result.Add(isArrayItem ? "@IsInt({ each: true })" : "@IsInt()");
        }
        else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) // number
        {
            result.Add(isArrayItem ? "@IsNumber({},{ each: true })" : "@IsNumber()");
        }
        else if (type == typeof(string) || type == typeof(char)) // string
        {
            result.Add(isArrayItem ? "@IsString({ each: true })" : "@IsString()");
        }
        else if (type == typeof(bool)) // boolean
        {
            result.Add(isArrayItem ? "@IsBoolean({ each: true })" : "@IsBoolean()");
        }
        else if (type == typeof(DateTime)) //Date
        {
            result.Add(isArrayItem ? "@IsDate({ each: true })" : "@IsDate()");
        }
        else if (type == typeof(Guid)) // string
        {
            result.Add(isArrayItem ? "@IsUUID({ each: true })" : "@IsUUID()");
        }
        else
        {
            //result.Add($"@IsObject()");
        }

        return result;
    }

    public List<string> GetValidationDecorators(Type type)
    {
        var result = new List<string>();
        if (type.IsAnyOf())
        {
            // no validation deco
        }
        else if (this.IsArray)
        {
            result.Add("@IsArray()");
            var arrayItem = type.GetGenericArguments()[0];
            var decos = GetValidationDecorators(arrayItem, isArrayItem: true, isRequired: this.IsRequired);

            if (IsTypeEnumerable(type))
            {
                var IsNestedNumberArray = decos.Any(_ => _.StartsWith("@IsNumber"));
                var IsNestedIntegerArray = decos.Any(_ => _.StartsWith("@IsInt"));
                var IsNestedStringArray = decos.Any(_ => _.StartsWith("@IsString"));

                if (IsNestedNumberArray)
                    result.Add("@IsNestedNumberArray()"); // for number[][] or number[][][] types
                else if (IsNestedIntegerArray)
                    result.Add("@IsNestedIntegerArray()"); // for number[][] or number[][][] types
                else if (IsNestedStringArray)
                    result.Add("@IsNestedStringArray()"); // for number[][] or number[][][] types
                else
                    result.AddRange(decos);
            }
            else
            {
                result.AddRange(decos);
            }

        }
        else
        {
            var decos = GetValidationDecorators(type, isArrayItem: false, isRequired: this.IsRequired);
            result.AddRange(decos);
        }

        if (this.IsRequired)
        {
            result.Add($"@IsDefined()");
        }
        else
        {
            result.Add($"@IsOptional()");
        }


        if (this.HasPattern)
        {
            result.Add($"@Matches(/{this.Pattern}/)");
        }
        if (this.HasMinimum)
        {
            result.Add($"@Min({this.Minimum})");
        }
        if (this.HasMaximum)
        {
            result.Add($"@Max({this.Maximum})");
        }
        if (this.HasMinLength)
        {
            result.Add($"@MinLength({this.MinLength})");
        }
        if (this.HasMaxLength)
        {
            result.Add($"@MaxLength({this.MaxLength})");
        }

        return result;

    }


    private static List<string> GetValidationDecorators(JsonSchema json, bool isArrayItem, bool isRequired)
    {
        var result = new List<string>();
        if (json.IsArray)
        {
            var arrayItem = json.Item;
            var decos = GetValidationDecorators(arrayItem, isArrayItem: true, isRequired: isRequired);

            result.Add("@IsArray({ each: true })"); // Ensures each item in the array is also an array.
            result.Add("@ValidateNested({each: true })");// Ensures each item in the array is validated as a nested object.
            result.Add($"@Type(() => Array)"); //Helps class-transformer understand that each item in the array should be treated as an array.

            result.AddRange(decos);

            return result;
        }


        var propType = json.Type.ToString();
        if (json.HasReference)
        {
            var refPropType = json.ActualSchema.Title;
            if (json.ActualSchema.IsEnumeration)
            {
                result.Add(isArrayItem ? $"@IsEnum({refPropType}, {{ each: true }})" : $"@IsEnum({refPropType})");
                result.Add($"@Type(() => String)");
            }
            else
            {
                result.Add(isArrayItem ? $"@IsInstance({refPropType}, {{ each: true }})" : $"@IsInstance({refPropType})");
                result.Add($"@Type(() => {refPropType})");
                result.Add(isArrayItem ? "@ValidateNested({ each: true })" : "@ValidateNested()");
            }

        }
        else if (propType == "Integer")
        {
            result.Add(isArrayItem ? "@IsInt({ each: true })" : "@IsInt()");
        }
        else if (propType == "Number")
        {
            result.Add(isArrayItem ? "@IsNumber({},{ each: true })" : "@IsNumber()");
        }
        else if (propType == "String")
        {
            result.Add(isArrayItem ? "@IsString({ each: true })" : "@IsString()");
        }
        else if (propType == "Boolean")
        {
            result.Add(isArrayItem ? "@IsBoolean({ each: true })" : "@IsBoolean()");
        }
        else
        {
            //result.Add($"@IsObject()");
        }

        return result;
    }
    public static List<string> GetValidationDecorators(JsonSchema json, bool isRequired)
    {
        var result = new List<string>();
        if (json.IsArray)
        {
            result.Add("@IsArray()");
            var arrayItem = json.Item;
            var decos = GetValidationDecorators(arrayItem, isArrayItem: true, isRequired: isRequired);

            if (arrayItem.IsArray)
            {
                var IsNestedNumberArray = decos.Any(_ => _.StartsWith("@IsNumber"));
                var IsNestedIntegerArray = decos.Any(_ => _.StartsWith("@IsInt"));
                var IsNestedStringArray = decos.Any(_ => _.StartsWith("@IsString"));

                if (IsNestedNumberArray)
                    result.Add("@IsNestedNumberArray()"); // for number[][] or number[][][] types
                else if (IsNestedIntegerArray)
                    result.Add("@IsNestedIntegerArray()"); // for number[][] or number[][][] types
                else if (IsNestedStringArray)
                    result.Add("@IsNestedStringArray()"); // for number[][] or number[][][] types
                else
                    result.AddRange(decos);
            }
            else
            {
                result.AddRange(decos);
            }


        }
        else
        {
            var decos = GetValidationDecorators(json, isArrayItem: false, isRequired: isRequired);
            result.AddRange(decos);
        }

        if (isRequired)
        {
            result.Add($"@IsDefined()");
        }
        else
        {
            result.Add($"@IsOptional()");
        }


        if (!string.IsNullOrEmpty(json.Pattern))
        {
            result.Add($"@Matches(/{json.Pattern}/)");
        }
        if (json.Minimum.HasValue)
        {
            result.Add($"@Min({json.Minimum})");
        }
        if (json.Maximum.HasValue)
        {
            result.Add($"@Max({json.Maximum})");
        }
        if (json.MinLength.HasValue)
        {
            result.Add($"@MinLength({json.MinLength})");
        }
        if (json.MaxLength.HasValue)
        {
            result.Add($"@MaxLength({json.MaxLength})");
        }

        return result;

    }

    public void AddTsImportTypes(string type)
    {
        if (type == Type)
            return;
        TsImports.Add(new TsImport(type, null));
    }

    private static string ConvertTsDefaultValue(Type propType, object defaultV)
    {
        var defaultValue = defaultV;
        var defaultCodeFormat = string.Empty;
        if (defaultValue == null) return defaultCodeFormat;

        if (defaultValue is string && !propType.IsEnum)
        {
            defaultCodeFormat = $"\"{defaultValue}\"";
        }
        else if (propType.IsEnum)     // is enum
        {
            var enumType = propType.Name;
            var cleanEnumValue = Helper.ToPascalCase(Helper.CleanName(defaultValue.ToString(), true), true);
            defaultCodeFormat = $"{enumType}.{cleanEnumValue}";
        }
        else if (propType.Name == "Boolean")
        {
            defaultCodeFormat = defaultValue.ToString().ToLower();
        }
        else
        {
            defaultCodeFormat = defaultValue?.ToString();
        }

        return defaultCodeFormat;
    }

    private static string ConvertTsDefaultValue(JsonSchema prop)
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
                defaultCodeFormat = isFullJsonObj ? $"{vType}.fromJS({jObj})" : $"new {vType}()";
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
            foreach (var item in jArray)
            {
                arrayCode.Add(GetDefaultFromJson(item).ToString());
            }
            defaultCodeFormat = $"[{string.Join(separator, arrayCode)}]";
        }
        else
        {
            defaultCodeFormat = jContainer.ToString();
        }

        return defaultCodeFormat;
    }
}
