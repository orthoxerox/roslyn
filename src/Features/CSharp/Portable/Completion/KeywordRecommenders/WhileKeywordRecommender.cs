// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class WhileKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public WhileKeywordRecommender()
            : base(SyntaxKind.WhileKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsStatementContext ||
                context.IsGlobalStatementContext)
            {
                return true;
            }

            // do {
            // } |

            // do {
            // } w|

            // Note: the case of
            //   do 
            //     Foo();
            //   |
            // is taken care of in the IsStatementContext case.

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.CloseBraceToken &&
                token.Parent.IsKind(SyntaxKind.Block) &&
                token.Parent.IsParentKind(SyntaxKind.DoStatement))
            {
                return true;
            }
			
			// from ...
			// ...
			// take w|

			// from ...
			// ...
			// skip w|
			
			if (token.Kind() == SyntaxKind.TakeKeyword || token.Kind() == SyntaxKind.SkipKeyword)
			{
                var takeOrSkipClause = token.Parent.FirstAncestorOrSelf<TakeOrSkipClauseSyntax>();
                if (takeOrSkipClause != null)
                {
                    if (token == takeOrSkipClause.TakeOrSkipKeyword)
                    {
                        return true;
                    }
                }
			}

            return false;
        }
    }
}
