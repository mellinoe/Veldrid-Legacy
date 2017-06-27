// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Globalization
{
    // Internal Contract for all Globalization APIs that are needed by lower levels of System.Private.CoreLib.
    // This is class acts as a gateway between everything in System.Private.CoreLib and System.Globalization. 
    internal partial class FormatProvider
    {
        public static IFormatProvider InvariantCulture { get { return CultureInfo.InvariantCulture; } }
   
        #region Parsing
        public static Single ParseSingle(ReadOnlySpan<char> value, NumberStyles options, IFormatProvider provider)
        {
            return FormatProvider.Number.ParseSingle(value, options, provider);
        }
        public static Boolean TryParseSingle(ReadOnlySpan<char> value, NumberStyles options, IFormatProvider provider, out Single result)
        {
            return FormatProvider.Number.TryParseSingle(value, options, provider, out result);
        }
        #endregion
    }
}
