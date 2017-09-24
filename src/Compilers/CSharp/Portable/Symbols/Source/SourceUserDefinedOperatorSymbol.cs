// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceUserDefinedOperatorSymbol : SourceUserDefinedOperatorSymbolBase
    {
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;

        /// <summary>
        /// A collection of type parameter constraints, populated when
        /// constraints for the first type parameter is requested.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintClause> _lazyTypeParameterConstraints;

        public static SourceUserDefinedOperatorSymbol CreateUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            OperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var location = syntax.OperatorToken.GetLocation();

            string name = OperatorFacts.OperatorNameFromDeclaration(syntax);

            return new SourceUserDefinedOperatorSymbol(
                containingType, name, location, syntax, diagnostics,
                syntax.Body == null && syntax.ExpressionBody != null);
        }

        // NOTE: no need to call WithUnsafeRegionIfNecessary, since the signature
        // is bound lazily using binders from a BinderFactory (which will already include an
        // UnsafeBinder, if necessary).
        private SourceUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            string name,
            Location location,
            OperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics,
            bool isExpressionBodied) :
            base(
                MethodKind.UserDefinedOperator,
                name,
                containingType,
                location,
                syntax.GetReference(),
                syntax.Body?.GetReference() ?? syntax.ExpressionBody?.GetReference(),
                syntax.Modifiers,
                diagnostics,
                isExpressionBodied)
        {
            if (syntax.TypeParameterList != null)
            {
                _typeParameters = MakeTypeParameters(syntax, diagnostics);
            }
            else
            {
                _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        internal new OperatorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (OperatorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax ParameterListSyntax
        {
            get
            {
                return GetSyntax().ParameterList;
            }
        }

        protected override TypeSyntax ReturnTypeSyntax
        {
            get
            {
                return GetSyntax().ReturnType;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public sealed override ImmutableArray<TypeParameterConstraintClause> TypeParameterConstraintClauses
        {
            get
            {
                if (_lazyTypeParameterConstraints.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var syntax = GetSyntax();
                    var withTypeParametersBinder =
                        this.DeclaringCompilation
                        .GetBinderFactory(syntax.SyntaxTree)
                        .GetBinder(syntax.ReturnType, syntax, this);
                    var constraints = this.MakeTypeParameterConstraints(
                        withTypeParametersBinder,
                        TypeParameters,
                        syntax.ConstraintClauses,
                        syntax.OperatorToken.GetLocation(),
                        diagnostics);

                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraints, constraints))
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyTypeParameterConstraints;
            }
        }

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(OperatorDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.TypeParameterList != null);

            var typeParameters = syntax.TypeParameterList.Parameters;
            var result = ArrayBuilder<TypeParameterSymbol>.GetInstance();

            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                if (parameter.VarianceKeyword.Kind() != SyntaxKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, parameter.VarianceKeyword.GetLocation());
                }

                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText;

                // Note: It is not an error to have a type parameter named the same as its enclosing method: void M<M>() {}

                for (int i = 0; i < result.Count; i++)
                {
                    if (name == result[i].Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                var tpEnclosing = ContainingType.FindEnclosingTypeParameter(name);
                if ((object)tpEnclosing != null)
                {
                    // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                    diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingType);
                }

                var syntaxRefs = ImmutableArray.Create(parameter.GetReference());
                var locations = ImmutableArray.Create(location);
                var typeParameter = new SourceMethodTypeParameterSymbol(
                    this,
                    name,
                    ordinal,
                    locations,
                    syntaxRefs);

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }
    }
}
