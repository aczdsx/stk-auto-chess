using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.EffectCodeFactory
{
    internal class CustomAttributeInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; set; }
        public int[] CodeIds;
    }
    
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        private string effectCodeAttributeName = "UseEffectCodeIds";
        private string effectCodeFactoryAttributeName = "EffectCodeFactory";
        
        public List<CustomAttributeInfo> AttributeInfos { get; } = new();
        public ClassDeclarationSyntax AttributeClassDecl { get; private set; }
        public ClassDeclarationSyntax FactoryClassDecl { get; private set; }
        public ClassDeclarationSyntax EffectCodeBaseClassDecl { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
                return;
            
            var hasAttribute = classDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Any(attr => attr.Name.ToString() == effectCodeAttributeName);

            if (hasAttribute)
            {
                AttributeInfos.Add(new CustomAttributeInfo { ClassDeclaration = classDeclaration });
            }

            if (FactoryClassDecl == null && classDeclaration.AttributeLists.SelectMany(x => x.Attributes).Any(x => x.Name.ToString() == effectCodeFactoryAttributeName))
            {
                FactoryClassDecl = classDeclaration;
            }

            if (AttributeClassDecl == null && classDeclaration.Identifier.Text.StartsWith(effectCodeAttributeName))
            {
                AttributeClassDecl = classDeclaration;
            }
            
            if (EffectCodeBaseClassDecl == null && classDeclaration.Identifier.Text == "EffectCodeBase")
            {
                EffectCodeBaseClassDecl = classDeclaration;
            }
        }
    }
}