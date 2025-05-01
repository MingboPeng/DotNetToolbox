using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateModels.Base;

public class EnumTemplateModelBase
{
    public string Description { get; set; }
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public string EnumName { get; set; }
    public List<EnumItemTemplateModelBase> EnumItems { get; set; }
    public EnumTemplateModelBase(JsonSchema json)
    {
        Description = json.Description?.Replace("\n", "\\n");
        EnumName = json.Title;
        EnumItems = json.Enumeration.Select(_ => _.ToString()).Select((_, i) => new EnumItemTemplateModelBase(i + 1, _)).ToList();


        //ClimateZone 
        if (EnumName == "ClimateZones")
            EnumItems.ForEach(_ => _.Key = $"ASHRAE_{_.Value}");

    }

    public EnumTemplateModelBase(System.Type type, System.Xml.Linq.XDocument xmlDoc)
    {
        //Description = json.Description?.Replace("\n", "\\n");
        EnumName = type.Name;
        EnumItems = Enum.GetNames(type).Select((_, i) => new EnumItemTemplateModelBase(i + 1, _)).ToList();

        string xmlEnumName = $"T:{type.FullName}"; // Fully qualified enum name
        Description = xmlDoc.Descendants("member")
                 .Where(m => (string)m.Attribute("name") == xmlEnumName)
                 .Select(m => m.Element("summary")?.Value.Trim())
                 .FirstOrDefault();

    }
}

public class EnumItemTemplateModelBase
{
    public int Index { get; set; }
    public string Value { get; set; }
    public string Key { get; set; }
    public EnumItemTemplateModelBase(int i, string key)
    {
        Index = i;
        Value = key;
        Key = Helper.CleanName(Helper.ToPascalCase(key, true), true);
    }


}
