using Fluid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateModels;
using TemplateModels.CSharp;
using Microsoft.CodeAnalysis;


namespace SchemaGenerator;


public class GenCsService : GenProcessorBase
{

    internal static void Execute(List<Type> services, List<Type> dtos, System.Xml.Linq.XDocument doc)
    {

        Console.WriteLine($"Current working dir: {workingDir}");
        //Console.WriteLine(string.Join(",", args));

        TemplateModels.Helper.Language = TemplateModels.TargetLanguage.CSharp;

        // template
        var templateDir = System.IO.Path.Combine(Generator.templateDir, TemplateModels.Helper.Language.ToString());


        // save to
        var srcSdkDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(rootDir), "src", $"{TemplateModels.Helper.Language}SDK");
        var srcModelDir = System.IO.Path.Combine(srcSdkDir, "Model"); // DTOs
        var srcServiceDir = System.IO.Path.Combine(srcSdkDir, "Service"); // Services, MessageProcessor, MethodName

        if (Directory.Exists(srcModelDir))
            Directory.Delete(srcModelDir, true);
        Directory.CreateDirectory(srcModelDir);

        if (Directory.Exists(srcServiceDir))
            Directory.Delete(srcServiceDir, true);
        Directory.CreateDirectory(srcServiceDir);


        TemplateModels.CSharp.ServiceTemplateModel.SDKName = _sdkName;
        TemplateModels.CSharp.ServiceTemplateModel.BuildSDKVersion = _version;
        TemplateModels.CSharp.ClassTemplateModel.SDKName = _sdkName;
        TemplateModels.CSharp.EnumTemplateModel.SDKName = _sdkName;


        foreach (var service in services)
        {
            // load template model
            Helper.Language = TargetLanguage.CSharp;
            var model = new ServiceTemplateModel(service, doc);


            // generate Service
            var serviceFile = GenService(templateDir, model, outputDir);
            // copy to src dir
            var targetSrcService = System.IO.Path.Combine(srcServiceDir, System.IO.Path.GetFileName(serviceFile));
            System.IO.File.Copy(serviceFile, targetSrcService, true);
            Console.WriteLine($"Generated Service is added as {targetSrcService}");


            // generate MethodName Enum
            var enumfile = GenMethodNameEnum(templateDir, model, outputDir);
            // copy to src dir
            var targetSrcEnum = System.IO.Path.Combine(srcServiceDir, System.IO.Path.GetFileName(enumfile));
            System.IO.File.Copy(enumfile, targetSrcEnum, true);
            Console.WriteLine($"Generated MethodName Enum is added as {targetSrcEnum}");


        }


        // generate DTO models
        foreach (var dto in dtos)
        {
            if (dto.IsEnum)
            {
                var m = new EnumTemplateModel(dto, doc);
                var f = GenDTOEnum(templateDir, m, outputDir);
                var targetSrcModel = System.IO.Path.Combine(srcModelDir, System.IO.Path.GetFileName(f));
                System.IO.File.Copy(f, targetSrcModel, true);
                Console.WriteLine($"Generated Service Enum is added as {targetSrcModel}");

            }
            else
            {
                var m = new ClassTemplateModel(dto, doc);
                var f = GenDTOModels(templateDir, m, outputDir);
                var targetSrcModel = System.IO.Path.Combine(srcModelDir, System.IO.Path.GetFileName(f));
                System.IO.File.Copy(f, targetSrcModel, true);
                Console.WriteLine($"Generated Service Model is added as {targetSrcModel}");
            }
        }

    }

    private static string GenMethodNameEnum(string templateDir, ServiceTemplateModel model, string outputDir, string fileExt = ".cs")
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "MethodName.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.ClassName}Method{fileExt}");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }


    private static string GenService(string templateDir, ServiceTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Service.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.ClassName}.cs");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }

    private static string GenDTOModels(string templateDir, ClassTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "ServiceDTO.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.CsClassName}.cs");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;

    }

    private static string GenDTOEnum(string templateDir, EnumTemplateModel model, string outputDir)
    {
        var templateSource = File.ReadAllText(Path.Combine(templateDir, "Enum.liquid"), System.Text.Encoding.UTF8);
        var code = Gen(templateSource, model);
        var file = System.IO.Path.Combine(outputDir, $"{model.EnumName}.cs");
        System.IO.File.WriteAllText(file, code, System.Text.Encoding.UTF8);
        return file;
    }


}
