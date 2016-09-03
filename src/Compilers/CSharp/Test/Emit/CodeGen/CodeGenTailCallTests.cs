// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenTailCallTests : EmitMetadataTestBase
    {
        [Fact]
        public void SimpleTailCall()
        {
            var text = @"
class Program
{
    static int M(int i)
    {
        return from M(i);
    }
}
";
            CompileAndVerify(text)
                .VerifyIL("Program.M(int)",
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  tail.
  IL_0003:  call       ""int Program.M(int)""
  IL_0008:  ret
}");
        }

        [Fact]
        public void TailRecursion()
        {
            var text = @"
using static System.Console;
class Program
{
    static void Main()
    {
        Write(M(1000000));
    }

    static int M(int i)
    {
        if (i==0) return 0;
        return from M(i-1);
    }
}
";
            CompileAndVerify(text, expectedOutput: "0")
                .VerifyIL("Program.M(int)",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  tail.
  IL_000a:  call       ""int Program.M(int)""
  IL_000f:  ret
}");
        }
    }
}
