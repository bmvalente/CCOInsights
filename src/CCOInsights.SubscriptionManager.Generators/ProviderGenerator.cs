﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CCOInsights.SubscriptionManager.Generators;

[Generator]
public class ProviderGenerator : ISourceGenerator
{

    public void Execute(GeneratorExecutionContext context)
    {

        var syntaxTrees = context.Compilation.SyntaxTrees;
        foreach (var syntaxTree in syntaxTrees)
        {
            var autoGenerateDeclarations = syntaxTree
                .GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(x => x.AttributeLists.Any(x => x.ToString().StartsWith("[GeneratedProvider"))).ToList();

            foreach (var autoGeneratedDeclaration in autoGenerateDeclarations)
            {

                var sourceBuilder = new StringBuilder("");
                var paramsToInject = TemplateParameters.Create(
                    autoGeneratedDeclaration.Parent.GetType().GetProperty("Name").GetValue(autoGeneratedDeclaration.Parent).ToString(),
                    autoGeneratedDeclaration.Identifier.ToString().Replace("Response", ""),
                    autoGeneratedDeclaration.Identifier.ToString(),
                    autoGeneratedDeclaration.AttributeLists[0].Attributes[0].ArgumentList.Arguments[0].ToString()
                );

                var template = getSourceCode();
                var sourceGenerated = replaceParametersInTemplate(template, paramsToInject);
                context.AddSource($"ProviderGenerated_{paramsToInject.GeneratedClassName}.g.cs", SourceText.From(sourceGenerated, Encoding.UTF8));

            }
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        //Uncomment this for attach a debugger in Compilation time and debug the source generator
        //#if DEBUG
        //            if (!Debugger.IsAttached)
        //                Debugger.Launch();

        //#endif
    }

    private string replaceParametersInTemplate(string template, TemplateParameters parameters)
    {
        return template
                .Replace("{{" + nameof(parameters.GeneratedNamespace) + "}}", parameters.GeneratedNamespace)
                .Replace("{{" + nameof(parameters.GeneratedClassName) + "}}", parameters.GeneratedClassName)
                .Replace("{{" + nameof(parameters.GeneratedResponseName) + "}}", parameters.GeneratedResponseName)
                .Replace("{{" + nameof(parameters.Path) + "}}", parameters.Path)
            ;
    }

    private string getSourceCode()
    {
        string path = "CCOInsights.SubscriptionManager.Generators.Templates.CodeGenerated.txt";
        using var stream = GetType().Assembly.GetManifestResourceStream(path);

        using var streamReader = new StreamReader(stream);

        return streamReader.ReadToEnd();
    }
}