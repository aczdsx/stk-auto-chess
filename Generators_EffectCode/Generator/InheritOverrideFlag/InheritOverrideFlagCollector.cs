using System.Collections.Generic;
using System.Linq;
using Generator.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator.InheritOverrideFlag
{
    [Generator]
    internal class InheritOverrideFlagCollector : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EffectCodeStatClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not EffectCodeStatClassSyntaxReceiver receiver)
                return;
            
            if (!receiver.IsValid())
                return;
            
            FileLogging.WriteLog("Start!!");

            // ClassDeclarationSyntax에서 INamedTypeSymbol을 가져옵니다.
            var semanticModel = context.Compilation.GetSemanticModel(receiver.EffectCodeStatBaseClassDeclaration.SyntaxTree);
            var effectCodeBaseSymbol = semanticModel.GetDeclaredSymbol(receiver.EffectCodeStatBaseClassDeclaration) as INamedTypeSymbol;
            semanticModel = context.Compilation.GetSemanticModel(receiver.EnumDeclaration.SyntaxTree);
            var effectCodeInheritFlagSymbol = semanticModel.GetDeclaredSymbol(receiver.EnumDeclaration) as INamedTypeSymbol;
            var enumNamespaceName = effectCodeInheritFlagSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            string fullEnumTypeName;
            FileLogging.WriteLog($"enumnamespace {enumNamespaceName}");
            if (enumNamespaceName == "<global namespace>")
                fullEnumTypeName = effectCodeInheritFlagSymbol.Name;
            else
                fullEnumTypeName = $"{enumNamespaceName}.{effectCodeInheritFlagSymbol.Name}";
            FileLogging.WriteLog($"fullenumtypename {fullEnumTypeName}");
            
            semanticModel = context.Compilation.GetSemanticModel(receiver.AttributeClassDeclaration.SyntaxTree);
            var effectCodeAttributeSymbol = semanticModel.GetDeclaredSymbol(receiver.AttributeClassDeclaration) as INamedTypeSymbol;
            var syntaxTrees = context.Compilation.SyntaxTrees;
            foreach (var syntaxTree in syntaxTrees)
            {
                semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();

                // 클래스 탐색
                foreach (var classDeclaration in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>())
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                    
                    if (classSymbol == null || classSymbol.IsAbstract)
                        continue;
                    
                    // 클래스가 EffectCodeBase를 상속받았는지 확인
                    if (!IsDerivedFromEffectCodeBase(classSymbol, effectCodeBaseSymbol))
                    {
                        continue; // 상속받지 않았다면 다음 클래스로 넘어감
                    }

                    FileLogging.WriteLog($"inherited class: {classSymbol.Name}");

                    var flags = new List<string>();

                    // 메서드 탐색
                    foreach (var method in classDeclaration.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
                    {
                        var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;

                        if (!(methodSymbol?.IsOverride ?? false))
                            continue;

                        var baseMethod = methodSymbol;
                        do
                        {
                            baseMethod = baseMethod.OverriddenMethod;
                        } while (baseMethod.IsOverride);

                        var flagAttribute = baseMethod.GetAttributes().FirstOrDefault(attr =>
                        {
                            var res = SymbolEqualityComparer.Default.Equals(attr.AttributeClass, effectCodeAttributeSymbol);
                            FileLogging.WriteLog($"{attr.AttributeClass?.Name} {effectCodeAttributeSymbol?.Name} {res}");
                            return res;
                        });

                        if (flagAttribute == null)
                            continue;
                        // 어트리뷰트의 첫 번째 생성자 인자 (EffectCodeInheritFlag 값을 얻음)
                        var flagValue = flagAttribute.ConstructorArguments.FirstOrDefault().Value;
                        if (flagValue == null)
                            continue;
                        if (flagAttribute.AttributeConstructor == null)
                            continue;
                        var enumType = flagAttribute.AttributeConstructor.Parameters[0].Type;

                        if (enumType.TypeKind != TypeKind.Enum)
                            continue;
                        // 열거형의 각 값과 숫자 비교하여 문자열로 변환
                        var enumMembers = enumType.GetMembers().OfType<IFieldSymbol>();
                        var matchingMember = enumMembers.FirstOrDefault(member =>
                            member.HasConstantValue && Equals(member.ConstantValue, flagValue));

                        if (matchingMember == null)
                            continue;
                        
                        FileLogging.WriteLog($"flag name {effectCodeInheritFlagSymbol.Name} {matchingMember.Name}");
                        flags.Add(matchingMember.Name);
                    }
                    
                    if (flags.Count == 0)
                        continue;
                    
                    var namespaceSymbol = classSymbol.ContainingNamespace;
                    var namespaceName = namespaceSymbol?.ToDisplayString() ?? string.Empty;
                    if (namespaceName == "<global namespace>")
                        namespaceName = string.Empty;
                    
                    var tt = new T4.TemplateInheritOverrideFlagCollector()
                    {
                        Namespace = namespaceName,
                        Name = classDeclaration.Identifier.ToString(),
                        EnumFlagName = fullEnumTypeName,
                        Flags = flags
                    };

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
            
            FileLogging.CloseFile();
        }

        // 특정 클래스가 EffectCodeBase를 상속받았는지 재귀적으로 확인하는 함수
        private bool IsDerivedFromEffectCodeBase(INamedTypeSymbol classSymbol, INamedTypeSymbol effectCodeBaseSymbol)
        {
            var baseType = classSymbol.BaseType;

            while (baseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, effectCodeBaseSymbol))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }
        
        // 모든 네임스페이스에서 클래스를 검색하는 함수
        static INamedTypeSymbol FindClassInAllNamespaces(INamespaceSymbol namespaceSymbol, string className)
        {
            FileLogging.WriteLog($"check: {namespaceSymbol}");
            // 현재 네임스페이스 내에서 클래스 검색
            var classSymbols = namespaceSymbol.GetTypeMembers(className);
            if (classSymbols.Any())
            {
                FileLogging.WriteLog("Find Class!!");
                FileLogging.WriteLog($"{classSymbols.First().Name}");
                return classSymbols.First();
            }

            // 하위 네임스페이스를 재귀적으로 검색
            foreach (var subNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                var result = FindClassInAllNamespaces(subNamespace, className);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
