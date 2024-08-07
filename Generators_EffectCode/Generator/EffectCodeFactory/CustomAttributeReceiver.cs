using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.EffectCodeFactory
{
    internal class UseEffectCodeIdsAttributeInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; set; }
        public int[] CodeIds;
    }
    
    
    internal class UseEffectCodeIdsAttributeReceiver :  ISyntaxReceiver
    {
        private const string attributeName = "UseEffectCodeIds";
        private const string partialClassName = "EffectCodePoolManager";
        
        public List<UseEffectCodeIdsAttributeInfo> AttributeInfos { get; } = new();
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
                    AttributeInfos.Add(new UseEffectCodeIdsAttributeInfo { ClassDeclaration = classDeclaration });
                }

                if (classDeclaration.Identifier.Text == partialClassName)
                {
                    SpecificNamedClass = classDeclaration;
                }
            }
        }
    }
}
