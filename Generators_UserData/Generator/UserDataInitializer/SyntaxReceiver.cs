using System.Collections.Generic;
using System.IO;
using System.Linq;
using Generator.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.UserDataInitializer
{    public class InitializeAttributeInfo
    {
        public string MethodName { get; set; }
        public int Priority { get; set; }
        public string Category { get; set; }
        public bool IsAsync { get; set; }
    }

    public class InitializeEffectCodeAttributeInfo
    {
        public string MethodName { get; set; }
        public int Priority { get; set; }
    }

    public class InitializeOwnContentsAttributeInfo
    {
        public string MethodName { get; set; }
        public int Priority { get; set; }
    }

    public class Source
    {
        public ClassDeclarationSyntax UserDataManager { get; set; }
        public EnumDeclarationSyntax DataCategoryEnum { get; set; }
        public List<InitializeAttributeInfo> InitializeAttributes { get; } = new();
        public List<InitializeEffectCodeAttributeInfo> InitializeEffectCodeAttributes { get; } = new();
        public List<InitializeOwnContentsAttributeInfo> InitializeOwnContentsAttributes { get; } = new();
        // public List<InitializeBadgeAttributeInfo> InitializeBadgeAttributes { get; } = new();

    }
    
    public class SyntaxReceiver : ISyntaxReceiver
    {
        private string UserDataManagerAttributeName = "GenerateUserDataInitializer";
        private string InitializeAttributeName = "Initialize";
        private string InitializeEffectCodeAttributeName = "InitializeEffectCode";
        private string InitializeOwnContentsAttributeName = "InitializeOwnContents";

        private Source source = new();
        public Source Source => source;
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            FindUserDataManager(syntaxNode);
            FindEnumDataCategory(syntaxNode);
            
            // We're only interested in method declarations
            if (syntaxNode is not MethodDeclarationSyntax methodDecl)
                return;

            // Traverse the attributes of the method
            foreach (var attributeList in methodDecl.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();

                    // Check if it's one of the known attributes
                    if (attributeName == InitializeAttributeName)
                    {
                        AddInitializeAttribute(methodDecl, attribute);
                    }
                    else if (attributeName == InitializeEffectCodeAttributeName)
                    {
                        AddInitializeEffectCodeAttribute(methodDecl, attribute);
                    }
                    else if (attributeName == InitializeOwnContentsAttributeName)
                    {
                        AddInitializeOwnContentsAttribute(methodDecl, attribute);
                    }
                }
            }
        }
        
        private void FindUserDataManager(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classDecl)
                return;
            
            if (source.UserDataManager != null)
                return;

            foreach (var attributeList in classDecl.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString() == UserDataManagerAttributeName)
                    {
                        source.UserDataManager = classDecl;
                        break;
                    }
                }
            }
        }
        
        private void FindEnumDataCategory(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not EnumDeclarationSyntax enumDecl)
                return;
            
            if (source.DataCategoryEnum != null)
                return;

            if (enumDecl.Identifier.Text == "DataCategory")
            {
                source.DataCategoryEnum = enumDecl;
            }
        }

        private void AddInitializeAttribute(MethodDeclarationSyntax methodDecl, AttributeSyntax attribute)
        {
            var attributeArgs = attribute.ArgumentList?.Arguments.Select(x => x.GetText().ToString()).ToArray();
            var info = new InitializeAttributeInfo
            {
                MethodName = methodDecl.Identifier.Text,
                Category = attributeArgs?.Length > 0 ? attributeArgs[0] : string.Empty,
                Priority = attributeArgs?.Length > 1 ? int.Parse(attributeArgs[1]) : 0,
                IsAsync = methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword)
            };

            FileLogging.WriteLog($"InitializeAttribute: {info.MethodName}, {info.Priority}, {info.Category}, {info.IsAsync}");
            source.InitializeAttributes.Add(info);
        }

        private void AddInitializeEffectCodeAttribute(MethodDeclarationSyntax methodDecl, AttributeSyntax attribute)
        {
            var attributeArgs = attribute.ArgumentList?.Arguments.Select(x => x.GetText().ToString()).ToArray();
            var info = new InitializeEffectCodeAttributeInfo
            {
                MethodName = methodDecl.Identifier.Text,
                Priority = attributeArgs?.Length > 0 ? int.Parse(attributeArgs[0]) : 0
            };

            FileLogging.WriteLog($"InitializeEffectCodeAttribute: {info.MethodName}, {info.Priority}");
            source.InitializeEffectCodeAttributes.Add(info);
        }

        private void AddInitializeOwnContentsAttribute(MethodDeclarationSyntax methodDecl, AttributeSyntax attribute)
        {
            var attributeArgs = attribute.ArgumentList?.Arguments.Select(x => x.GetText().ToString()).ToArray();
            var info = new InitializeOwnContentsAttributeInfo
            {
                MethodName = methodDecl.Identifier.Text,
                Priority = attributeArgs?.Length > 0 ? int.Parse(attributeArgs[0]) : 0
            };

            FileLogging.WriteLog($"InitializeOwnContentsAttribute: {info.MethodName}, {info.Priority}");
            source.InitializeOwnContentsAttributes.Add(info);
        }

        // private void AddInitializeBadgeAttribute(MethodDeclarationSyntax methodDecl, AttributeSyntax attribute)
        // {
        //     var attributeArgs = attribute.ArgumentList.Arguments.Select(x => x.GetText().ToString()).ToArray();
        //     var info = new InitializeBadgeAttributeInfo
        //     {
        //         MethodName = methodDecl.Identifier.Text,
        //         CustomCategory = attributeArgs.Length > 0 ? attributeArgs[0] : string.Empty,
        //         Priority = attributeArgs.Length > 1 ? int.Parse(attributeArgs[1]) : 0
        //     };
        //
        //     // FileLogging.WriteLog($"InitializeBadgeAttribute: {info.MethodName}, {info.Priority}, {info.CustomCategory}, {info.IsAsync}");
        //     source.InitializeBadgeAttributes.Add(info);
        // }
    }
}
