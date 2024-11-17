using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        return $@"using pote.Config.Shared.Secrets;
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

    public class SyntaxReceiver : ISyntaxContextReceiver
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
                    var attributes = fieldSymbol.GetAttributes();
                    foreach (var attribute in attributes)
                    {
                        var attributeClass = attribute.AttributeClass;
                        if (attributeClass is null) continue;
                        // var containingNamespace = attributeClass.ContainingNamespace;
                        // var namespaceName = containingNamespace.ToDisplayString();
                        // var fullyQualifiedName = attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        // var d = attributeClass.ToString();
                        // var d2 = attributeClass.Name;
                        var displayString = attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (displayString == "pote.Config.Shared.Secrets.SecretAttribute" || displayString == "global::pote.Config.Shared.Secrets.SecretAttribute" || displayString == "Secret")
                        {
                            Fields.Add(fieldSymbol);
                        }
                    }
                    // if (attributes.Any(ad => ad.AttributeClass?.ToDisplayString() == "pote.Config.Shared.Secrets.SecretAttribute"))
                    // {
                    //     Fields.Add(fieldSymbol);
                    // }
                }
            }
        }
    }
}