using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.EffectCodeFactory
{
    [Generator]
    internal class EffectCodeFactoryGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new UseEffectCodeIdsAttributeReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not UseEffectCodeIdsAttributeReceiver receiver)
                return;

            var compilation = context.Compilation;
            var customAttributeClasses = new List<(INamedTypeSymbol, UseEffectCodeIdsAttributeInfo)>();

            foreach (var info in receiver.AttributeInfos)
            {
                var model = compilation.GetSemanticModel(info.ClassDeclaration.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(info.ClassDeclaration) as INamedTypeSymbol;

                if (classSymbol != null)
                {
                    var attributeData = classSymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Name == "UseEffectCodeIdsAttribute");
                    if (attributeData != null)
                    {
                        var codeIds = attributeData.ConstructorArguments[0].Values.Select(v => (int)v.Value).ToArray();
                        info.CodeIds = codeIds;
                    }

                    customAttributeClasses.Add((classSymbol, info));
                }
            }
            var namespaceDeclaration = receiver.SpecificNamedClass.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var managerClassName = receiver.SpecificNamedClass.Identifier.Text;
            var tt = new TemplateClassCollector()
            {
                Namespace = namespaceDeclaration?.Name.ToString() ?? string.Empty,
                Name = managerClassName,
                Methods = new List<(int codeId, string effectCodeImpl)>()
            };

            foreach (var (classSymbol, info) in customAttributeClasses)
            {
                var effectCodeClassName = classSymbol.Name;
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
