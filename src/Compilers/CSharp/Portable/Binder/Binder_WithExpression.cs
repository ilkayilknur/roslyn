// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts a <see cref="WithExpressionSyntax"/> into a <see cref="BoundExpression"/>.
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindWithExpression(WithExpressionSyntax syntax, DiagnosticBag diagnostics)
        {
            // PROTOTYPE: this entire method is likely to change
            var receiver = BindRValueWithoutTargetType(syntax.Receiver, diagnostics);
            var receiverType = receiver.Type;

            var lookupResult = LookupResult.GetInstance();
            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;
            bool hasErrors = false;

            if (receiverType is null || receiverType.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_InvalidWithReceiverType, syntax.Receiver.Location);
                receiverType = CreateErrorType();
            }

            MethodSymbol? cloneMethod = null;
            if (!receiverType.IsErrorType())
            {
                // PROTOTYPE: The receiver type must have a instance method called 'Clone' with no parameters
                LookupMembersInType(
                    lookupResult,
                    receiverType,
                    WellKnownMemberNames.CloneMethodName,
                    arity: 0,
                    ConsList<TypeSymbol>.Empty,
                    LookupOptions.MustBeInstance | LookupOptions.MustBeInvocableIfMember,
                    this,
                    diagnose: false,
                    ref useSiteDiagnostics);

                // PROTOTYPE: Should handle hiding/overriding
                if (lookupResult.IsMultiViable)
                {
                    foreach (var symbol in lookupResult.Symbols)
                    {
                        if (symbol is MethodSymbol { ParameterCount: 0 } m)
                        {
                            cloneMethod = m;
                            break;
                        }
                    }
                }

                lookupResult.Clear();
                // PROTOTYPE: discarding use-site diagnostics
                useSiteDiagnostics = null;

                if (cloneMethod is null)
                {
                    hasErrors = true;
                    diagnostics.Add(ErrorCode.ERR_NoSingleCloneMethod, syntax.Receiver.Location, receiverType);
                }
                else
                {
                    // Check return type
                    if (!receiverType.IsEqualToOrDerivedFrom(
                            cloneMethod.ReturnType,
                            TypeCompareKind.ConsiderEverything,
                            ref useSiteDiagnostics))
                    {
                        hasErrors = true;
                        diagnostics.Add(
                            ErrorCode.ERR_ContainingTypeMustDeriveFromWithReturnType,
                            syntax.Receiver.Location,
                            receiverType,
                            cloneMethod.ReturnType);
                    }

                    // PROTOTYPE: discarding use-site diagnostics
                    useSiteDiagnostics = null;
                }
            }

            var cloneReturnType = cloneMethod?.ReturnType;
            // PROTOTYPE: Handle dynamic
            var implicitReceiver = new BoundObjectOrCollectionValuePlaceholder(
                syntax.Receiver,
                // formatting
            #pragma warning disable IDE0055
                cloneReturnType ?? receiverType) { WasCompilerGenerated = true };
            #pragma warning restore IDE0055

            var args = ArrayBuilder<(Symbol?, BoundExpression)>.GetInstance();
            // Bind with expression arguments
            foreach (var initializer in syntax.Initializers)
            {
                var propSyntax = initializer.NameEquals?.Name;
                var propName = propSyntax?.Identifier.Text;
                BoundExpression? boundMember = null;
                if (!(propName is null))
                {
                    var location = initializer.NameEquals!.Name.Location;
                    boundMember = BindInstanceMemberAccess(
                        node: propSyntax,
                        right: propSyntax,
                        boundLeft: implicitReceiver,
                        rightName: propName,
                        rightArity: 0,
                        typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>),
                        typeArgumentsWithAnnotations: default(ImmutableArray<TypeWithAnnotations>),
                        invoked: false,
                        indexed: false,
                        diagnostics: diagnostics);

                    hasErrors = boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

                    boundMember = CheckValue(boundMember, BindValueKind.Assignable, diagnostics);
                }

                var expr = BindValue(initializer.Expression, diagnostics, BindValueKind.RValue);
                if (!(boundMember is null))
                {
                    var assignment = BindAssignment(initializer, boundMember, expr, isRef: false, diagnostics);
                    expr = assignment.Right;
                }
                args.Add((boundMember?.ExpressionSymbol, expr));
            }

            lookupResult.Free();

            return new BoundWithExpression(
                syntax,
                receiver,
                cloneMethod,
                args.ToImmutableAndFree(),
                cloneReturnType ?? receiverType,
                hasErrors: hasErrors);
        }
    }
}