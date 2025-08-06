using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateModels.Base;

public class ClassTemplateModelBase
{
    public bool IsAbstract { get; set; }
    public string ClassName { get; set; }
    public string Inheritance { get; set; } // parent, base class
    public JsonSchema InheritedSchema { get; set; } // children
    public bool HasInheritance => !string.IsNullOrEmpty(Inheritance); // has parent
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public string Description { get; set; }
    public string Discriminator { get; set; }
    public string BaseDiscriminator { get; set; } // type reference keyword: "type"

    public ClassTemplateModelBase(JsonSchema json)
    {
        Description = json.Description?.Replace("\n", "\\n")?.Replace("\"", "\"\"");
        BaseDiscriminator = json.Discriminator;

        ClassName = json.Title;
        Discriminator = ClassName;
        InheritedSchema = json.InheritedSchema;
        Inheritance = InheritedSchema?.Title;
    }

    public ClassTemplateModelBase(Type classType, System.Xml.Linq.XDocument xmlDoc)
    {
        Description = GetClassDoc(classType, xmlDoc);

        ClassName = classType.Name;
        Discriminator = ClassName;

        if (classType.BaseType != null && classType.BaseType != typeof(object)) {
            Inheritance = classType.BaseType?.Name;
        }
      

        this.IsAbstract = classType.IsAbstract;

    }


    private static string GetClassDoc(Type classType, System.Xml.Linq.XDocument xmlDoc)
    {
        string className = $"T:{classType.FullName}"; // Fully qualified class name

        var summary = xmlDoc.Descendants("member")
                         .Where(m => (string)m.Attribute("name") == className)
                         .Select(m => m.Element("summary")?.Value.Trim())
                         .FirstOrDefault();
        return summary;
    }
}
