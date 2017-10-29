// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts a WithExpressionSyntax into a BoundExpression
    /// </summary>
    internal partial class Binder
    {
        internal BoundExpression BindWith(WithExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var boundExpr = BindExpression(node.Expression, diagnostics);

            bool tempNeeded = false;

            node = SimplifyCompoundAssignment(node, diagnostics, ref tempNeeded);


            //second simplification: factoring out common accessor paths in adjacent clauses:
            // e1 with { ... , .a.b.c = e2 , .a.b.d = e3 , ... } is transformed into
            // var tmp1 = e1; tmp1 with { ... , .a.b = tmp1.a.b with { .c = e2 , .d = e3 } , ... }

            node = SplitAccessorPaths(node, diagnostics, ref tempNeeded);

            //at this point our with expressions are very simple and can be bound by trivial methods
            //the only problem is the temp variable that we've introduced, how can we bind it?
            //tentative solution: use a placeholder and replace it during rewriting
            //will need a new binder that will redirect it to the bound expression of the clause
        }

        private WithExpressionSyntax SimplifyCompoundAssignment(WithExpressionSyntax node, DiagnosticBag diagnostics, ref bool tempNeeded)
        {
            //first simplification: removal of compound assignment:
            // e1 with { ... , .a.b.c += e2 , ... } is transformed into
            // var tmp1 = e1; tmp1 with { ... , .a.b.c = tmp1.a.b.c + e2 , ... }

            var wecs = node.WithExpressionClauses;

            for (int i = 0; i < wecs.Count; i++)
            {
                var wec = wecs[i];
                if (wec.OperatorToken.Kind() != SyntaxKind.EqualsToken)
                {
                    tempNeeded = true;
                    var newKind = GetBinaryOperationKindFromAssignmentKind(wec.OperatorToken.Kind());
                    var left = TransformBindingIntoAccessExpression(wec.AccessorPath);
                    var right = wec.ValueExpression;

                    var newExpression = SyntaxFactory.BinaryExpression(newKind, left, right);

                    wec = wec.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.EqualsToken));
                    wec = wec.WithValueExpression(newExpression);

                    wecs = wecs.Replace(wecs[i], wec);
                }
            }

            node = node.WithWithExpressionClauses(wecs);
            return node;
        }

        private WithExpressionSyntax SplitAccessorPaths(WithExpressionSyntax node, DiagnosticBag diagnostics, ref bool tempNeeded)
        {
            //third simplification: splitting accessor paths:
            // e1 with { ... , .a.b = e2 , ... } is transformed into
            // var tmp1 = e1; tmp1 with { ... , .a = tmp1.a with { .b = e2 } , ... }

            var wecs = node.WithExpressionClauses;

            for (int i = 0; i < wecs.Count; i++)
            {
                var wec = wecs[i];
                var ap = wec.AccessorPath;

                if (ap.Count > 1)
                {
                    tempNeeded = true;

                    var newHeadAp = new SyntaxList<ExpressionSyntax>(ap[0]);
                    var innerExpr = TransformBindingIntoAccessExpression(newHeadAp);
                    var newTailAp = ap.RemoveAt(0);

                    var innerWec = SyntaxFactory.WithExpressionClause(
                        newTailAp, 
                        SyntaxFactory.Token(SyntaxKind.EqualsToken), 
                        wec.ValueExpression);

                    var newExpression = SyntaxFactory.WithExpression(
                        innerExpr, 
                        new SeparatedSyntaxList<WithExpressionClauseSyntax>(innerWec, 0));

                    wec = wec.WithValueExpression(newExpression);

                    wecs = wecs.Replace(wecs[i], wec);
                }
            }

            node = node.WithWithExpressionClauses(wecs);
            return node;
        }

        private ExpressionSyntax TransformBindingIntoAccessExpression(SyntaxList<ExpressionSyntax> accessorPath)
        {
            ExpressionSyntax result = SyntaxFactory.IdentifierName("xyz"); //FIXME
            foreach (var item in accessorPath)
            {
                switch (item.Kind())
                {
                    case SyntaxKind.MemberBindingExpression:
                        result = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, 
                            result, 
                            ((MemberBindingExpressionSyntax)item).Name);
                        break;
                    case SyntaxKind.ElementBindingExpression:
                        result = SyntaxFactory.ElementAccessExpression(
                            result,
                            ((ElementBindingExpressionSyntax)item).ArgumentList);
                        break;
                }
            }
            return result;
        }

        private SyntaxKind GetBinaryOperationKindFromAssignmentKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.LessThanLessThanEqualsToken:
                    return SyntaxKind.LeftShiftExpression;
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return SyntaxKind.RightShiftExpression;
                case SyntaxKind.SlashEqualsToken:
                    return SyntaxKind.DivideExpression;
                case SyntaxKind.AsteriskEqualsToken:
                    return SyntaxKind.MultiplyExpression;
                case SyntaxKind.BarEqualsToken:
                    return SyntaxKind.BitwiseOrExpression;
                case SyntaxKind.AmpersandEqualsToken:
                    return SyntaxKind.BitwiseAndExpression;
                case SyntaxKind.PlusEqualsToken:
                    return SyntaxKind.AddExpression;
                case SyntaxKind.MinusEqualsToken:
                    return SyntaxKind.SubtractExpression;
                case SyntaxKind.CaretEqualsToken:
                    return SyntaxKind.ExclusiveOrExpression;
                case SyntaxKind.PercentEqualsToken:
                    return SyntaxKind.ModuloExpression;

                default:
                    throw null;
            }
        }

        internal BoundExpression BindWithDegenerate(WithExpressionSyntax node, DiagnosticBag diagnostics)
        {
]           Debug.Assert(node.WithExpressionClauses.All(wec => wec.OperatorToken.Kind() == SyntaxKind.EqualsToken));
            Debug.Assert(node.WithExpressionClauses.All(wec => wec.AccessorPath.Count == 1));

            const string methodName = "With";

            var expr = node.Expression;



        }

    }


}
