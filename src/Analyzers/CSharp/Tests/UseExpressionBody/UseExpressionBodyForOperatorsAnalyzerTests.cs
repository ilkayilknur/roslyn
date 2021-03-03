﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseExpressionBodyDiagnosticAnalyzer,
        UseExpressionBodyCodeFixProvider>;

    public class UseExpressionBodyForOperatorsAnalyzerTests
    {
        private static async Task TestWithUseExpressionBody(string code, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options =
                { { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement } }
            }.RunAsync();
        }

        private static async Task TestWithUseBlockBody(string code, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options =
                { { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement } }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        [|Bar|]();
    }
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2) => Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        return [|Bar|]();
    }
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2) => Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        [|throw|] new NotImplementedException();
    }
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2) => throw new NotImplementedException();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        [|throw|] new NotImplementedException(); // comment
    }
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2) => throw new NotImplementedException(); // comment
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2) [|=>|] Bar();
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        return Bar();
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2) [|=>|] throw new NotImplementedException();
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        throw new NotImplementedException();
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            var code = @"
class C
{
    public static C operator +(C c1, C c2) [|=>|] throw new NotImplementedException(); // comment
}";
            var fixedCode = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        throw new NotImplementedException(); // comment
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }
    }
}
