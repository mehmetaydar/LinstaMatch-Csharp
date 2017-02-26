//*********************************************************************************************************
//MinHasher - Example minhashing engine.
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinstaMatch
{
    class MinHasher
    {
        public int signatureSize;
        public Tuple<int, int>[] minhashes;
        private double sim_threshold = 0.5; //minimum value for a pair to be in the same class / came from RoleSimJaccard c++ proj.
        public int ROWSINBAND = 5;
        private int m_numBands;

        public MinHasher(int SignatureSize, double sim_threshold_)
        {
            signatureSize = SignatureSize;
            sim_threshold = sim_threshold_;
            setRowsInBand( rbHashSelector(sim_threshold) );
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
            int intersection = setA.Intersect(setB).Count();
            return intersection / (double)setA.Length;
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
        public Dictionary<int, int[]> createMinhashCollection<T>(Dictionary<int, T[]> documents)
        {
            Dictionary<int, int[]> minhashCollection = new Dictionary<int, int[]>(documents.Count);

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
        public Dictionary<string, HashSet<int>> createBandBuckets<T>(Dictionary<int, T[]> documents, Dictionary<int, int[]> docMinhashes)
        {
            Dictionary<string, HashSet<int>> m_lshBuckets = new Dictionary<string, HashSet<int>>();

            int s;
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

                    if(m_lshBuckets.ContainsKey(sum))
                    {
                        m_lshBuckets[sum].Add(s);
                    }
                    else
                    {
                        var set = new HashSet<int>();
                        set.Add(s);
                        m_lshBuckets.Add(sum, set);
                    }
                }
            }
            foreach(var bucket in m_lshBuckets)
            {
                if (bucket.Value.Count > 1)
                {
                    HashSet<int> x = bucket.Value;
                    //Console.WriteLine(bucket.Value.ToArray());
                }
            }
            return m_lshBuckets;
        }

        public int FindClosest<T>(int docKey, Dictionary<int, T[]> documents, Dictionary<int, int[]> docMinhashes, Dictionary<string, HashSet<int>> m_lshBuckets)
        {
            //First find potential "close" candidates
            HashSet<int> potentialSetIndexes = new HashSet<int>();
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

                foreach (var i in m_lshBuckets[sum].Where(i => i != docKey))
                {
                    potentialSetIndexes.Add(i);
                }
            }

            //From the candidates compute similarity using min-hash and find the index of the closet set
            int minIndex = -1;
            double similarityOfMinIndex = 0.0;
            foreach (int candidateIndex in potentialSetIndexes.Where(i => i != docKey))
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
        public Dictionary<string, Tuple<int, int, double>> generateVertexPairs(Dictionary<string, HashSet<int>> m_lshBuckets, Dictionary<int, int[]> docMinhashes, bool exclude_sim_under_threshold, string output_file_name)
        {
            //Dictionary<string, HashSet<int>> m_lshBuckets = new Dictionary<string, HashSet<int>>();
            Dictionary<string, Tuple<int, int, double>>  pairsDictionary = new Dictionary<string, Tuple<int, int, double>>();
            List<int> docList;
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
            foreach (var bucket in m_lshBuckets)
            {
                bucketIndex++;
                docList = bucket.Value.ToList();
                int i = 0;
                int j = i + 1;
                for (i= 0; i< docList.Count; i++)
                {
                    for(j = i+1; j< docList.Count; j++)
                    {
                        sum = docList[i] + "#" + docList[j];
                        if (!pairsDictionary.ContainsKey(sum))
                        {
                            jaccard = calculateJaccard(docMinhashes[docList[i]], docMinhashes[docList[j]]);
                            if (!exclude_sim_under_threshold || jaccard >= sim_threshold)
                            {
                                pairsDictionary.Add(sum, new Tuple<int, int, double>(docList[i], docList[j], jaccard));
                                if (output_file_name != null)
                                    wr.WriteLine( docList[i]+sep+docList[j]+sep+jaccard);
                            }
                        }
                        loopCount++;
                    }
                }
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
    }
}
