using System.Collections.Generic;
using System.Linq;

namespace YALCT
{
    public static class StringExtensions
    {
        public static string ToSystemString(this IEnumerable<char> source)
        {
            return new string(source.ToArray());
        }
    }
}