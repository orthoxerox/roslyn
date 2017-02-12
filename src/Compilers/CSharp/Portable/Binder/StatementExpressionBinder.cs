// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder owns the scope for a statement expression.
    /// </summary>
    internal sealed class StatementExpressionBinder : LocalScopeBinder
    {
        private readonly StatementExpressionSyntax _expression;

        public StatementExpressionBinder(Binder enclosing, StatementExpressionSyntax statementExpression)
            : base(enclosing, enclosing.Flags)
        {
            _expression = statementExpression;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return BuildLocals(_expression.Statements, this);
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            return ImmutableArray<LocalFunctionSymbol>.Empty;
        }

        internal override bool IsLocalFunctionsScopeBinder {
            get {
                return false;
            }
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            return ImmutableArray<LabelSymbol>.Empty;
        }

        internal override bool IsLabelsScopeBinder {
            get {
                return false;
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator) {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override SyntaxNode ScopeDesignator {
            get {
                return _expression;
            }
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator) {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
