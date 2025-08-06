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
    internal static void Execute(bool genCs, bool genTs)
    {
        // schema source dir
        //var sourceDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(rootDir), "ServiceSource");
        // load service interface from cs file
        LoadServiceSource(servieSourceDir, out var services, out var dtos);

        if (genCs)
        {
            // generate CS services and dtos 
            GenCsService.Execute(services, dtos, _sourceDoc);
        }

        if (genTs)
        {
            // generate TS services and dtos
            GenTsService.Execute(services, dtos, _sourceDoc);
        }

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
        Assembly sourceAssembly = null;
        string xmlDocFilePath = string.Empty;

        // get all package dlls
        var binDir = System.IO.Path.Combine(sourceDir, "bin");
        if (Directory.Exists(binDir))
        {
            var dllFiles = System.IO.Directory.GetFiles(binDir, "*.dll", SearchOption.AllDirectories).ToList();
            var sourceDll = dllFiles?.FirstOrDefault(x => System.IO.Path.GetFileName(x) == "source.dll");
            if (!string.IsNullOrEmpty(sourceDll))
                dllFiles = dllFiles.Where(_ => _ != sourceDll).ToList();
            sourceAssembly = Assembly.LoadFrom(sourceDll);
            foreach (var item in dllFiles)
            {
                Assembly.LoadFrom(item);
            }

            // get xml doc file path
            xmlDocFilePath = System.IO.Path.ChangeExtension(sourceDll, "xml");

        }

        if (sourceAssembly == null)
        {
            Console.WriteLine($"Loading/compiling source code from: {sourceDir}");
            var csFiles = System.IO.Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);


            var syntaxTrees = csFiles.Select(_ => CSharpSyntaxTree.ParseText(File.ReadAllText(_))).ToList();

            var references = new[]
            {
             MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.dll
             MetadataReference.CreateFromFile(typeof(RequiredAttribute).Assembly.Location), // System.ComponentModel.DataAnnotations.dll
             MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location), // System.Runtime.dll
             MetadataReference.CreateFromFile( System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")) // System.Runtime.dll
            }.ToList();
            //references.AddRange(dllFiles.Select(_ => MetadataReference.CreateFromFile(_)));


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
            sourceAssembly = Assembly.Load(ms.ToArray());

            // get doc
            xmlStream.Seek(0, SeekOrigin.Begin);
            string xmlDocumentation = new StreamReader(xmlStream).ReadToEnd();

            var xmlDocFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sourceDoc.xml");
            if (System.IO.File.Exists(xmlDocFile))
                System.IO.File.Delete(xmlDocFile);
            File.WriteAllText(xmlDocFile, xmlDocumentation);
            xmlDocFilePath = xmlDocFile;

            Console.WriteLine("Compilation completed!");
        }
       
        var types = sourceAssembly.GetTypes().ToList();

        var xmlDoc = System.Xml.Linq.XDocument.Load(xmlDocFilePath);
        _sourceDoc = xmlDoc;

        return types;

    }

}
