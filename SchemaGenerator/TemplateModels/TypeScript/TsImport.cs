using System.Collections.Generic;
using System.Linq;
namespace TemplateModels.TypeScript;

public class TsImport
{
    public string Name { get; set; }
    public string From { get; set; }
    public TsImport(string name, string from)
    {
        Name = name;
        From = from;
    }

    public void Check()
    {

        if (string.IsNullOrEmpty(From))
        {
            From = $"./{Name}";
        }
        else if (From.StartsWith(SchemaGenerator.Generator.moduleName ?? "DTO"))
        {
            From = $"./{Name}";
        }
        else if (MODULEMAPPER.TryGetValue(From, out var newFrom))
        {
            From = newFrom;
        }
        else
        {
            // clean From
            From = From.Split('.')?.First().Replace("_", "-");
        }


    }

    private static Dictionary<string, string> MODULEMAPPER = new Dictionary<string, string>
    {
        {"DragonflySchema", "dragonfly-schema" },
        {"HoneybeeSchema", "honeybee-schema" }
    };


}
