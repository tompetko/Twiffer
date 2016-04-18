using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Twiffer.Extensions
{
    public static class StringCollectionExtensions
    {
        public static string[] ToArray(this StringCollection self)
        {
            var result = new List<string>();

            result.AddRange(self.Cast<string>());

            return result.ToArray();
        }
    }
}
