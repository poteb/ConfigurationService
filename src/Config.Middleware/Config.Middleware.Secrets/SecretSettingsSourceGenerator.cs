using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace pote.Config.Middleware.Secrets;

[Generator]
public class SecretSettingsSourceGenerator : ISourceGenerator
{
    private int i = 0;
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            return;

        foreach (var group in receiver.Fields.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            string classSource = ProcessClass(group.Key, group.ToList());
            context.AddSource($"{group.Key.Name}_SecretProperties.g.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    private string ProcessClass(ISymbol classSymbol, List<IFieldSymbol> fields)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var propertyBuilder = new StringBuilder();
        foreach (var field in fields)
        {
            propertyBuilder.AppendLine(CreatePropertyForField(field));
        }

        propertyBuilder.AppendLine();
        propertyBuilder.AppendLine(CreateSecretResolverProperty());

        return $@"using pote.Config.Middleware.Secrets;
using pote.Config.Shared;
namespace {namespaceName}
{{
    public partial class {className} : ISecretSettings
    {{
{propertyBuilder}
    }}
}}";
    }

    private string CreatePropertyForField(IFieldSymbol field)
    {
        var fieldName = field.Name;
        var propertyName = char.ToUpper(fieldName.TrimStart('_')[0]) + fieldName.TrimStart('_').Substring(1);
        if (propertyName == fieldName)
            propertyName = "G_" + propertyName;
        var fieldType = field.Type.ToDisplayString();

        return $@"
        private bool _is{propertyName}Resolved;
        public {fieldType} {propertyName}
        {{
            get 
            {{
                if (!this._is{propertyName}Resolved)
                {{
                    this.{fieldName} = SecretResolver.ResolveSecret(this.{fieldName});
                }}
                this._is{propertyName}Resolved = true;
                return this.{fieldName};
            }}
            set => this.{fieldName} = value;
        }}";
    }

    private string CreateSecretResolverProperty()
    {
        return "        public ISecretResolver SecretResolver { get; set; }";
    }

    class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IFieldSymbol> Fields { get; } = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax &&
                fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol is null) continue;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == "pote.Config.Middleware.Secrets.SecretAttribute"))
                    {
                        Fields.Add(fieldSymbol);
                    }
                }
            }
        }
    }
}