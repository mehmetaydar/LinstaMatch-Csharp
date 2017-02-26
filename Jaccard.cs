/*
 * https://nickgrattan.wordpress.com/2014/02/18/jaccard-similarity-index-for-measuring-document-similarity/
 */

using System.Collections.Generic;
using System.Linq;

namespace LinstaMatch
{
    public class Jaccard
    {
        public static double Calc(HashSet<int> hs1, HashSet<int> hs2)
        {
            return ((double)hs1.Intersect(hs2).Count() / (double)hs1.Union(hs2).Count());
        }
        public static double Calc(HashSet<uint> hs1, HashSet<uint> hs2)
        {
            return ((double)hs1.Intersect(hs2).Count() / (double)hs1.Union(hs2).Count());
        }

        public static double Calc(List<int> ls1, List<int> ls2)
        {
            HashSet<int> hs1 = new HashSet<int>(ls1);
            HashSet<int> hs2 = new HashSet<int>(ls2);
            return Calc(hs1, hs2);
        }
        public static int unionSize(List<int> ls1, List<int> ls2)
        {
            HashSet<int> hs1 = new HashSet<int>(ls1);
            HashSet<int> hs2 = new HashSet<int>(ls2);
            return hs1.Union(hs2).Count();
        }
        public static double Calc(List<uint> ls1, List<uint> ls2)
        {
            HashSet<uint> hs1 = new HashSet<uint>(ls1);
            HashSet<uint> hs2 = new HashSet<uint>(ls2);
            return Calc(hs1, hs2);
        }
    }
}
