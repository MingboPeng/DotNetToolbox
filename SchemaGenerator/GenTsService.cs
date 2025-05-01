using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TemplateModels.TypeScript;

namespace SchemaGenerator;

public class GenTsService : GenProcessorBase
{
    internal static void Execute(List<Type> services, List<Type> dtos, System.Xml.Linq.XDocument doc)
    {
        TemplateModels.Helper.Language = TemplateModels.TargetLanguage.TypeScript;

        TemplateModels.TypeScript.ServiceTemplateModel.SDKName = _sdkName;
        TemplateModels.TypeScript.ServiceTemplateModel.BuildSDKVersion = _version;

        // template
        var templateDir = System.IO.Path.Combine(Generator.templateDir, TemplateModels.Helper.Language.ToString());

        var srcDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(rootDir), "src", $"{TemplateModels.Helper.Language}SDK", "src");
        var srcModelDir = System.IO.Path.Combine(srcDir, "model");
        var srcServiceDir = System.IO.Path.Combine(srcDir, "service");

        if (Directory.Exists(srcModelDir))
            Directory.Delete(srcModelDir, true);
        Directory.CreateDirectory(srcModelDir);

        if (Directory.Exists(srcServiceDir))
            Directory.Delete(srcServiceDir, true);
        Directory.CreateDirectory(srcServiceDir);

        foreach (var service in services)
        {
            var m = new TemplateModels.TypeScript.ServiceTemplateModel(service, doc);
            // generate service class
            var classfile = GenService(templateDir, m, outputDir);
            // copy to src dir
            var targetSrcClass = System.IO.Path.Combine(srcServiceDir, System.IO.Path.GetFileName(classfile));
            System.IO.File.Copy(classfile, targetSrcClass, true);
            Console.WriteLine($"Generated {m.ClassName} is added as {targetSrcClass}");

            // generate MethodName Enum
            var enumfile = GenMethodNameEnum(templateDir, m, outputDir);
            // copy to src dir
            var targetSrcEnum = System.IO.Path.Combine(srcServiceDir, System.IO.Path.GetFileName(enumfile));
            System.IO.File.Copy(enumfile, targetSrcEnum, true);
            Console.WriteLine($"Generated MethodName Enum is added as {targetSrcEnum}");
        }

        // gen TypeScript index.ts for service
        GenIndexFromFolder(templateDir, srcServiceDir);

        // generate DTO models
        foreach (var dto in dtos)
        {
            if (dto.IsEnum)
            {
                var m = new TemplateModels.TypeScript.EnumTemplateModel(dto, doc);
                var f = GenDTOEnum(templateDir, m, outputDir);
                var targetSrcModel = System.IO.Path.Combine(srcModelDir, System.IO.Path.GetFileName(f));
                System.IO.File.Copy(f, targetSrcModel, true);
                Console.WriteLine($"Generated Service Enum is added as {targetSrcModel}");

            }
            else
            {
                var m = new TemplateModels.TypeScript.ClassTemplateModel(dto, doc);
                var f = GenDTOModels(templateDir, m, outputDir);
                var targetSrcModel = System.IO.Path.Combine(srcModelDir, System.IO.Path.GetFileName(f));
                System.IO.File.Copy(f, targetSrcModel, true);
                Console.WriteLine($"Generated Service Model is added as {targetSrcModel}");
            }
        }

        //// gen TypeScript index.ts for DTO models
        GenIndexFromFolder(templateDir, srcModelDir);

    }


    private static string GenMethodNameEnum(string templateDir, TemplateModels.TypeScript.ServiceTemplateModel model, string outputDir, string fileExt = ".ts")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "MethodName.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.ClassName}Method{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }


    private static string GenService(string templateDir, TemplateModels.TypeScript.ServiceTemplateModel model, string outputDir, string fileExt = ".ts")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Service.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.ClassName}{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }

    private static string GenDTOModels(string templateDir, TemplateModels.TypeScript.ClassTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Class2.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.ClassName}.ts");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;

    }

    private static string GenDTOEnum(string templateDir, TemplateModels.TypeScript.EnumTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Enum.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.EnumName}.ts");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }

    private static string GenIndexFromFolder(string templateDir, string folder) 
    {
        // gen TypeScript index.ts for DTO models
        var indexTsPath = System.IO.Path.Combine(folder, "index.ts");
        var tsFiles = System.IO.Directory.GetFiles(folder, "*.ts");
        var names = tsFiles.Select(_ => System.IO.Path.GetFileNameWithoutExtension(_)).OrderBy(_ => _).ToList();
        var indexModel = new IndexTemplateModel();
        indexModel.Files = names;
        return GenIndex(templateDir, indexModel, folder);

    }
    private static string GenIndex(string templateDir, IndexTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Index.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"index.ts");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }
}




