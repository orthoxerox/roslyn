// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// This portion of the binder converts a QueryExpressionSyntax into a BoundExpression
    /// </summary>
    internal partial class Binder
    {
        internal BoundExpression BindForwardPipeOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var arg = node.Left;
            var func = node.Right;

            
            //first we have to check for the ugly case of  `arg |> obj?.Method`
            if (func.Kind() == SyntaxKind.ConditionalAccessExpression) {
                var ca = (ConditionalAccessExpressionSyntax)func;
                //we cannot just replace its WhenNotNull with an invocation expression,
                //since it might be another conditional access expression
                ca = InsertInvocationIntoConditionalAccessExpression(ca, arg);
                return BindConditionalAccessExpression(ca, diagnostics);
            }

            if (func.Kind() == SyntaxKind.ObjectCreationExpression)
            {
                var oc = (ObjectCreationExpressionSyntax)func;
                if (oc.ArgumentList.OpenParenToken.IsMissing) //no argument list is present
                {
                    oc = oc.WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(arg) })));
                    return BindObjectCreationExpression(oc, diagnostics);
                }
            }

            var analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.Add(BindExpression(arg, diagnostics));

            var boundExpression = BindMethodGroup(func, invoked: true, indexed: false, diagnostics: diagnostics);
            boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
            string name = boundExpression.Kind == BoundKind.MethodGroup ? GetName(func) : null;
            var result = BindInvocationExpression(node, func, name, boundExpression, analyzedArguments, diagnostics);

            analyzedArguments.Free();
            return result;
        }

        private ConditionalAccessExpressionSyntax InsertInvocationIntoConditionalAccessExpression(
            ConditionalAccessExpressionSyntax ca, 
            ExpressionSyntax arg)
        {
            var wnn = ca.WhenNotNull;
            if (wnn.Kind() == SyntaxKind.ConditionalAccessExpression) 
            {
                return ca.WithWhenNotNull(InsertInvocationIntoConditionalAccessExpression(
                    (ConditionalAccessExpressionSyntax)wnn,
                    arg));
            } 
            else 
            {
                return ca.WithWhenNotNull(
                    SyntaxFactory.InvocationExpression(
                        ca.WhenNotNull,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(arg) }))));
            }
        }
    }
}
