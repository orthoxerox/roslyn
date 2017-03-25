using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    class PlaceholderReplacementVisitor : CSharpSyntaxRewriter
    {
        private readonly IdentifierNameSyntax node;

        public PlaceholderReplacementVisitor(IdentifierNameSyntax node)
            : base(visitIntoStructuredTrivia: false)
        {
            this.node = node;
        }

        public override SyntaxNode VisitPlaceholderName(PlaceholderNameSyntax node)
        {
            return this.node;
        }
    }

    class PlaceholderLocationVisitor : CSharpSyntaxVisitor<bool>
    {
        public static PlaceholderLocationVisitor Default = new PlaceholderLocationVisitor();

        public override bool VisitPlaceholderName(PlaceholderNameSyntax node)
        {
            return true;
        }

        public override bool DefaultVisit(SyntaxNode node)
        {
            foreach (var childNode in node.ChildNodes())
            {
                if (Visit(childNode)) return true;
            }

            return false;
        }
    }
}
