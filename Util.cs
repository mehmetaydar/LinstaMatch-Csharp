using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinstaMatch
{
    public class Util
    {
        //SepInputReader<int, string> sepInputReader
        public static Dictionary<T1, T[]> getSampleFromDict<T1, T>( Dictionary<T1, T[]> original, int sample_size )
        {
            Dictionary<T1, T[]> sample = new Dictionary<T1, T[]>();
            Random rnd = new Random();
            foreach (var sample_key in original.Keys.OrderBy(x => rnd.Next()).Take(sample_size))
            {
                sample.Add(sample_key, original[sample_key]);   
            }
            return sample;
        }
        public static Dictionary<T1, List<int>> getSampleFromDict<T1>(Dictionary<T1, List<int>> original, int sample_size)
        {
            Dictionary<T1, List<int>> sample = new Dictionary<T1, List<int>>();
            Random rnd = new Random();
            foreach (var sample_key in original.Keys.OrderBy(x => rnd.Next()).Take(sample_size))
            {
                sample.Add(sample_key, original[sample_key]);
            }
            return sample;
        }

        public static Dictionary<string, Tuple<T1, T1, double>> getActualPairsDictionary<T1, T>(Dictionary<T1, T[]> wordList, double threshold)
        {
            Dictionary<string, Tuple<T1, T1, double>> pairsDictionary = new Dictionary<string, Tuple<T1, T1, double>>();
            List<T1> docList = wordList.Keys.ToList();
            int i, j;
            string sum;
            double jaccard;
            for (i = 0; i < docList.Count; i++)
            {
                for (j = i + 1; j < docList.Count; j++)
                {
                    //sum = docList[i] + "#" + docList[j];
                    sum = getKeyFromPair(docList[i], docList[j]);
                    if (!pairsDictionary.ContainsKey(sum))
                    {
                        jaccard = MinHasher2.calculateJaccard(wordList[docList[i]], wordList[ docList[j] ]);
                        if (jaccard >= threshold)
                            pairsDictionary.Add(sum, new Tuple<T1, T1, double>(docList[i], docList[j], jaccard));
                    }
                }
            }
            return pairsDictionary;
        }

        public static Dictionary<string, Tuple<T1, T1, double>> getActualPairsDictionary<T1>(Dictionary<T1, List<int>> wordList, double threshold)
        {
            Dictionary<string, Tuple<T1, T1, double>> pairsDictionary = new Dictionary<string, Tuple<T1, T1, double>>();
            List<T1> docList = wordList.Keys.ToList();
            int i, j;
            string sum;
            double jaccard;
            for (i = 0; i < docList.Count; i++)
            {
                for (j = i + 1; j < docList.Count; j++)
                {
                    //sum = docList[i] + "#" + docList[j];
                    sum = getKeyFromPair(docList[i], docList[j]);
                    if (!pairsDictionary.ContainsKey(sum))
                    {
                        jaccard = Jaccard.Calc(wordList[docList[i]], wordList[docList[j]]);
                        if (jaccard >= threshold)
                            pairsDictionary.Add(sum, new Tuple<T1, T1, double>(docList[i], docList[j], jaccard));
                    }
                }
            }
            return pairsDictionary;
        }
        /*
         * Check to see if jaccard(minhashed) = jaccard(actual documents)
         */ 
        public static double calculateMinHashFunctionsAccuracy<T1, T>(Dictionary<T1, T[]> wordListActual, Dictionary<T1, int[]> wordListMinHash)
        {
            List<T1> docList = wordListActual.Keys.ToList();
            int i, j;
            double jaccard_actual, jaccard_minhash;
            double total_diff_perc = 0;
            double diff_perc;
            int pair_count = 0;
            for (i = 0; i < docList.Count; i++)
            {
                for (j = i + 1; j < docList.Count; j++)
                {
                    jaccard_actual = MinHasher2.calculateJaccard(wordListActual[docList[i]], wordListActual[docList[j]]);
                    if (jaccard_actual > 0)
                    {
                        jaccard_minhash = MinHasher2.calculateJaccard(wordListMinHash[docList[i]],
                            wordListMinHash[docList[j]]);
                        diff_perc = (Math.Abs(jaccard_minhash - jaccard_actual)/jaccard_actual)*100;
                        total_diff_perc += diff_perc;
                        pair_count++;
                    }
                }
            }
            double avg_diff_perc = total_diff_perc/pair_count;
            Console.WriteLine("Average diff from Actual and MinHash Jaccard is: " + avg_diff_perc + " %");
            return avg_diff_perc;
        }

        public static double calculateMinHashFunctionsAccuracy<T1>(Dictionary<T1, List<int>> wordListActual, Dictionary<T1, List<uint>> wordListMinHash)
        {
            List<T1> docList = wordListActual.Keys.ToList();
            int i, j;
            double jaccard_actual, jaccard_minhash;
            double total_diff_perc = 0;
            double diff_perc;
            int pair_count = 0;
            for (i = 0; i < docList.Count; i++)
            {
                for (j = i + 1; j < docList.Count; j++)
                {
                    jaccard_actual = Jaccard.Calc(wordListActual[docList[i]], wordListActual[docList[j]]);
                    if (jaccard_actual > 0)
                    {
                        jaccard_minhash = Jaccard.Calc(wordListMinHash[docList[i]],
                            wordListMinHash[docList[j]]);
                        diff_perc = (Math.Abs(jaccard_minhash - jaccard_actual) / jaccard_actual) * 100;
                        total_diff_perc += diff_perc;
                        pair_count++;
                    }
                }
            }
            double avg_diff_perc = total_diff_perc / pair_count;
            Console.WriteLine("Average diff from Actual and MinHash Jaccard is: " + avg_diff_perc + " %");
            return avg_diff_perc;
        }

        public static double calculateRecall<T1>(Dictionary<string, Tuple<T1, T1, double>> actualPairsDictionary, Dictionary<string, Tuple<T1, T1, double>> pairsDictionary)
        {
            //actualPairsDictionary = the way it supposed to be (fn)
            //pairsDictionary = the way the algorithm did ( tp + fp )
            double recall = 0;
            int tp_and_fn = actualPairsDictionary.Keys.Count;
            int tp = 0;
            foreach (string k in pairsDictionary.Keys)
            {
                if (actualPairsDictionary.ContainsKey(k))
                {
                    tp++;
                }
                else
                {
                    int x = 1;
                }
            }
            recall = (double) tp/(double) (tp_and_fn);
            Console.WriteLine("Recall is: " + recall*100 + "%");
            return recall;
        }

        public static String getKeyFromPair<T1>(T1 p1, T1 p2)
        {
            String s1, s2;
            if (p1.ToString().GetHashCode() <= p2.ToString().GetHashCode())
            {
                s1 = p1.ToString();
                s2 = p2.ToString();
            }
            else
            {
                s1 = p2.ToString();
                s2 = p1.ToString();
            }
            String key = s1 + "#" + s2;
            return key;
            /*String s1, s2;
            if (p1.ToString().CompareTo(p2.ToString()) >=0 )
            {
                s1 = p1.ToString();
                s2 = p2.ToString();
            }
            else
            {
                s1 = p2.ToString();
                s2 = p1.ToString();
            }
            String key = s1 + "#" + s2;
            return key;*/
        }

        public static Dictionary<string, T[]> mergeTwoWordLists<T1, T>(Dictionary<T1, T[]> list1, Dictionary<T1, T[]> list2, string prefix1 = "|first|", string prefix2 = "|second|", string sep = "-")
        {
            Dictionary<string, T[]> list3 = new Dictionary<string, T[]>();
            string key;
            foreach (var w in list1)
            {
                key = prefix1 + sep+  w.Key.ToString();
                list3.Add(key, w.Value);
            }
            foreach (var w in list2)
            {
                key = prefix2 + sep + w.Key.ToString();
                list3.Add(key, w.Value);
            }
            return list3;
        }

        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^A-Za-z0-9 _]", ""); 
        }

    }
}
