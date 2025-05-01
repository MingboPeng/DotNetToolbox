using NSwag;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TemplateModels.Base;

namespace TemplateModels.CSharp;

public class MethodTemplateModel: MethodTemplateModelBase
{
    public PropertyTemplateModel ReturnType { get; set; }
    public List<PropertyTemplateModel> Params { get; set; }
  

    public MethodTemplateModel(string pathName, OpenApiPathItem openApi):base(pathName, openApi)
    {
        var operation = openApi.First().Value;

        var requestBody = operation.RequestBody;
        Params = ParamSchemas
            .Select(_ => new PropertyTemplateModel(_.name, _.schema, _.required, false))?
            .ToList() ?? new List<PropertyTemplateModel>();
        HasParameter = (Params?.Any()).GetValueOrDefault();

        var returnObj = operation.Responses["200"]?.Content?.FirstOrDefault().Value?.Schema; // Successful Response
        ReturnType = new PropertyTemplateModel("", returnObj, false, false);

        if (returnObj != null)
        {
            ReturnTypeName = ReturnType.Type;
        }

        if (string.IsNullOrEmpty(ReturnTypeName) || ReturnTypeName == "None")
        {
            ReturnTypeName = "void";
        }

        HasReturn = ReturnTypeName != "void";

    }


    public MethodTemplateModel(MethodInfo methodInfo, MethodDoc document = default):base(methodInfo, document)
    {
        Params = methodInfo.GetParameters().Select(_ => new PropertyTemplateModel(_, document?.Params?.GetValueOrDefault(_.Name))).ToList();
        HasParameter = (Params?.Any()).GetValueOrDefault();
    }

}
