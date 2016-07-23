﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a source assembly symbol exposed by the compiler.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISourceAssemblySymbol : IAssemblySymbol
    {
        Compilation Compilation { get; }
    }
}