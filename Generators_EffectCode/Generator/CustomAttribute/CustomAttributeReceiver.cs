using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.CustomAttribute
{
    internal class CustomAttributeInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; set; }
        public int[] CodeIds;
    }
    
    
    internal class CustomAttributeReceiver : ISyntaxReceiver
    {
        private string attributeName = "UseEffectCodeIds";
        private string partialClassName = "EffectCodePoolManager";
        
        public List<CustomAttributeInfo> AttributeInfos { get; } = new();
        public ClassDeclarationSyntax SpecificNamedClass { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                var hasAttribute = classDeclaration.AttributeLists
                    .SelectMany(attrList => attrList.Attributes)
                    .Any(attr => attr.Name.ToString() == attributeName);

                if (hasAttribute)
                {
                    AttributeInfos.Add(new CustomAttributeInfo { ClassDeclaration = classDeclaration });
                }

                if (classDeclaration.Identifier.Text == partialClassName)
                {
                    SpecificNamedClass = classDeclaration;
                }
            }
        }
    }
}