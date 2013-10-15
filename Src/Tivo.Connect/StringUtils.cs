//-----------------------------------------------------------------------
// <copyright file="StringUtils.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Tivo.Connect
{
    public static class StringUtils
    {
        public static string SplitCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

#if WINDOWS_PHONE || NETFX
            return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
#else
            return Regex.Replace(input, "([A-Z])", " $1").Trim();
#endif
        }

        public static string UppercaseFirst(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            char[] chars = input.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);

            return new string(chars);
        }
    }
}
