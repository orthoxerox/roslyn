// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenStatementExpressionTests : CSharpTestBase
    {
        [Fact]
        public void TestBasicEmit()
        {
            var source = @"
using System;
class C 
{ 
    public static void Main() 
    { 
        Console.Write((var s = ""test""; s));
    }
}
";
            string expectedOutput =
@"test";
            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);
        }

    }
}
