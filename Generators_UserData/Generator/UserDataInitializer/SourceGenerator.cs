using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Generator.Util;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator.UserDataInitializer
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            FileLogging.WriteLog($"start! {context.Compilation.AssemblyName}");
            try
            {
                if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                {
                    FileLogging.WriteLog($"receiver not exist {context.Compilation.AssemblyName}");
                    return;
                }

                var source = receiver.Source;
                if (source.UserDataManager == null)
                {
                    FileLogging.WriteLog($"UserDataManager is null {context.Compilation.AssemblyName}");
                    return;
                }
                
                source.InitializeAttributes.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                source.InitializeEffectCodeAttributes.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                source.InitializeOwnContentsAttributes.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                // source.InitializeBadgeAttributes.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                
                // foreach (var ia in source.InitializeAttributes)
                // {
                //     FileLogging.WriteLog($"InitializeAttribute: {ia.MethodName}, {ia.Priority}, {ia.Category}, {ia.IsAsync}");
                // }
                // foreach (var ia in source.InitializeEffectCodeAttributes)
                // {
                //     FileLogging.WriteLog($"InitializeEffectCodeAttribute: {ia.MethodName}, {ia.Priority}, {ia.CustomCategory}, {ia.IsAsync}");
                // }
                // foreach (var ia in source.InitializeOwnContentsAttributes)
                // {
                //     FileLogging.WriteLog($"InitializeOwnContentsAttribute: {ia.MethodName}, {ia.Priority}, {ia.CustomCategory}, {ia.IsAsync}");
                // }
                // foreach (var ia in source.InitializeBadgeAttributes)
                // {
                //     FileLogging.WriteLog($"InitializeBadgeAttribute: {ia.MethodName}, {ia.Priority}, {ia.CustomCategory}, {ia.IsAsync}");
                // }

                if (source.InitializeAttributes.Count == 0 &&
                    source.InitializeOwnContentsAttributes.Count == 0 &&
                    source.InitializeEffectCodeAttributes.Count == 0)
                {
                    FileLogging.WriteLog("No InitializeAttribute found. Skip generating UserDataInitializer.");
                    return;
                }
                
                // Cysharp.Threading.Tasks 네임스페이스에 UniTask가 있는지 확인
                var hasUniTask = context.Compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask") != null;
                var model = context.Compilation.GetSemanticModel(source.UserDataManager.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(source.UserDataManager);
                var namespaceSymbol = classSymbol.ContainingNamespace;
                var namespaceName = namespaceSymbol?.ToDisplayString() ?? string.Empty;
                if (namespaceName == "<global namespace>")
                    namespaceName = string.Empty;
                
                model = context.Compilation.GetSemanticModel(source.DataCategoryEnum.SyntaxTree);
                var enumSymbol = model.GetDeclaredSymbol(source.DataCategoryEnum);
                var enumName = enumSymbol.Name;
                if (enumSymbol.ContainingNamespace.ToDisplayString() != "<global namespace>")
                {
                    enumName = $"{enumSymbol.ContainingNamespace.ToDisplayString()}.{enumName}";
                }

                var tt = new T4.TemplateUserDataInitializer()
                {
                    Source = source,
                    HasUniTask = hasUniTask,
                    Namespace = namespaceName,
                    Name = classSymbol.Name,
                    DataCategoryEnumNamespace = enumSymbol.ContainingNamespace.ToDisplayString() != "<global namespace>" ? enumSymbol.ContainingNamespace.ToDisplayString() : string.Empty,
                    DataCategoryEnumName = enumName
                };
                
                // 생성 코드 cs 이름
                string hintName = string.IsNullOrEmpty(tt.Namespace)
                    ? $"{tt.Name}.Generated.cs"
                    : $"{tt.Namespace}.{tt.Name}.Generated.cs";

                string text = CSharpSyntaxTree.ParseText(tt.TransformText()).GetRoot().NormalizeWhitespace().ToFullString();
                FileLogging.WriteLog(text);
                context.AddSource(hintName, SourceText.From(text, Encoding.UTF8));
                // FileLogging.CloseFile();

            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("GEN001", "Exception", e.Message, "SourceGenerator", DiagnosticSeverity.Error, true),
                    Location.None));
            }
        }
    }
}