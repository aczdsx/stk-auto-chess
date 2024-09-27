using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.EffectCodeFactory
{
    [Generator]
    internal class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            if (receiver.FactoryClassDecl == null)
                return;
            
            if (receiver.AttributeClassDecl == null)
                return;

            if (receiver.EffectCodeBaseClassDecl == null)
                return;

            var compilation = context.Compilation;
            var customAttributeClasses = new List<(INamedTypeSymbol, CustomAttributeInfo)>();

            var model = compilation.GetSemanticModel(receiver.AttributeClassDecl.SyntaxTree);
            var attributeClassSymbol = model.GetDeclaredSymbol(receiver.AttributeClassDecl) as INamedTypeSymbol;
            foreach (var info in receiver.AttributeInfos)
            {
                model = compilation.GetSemanticModel(info.ClassDeclaration.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(info.ClassDeclaration) as INamedTypeSymbol;

                if (classSymbol != null)
                {
                    var attributeData = classSymbol.GetAttributes().FirstOrDefault(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attributeClassSymbol));
                    if (attributeData != null)
                    {
                        var codeIds = attributeData.ConstructorArguments[0].Values.Select(v => (int)v.Value).ToArray();
                        info.CodeIds = codeIds;
                    }

                    customAttributeClasses.Add((classSymbol, info));
                }
            }

            var namespaceDeclaration = receiver.FactoryClassDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var managerClassName = receiver.FactoryClassDecl.Identifier.Text;
            
            var effectCodeBaseClassName = receiver.EffectCodeBaseClassDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(effectCodeBaseClassName))
                effectCodeBaseClassName = receiver.EffectCodeBaseClassDecl.Identifier.Text;
            else 
                effectCodeBaseClassName = $"{effectCodeBaseClassName}.{receiver.EffectCodeBaseClassDecl.Identifier.Text}";
            
            var tt = new T4.TemplateEffectCodeFactory()
            {
                Namespace = namespaceDeclaration?.Name.ToString() ?? string.Empty,
                Name = managerClassName,
                Methods = new List<(int codeId, string effectCodeImpl)>(),
                EffectCodeBaseClassName = effectCodeBaseClassName
            };

            foreach (var (classSymbol, info) in customAttributeClasses)
            {
                var namespaceSymbol = classSymbol.ContainingNamespace;
                var namespaceName = namespaceSymbol?.ToDisplayString() ?? string.Empty;
                if (namespaceName == "<global namespace>")
                    namespaceName = string.Empty;

                if (!string.IsNullOrEmpty(namespaceName))
                {
                    namespaceName += ".";
                }
                var effectCodeClassName = namespaceName + classSymbol.Name;
                tt.Methods.AddRange(info.CodeIds.Select(codeId => (codeId, className: effectCodeClassName)));
            }

            // 생성 코드 cs 이름
            string hintName = string.IsNullOrEmpty(tt.Namespace)
                ? $"{tt.Name}.Generated.cs"
                : $"{tt.Namespace}.{tt.Name}.Generated.cs";

            // 코드 포멧 정렬
            var text = CSharpSyntaxTree.ParseText(tt.TransformText()).GetRoot().NormalizeWhitespace().ToFullString();

            // 코드 생성
            context.AddSource(hintName, text);
        }
    }
}
