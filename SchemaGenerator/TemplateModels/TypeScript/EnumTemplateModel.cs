
using TemplateModels.Base;
using NJsonSchema;

namespace TemplateModels.TypeScript;
public class EnumTemplateModel: EnumTemplateModelBase
{
    public EnumTemplateModel(JsonSchema json):base(json)
    {
    }
    public EnumTemplateModel(System.Type type, System.Xml.Linq.XDocument xmlDoc) : base(type, xmlDoc)
    {
    }
}


public class EnumItemTemplateModel: EnumItemTemplateModelBase
{
    public EnumItemTemplateModel(int i, string key) : base(i, key) { }

}