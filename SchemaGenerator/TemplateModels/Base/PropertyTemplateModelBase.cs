using NJsonSchema;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections;
using System;

namespace TemplateModels.Base;

public class PropertyTemplateModelBase
{
    public string PropertyName { get; set; }
    public string Type { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsRequired { get; set; }
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public string Description { get; set; }

    public object Default { get; set; }
    public string DefaultCodeFormat { get; set; }
    public bool HasDefault => Default != null;
    public bool HasVeryLongDefault => HasDefault && (DefaultCodeFormat?.Length > 100);
    public bool IsAnyOf { get; set; }
    public List<JsonSchema> AnyOf { get; set; }

    public bool IsArray { get; set; }

    public string Pattern { get; set; }
    public bool HasPattern => !string.IsNullOrEmpty(Pattern);
    public decimal? Maximum { get; set; }
    public bool HasMaximum => Maximum.HasValue;
    public decimal? Minimum { get; set; }
    public bool HasMinimum => Minimum.HasValue;
    //public bool IsInherited { get; set; }
    public bool IsValueType { get; set; }
    public bool IsEnumType { get; set; }
    public int? MaxLength { get; set; }
    public bool HasMaxLength => MaxLength.HasValue;
    public int? MinLength { get; set; }
    public bool HasMinLength => MinLength.HasValue;

    public PropertyTemplateModelBase(string name, JsonSchemaProperty json): this(name, json, json.IsRequired, json.IsReadOnly)
    {
    }

    public PropertyTemplateModelBase(string name, JsonSchema json, bool isRequired, bool isReadOnly)
    {
        PropertyName = name;
        Default = json.Default;

        Description = json.Description?.Replace("\n", "\\n")?.Replace("\"", "\"\"");

        AnyOf = json.AnyOf?.ToList();
        IsAnyOf = (AnyOf?.Any()).GetValueOrDefault();
        IsReadOnly = isReadOnly;
        IsRequired = isRequired;
        IsArray = json.IsArray;
    }

    public PropertyTemplateModelBase(ParameterInfo parameterInfo)
    {
        this.Type = Helper.GetCheckTypeName(parameterInfo.ParameterType);
        this.PropertyName = parameterInfo.Name;

        // get default attribute
        if (parameterInfo.DefaultValue != DBNull.Value)
            this.Default = parameterInfo.DefaultValue;

        // get required attribute
        this.IsRequired = !parameterInfo.IsOptional;

        // IsArray or List
        bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(parameterInfo.ParameterType);
        IsArray = isEnumerable && parameterInfo.ParameterType != typeof(string);

   

    }

    public PropertyTemplateModelBase(PropertyInfo propertyInfo, System.Xml.Linq.XDocument xmlDoc)
    {
        this.Type = Helper.GetCheckTypeName(propertyInfo.PropertyType);
        this.PropertyName = propertyInfo.Name;

        // get default attribute
        var defaultAtt = propertyInfo.GetCustomAttribute<DefaultValueAttribute>(true);
        if (defaultAtt != null)
            this.Default = defaultAtt.Value;

        // get required attribute
        var requiredAtt = propertyInfo.GetCustomAttribute<RequiredAttribute>(true);
        this.IsRequired = requiredAtt != null;


        // documentation
        this.Description = GetPropertyDoc(propertyInfo, xmlDoc);

        // IsArray or List
        bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType);
        IsArray = isEnumerable && propertyInfo.PropertyType != typeof(string);

        // IsReadyOnly
        this.IsReadOnly = !propertyInfo.CanWrite;

    }


    public override string ToString()
    {
        return PropertyName;
    }

    private static string GetPropertyDoc(PropertyInfo property, System.Xml.Linq.XDocument xmlDoc)
    {
        string propertyName = $"P:{property.DeclaringType.FullName}.{property.Name}";// Fully qualified property name

        var summary = xmlDoc.Descendants("member")
                 .Where(m => (string)m.Attribute("name") == propertyName)
                 .Select(m => m.Element("summary")?.Value.Trim())
                 .FirstOrDefault();

        return summary;
    }
}
