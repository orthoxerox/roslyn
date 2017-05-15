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
            var binder = (ForwardPipeBinder)GetBinder(node);

            return binder.BindForwardPipeParts(diagnostics, this);
        }
    }
}
