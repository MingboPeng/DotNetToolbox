using System;
using System.IO;
//using TemplateModels.CSharp;

namespace SchemaGenerator;

/// <summary>
/// This is old, use GenTsService
/// </summary>
public class GenTsProcessor : GenProcessorBase
{
    internal static void Execute()
    {
        TemplateModels.Helper.Language = TemplateModels.TargetLanguage.TypeScript;

        var doc = GetDoc(out var mapper);
        // template
        var templateDir = System.IO.Path.Combine(Generator.templateDir, TemplateModels.Helper.Language.ToString());

        //var classModels = new List<TemplateModels.TypeScript.ClassTemplateModel>();
        var srcDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(rootDir), "src", $"{TemplateModels.Helper.Language}SDK", "src");
        Directory.CreateDirectory(srcDir);

        var m = new TemplateModels.TypeScript.ProcessorTemplateModel(doc, "MessageProcessor", mapper);

        TemplateModels.TypeScript.ProcessorTemplateModel.SDKName = _sdkName;
        TemplateModels.TypeScript.ProcessorTemplateModel.BuildSDKVersion = _version;

        // generate service class
        var classfile = GenService(templateDir, m, outputDir);
        // copy to src dir
        var targetSrcClass = System.IO.Path.Combine(srcDir, System.IO.Path.GetFileName(classfile));
        System.IO.File.Copy(classfile, targetSrcClass, true);
        Console.WriteLine($"Generated {m.ClassName} is added as {targetSrcClass}");

        // generate MethodName Enum
        var enumfile = GenMethodNameEnum(templateDir, m, outputDir);
        // copy to src dir
        var targetSrcEnum = System.IO.Path.Combine(srcDir, System.IO.Path.GetFileName(enumfile));
        System.IO.File.Copy(enumfile, targetSrcEnum, true);
        Console.WriteLine($"Generated MethodName Enum is added as {targetSrcEnum}");
    }


    private static string GenMethodNameEnum(string templateDir, TemplateModels.TypeScript.ProcessorTemplateModel model, string outputDir, string fileExt = ".ts")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "MethodName.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"MethodName{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }


    private static string GenService(string templateDir, TemplateModels.TypeScript.ProcessorTemplateModel model, string outputDir, string fileExt = ".ts")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Service.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"Service{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }
}




