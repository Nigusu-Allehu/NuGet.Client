// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;


namespace NuGet.Packaging
{
#pragma warning disable RS0016 // Add public types and members to the declared API
    public class TokenizedPathCompare : IComparer<List<string>>
#pragma warning restore RS0016 // Add public types and members to the declared API
    {
#pragma warning disable RS0016 // Add public types and members to the declared API
        public int Compare(List<string> x, List<string> y)
#pragma warning restore RS0016 // Add public types and members to the declared API
        {
            int minLength = Math.Min(x.Count, y.Count);

            for (int i = 0; i < minLength; i++)
            {
                int result = string.Compare(x[i], y[i], StringComparison.OrdinalIgnoreCase);
                if (result != 0) return result;
            }

            return x.Count.CompareTo(y.Count);
        }
    }

}
