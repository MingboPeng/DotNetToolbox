
using TemplateModels.Base;
using NJsonSchema;

namespace TemplateModels.CSharp;
public class EnumTemplateModel: EnumTemplateModelBase
{
    public static string SDKName { get; set; }
    public string NameSpaceName => SDKName;
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