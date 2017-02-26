//*********************************************************************************************************
//MinHasher - Example minhashing engine.
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinstaMatch
{
    class MinHasher2
    {
        public int signatureSize;
        public Tuple<int, int>[] minhashes;
        private double sim_threshold = 0.5; //minimum value for a pair to be in the same class / came from RoleSimJaccard c++ proj.
        public int ROWSINBAND = 5;
        private int m_numBands;
        private int bucketPairwiseLimit = 100;
        private double atn = 0.05; //maximum accepted false negative rate

        public MinHasher2(int SignatureSize, double sim_threshold_)
        {
            signatureSize = SignatureSize;
            sim_threshold = sim_threshold_;

            Console.WriteLine("Num hash functions: " + signatureSize);
            Console.WriteLine("Maximum accepted false negative rate: " + atn);

            setRowsInBand(rbHashSelector2(sim_threshold, SignatureSize, atn));

            //setRowsInBand( rbHashSelector(sim_threshold) );

            //setRowsInBand( 3 );

            minhashes = new Tuple<int, int>[SignatureSize];
            //Create our unique family of hashing function seeds.
            createMinhashSeeds();
        }
        public void setRowsInBand(int rows_in_band)
        {
            ROWSINBAND = rows_in_band;
            m_numBands = signatureSize / ROWSINBAND;
        }

        private void createMinhashSeeds()
        {
            HashSet<int> skipDups = new HashSet<int>();
            Random r = new Random();
            for (int i = 0; i < minhashes.Length; i++)
            {
                Tuple<int, int> seed = new Tuple<int, int>(r.Next(), r.Next());

                if (skipDups.Add(seed.GetHashCode()))
                    minhashes[i] = seed;
                else
                    i--;  //duplicate seed, try again 
            }
        }

        public static int LSHHash<T>(T inputData, int seedOne, int seedTwo)
        {   
            //Faster, Does not throw exception for overflows during hashing.
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                hash = hash * 16777619 ^ seedOne.GetHashCode();
                hash = hash * 16777619 ^ seedTwo.GetHashCode();
                hash = hash * 16777619 ^ inputData.GetHashCode();
                return hash;
            }
        }

        public static double calculateJaccard<T>(T[] setA, T[] setB)
        {
            /*int intersection = setA.Intersect(setB).Count();
            return intersection / (double)setA.Length;*/

            double intersection = setA.Intersect(setB).Count();
            double union = setA.Union(setB).Count();
            double jacc = intersection / union;
            return jacc;

        }

        /*
         * in the matrix creates a whole column for a given document
         * input a document
         * returns its minhash values foeach has
         */ 
        public int[] getMinHashSignature<T>(T[] tokens)
        {
            //Create a new signature initialized to all int max values
            int[] minHashValues = Enumerable.Repeat(int.MaxValue, signatureSize).ToArray();
            
            HashSet<T> skipDups = new HashSet<T>();
            //Go through every single token 
            foreach (var token in tokens)
            {   //We do not want to hash the same token value more than once...
                if (skipDups.Add(token))
                {   //Hash each unique token with each unique hashing function
                    for (int i = 0; i < signatureSize; i++)
                    {   //Use the same seeds everytime for each hashing function (this is very important!!!)
                        Tuple<int,int> seeds =  minhashes[i];
                        int currentHashValue = LSHHash(token, seeds.Item1, seeds.Item2);
                        //Only retain the minimum value produced by each unique hashing function.
                        if (currentHashValue < minHashValues[i])
                            minHashValues[i] = currentHashValue;
                    }
                }
            }
            return minHashValues;
        }

        /*
         * creates the matrix. col = documents. rows = values of the minhash functions
         */ 
        public Dictionary<T1, int[]> createMinhashCollection<T1,T>(Dictionary<T1, T[]> documents)
        {
            Dictionary<T1, int[]> minhashCollection = new Dictionary<T1, int[]>(documents.Count);

            foreach (var document in documents)
            {
                int[] minhashSignature = getMinHashSignature(document.Value);
                minhashCollection.Add(document.Key, minhashSignature);
            }
            return minhashCollection;
        }

        /*
         * creates the band buckets
         * first index is Set, second index contains hashValue (so index is hash function)
         * 
        */
        public Dictionary<string, HashSet<T1>> createBandBuckets<T1, T>(Dictionary<T1, T[]> documents, Dictionary<T1, int[]> docMinhashes)
        {
            Dictionary<string, HashSet<T1>> m_lshBuckets = new Dictionary<string, HashSet<T1>>();
            //Dictionary<string, int> bigBuckets = new Dictionary<string, int>();
            int index = 0;

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
                        int selectedHash = docMinhashes[s][b*ROWSINBAND + i];
                        sum += selectedHash + "#"; //minHashMatrix[s, b*ROWSINBAND+i];
                    }
                    /*if (bigBuckets.ContainsKey(sum))
                    {
                        //do nothing.ignore it is a big bucket and it won't be processed for pairwise candidate pair generation. we do this for memory efficieny
                    }
                    else */if(m_lshBuckets.ContainsKey(sum))
                    {
                        m_lshBuckets[sum].Add(s);
                        /*if (m_lshBuckets[sum].Count > this.bucketPairwiseLimit)
                        {
                            bigBuckets.Add(sum, 1);
                            m_lshBuckets.Remove(sum);
                        }*/
                    }
                    else
                    {
                        var set = new HashSet<T1>();
                        set.Add(s);
                        m_lshBuckets.Add(sum, set);
                    }
                }
                if(index++ %10000 == 0)
                    Console.Write(".");
            }
            return m_lshBuckets;
        }

        public T1 FindClosest<T1, T>(T1 docKey, Dictionary<T1, T[]> documents, Dictionary<T1, int[]> docMinhashes, Dictionary<string, HashSet<T1>> m_lshBuckets)
        {
            //First find potential "close" candidates
            HashSet<T1> potentialSetIndexes = new HashSet<T1>();
            for (int b = 0; b < m_numBands; b++)
            {
                //combine all 5 MH values and then hash get its hashcode
                //need not be sum
                string sum = "";
                for (int i = 0; i < ROWSINBAND; i++)
                {
                    int selectedHash = docMinhashes[docKey][b * ROWSINBAND + i];
                    sum += selectedHash + "#"; //minHashMatrix[s, b*ROWSINBAND+i];
                }

                foreach (var i in m_lshBuckets[sum].Where(i => !i.Equals(docKey)))
                {
                    potentialSetIndexes.Add(i);
                }
            }

            //From the candidates compute similarity using min-hash and find the index of the closet set
            T1 minIndex = default(T1);
            double similarityOfMinIndex = 0.0;
            foreach (T1 candidateIndex in potentialSetIndexes.Where(i => !i.Equals(docKey)))
            {
                double similarity = calculateJaccard(docMinhashes[docKey], docMinhashes[candidateIndex]);
                if (similarity > similarityOfMinIndex)
                {
                    similarityOfMinIndex = similarity;
                    minIndex = candidateIndex;
                }
            }

            Console.WriteLine("Closest doc index: " + minIndex);
            Console.WriteLine("Closest doc similarity: " + similarityOfMinIndex);
            Console.WriteLine("potential candidates size: " + potentialSetIndexes.Count);
            return minIndex;
        }

        /*
         *  Graph::generateCommonPairs
	        Generate the list of Vertex pairs that share common properties. The pairs in this list will be input to the OurSim calculations
	        We are doing this to recover from n square complexity	
        */
        public Dictionary<string, Tuple<T1, T1, double>> generateVertexPairs<T1, T>(Dictionary<string, HashSet<T1>> m_lshBuckets, Dictionary<T1, int[]> docMinhashes, Dictionary<T1, T[]> wordList ,bool exclude_sim_under_threshold, string output_file_name, bool instance_match = false, string prefix1 = "|first|", string prefix2 = "|second|", string sep_prefix = "-")
        {
            List<KeyValuePair<string, HashSet<T1>>> myList = listBucketSizes(m_lshBuckets, output_file_name);

            //Dictionary<string, HashSet<int>> m_lshBuckets = new Dictionary<string, HashSet<int>>();
            Dictionary<string, Tuple<T1, T1, double>>  pairsDictionary = new Dictionary<string, Tuple<T1, T1, double>>();
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
                wr.WriteLine(string.Format( "vid1{0}vid2{0}minhash_sim", sep));
            }

            string key1, key2;
            foreach (var bucket in myList)
            {
                bucketIndex++;
                if(bucket.Value.Count <=1)
                    continue;
                if (bucket.Value.Count > bucketPairwiseLimit)
                {
                    Console.WriteLine("Broke on greater bucket pairwise limit: " + bucket.Value.Count);
                    break;
                }
                docList = bucket.Value.ToList();
                int i = 0;
                int j = i + 1;
                
                for (i= 0; i< docList.Count; i++)
                {
                    for(j = i+1; j< docList.Count; j++)
                    {
                        if (!instance_match)//match within one file
                        {
                            sum = Util.getKeyFromPair(docList[i], docList[j]);
                            if (!pairsDictionary.ContainsKey(sum))
                            {
                                //jaccard = calculateJaccard(docMinhashes[docList[i]], docMinhashes[docList[j]]);
                                //jaccard = calculateJaccard(wordList[docList[i]], wordList[docList[j]]);
                                jaccard = -1;
                                if (!exclude_sim_under_threshold || jaccard >= sim_threshold)
                                {
                                    pairsDictionary.Add(sum, new Tuple<T1, T1, double>(docList[i], docList[j], jaccard));
                                    if (output_file_name != null)
                                        wr.WriteLine(docList[i] + sep + docList[j] + sep + jaccard);
                                }
                            }
                        }
                        else //match between two files
                        {
                            key1 = docList[i].ToString();
                            key2 = docList[j].ToString();
                            if ((key1.StartsWith(prefix1 + sep_prefix) && key2.StartsWith(prefix2 + sep_prefix)) ||
                                (key1.StartsWith(prefix2 + sep_prefix) && key2.StartsWith(prefix1 + sep_prefix))) //each from different files
                            {
                                sum = Util.getKeyFromPair(key1, key2);
                                if (!pairsDictionary.ContainsKey(sum))
                                {
                                    //jaccard = calculateJaccard(wordList[docList[i]], wordList[docList[j]]);
                                    jaccard = calculateJaccard(wordList[docList[i]], wordList[docList[j]]);
                                    if (!exclude_sim_under_threshold || jaccard >= sim_threshold)
                                    {
                                        pairsDictionary.Add(sum,
                                            new Tuple<T1, T1, double>(docList[i], docList[j], jaccard));
                                        if (output_file_name != null)
                                            wr.WriteLine(docList[i] + sep + docList[j] + sep + jaccard);
                                    }
                                }
                            }
                        }
                        loopCount++;
                    }
                }
            }

            if (instance_match)
            {
                Dictionary<string, int> alreadyMatched = new Dictionary<string, int>();
                Console.WriteLine("Sorting pairsDictionary by their similarity descending ...");
                List<KeyValuePair<string, Tuple<T1, T1, double>>> pairList = pairsDictionary.ToList();
                pairList.Sort(
                    delegate(KeyValuePair<string, Tuple<T1, T1, double>> pair1,
                    KeyValuePair<string, Tuple<T1, T1, double>> pair2)
                    {
                        return pair2.Value.Item3.CompareTo(pair1.Value.Item3); //sorts by their similarity(jaccard) descending
                    }
                );
                Console.WriteLine("Sorted pairsDictionary by their similarity descending.");

                Console.WriteLine("Cleaned pairsDictionary...");
                foreach (var pair in pairList)
                {
                    key1 = pair.Value.Item1.ToString();
                    if (!alreadyMatched.ContainsKey(key1))
                        alreadyMatched.Add(key1, 1);
                    else//matched before with a higher similarity to another one so remove this
                        pairsDictionary.Remove(pair.Key);

                    key2 = pair.Value.Item2.ToString();
                    if (!alreadyMatched.ContainsKey(key2))
                        alreadyMatched.Add(key2, 1);
                    else//matched before with a higher similarity to another one so remove this
                        pairsDictionary.Remove(pair.Key);
                }
                Console.WriteLine("Cleaned pairsDictionary.");
            }

            Console.WriteLine("\r\nBucket generating candidate pairs complexity: " +  loopCount);
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

        public List<KeyValuePair<string, HashSet<T1>>> listBucketSizes<T1>(Dictionary<string, HashSet<T1>> m_lshBuckets, string output_file_name, bool writeToFile = false)
        {
            Console.WriteLine("Sorting buckets by size ascending ...");
            List<KeyValuePair<string, HashSet<T1>>> myList = m_lshBuckets.ToList();
            myList.Sort(
                delegate(KeyValuePair<string, HashSet<T1>> pair1,
                KeyValuePair<string, HashSet<T1>> pair2)
                {
                    return pair1.Value.Count.CompareTo(pair2.Value.Count); //sorts by bucket size ascending
                }
            );
            Console.WriteLine("Sorted buckets by size ascending");            
            //Dictionary<string, HashSet<int>> m_lshBuckets = new Dictionary<string, HashSet<int>>();

            if (writeToFile)
            {
                Console.WriteLine("Writing bucket size to file ...");
                string sum;
                int bucketIndex = 0;
                StreamWriter wr = null;
                string sep = " #-# ";
                string temp_file_name = output_file_name + ".tempbucketsizes";
                if (output_file_name != null)
                {
                    wr = new StreamWriter(temp_file_name); //write the pairs to a file
                    wr.WriteLine("-bucket sizes-");
                }
                foreach (var bucket in myList)
                {
                    bucketIndex++;
                    if (bucket.Value.Count <= 1)
                        continue;
                    string write = string.Format("{0} ::: {1}", bucket.Key, bucket.Value.Count);
                    wr.WriteLine(write);
                }
                if (wr != null)
                {
                    wr.Close();
                }
                Console.WriteLine("Wrote bucket size to file.");
            }
            return myList;
        }


        /*
         * based on the S-shape graph 1 􀀀 (1 􀀀 sr)b - select r and b
         */
        public int rbHashSelector(double sim_threshold)
        {
            int numHashFunctions = signatureSize;
            Console.WriteLine(Math.Pow(2, 4));
            Console.WriteLine(Math.Pow(1, 5));
            Console.WriteLine(Math.Pow(10, 0));
            Console.WriteLine(Math.Pow(3, 2));
            Console.WriteLine(Math.Pow(6, 2.3));

            Console.WriteLine(1.0- (double)Math.Pow(0.5, 500));
            //int numHashFunc = 500;
            int n = numHashFunctions;
            double s = sim_threshold;//sim

            Console.WriteLine("sim-threshold: " + sim_threshold);
            int r = 1;
            while (true)
            {
                int b = n/r;
                double bucketProb = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(s, r), b);
                double simProjectionOf_ST_WithThis = Math.Pow(1 - Math.Pow(1 - s, 1.0/(double) b), 1.0/(double) r);
                Console.WriteLine();
                Console.WriteLine("r:" + r + "\t s:" + s + "-\t->bucketProb:" + bucketProb);
                Console.WriteLine("r:" + r + "\t bucketProb:" + sim_threshold + "\t->simProjection:" + simProjectionOf_ST_WithThis);

                if (bucketProb < sim_threshold && simProjectionOf_ST_WithThis > bucketProb)
                {
                    r = r -1;
                    break;
                }
                r++;
            }
            Console.WriteLine("Optimum maximum r is: " + r);
            return r; //r will set to RowsInBand / number of hash functions in a band
        }

        /*
         * This is the one being used
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
            while (r<= n && r<=20)
            {
                int b = n / r;
                double bucketProb = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(s, r), b);
                fail_prob_perc = (1.0 - bucketProb)*100.0;
                Console.WriteLine();
                Console.WriteLine("r:" + r + "\tb:" +b +  "-\t->bucketProb:" + bucketProb + "\tfail-prob-perc:" +fail_prob_perc + "%");
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
        public int rbHashSelector3(double sim_threshold)
        {
            int numHashFunctions = 100;
            int r = 5;
            //int numHashFunc = 500;
            int n = numHashFunctions;
            double s = 0.1;//sim
            while (s<= 1)
            {
                int b = n / r;
                double bucketProb = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(s, r), b);
                //double simProjectionOf_ST_WithThis = Math.Pow(1 - Math.Pow(1 - s, 1.0 / (double)b), 1.0 / (double)r);
                Console.WriteLine();
                Console.WriteLine("s:" + s + "-\t->bucketProb:" + bucketProb);
                //Console.WriteLine("r:" + r + "\t bucketProb:" + sim_threshold + "\t->simProjection:" + simProjectionOf_ST_WithThis);
                s+=0.1;
            }
            //Console.WriteLine("Optimum maximum r is: " + r);
            return r; //r will set to RowsInBand / number of hash functions in a band
        }

        

    }
}
