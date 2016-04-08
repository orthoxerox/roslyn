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

            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();

            analyzedArguments.Arguments.Add(BindExpression(arg, diagnostics));

            BoundExpression boundExpression = BindMethodGroup(func, invoked: true, indexed: false, diagnostics: diagnostics);
            boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
            string name = boundExpression.Kind == BoundKind.MethodGroup ? GetName(func) : null;
            var result = BindInvocationExpression(node, func, name, boundExpression, analyzedArguments, diagnostics);

            analyzedArguments.Free();
            return result;
        }
    }
}
