// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts an AwaitExpressionSyntax into a BoundExpression
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindLiftExpression(LiftExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var operand = BindExpression(node.Operand, diagnostics);
            var innerLiftExpression = operand as BoundLiftExpression;

            if (innerLiftExpression != null)
            {
                //e.g., x!!
                //We have to create a higher-rank LiftExpression
                return new BoundLiftExpression(node, innerLiftExpression.Operand, innerLiftExpression.Rank + 1, workwitharray);
            } else {
                var innerType = operand.Type as NamedTypeSymbol; //!!!arrays

                if (innerType != null) {
                    var innerMembers = innerType.GetMembersUnordered();
                    TypeSymbol outerType = null;
                    if (HasSelectAndZip(innerMembers, out outerType))
                    {
                        ///!!!
                    }
                }

                return new BoundLiftExpression(node, operand, 1, );

            }


        }

        private static bool HasSelectAndZip(ImmutableArray<Symbol> innerMembers, out TypeSymbol outerType)
        {
            throw new NotImplementedException();
        }
    }
}
