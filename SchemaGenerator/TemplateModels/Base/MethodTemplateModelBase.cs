using NSwag;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TemplateModels.Base;

public class MethodTemplateModelBase
{
    public string OperationId { get; set; }
    public string MethodName { get; set; }
    public bool HasReturn { get; set; }
    public bool HasParameter { get; set; }

    public string ReturnDoc { get; set; }

    public string Summary { get; set; }
    public string Document { get; set; }

    public string ReturnTypeName { get; set; }
    // void or type name
    //public PropertyTemplateModelBase ReturnType { get; set; }
    //public List<PropertyTemplateModelBase> Params { get; set; }

    protected List<(NJsonSchema.JsonSchema schema, bool required, string name)> ParamSchemas { get; } = new List<(NJsonSchema.JsonSchema schema, bool required, string name)>();
    public MethodTemplateModelBase(string pathName, OpenApiPathItem openApi)
    {
        this.OperationId = openApi?.FirstOrDefault().Value?.OperationId;
        var operationName = string.IsNullOrEmpty(OperationId) ? pathName : OperationId;

        this.MethodName = Helper.CleanMethodName(operationName);
        var operation = openApi.First().Value;
        this.Summary = operation.Summary;
        this.Document = operation.Description;

        // all reference and non-reference type parameters
        if (operation?.ActualParameters != null && operation.ActualParameters.Any())
            ParamSchemas = operation.ActualParameters
                .Select(_ => (_.Schema, _.IsRequired, name: _.Kind == OpenApiParameterKind.Body ? _?.Schema?.Title : _.Name))
                .ToList();


    }


    public MethodTemplateModelBase(MethodInfo methodInfo, MethodDoc document = default)
    {

        var returnParam = methodInfo.ReturnParameter;
        ReturnTypeName = Helper.GetCheckTypeName(returnParam.ParameterType);
        HasReturn = !string.IsNullOrEmpty(ReturnTypeName) && returnParam.ParameterType != typeof(void) && returnParam.ParameterType != typeof(Task);

        MethodName = methodInfo.Name;
       

        Document = document?.Summary;
        ReturnDoc = document?.Returns;

    }

    public override string ToString()
    {
        return this.MethodName;
    }

}
