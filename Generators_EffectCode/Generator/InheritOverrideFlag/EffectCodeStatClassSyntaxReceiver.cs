using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.InheritOverrideFlag
{
    public class EffectCodeStatClassSyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax EffectCodeStatBaseClassDeclaration { get; private set; }
        public EnumDeclarationSyntax EnumDeclaration { get; private set; }
        public ClassDeclarationSyntax AttributeClassDeclaration { get; private set; }
        
        public bool IsValid()
        {
            return EffectCodeStatBaseClassDeclaration != null && EnumDeclaration != null && AttributeClassDeclaration != null;
        }

        // 구문 트리를 순회하며 클래스 선언을 수집합니다.
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // 클래스 선언인 경우 수집
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                if (classDeclaration.Identifier.Text == "EffectCodeStatBase")
                {
                    EffectCodeStatBaseClassDeclaration = classDeclaration;
                    return;
                }
                if (classDeclaration.Identifier.Text == "AssignEffectCodeFlagAttribute")
                {
                    AttributeClassDeclaration = classDeclaration;
                    return;
                }
            }
            
            if (syntaxNode is EnumDeclarationSyntax enumDeclaration)
            {
                if (enumDeclaration.Identifier.Text == "EffectCodeInheritFlag")
                {
                    EnumDeclaration = enumDeclaration;
                    return;
                }
            }
        }
    }
}
