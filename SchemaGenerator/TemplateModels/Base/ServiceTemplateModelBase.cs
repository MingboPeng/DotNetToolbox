using System.Linq;
using System.Reflection;
using System.Text;

namespace TemplateModels.Base;

public class ServiceTemplateModelBase
{
    public static MethodDoc GetDoc(System.Xml.Linq.XDocument xmlDoc, MethodInfo methodInfo)
    {
        var methodName = GetXmlDocumentationMemberName(methodInfo);
        var member = xmlDoc.Descendants("member").FirstOrDefault(m => m.Attribute("name").Value.Equals(methodName));

        var doc = new MethodDoc();
        doc.Summary = member?.Element("summary")?.Value.Trim();
        doc.Params = member?.Elements("param").ToDictionary(_ => _.Attribute("name").Value, _ => _.Value);
        doc.Returns = member?.Element("returns")?.Value.Trim();
        return doc;
    }
    public static string GetXmlDocumentationMemberName(MethodInfo methodInfo)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("M:"); // Prefix for methods
        stringBuilder.Append(methodInfo.DeclaringType.FullName); // Full name of the containing type
        stringBuilder.Append(".");
        stringBuilder.Append(methodInfo.Name); // Method name

        var parameters = methodInfo.GetParameters();
        if (parameters.Length > 0)
        {
            stringBuilder.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    stringBuilder.Append(",");

                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsGenericType)
                {
                    // Handle generic types
                    string typeName = parameterType.GetGenericTypeDefinition().FullName;
                    typeName = typeName.Substring(0, typeName.IndexOf('`')); // Remove the generic type parameter count
                    stringBuilder.Append(typeName);
                    stringBuilder.Append("{");
                    var genericArguments = parameterType.GetGenericArguments();
                    for (int j = 0; j < genericArguments.Length; j++)
                    {
                        if (j > 0)
                            stringBuilder.Append(",");
                        stringBuilder.Append(genericArguments[j].FullName);
                    }
                    stringBuilder.Append("}");
                }
                else if (parameterType.IsArray)
                {
                    // Handle array types
                    stringBuilder.Append(parameterType.GetElementType().FullName);
                    stringBuilder.Append("[]");
                }
                else
                {
                    // Non-generic parameter type
                    stringBuilder.Append(parameterType.FullName);
                }
            }
            stringBuilder.Append(")");
        }

        return stringBuilder.ToString();
    }

}
