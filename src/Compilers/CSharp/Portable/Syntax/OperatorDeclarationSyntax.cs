using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class OperatorDeclarationSyntax
    {
        public OperatorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            SyntaxToken operatorKeyword,
            SyntaxToken operatorToken,
            ParameterListSyntax parameterList,
            BlockSyntax body,
            ArrowExpressionClauseSyntax expressionBody,
            SyntaxToken semicolonToken)
        {
            return Update(
                attributeLists,
                modifiers,
                returnType,
                operatorKeyword,
                operatorToken,
                TypeParameterList,
                parameterList,
                ConstraintClauses,
                body,
                expressionBody,
                semicolonToken);
        }
    }
}
