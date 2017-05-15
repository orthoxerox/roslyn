﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SyntaxNodeExtensions
    {
        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : CSharpSyntaxNode
        {
            return (TNode)node.Green.SetAnnotations(annotations).CreateRed();
        }

        public static bool IsAnonymousFunction(this SyntaxNode syntax)
        {
            Debug.Assert(syntax != null);
            switch (syntax.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsQuery(this SyntaxNode syntax)
        {
            Debug.Assert(syntax != null);
            switch (syntax.Kind())
            {
                case SyntaxKind.FromClause:
                case SyntaxKind.GroupClause:
                case SyntaxKind.JoinClause:
                case SyntaxKind.JoinIntoClause:
                case SyntaxKind.LetClause:
                case SyntaxKind.OrderByClause:
                case SyntaxKind.QueryContinuation:
                case SyntaxKind.QueryExpression:
                case SyntaxKind.SelectClause:
                case SyntaxKind.WhereClause:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// This method is used to keep the code that generates binders in sync
        /// with the code that searches for binders.  We don't want the searcher
        /// to skip over any nodes that could have associated binders, especially
        /// if changes are made later.
        /// 
        /// "Local binder" is a vague term that refers to binders that represent
        /// scopes for names (e.g. BlockBinders) rather than binders that tweak
        /// default behaviors (e.g. FieldInitializerBinders).  Local binders are
        /// created by LocalBinderFactory.
        /// </summary>
        internal static bool CanHaveAssociatedLocalBinder(this SyntaxNode syntax)
        {
            SyntaxKind kind;
            return syntax.IsAnonymousFunction() ||
                syntax is StatementSyntax ||
                (kind = syntax.Kind()) == SyntaxKind.CatchClause ||
                kind == SyntaxKind.CatchFilterClause ||
                kind == SyntaxKind.SwitchSection ||
                kind == SyntaxKind.EqualsValueClause ||
                kind == SyntaxKind.Attribute ||
                kind == SyntaxKind.ArgumentList ||
                kind == SyntaxKind.ArrowExpressionClause ||
                kind == SyntaxKind.ForwardPipeExpression ||
                IsValidScopeDesignator(syntax as ExpressionSyntax);
        }

        internal static bool IsValidScopeDesignator(this ExpressionSyntax expression)
        {
            // All these nodes are valid scope designators due to the pattern matching and out vars features.
            CSharpSyntaxNode parent = expression?.Parent;
            switch (parent?.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((LambdaExpressionSyntax)parent).Body == expression;

                case SyntaxKind.SwitchStatement:
                    return ((SwitchStatementSyntax)parent).Expression == expression;

                case SyntaxKind.ForStatement:
                    var forStmt = (ForStatementSyntax)parent;
                    return forStmt.Condition == expression || forStmt.Incrementors.FirstOrDefault() == expression;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    return ((CommonForEachStatementSyntax)parent).Expression == expression;

                case SyntaxKind.Argument:
                    return expression.HasPlaceholders();

                default:
                    return false;
            }
        }

        internal static CSharpSyntaxNode AnonymousFunctionBody(this SyntaxNode lambda)
        {
            switch (lambda.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    return ((SimpleLambdaExpressionSyntax)lambda).Body;
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)lambda).Body;
                case SyntaxKind.AnonymousMethodExpression:
                    return ((AnonymousMethodExpressionSyntax)lambda).Block;

                default:
                    throw ExceptionUtilities.UnexpectedValue(lambda.Kind());
            }
        }

        /// <summary>
        /// Given an initializer expression infer the name of anonymous property or tuple element.
        /// Returns default if unsuccessful
        /// </summary>
        internal static SyntaxToken ExtractAnonymousTypeMemberName(this ExpressionSyntax input)
        {
            while (true)
            {
                switch (input.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        return ((IdentifierNameSyntax)input).Identifier;

                    case SyntaxKind.SimpleMemberAccessExpression:
                        input = ((MemberAccessExpressionSyntax)input).Name;
                        continue;

                    case SyntaxKind.ConditionalAccessExpression:
                        input = ((ConditionalAccessExpressionSyntax)input).WhenNotNull;
                        if (input.Kind() == SyntaxKind.MemberBindingExpression)
                        {
                            return ((MemberBindingExpressionSyntax)input).Name.Identifier;
                        }

                        continue;

                    default:
                        return default(SyntaxToken);
                }
            }
        }

        internal static TypeSyntax SkipRef(this TypeSyntax syntax, out RefKind refKind)
        {
            refKind = RefKind.None;
            if (syntax.Kind() == SyntaxKind.RefType)
            {
                refKind = RefKind.Ref;
                syntax = ((RefTypeSyntax)syntax).Type;
            }

            return syntax;
        }

        internal static ExpressionSyntax SkipRef(this ExpressionSyntax syntax, out RefKind refKind)
        {
            refKind = RefKind.None;
            if (syntax?.Kind() == SyntaxKind.RefExpression)
            {
                refKind = RefKind.Ref;
                syntax = ((RefExpressionSyntax)syntax).Expression;
            }

            return syntax;
        }
    }
}
