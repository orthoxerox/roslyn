// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class UntilKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public UntilKeywordRecommender()
            : base(SyntaxKind.UntilKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;
			
			// from ...
			// ...
			// take u|

			// from ...
			// ...
			// skip u|
			
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
