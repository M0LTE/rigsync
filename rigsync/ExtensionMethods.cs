using System.Collections.Generic;
using System.Linq;

namespace SeleniumTest
{
    internal static partial class ExtensionMethods
    {
        public static bool EndsWith(this List<char> list, string toCheckFor)
        {
            if (list.Count() < toCheckFor.Length)
                return false;

            for (int i = 0; i < toCheckFor.Length; i++)
            {
                if (list[list.Count() - toCheckFor.Length + i] != toCheckFor[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
