using System;
using System.Collections.Generic;
using NJsonSchema;
using NJsonSchema.Generation;
using System.IO;
//using TemplateModels.CSharp;
using TemplateModels;
using System.Linq;

namespace SchemaGenerator;

/// <summary>
/// This is old, use GenCsService
/// </summary>
public class GenCsProcessor : GenProcessorBase
{
    internal static void Execute()
    {
        TemplateModels.Helper.Language = TemplateModels.TargetLanguage.CSharp;

        var doc = GetDoc(out var docMapper);
        // template
        var templateDir = System.IO.Path.Combine(Generator.templateDir, TemplateModels.Helper.Language.ToString());

        var srcDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(rootDir), "src", $"{TemplateModels.Helper.Language}SDK", "Path");
        Directory.CreateDirectory(srcDir);

        var m = new TemplateModels.CSharp.ProcessorTemplateModel(doc, "MessageProcessor");
        m.CsImports = docMapper.All
            .Select(_ => _.Module.Split(".").First()) // get the package name: honeybee_schema.radiance.modifierset
            .Distinct() //honeybee_schema, dragonfly_schema
            .Where(_ => _ != moduleName) //dragonfly_schema
            .Select(_ => Helper.ToPascalCase(_)).ToList(); //DragonflySchema

        TemplateModels.CSharp.ProcessorTemplateModel.SDKName = _sdkName;
        TemplateModels.CSharp.ProcessorTemplateModel.BuildSDKVersion = _version;

        // generate Service
        var file = GenService(templateDir, m, outputDir);
        // copy to src dir
        var targetSrcTs = System.IO.Path.Combine(srcDir, System.IO.Path.GetFileName(file));
        System.IO.File.Copy(file, targetSrcTs, true);
        Console.WriteLine($"Generated Service is added as {targetSrcTs}");


        // generate processor class
        var classfile = GenProcessorClass(templateDir, m, outputDir);
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

    private static string GenMethodNameEnum(string templateDir, TemplateModels.CSharp.ProcessorTemplateModel model, string outputDir, string fileExt = ".cs")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "MethodName.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"MethodName{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }

    private static string GenProcessorClass(string templateDir, TemplateModels.CSharp.ProcessorTemplateModel model, string outputDir, string fileExt = ".cs")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "MessageProcessor.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.ClassName}{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }


    private static string GenService(string templateDir, TemplateModels.CSharp.ProcessorTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Service.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"Service.cs");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }
}




