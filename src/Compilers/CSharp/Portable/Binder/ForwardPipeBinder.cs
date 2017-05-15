// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Collections.Immutable;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ForwardPipeBinder : LocalScopeBinder
    {
        private readonly BinaryExpressionSyntax _syntax;
        private readonly Binder _enclosing;
        private static PlaceholderNameSyntax _placeholder = SyntaxFactory.PlaceholderName();
        private BoundExpression _boundArg;
        
        public ForwardPipeBinder(Binder enclosing, BinaryExpressionSyntax syntax)
            : base(enclosing, enclosing.Flags | BinderFlags.InPlaceholderExpression)
        {
            Debug.Assert(syntax != null && syntax.IsKind(SyntaxKind.ForwardPipeExpression));
            _syntax = syntax;
            _enclosing = enclosing;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            /*
            var local = SourceLocalSymbol.MakeLocal(
                ContainingMemberOrLambda, 
                this, 
                false,
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier("@"), 
                LocalDeclarationKind.RegularVariable, 
                SyntaxFactory.EqualsValueClause(_syntax.Left), 
                _enclosing);
            */
            var local = new SynthesizedLocal(
                (MethodSymbol)ContainingMemberOrLambda,
                _boundArg.Type,
                SynthesizedLocalKind.PipePlaceholder,
                _syntax);
            return ImmutableArray.Create<LocalSymbol>(local);
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _syntax;
            }
        }

        public override BoundExpression BindPlaceholder(PlaceholderNameSyntax node, bool invoked, DiagnosticBag diagnostics)
        {
            return new BoundLocal(node, Locals[0], ConstantValue.NotAvailable, Locals[0].Type);
        }

        internal BoundExpression BindForwardPipeParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            Debug.Assert(_enclosing == originalBinder);
            var arg = _syntax.Left;
            var func = _syntax.Right;

            _boundArg = originalBinder.BindExpression(arg, diagnostics);

            var sideEffect = new BoundAssignmentOperator(
                arg,
                BindPlaceholder(_placeholder, false, diagnostics),
                _boundArg,
                _boundArg.Type);

            //We can just create a sequence outright
            if (func.HasPlaceholders())
            {
                //We don't do anything
                //func = (ExpressionSyntax)new PlaceholderReplacementVisitor(_placeholder).Visit(func);
            }
            //first we have to check for the ugly case of  `arg |> obj?.Method`
            else if (func.Kind() == SyntaxKind.ConditionalAccessExpression)
            {
                var ca = (ConditionalAccessExpressionSyntax)func;
                //we cannot just replace its WhenNotNull with an invocation expression,
                //since it might be another conditional access expression
                func = InsertInvocationIntoConditionalAccessExpression(ca, _placeholder);
                //return BindConditionalAccessExpression(ca, diagnostics);
            }
            else if (func.Kind() == SyntaxKind.ObjectCreationExpression)
            {
                var oc = (ObjectCreationExpressionSyntax)func;
                if (oc.ArgumentList.OpenParenToken.IsMissing) //no argument list is present
                {
                    func = oc.WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(_placeholder) })));
                }
                else
                {
                    //TODO: add diagnostics
                    return BadExpression(_syntax, LookupResultKind.NotInvocable);
                }
            }
            else
            {
                func = SyntaxFactory.InvocationExpression(
                    func, 
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new[] { SyntaxFactory.Argument(_placeholder) })));
            }

            var boundFunc = BindExpression(func, diagnostics);

            var boundSequence = new BoundSequence(
                _syntax,
                Locals,
                ImmutableArray.Create<BoundExpression>(sideEffect),
                boundFunc,
                boundFunc.Type);

            return boundSequence;
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
