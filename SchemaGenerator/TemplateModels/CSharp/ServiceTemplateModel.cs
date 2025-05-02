
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateModels.CSharp;
public class ServiceTemplateModel: TemplateModels.Base.ServiceTemplateModelBase
{
    public static string SDKName { get; set; }
    public string NameSpaceName => SDKName;

    public static string BuildSDKVersion;
    public string SDKVersion => BuildSDKVersion;

    public string InterfaceName => $"I{ClassName}";
    public string ClassName { get; set; }
    public List<MethodTemplateModel> Methods { get; set; }

    public bool HasMethod => Methods.Any();

    public List<string> CsImports { get; set; } = new List<string>(); // other NuGet packages
    public List<string> CsPackages => CsImports;
    public bool HasCsImports => CsPackages.Any();

    public ServiceTemplateModel(Type classType, System.Xml.Linq.XDocument xmlDoc)
    {
        var name = classType.Name;
        ClassName = name.StartsWith("I") ? name.Substring(1) : name;

        Methods = classType.GetMethods().Select(_ => new MethodTemplateModel(_, GetDoc(xmlDoc, _))).ToList();
        // update operation id for matching methods between C# and TS.
        // OperationId for each methods have to be kept same between C# and Ts
        Methods.ForEach(_ => _.OperationId = $"{ClassName}.{_.MethodName}");

    }

}
