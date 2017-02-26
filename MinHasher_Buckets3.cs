using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinstaMatch
{
    public class MinHasher_Buckets3
    {
        public MinHasher3 mh;
        private double sim_threshold = 0.5; //minimum value for a pair to be in the same class / came from RoleSimJaccard c++ proj.
        private double atn = 0.05;
        public int ROWSINBAND = 5;
        private int m_numBands;

        public MinHasher_Buckets3(MinHasher3 mh, double sim_threshold_, double atn_)
        {
            this.mh = mh;
            this.sim_threshold = sim_threshold_;
            this.atn = atn_;
            setRowsInBand(rbHashSelector2(sim_threshold, mh.NumHashFunctions, atn));
        }
        public void setRowsInBand(int rows_in_band)
        {
            ROWSINBAND = rows_in_band;
            m_numBands = this.mh.NumHashFunctions / ROWSINBAND;
        }
        /*
         *  based on the S-shape graph 1 􀀀 (1 􀀀 sr)b , sim_threshold and atn- select r and b
         */
        public int rbHashSelector2(double sim_threshold, int _numHashFunctions, double fail_rate)
        {
            int numHashFunctions = _numHashFunctions;
            //int numHashFunc = 500;
            int n = numHashFunctions;
            double s = sim_threshold;//sim
            //double fail_rate = 0.05;
            double fail_prob_perc = 0;
            Console.WriteLine("sim-threshold: " + sim_threshold);
            int r = 1;
            while (r <= n && r <= 20)
            {
                int b = n / r;
                double bucketProb = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(s, r), b);
                fail_prob_perc = (1.0 - bucketProb) * 100.0;
                Console.WriteLine();
                Console.WriteLine("r:" + r + "\tb:" + b + "-\t->bucketProb:" + bucketProb + "\tfail-prob-perc:" + fail_prob_perc + "%");
                if (1 - bucketProb > fail_rate)
                {
                    r = r - 1;
                    break;
                }
                /*if (r > 20)
                    break;*/

                /*bucketProb = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(s2, r), b);
                Console.WriteLine();
                Console.WriteLine("r:" + r + "\tb:" + b + "\ts:" + s2 + "-\t->bucketProb:" + bucketProb);

                bucketProb = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(s3, r), b);
                Console.WriteLine();
                Console.WriteLine("r:" + r + "\tb:" + b + "\ts:" + s3 + "-\t->bucketProb:" + bucketProb);*/


                r++;
            }
            Console.WriteLine("Optimum r is: " + r);
            return r; //r will set to RowsInBand / number of hash functions in a band
        }

        /*
         * creates the matrix. col = documents. rows = values of the minhash functions
         * the matrix will be used in the banding tecnique
         */
        public Dictionary<T1, List<uint>> createMinhashCollection<T1>(Dictionary<T1, List<int>> documents)
        {
            Dictionary<T1, List<uint>> minhashCollection = new Dictionary<T1, List<uint>>(documents.Count);

            foreach (var document in documents)
            {
                List<uint> minhashSignature = this.mh.GetMinHash(document.Value);
                minhashCollection.Add(document.Key, minhashSignature);
            }
            return minhashCollection;
        }
        /*
         * creates the band buckets
         * first index is Set, second index contains hashValue (so index is hash function)
         * 
        */
        public Dictionary<string, HashSet<T1>> createBandBuckets<T1, T>(Dictionary<T1, List<int>> documents, Dictionary<T1, List<uint>> docMinhashes)
        {
            Dictionary<string, HashSet<T1>> m_lshBuckets = new Dictionary<string, HashSet<T1>>();

            T1 s;
            foreach (var document in documents)
            {
                s = document.Key;
                for (int b = 0; b < m_numBands; b++)
                {
                    //combine all 5 MH values and then hash get its hashcode
                    //need not be sum
                    string sum = "";
                    for (int i = 0; i < ROWSINBAND; i++)
                    {
                        uint selectedHash = docMinhashes[s][b * ROWSINBAND + i];
                        sum += selectedHash + "#"; //minHashMatrix[s, b*ROWSINBAND+i];
                    }

                    if (m_lshBuckets.ContainsKey(sum))
                    {
                        m_lshBuckets[sum].Add(s);
                    }
                    else
                    {
                        var set = new HashSet<T1>();
                        set.Add(s);
                        m_lshBuckets.Add(sum, set);
                    }
                }
            }
            foreach (var bucket in m_lshBuckets)
            {
                if (bucket.Value.Count > 1)
                {
                    HashSet<T1> x = bucket.Value;
                    //Console.WriteLine(bucket.Value.ToArray());
                }
            }
            return m_lshBuckets;
        }
        /*
         *  Graph::generateCommonPairs
	        Generate the list of Vertex pairs that share common properties. The pairs in this list will be input to the OurSim calculations
	        We are doing this to recover from n square complexity	
        */
        public Dictionary<string, Tuple<T1, T1, double>> generateVertexPairs<T1, T>(Dictionary<string, HashSet<T1>> m_lshBuckets, Dictionary<T1, List<uint>> docMinhashes, Dictionary<T1, List<int>> wordList, bool exclude_sim_under_threshold, string output_file_name)
        {
            //Dictionary<string, HashSet<int>> m_lshBuckets = new Dictionary<string, HashSet<int>>();
            Dictionary<string, Tuple<T1, T1, double>> pairsDictionary = new Dictionary<string, Tuple<T1, T1, double>>();
            List<T1> docList;
            string sum;
            int loopCount = 0;
            double jaccard;
            int bucketIndex = 0;
            StreamWriter wr = null;
            string sep = " #-# ";
            string temp_file_name = output_file_name + ".temp";
            if (output_file_name != null)
            {
                wr = new StreamWriter(temp_file_name); //write the pairs to a file
                wr.WriteLine("-common_pairs-");
                wr.WriteLine(string.Format("vid1{0}vid2{0}minhash_sim", sep));
            }
            foreach (var bucket in m_lshBuckets)
            {
                bucketIndex++;
                if (bucket.Value.Count <= 1)
                    continue;
                docList = bucket.Value.ToList();
                int i = 0;
                int j = i + 1;
                for (i = 0; i < docList.Count; i++)
                {
                    for (j = i + 1; j < docList.Count; j++)
                    {
                        //sum = docList[i] + "#" + docList[j];
                        sum = Util.getKeyFromPair(docList[i], docList[j]);
                        if (!pairsDictionary.ContainsKey(sum))
                        {
                            //jaccard = calculateJaccard(docMinhashes[docList[i]], docMinhashes[docList[j]]);
                            jaccard = Jaccard.Calc(wordList[docList[i]], wordList[docList[j]]);
                            if (!exclude_sim_under_threshold || jaccard >= sim_threshold)
                            {
                                pairsDictionary.Add(sum, new Tuple<T1, T1, double>(docList[i], docList[j], jaccard));
                                if (output_file_name != null)
                                    wr.WriteLine(docList[i] + sep + docList[j] + sep + jaccard);
                            }
                        }
                        loopCount++;
                    }
                }
            }
            Console.WriteLine("\r\nBucket generating candidate pairs complexity: " + loopCount);
            if (wr != null)
            {
                wr.Close();
                wr = new StreamWriter(output_file_name);
                wr.WriteLine(pairsDictionary.Count); //prepending the size of the pairs. needed for c++ vector space allocation
                StreamReader rd = new StreamReader(temp_file_name);
                string buf;
                while ((buf = rd.ReadLine()) != null)
                {
                    wr.WriteLine(buf);
                }
                rd.Close();
                wr.Close();
                File.Delete(temp_file_name);
            }
            return pairsDictionary;
        }
    }
}
