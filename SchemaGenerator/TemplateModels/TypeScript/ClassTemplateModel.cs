﻿using TemplateModels.Base;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NSwag;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaGenerator;
using System;

namespace TemplateModels.TypeScript;
public class ClassTemplateModel : ClassTemplateModelBase
{

    public List<ClassTemplateModel> DerivedClasses { get; set; } // children classes

    public bool HasProperties => Properties.Any();
    public List<PropertyTemplateModel> Properties { get; set; }
    public List<TsImport> TsImports { get; set; } = new List<TsImport>();
    public bool HasTsImports => TsImports.Any();
    public List<string> TsValidatorImports { get; set; }
    public string TsValidatorImportsCode { get; set; }

    public List<string> TsNestedValidatorImports { get; set; }
    public string TsNestedValidatorImportsCode { get; set; }
    public bool HasTsNestedValidator => !string.IsNullOrEmpty(TsNestedValidatorImportsCode);

    public ClassTemplateModel(OpenApiDocument doc, JsonSchema json, Mapper mapper = default) : base(json)
    {
        mapper = mapper ?? new Mapper(null, null);
        Properties = json.ActualProperties.Select(_ => new PropertyTemplateModel(_.Key, _.Value)).ToList();

        DerivedClasses = json.GetDerivedSchemas(doc).Select(_ => new ClassTemplateModel(doc, _.Key, mapper)).ToList();


        IsAbstract = DerivedClasses.Any() && InheritedSchema == null;

        TsImports = Properties.SelectMany(_ => _.TsImports).Select(_ => new TsImport(_.Name, from: mapper.TryGetModule(_.From ?? _.Name))).ToList();

        // add base class reference
        if (!string.IsNullOrEmpty(Inheritance))
            TsImports.Add(new TsImport(Inheritance, from: mapper.TryGetModule(Inheritance)));

        // remove importing self
        var tsImports = TsImports.Where(_ => _.Name != ClassName);
        // remove duplicates
        TsImports = tsImports.GroupBy(_ => _.Name).Select(_ => _.First()).OrderBy(_ => _.Name).ToList();

        // fix TsImports
        TsImports.ForEach(_ => _.Check());

        var paramValidators = Properties.SelectMany(_ => _.ValidationDecorators).Select(_ => ValidationDecoratorToImport(_)).ToList();
        paramValidators.Add("validate");
        paramValidators.Add("ValidationError as TsValidationError");
        TsValidatorImports = paramValidators.Distinct().ToList();
        TsValidatorImports = TsValidatorImports.Where(_=>_ != "Type").ToList();
        var nestedValidators = TsValidatorImports.Where(x => x.StartsWith("IsNested")).ToList();
        if (nestedValidators.Any())
        {
            TsValidatorImports = TsValidatorImports.Where(_ => !_.StartsWith("IsNested")).ToList();
            TsNestedValidatorImports = nestedValidators;
            TsNestedValidatorImportsCode = string.Join(", ", TsNestedValidatorImports);
        }
        TsValidatorImportsCode = string.Join(", ", TsValidatorImports);

    }


    public ClassTemplateModel(Type classType, System.Xml.Linq.XDocument xmlDoc) : base(classType, xmlDoc)
    {
        Properties = classType.GetProperties().Select(_ => new PropertyTemplateModel(_, xmlDoc)).ToList();
        //ClassName = Helper.CleanName(ClassName);

        TsImports = Properties?.SelectMany(_ => _.TsImports)?.Distinct()?.ToList() ?? new List<TsImport>();

        // add base class reference
        if (!string.IsNullOrEmpty(Inheritance))
            TsImports.Add(new TsImport(Inheritance, from: null));

        // remove importing self
        var tsImports = TsImports.Where(_ => _.Name != ClassName).ToList();
        // remove importing System for String/ Double
        tsImports = tsImports.Where(_ => _.From != "System").ToList();

        // remove duplicates
        TsImports = tsImports.GroupBy(_ => _.Name).Select(_ => _.First()).OrderBy(_ => _.Name).ToList();

        // fix TsImports
        TsImports.ForEach(_ => _.Check());

        var paramValidators = Properties.SelectMany(_ => _.ValidationDecorators).Select(_ => ValidationDecoratorToImport(_)).ToList();
        paramValidators.Add("validate");
        paramValidators.Add("ValidationError as TsValidationError");
        TsValidatorImports = paramValidators.Distinct().ToList();
        TsValidatorImports = TsValidatorImports.Where(_ => _ != "Type").ToList();
        var nestedValidators = TsValidatorImports.Where(x => x.StartsWith("IsNested")).ToList();
        if (nestedValidators.Any())
        {
            TsValidatorImports = TsValidatorImports.Where(_ => !_.StartsWith("IsNested")).ToList();
            TsNestedValidatorImports = nestedValidators;
            TsNestedValidatorImportsCode = string.Join(", ", TsNestedValidatorImports);
        }
        TsValidatorImportsCode = string.Join(", ", TsValidatorImports);
    }


    public static string ValidationDecoratorToImport(string decorator)
    {
        string pattern = @"@([a-zA-Z]+)\(";
        Match match = Regex.Match(decorator, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        else
        {
            return decorator;
        }
    }



}