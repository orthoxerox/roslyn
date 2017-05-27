// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class SkipKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public SkipKeywordRecommender()
            : base(SyntaxKind.SkipKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;

            // var q = from x in y
            //         |
            if (!token.IntersectsWith(context.Position) &&
                token.IsLastTokenOfQueryClause())
            {
                return true;
            }

            return false;
        }
    }
}
