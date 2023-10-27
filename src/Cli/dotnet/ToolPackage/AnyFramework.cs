// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.ToolPackage
{
    internal class AnyFramework : NuGetFramework
    {
        internal static AnyFramework Instance { get; } = new AnyFramework();


        private AnyFramework()
            : base(NuGetFramework.AnyFramework)
        {
        }
    }
}
