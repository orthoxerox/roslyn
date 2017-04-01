// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenPlaceholderTests : CSharpTestBase
    {
        [Fact]
        public void TestEmit()
        {
            var src =
@"
using System;

static class Program
{
    static int Foo(Func<int, int> func)
    {
        return func(2);
    }

    static void Main()
    {
        var x = Foo(@ + 1);
        System.Console.Write(x);
    }
}";
            var verify = CompileAndVerify(
                src,
                expectedOutput: "3");
            
        }
    }
}
