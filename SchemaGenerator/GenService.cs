using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.ComponentModel.DataAnnotations;


namespace SchemaGenerator;

public class GenService : GenProcessorBase
{
    internal static void Execute()
    {
        // schema source dir
        var sourceDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(rootDir), "ServiceSource");
        // load service interface from cs file
        LoadServiceSource(sourceDir, out var services, out var dtos);

        // generate CS services and dtos 
        GenCsService.Execute(services, dtos, _sourceDoc);

        // generate TS services and dtos
        GenTsService.Execute(services, dtos, _sourceDoc);

    }

    private static System.Xml.Linq.XDocument _sourceDoc;

    private static void LoadServiceSource(string sourceDir, out List<Type> services, out List<Type> dtos)
    {
        var types = LoadSourceCode(sourceDir);
        services = types.Where(_ => _.Namespace == "Service").ToList();
        dtos = types.Where(_ => _.Namespace == "DTO").ToList();
    }

    private static List<Type> LoadSourceCode(string sourceDir)
    {
        Console.WriteLine("Loading/compiling source code from:", sourceDir);
        var csFiles = System.IO.Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
        var syntaxTrees = csFiles.Select(_ => CSharpSyntaxTree.ParseText(File.ReadAllText(_))).ToList();

        var references = new[]
        {
             MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.dll
             MetadataReference.CreateFromFile(typeof(RequiredAttribute).Assembly.Location), // System.ComponentModel.DataAnnotations.dll
             MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location), // System.Runtime.dll
             MetadataReference.CreateFromFile( System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")) // System.Runtime.dll
        };


        var compilation = CSharpCompilation.Create("DynamicAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTrees);

        using var ms = new MemoryStream();
        using var xmlStream = new MemoryStream(); // XML documentation output

        EmitResult result = compilation.Emit(ms, xmlDocumentationStream: xmlStream);

        if (!result.Success)
        {
            foreach (var err in result.Diagnostics)
            {
                Console.WriteLine(err);
            }

            throw new Exception("LoadServiceSource: Compilation failed.");
        }

        ms.Seek(0, SeekOrigin.Begin);
        Assembly assembly = Assembly.Load(ms.ToArray());
        var types = assembly.GetTypes().ToList();


        // get doc
        xmlStream.Seek(0, SeekOrigin.Begin);
        string xmlDocumentation = new StreamReader(xmlStream).ReadToEnd();

        var xmlDocFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sourceDoc.xml");
        if (System.IO.File.Exists(xmlDocFile))
            System.IO.File.Delete(xmlDocFile);
        File.WriteAllText(xmlDocFile, xmlDocumentation);


        var xmlDoc = System.Xml.Linq.XDocument.Load(xmlDocFile);
        _sourceDoc = xmlDoc;

        Console.WriteLine("Compilation completed!");
        return types;

    }

}
