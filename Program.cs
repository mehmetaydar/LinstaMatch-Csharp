//*********************************************************************************************************
//MinHasher - Example minhashing system with Jaccard comparisons.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LinstaMatch
{
    class Program
    {
        private static string dataset_main_location = @"..\..\input\";
        static void Main(string[] args)
        {
            /*Evaluations mentined in LinstaMatch Paper - start*/

            processNewsCorporaFiles(true, 5000);  //Process a sample of 5000 instances from news-aggregator dataset for candidate-pairs-generation
            //processNewsCorporaFiles(false, 5000); //Process All news-aggregator dataset for candidate-pairs-generation

            //processAmazonJsonDumpFiles(true, 5000); //Process a sample of 5000 instances from Amazon office products metadata dataset for candidate-pairs-generation
            //processAmazonJsonDumpFiles(false, 5000); //Process All Amazon office products metadata dataset for candidate-pairs-generation

            //processMashableFiles(true, 5000);  //Process a sample of 5000 instances from mashable (online news popularity) dataset for candidate-pairs-generation
            //processMashableFiles(false, 5000); //Process All mashable (online news popularity) dataset for candidate-pairs-generation


            //processUobmLargeFiles_InstanceMatch(false, 1000); //Process UOBM-Mainbox files used in OAEI 2016 campaign for instance matching
            //processSpimbenchFiles_InstanceMatch(false, 1000); //Process Spimbench files used in OAEI 2016 campaign for instance matching

            /*Evaluations mentined in LinstaMatch Paper - end*/



            /*
             * these tests were not mentioned in the paper
            //processNumbersTest3(false, 1000);
            //MinHasher3TestFunc1();
            //JaccardTest1();
            //processNumbersTest(false, 1000);
            //generatePairsFileForRoleSim();
             * */
        }
        static void processNewsCorporaFiles(bool sample = false, int sample_size = 1000)
        {
            //to try minhash on news corpora file
            //string file = @"C:\Users\maydar\Dropbox\Semantic Study\ScabilityPaper\datasets\news aggregator\newsCorpora.csv-clean.txt";
            string file = dataset_main_location + @"\news aggregator\newsCorpora.csv-clean.txt";
            string pair_output_filename = file + "_minhashpairs.txt";

            int numHashFunctions = 130;
            double simThreshold = 0.65;
            bool exclude_sim_under_threshold = false; //vertex pairs which have estimated similarity under the threshold will be excluded if set
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            Dictionary<int, string[]> wordList;

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw0 = new Stopwatch();
            Stopwatch sw = new Stopwatch();

            int[] index_locations = { 0, 1, 2 };
            string sep = @"\t";
            int limit = -1;
            if (sample)
            {
                limit = sample_size;
                Console.WriteLine("Sample size: " + sample_size);
            }
            SepInputReader<int, string> sepInputReader = new SepInputReader<int, string>(file, index_locations, sep, false, limit);
            Dictionary<int, string> groundTruth = sepInputReader.groundTruth;

            //Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(flatInputReader.vertexLabelList);
            wordList = sepInputReader.wordList;
            Console.WriteLine(string.Format("\r\nInstances count: {0}", wordList.Count));
            long possiblePairCount = PermutationsAndCombinations.nCr(wordList.Count, 2);

            /*if (!sample)
                wordList = sepInputReader.wordList;
            else
            {
                wordList = Util.getSampleFromDict(sepInputReader.wordList, sample_size);
            }*/

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw0.Restart();
            sw.Restart();

            Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(wordList);
            if (sample)
            {
                //double avg_diff_perc_from_actual_and_minhash_jaccard = Util.calculateMinHashFunctionsAccuracy(wordList, docMinhashes);
            }

            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<int>> m_lshBuckets = minHasher.createBandBuckets(wordList, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(wordList.Count, 3) / 5);

            /*
            sw.Restart();
            Console.WriteLine("\r\nListing buckets sizes ... ");
            minHasher.listBucketSizes(m_lshBuckets, pair_output_filename);
            Console.WriteLine("Listing buckets sizes in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();*/

            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<int, int, double>> pairsDictionary = minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, wordList, exclude_sim_under_threshold, null);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            sw0.Stop();
            Console.WriteLine("\r\nTook total time of: " + sw0.Elapsed.ToString("mm\\:ss\\.ff"));

            int foundPairsCount = pairsDictionary.Count;
            double prunePercentage = ((double)(possiblePairCount - foundPairsCount) / (double)possiblePairCount) * 100.0;

            Cluster<int, string> cls = new Cluster<int, string>(pairsDictionary, groundTruth);
            //cls.generateClusers1();
            //double precision_from_groundTruth = cls.calculatePrecision_fromGroundTruth();
            sw.Restart();
            double precision_from_actualSimilarity = cls.calculatePrecision_fromActualSimilarity(wordList, simThreshold);
            Console.WriteLine("Calculated precision from found pairs in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            if (sample)
            {
                sw.Restart();
                Console.WriteLine("Calculating recall from actual should be pairs:");
                Dictionary<string, Tuple<int, int, double>> actualPairsDictionary = Util.getActualPairsDictionary(wordList, simThreshold);
                double recall = Util.calculateRecall<int>(actualPairsDictionary, pairsDictionary);
                Console.WriteLine("Calculated recall from the algorithm vs pairwise-comparison in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
                sw.Stop();

                /*Dictionary<string, Tuple<int, int, double>> actualMinHashPairsDictionary = Util.getActualPairsDictionary(docMinhashes, simThreshold);
                Console.WriteLine("Calculating recall from actual MinHash pairs:");
                recall = Util.calculateRecall<int>(actualMinHashPairsDictionary, pairsDictionary);*/

                int a = 0;
            }

            Console.WriteLine(string.Format("\r\nPossible pairs count: {0}", possiblePairCount));
            Console.WriteLine(string.Format("\r\nFound pairs count: {0}", foundPairsCount));
            Console.WriteLine(string.Format("\r\nPrune percentage: {0}", prunePercentage));

            int x = 1;
            Console.ReadKey();
        }
        static void processAmazonJsonDumpFiles(bool sample = false, int sample_size = 1000)
        {
            Console.WriteLine("Amazon meta data will be made available (for research purposes) on request. Please contact Julian McAuley (julian.mcauley@gmail.com) to obtain a link.");
            //to try minhash on amazon json dump files
            string amz_json_file = @"C:\Users\maydar\Documents\Sony Backup\PROJECTS\amazon\review-dumps\test\meta_Office_Products.json.gz";
            
            string pair_output_filename = amz_json_file + "_minhashpairs.txt";


            int numHashFunctions = 130;
            double simThreshold = 0.65;
            bool exclude_sim_under_threshold = false; //vertex pairs which have estimated similarity under the threshold will be excluded if set
            //MinHasher minHasher = new MinHasher(numHashFunctions, simThreshold);
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            Dictionary<string, string[]> wordList;

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw = new Stopwatch();
            Stopwatch sw0 = new Stopwatch();

            int limit = -1;
            if (sample)
            {
                limit = sample_size;
                Console.WriteLine("Sample size: " + sample_size);
            }
            AmazonJsonInputReader amzInputReader = new AmazonJsonInputReader(amz_json_file, false, limit);

            //Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(flatInputReader.vertexLabelList);
            /*if (!sample)
                wordList = amzInputReader.productWordList;
            else
            {
                wordList = Util.getSampleFromDict(amzInputReader.productWordList, sample_size);
            }*/

            wordList = amzInputReader.productWordList;
            Console.WriteLine(string.Format("\r\nInstances count: {0}", wordList.Count));
            long possiblePairCount = PermutationsAndCombinations.nCr(wordList.Count, 2);
            Console.WriteLine(" ");

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw0.Restart();
            sw.Restart();

            //Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(flatInputReader.vertexLabelList);
            Dictionary<string, int[]> docMinhashes = minHasher.createMinhashCollection(wordList);

            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<string>> m_lshBuckets = minHasher.createBandBuckets(wordList, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(wordList.Count, 3) / 5);

            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<string, string, double>> pairsDictionary =
                minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, wordList, exclude_sim_under_threshold, null);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            sw0.Stop();
            Console.WriteLine("\r\nTook total time of: " + sw0.Elapsed.ToString("mm\\:ss\\.ff"));
            int foundPairsCount = pairsDictionary.Count;
            double prunePercentage = ((double)(possiblePairCount - foundPairsCount) / (double)possiblePairCount) * 100.0;

            Console.WriteLine("\r\nBucket pairsDictionary size: " + pairsDictionary.Count);

            Cluster<string, string> cls = new Cluster<string, string>(pairsDictionary, null);
            //cls.generateClusers1();
            //double precision_from_groundTruth = cls.calculatePrecision_fromGroundTruth();
            double precision_from_actualSimilarity = cls.calculatePrecision_fromActualSimilarity(wordList, simThreshold);

            if (sample && limit <= 50000)
            {
                sw.Restart();
                Console.WriteLine("Calculating recall from actual should be pairs:");
                Dictionary<string, Tuple<string, string, double>> actualPairsDictionary = Util.getActualPairsDictionary(wordList, simThreshold);
                double recall = Util.calculateRecall<string>(actualPairsDictionary, pairsDictionary);
                Console.WriteLine("Calculated recall from the algorithm vs pairwise-comparison in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
                sw.Stop();
                int a = 0;
            }

            Console.WriteLine(string.Format("\r\nPossible pairs count: {0}", possiblePairCount));
            Console.WriteLine(string.Format("\r\nFound pairs count: {0}", foundPairsCount));
            Console.WriteLine(string.Format("\r\nPrune percentage: {0}", prunePercentage));

            Console.ReadKey();
        }
        static void processMashableFiles(bool sample = false, int sample_size = 1000)
        {
            //to try minhash on news corpora file
            //string file = @"C:\Users\maydar\Dropbox\Semantic Study\ScabilityPaper\datasets\OnlineNewsPopularity\OnlineNewsPopularity2.csv-clean.txt";
            string file = dataset_main_location + @"\OnlineNewsPopularity\OnlineNewsPopularity2.csv-clean.txt";
            string pair_output_filename = file + "_minhashpairs.txt";


            int numHashFunctions = 130;
            double simThreshold = 0.65;
            bool exclude_sim_under_threshold = false; //vertex pairs which have estimated similarity under the threshold will be excluded if set
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            Dictionary<int, string[]> wordList;

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw0 = new Stopwatch();
            Stopwatch sw = new Stopwatch();
            int limit = -1;
            if (sample)
            {
                limit = sample_size;
                Console.WriteLine("Sample size: " + sample_size);
            }
            int[] index_locations = { 0, 1, 2 };
            string sep = @"\t";
            SepInputReader<int, string> sepInputReader = new SepInputReader<int, string>(file, index_locations, sep,
                false, limit);
            Dictionary<int, string> groundTruth = sepInputReader.groundTruth;

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw0.Restart();
            sw.Restart();

            //Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(flatInputReader.vertexLabelList);
            /*if (!sample)
                wordList = sepInputReader.wordList;
            else
            {
                wordList = Util.getSampleFromDict(sepInputReader.wordList, sample_size);
            }*/
            wordList = sepInputReader.wordList;
            Console.WriteLine(string.Format("\r\nInstances count: {0}", wordList.Count));
            long possiblePairCount = PermutationsAndCombinations.nCr(wordList.Count, 2);

            Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(wordList);

            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<int>> m_lshBuckets = minHasher.createBandBuckets(wordList, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(wordList.Count, 3) / 5);

            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<int, int, double>> pairsDictionary = minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, wordList, exclude_sim_under_threshold, null);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            sw0.Stop();
            Console.WriteLine("\r\nTook total time of: " + sw0.Elapsed.ToString("mm\\:ss\\.ff"));
            int foundPairsCount = pairsDictionary.Count;
            double prunePercentage = ((double)(possiblePairCount - foundPairsCount) / (double)possiblePairCount) * 100.0;

            Console.WriteLine("\r\nBucket pairsDictionary size: " + pairsDictionary.Count);

            Cluster<int, string> cls = new Cluster<int, string>(pairsDictionary, groundTruth);
            //cls.generateClusers1();
            double precision = cls.calculatePrecision_fromGroundTruth();
            double precision_from_actualSimilarity = cls.calculatePrecision_fromActualSimilarity(wordList, simThreshold);

            if (sample)
            {
                sw.Restart();
                Console.WriteLine("Calculating recall from actual should be pairs:");
                Dictionary<string, Tuple<int, int, double>> actualPairsDictionary = Util.getActualPairsDictionary(wordList, simThreshold);
                double recall = Util.calculateRecall<int>(actualPairsDictionary, pairsDictionary);
                Console.WriteLine("Calculated recall from the algorithm vs pairwise-comparison in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
                sw.Stop();
            }

            Console.WriteLine(string.Format("\r\nPossible pairs count: {0}", possiblePairCount));
            Console.WriteLine(string.Format("\r\nFound pairs count: {0}", foundPairsCount));
            Console.WriteLine(string.Format("\r\nPrune percentage: {0}", prunePercentage));

            Console.ReadKey();
        }

        private static void processUobmLargeFiles_InstanceMatch(bool sample = false, int sample_size = 1000)
        {
            Console.WriteLine("Processing UOBM_large ...");
            string file1 = dataset_main_location + @"\IM2016_UOBM_large\Abox1.nt";
            string file2 = dataset_main_location + @"\IM2016_UOBM_large\Abox2.nt";
            string file_gt =
                dataset_main_location + @"\IM2016_UOBM_large\refalign.rdf";
            int numHashFunctions = 256;
            double simThreshold = 0.5;


            //ground truth file
            string pair_output_filename = file1 + "_minhashpairs.txt";
            string prefix1 = "|first|", prefix2 = "|second|", sep_prefix = "-";


            bool exclude_sim_under_threshold = false;
            //vertex pairs which have estimated similarity under the threshold will be excluded if set
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            Dictionary<string, string[]> wordList;

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw = new Stopwatch();
            Stopwatch sw0 = new Stopwatch();
            int limit = -1;
            if (sample)
            {
                limit = sample_size;
                Console.WriteLine("Sample size: " + sample_size);
            }
            UobmInputReader uobmInputReader = new UobmInputReader(file1, file2, file_gt, limit, prefix1, prefix2,
                sep_prefix);
            wordList = uobmInputReader.wordList;
            Console.WriteLine(string.Format("\r\nInstances count: {0}", wordList.Count));

            //long possiblePairCount = PermutationsAndCombinations.nCr(wordList.Count, 2);
            long possiblePairCount = uobmInputReader.possiblePairsCount;

            sw0.Restart();
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw.Restart();
            Dictionary<string, int[]> docMinhashes = minHasher.createMinhashCollection(wordList);
            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<string>> m_lshBuckets = minHasher.createBandBuckets(wordList, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(wordList.Count, 3) / 5);

            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<string, string, double>> pairsDictionary =
                minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, wordList, exclude_sim_under_threshold, null,
                    true, prefix1, prefix2, sep_prefix);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " +
                              sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();
            sw0.Stop();
            Console.WriteLine("\r\nTook total time of: " + sw0.Elapsed.ToString("mm\\:ss\\.ff"));
            int foundPairsCount = pairsDictionary.Count;
            double prunePercentage = ((double)(possiblePairCount - foundPairsCount) / (double)possiblePairCount) * 100.0;

            Cluster<string, string> cls = new Cluster<string, string>(pairsDictionary, null);
            sw.Restart();
            double precision_from_actualSimilarity = cls.calculatePrecision_fromActualSimilarity(wordList, simThreshold);
            Console.WriteLine("Calculated precision from found pairs in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            sw.Restart();
            Console.WriteLine("Calculating recall from ground truth:");
            double recall = Util.calculateRecall<string>(uobmInputReader.gtPairsDictionary, pairsDictionary);
            Console.WriteLine("Calculated recall from the algorithm vs pairwise-comparison in Time : " +
                              sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();
            double fmeasure = 2 * ((precision_from_actualSimilarity * recall) / (precision_from_actualSimilarity + recall));
            Console.WriteLine("F-measure: " + fmeasure);

            Console.WriteLine(string.Format("\r\nPossible pairs count: {0}", possiblePairCount));
            Console.WriteLine(string.Format("\r\nFound pairs count: {0}", foundPairsCount));
            Console.WriteLine(string.Format("\r\nPrune percentage: {0}", prunePercentage));
            Console.ReadKey();
        }

        private static void processSpimbenchFiles_InstanceMatch(bool sample = false, int sample_size = 1000)
        {
            Console.WriteLine("Processing Spimbench_large ...");
            string file1 = dataset_main_location + @"\IM2016_Spimbench_large\Abox1.nt";
            string file2 = dataset_main_location + @"\IM2016_Spimbench_large\Abox2.nt";
            string file_gt =
                dataset_main_location + @"\IM2016_Spimbench_large\refalign.rdf";
            int numHashFunctions = 128;
            double simThreshold = 0.3;

                //ground truth file
            string pair_output_filename = file1 + "_minhashpairs.txt";
            string prefix1 = "|first|", prefix2 = "|second|", sep_prefix = "-";

            
            bool exclude_sim_under_threshold = false;
                //vertex pairs which have estimated similarity under the threshold will be excluded if set
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            Dictionary<string, string[]> wordList;

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw = new Stopwatch();
            Stopwatch sw0 = new Stopwatch();
            int limit = -1;
            if (sample)
            {
                limit = sample_size;
                Console.WriteLine("Sample size: " + sample_size);
            }
            UobmInputReader uobmInputReader = new UobmInputReader(file1, file2, file_gt, limit, prefix1, prefix2,
                sep_prefix);
            wordList = uobmInputReader.wordList;
            Console.WriteLine(string.Format("\r\nInstances count: {0}", wordList.Count));
            
            //long possiblePairCount = PermutationsAndCombinations.nCr(wordList.Count, 2);
            long possiblePairCount = uobmInputReader.possiblePairsCount;

            sw0.Restart();
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw.Restart();
            Dictionary<string, int[]> docMinhashes = minHasher.createMinhashCollection(wordList);
            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<string>> m_lshBuckets = minHasher.createBandBuckets(wordList, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(wordList.Count, 3)/5);

            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<string, string, double>> pairsDictionary =
                minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, wordList, exclude_sim_under_threshold, null,
                    true, prefix1, prefix2, sep_prefix);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " +
                              sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();
            sw0.Stop();
            Console.WriteLine("\r\nTook total time of: " + sw0.Elapsed.ToString("mm\\:ss\\.ff"));
            int foundPairsCount = pairsDictionary.Count;
            double prunePercentage = ((double)(possiblePairCount - foundPairsCount) / (double)possiblePairCount) * 100.0;

            Cluster<string, string> cls = new Cluster<string, string>(pairsDictionary, null);
            sw.Restart();
            double precision_from_actualSimilarity = cls.calculatePrecision_fromActualSimilarity(wordList, simThreshold);
            Console.WriteLine("Calculated precision from found pairs in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            sw.Restart();
            Console.WriteLine("Calculating recall from ground truth:");
            double recall = Util.calculateRecall<string>(uobmInputReader.gtPairsDictionary, pairsDictionary);
            Console.WriteLine("Calculated recall from the algorithm vs pairwise-comparison in Time : " +
                              sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();
            double fmeasure = 2*((precision_from_actualSimilarity*recall)/(precision_from_actualSimilarity + recall));
            Console.WriteLine("F-measure: " + fmeasure);

            Console.WriteLine(string.Format("\r\nPossible pairs count: {0}", possiblePairCount));
            Console.WriteLine(string.Format("\r\nFound pairs count: {0}", foundPairsCount));
            Console.WriteLine(string.Format("\r\nPrune percentage: {0}", prunePercentage));
            Console.ReadKey();
        }

        static void processNewsCorporaFiles_InstanceMatch(bool sample = false, int sample_size = 1000)
        {
            //to try minhash on news corpora file
            string file = @"C:\Users\maydar\Dropbox\Semantic Study\ScabilityPaper\datasets\news aggregator\newsCorpora.csv-clean.txt";
            string pair_output_filename = file + "_minhashpairs.txt";

            int numHashFunctions = 130;
            double simThreshold = 0.65;
            bool exclude_sim_under_threshold = false; //vertex pairs which have estimated similarity under the threshold will be excluded if set
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            Dictionary<int, string[]> wordList1, wordList2;
            Dictionary<string, string[]> wordList3;

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw = new Stopwatch();

            int[] index_locations = { 0, 1, 2 };
            string sep = @"\t";
            int limit = -1;
            if (sample)
                limit = sample_size;
            
            SepInputReader<int, string> sepInputReader1 = new SepInputReader<int, string>(file, index_locations, sep, false, limit);
            wordList1 = sepInputReader1.wordList;
            SepInputReader<int, string> sepInputReader2 = new SepInputReader<int, string>(file, index_locations, sep, false, limit);
            wordList2 = sepInputReader2.wordList;
            
            Console.WriteLine("\r\nMerging the two wordLists ... ");
            wordList3 = Util.mergeTwoWordLists(wordList1, wordList2);

            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw.Restart();
            Dictionary<string, int[]> docMinhashes = minHasher.createMinhashCollection(wordList3);
            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<string>> m_lshBuckets = minHasher.createBandBuckets(wordList3, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(wordList3.Count, 3) / 5);
            
            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<string, string, double>> pairsDictionary = minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, wordList3, exclude_sim_under_threshold, null,
                true);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Cluster<string, string> cls = new Cluster<string, string>(pairsDictionary, null);
            sw.Restart();
            double precision_from_actualSimilarity = cls.calculatePrecision_fromActualSimilarity(wordList3, simThreshold);
            Console.WriteLine("Calculated precision from found pairs in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            if (sample)
            {
                sw.Restart();
                Console.WriteLine("Calculating recall from actual should be pairs:");
                Dictionary<string, Tuple<string, string, double>> actualPairsDictionary = Util.getActualPairsDictionary(wordList3, simThreshold);
                double recall = Util.calculateRecall<string>(actualPairsDictionary, pairsDictionary);
                Console.WriteLine("Calculated recall from the algorithm vs pairwise-comparison in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
                sw.Stop();

                /*Dictionary<string, Tuple<int, int, double>> actualMinHashPairsDictionary = Util.getActualPairsDictionary(docMinhashes, simThreshold);
                Console.WriteLine("Calculating recall from actual MinHash pairs:");
                recall = Util.calculateRecall<int>(actualMinHashPairsDictionary, pairsDictionary);*/

                int a = 0;
            }

            int x = 1;

            Console.ReadKey();
        }
        static void processNumbersTest3(bool sample = false, int sample_size = 1000)
        {
            int numHashFunctions = 128;
            int universeSize = 1000;
            double simThreshold = 0.65;
            double atn = 0.05;

            MinHasher2 mh2 = new MinHasher2(numHashFunctions, simThreshold);

            NumberDocumentCreator numDocCreator2 = new NumberDocumentCreator(10, universeSize);

            int[] a1 = numDocCreator2.createDocument(universeSize);
            int[] a2 = numDocCreator2.createDocument(universeSize);

            Console.WriteLine("Actual jaccaard: " + MinHasher2.calculateJaccard(a1, a2));
            Console.WriteLine("MinHash jaccaard: " + MinHasher2.calculateJaccard(mh2.getMinHashSignature(a1), mh2.getMinHashSignature(a2)));

            return;
            MinHasher3 mh = new MinHasher3(universeSize, numHashFunctions);
            MinHasher_Buckets3 mhb = new MinHasher_Buckets3(mh, simThreshold, atn);

            NumberDocumentCreator numDocCreator = new NumberDocumentCreator(10, universeSize);

            List<int> s1 = numDocCreator.createDocument(universeSize).ToList();
            List<int> s2 = numDocCreator.createDocument(universeSize).ToList();

            Console.WriteLine("Actual jaccaard: " + Jaccard.Calc(s1, s2));
            Console.WriteLine("MinHash jaccaard: " + Jaccard.Calc(mh.GetMinHash(s1), mh.GetMinHash(s2)));
            return;
            Dictionary<int, List<int>> wordList = numDocCreator.documentCollectionList;

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            Dictionary<int, List<uint>> docMinhashes = mhb.createMinhashCollection(wordList); //minHasher.createMinhashCollection(wordList);
            double avg_diff_perc_from_actual_and_minhash_jaccard = Util.calculateMinHashFunctionsAccuracy(wordList, docMinhashes);

            /*StringDocumentCreator strDocCreator = new StringDocumentCreator(100, 10000);

            Dictionary<int, string[]> wordList2 = strDocCreator.documentCollection;

             //Now create a MinHasher object to minhash each of the documents created above
             //using 300 unique hashing functions.
             //MinHasher minHasher = new MinHasher(500, 5);
             Console.WriteLine("\r\nGenerating MinHash signatures ... ");
             Dictionary<int, int[]> docMinhashes2 = minHasher.createMinhashCollection(wordList2);
             double avg_diff_perc_from_actual_and_minhash_jaccard2 = Util.calculateMinHashFunctionsAccuracy(wordList2, docMinhashes2);
             */

            Console.ReadKey();
        }
        public static void MinHasher3TestFunc1()
        {
            List<int> inums1 = new List<int>();
            inums1.Add(10);
            inums1.Add(8);
            inums1.Add(11);
            inums1.Add(13);
            inums1.Add(2);
            inums1.Add(17);
            inums1.Add(3);
            inums1.Add(1);
            inums1.Add(19);
            inums1.Add(11);
            inums1.Add(100);
            inums1.Add(82);
            inums1.Add(115);
            inums1.Add(13);
            inums1.Add(2);
            inums1.Add(107);
            inums1.Add(3);
            inums1.Add(1);
            inums1.Add(19);
            inums1.Add(110);
            inums1.Add(10);
            inums1.Add(8);
            inums1.Add(110);
            inums1.Add(131);
            inums1.Add(2);
            inums1.Add(173);
            inums1.Add(3);
            inums1.Add(1);
            inums1.Add(19);
            inums1.Add(114);
            inums1.Add(10);
            inums1.Add(8);
            inums1.Add(11);
            inums1.Add(13);
            inums1.Add(2);
            inums1.Add(17);
            inums1.Add(3);
            inums1.Add(1);
            inums1.Add(19);
            inums1.Add(115);
            inums1.Add(10);
            inums1.Add(8);
            inums1.Add(11);
            inums1.Add(133);
            inums1.Add(2);
            inums1.Add(17);
            inums1.Add(3);
            inums1.Add(1);
            inums1.Add(19);
            inums1.Add(11);
            inums1.Add(10);
            inums1.Add(8);
            inums1.Add(11);
            inums1.Add(13);
            inums1.Add(2);
            inums1.Add(17);
            inums1.Add(3);
            inums1.Add(1);
            inums1.Add(19);
            inums1.Add(171);
            
            List<int> inums2 = new List<int>();
            inums2.Add(1);
            inums2.Add(2);
            inums2.Add(5);
            inums2.Add(9);
            inums2.Add(12);
            inums2.Add(17);
            inums2.Add(13);
            inums2.Add(11);
            inums2.Add(9);
            inums2.Add(10);
            inums2.Add(1);
            inums2.Add(2);
            inums2.Add(5);
            inums2.Add(9);
            inums2.Add(12);
            inums2.Add(17);
            inums2.Add(13);
            inums2.Add(11);
            inums2.Add(9);
            inums2.Add(10);
            inums2.Add(1);
            inums2.Add(2);
            inums2.Add(5);
            inums2.Add(9);
            inums2.Add(12);
            inums2.Add(17);
            inums2.Add(13);
            inums2.Add(151);
            inums2.Add(9);
            inums2.Add(510);
            inums2.Add(1);
            inums2.Add(2);
            inums2.Add(5);
            inums2.Add(9);
            inums2.Add(12);
            inums2.Add(17);
            inums2.Add(13);
            inums2.Add(11);
            inums2.Add(95);
            inums2.Add(10);
            inums2.Add(1);
            inums2.Add(23);
            inums2.Add(5);
            inums2.Add(9);
            inums2.Add(162);
            inums2.Add(17);
            inums2.Add(13);
            inums2.Add(11);
            inums2.Add(93);
            inums2.Add(10);
            inums2.Add(19);
            inums2.Add(23);
            inums2.Add(5);
            inums2.Add(9);
            inums2.Add(12);
            inums2.Add(17);
            inums2.Add(13);
            inums2.Add(141);
            inums2.Add(94);
            inums2.Add(10);

            int universeSize = Jaccard.unionSize(inums1, inums2);
            MinHasher3 mh = new MinHasher3(universeSize, 135);
            List<uint> hvs1 = mh.GetMinHash(inums1).ToList();
            List<uint> hvs2 = mh.GetMinHash(inums2).ToList();
            Console.WriteLine();
            Console.WriteLine("Estimated similarity: " + mh.Similarity(hvs1, hvs2));
            Console.WriteLine("Jaccard similarity: " + Jaccard.Calc(inums1, inums2));
            Console.WriteLine("done");
        }
        public static void JaccardTest1()
        {
            Dictionary<int, string> wordDict = new Dictionary<int, string>();
            wordDict.Add(1, "Word1");
            wordDict.Add(2, "Word2");
            wordDict.Add(3, "Word3");
            wordDict.Add(4, "Word4");

            List<int> doc1 = new List<int>();
            doc1.Add(2);
            doc1.Add(3);
            doc1.Add(4);
            doc1.Add(2);

            List<int> doc2 = new List<int>();
            doc2.Add(1);
            doc2.Add(5);
            doc2.Add(4);
            doc2.Add(2);

            List<int> doc3 = new List<int>();
            doc3.Add(1);

            Console.WriteLine("Jaccard: " + Jaccard.Calc(doc1, doc2));
            Console.WriteLine("Jaccard: " + Jaccard.Calc(doc1, doc1));
            Console.WriteLine("Jaccard: " + Jaccard.Calc(doc1, doc3));
        }
        static void processNumbersTest(bool sample = false, int sample_size = 1000)
        {
            //to try minhash on news corpora file
            string file = @"C:\Users\maydar\Dropbox\Semantic Study\ScabilityPaper\datasets\news aggregator\newsCorpora.csv-clean.txt";
            string pair_output_filename = file + "_minhashpairs.txt";

            int numHashFunctions = 2000;
            double simThreshold = 0.65;
            bool exclude_sim_under_threshold = false; //vertex pairs which have estimated similarity under the threshold will be excluded if set
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);
            
            NumberDocumentCreator numDocCreator = new NumberDocumentCreator(10, 100000);
            Dictionary<int, int[]> wordList = numDocCreator.documentCollection;

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(wordList);
            double avg_diff_perc_from_actual_and_minhash_jaccard = Util.calculateMinHashFunctionsAccuracy(wordList, docMinhashes);

           /*StringDocumentCreator strDocCreator = new StringDocumentCreator(100, 10000);

           Dictionary<int, string[]> wordList2 = strDocCreator.documentCollection;

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            Dictionary<int, int[]> docMinhashes2 = minHasher.createMinhashCollection(wordList2);
            double avg_diff_perc_from_actual_and_minhash_jaccard2 = Util.calculateMinHashFunctionsAccuracy(wordList2, docMinhashes2);
            */

            Console.ReadKey();
        }
        
        static void generatePairsFileForRoleSim()
        {
            //to generate pair file for role-sim jaccard
            string rdf_flat_file =
                 @"C:\Users\maydar\Dropbox\Semantic Study\ScabilityPaper\c#-code\LshMinhash\LshMinhash\input\infobox_properties_10000_flat.txt";
            string pair_output_filename = rdf_flat_file + "_minhashpairs.txt";
            int numHashFunctions = 500;
            double simThreshold = 0.5;
            bool exclude_sim_under_threshold = true; //vertex pairs which have estimated similarity under the threshold will be excluded if set
            //MinHasher minHasher = new MinHasher(numHashFunctions, simThreshold);
            MinHasher2 minHasher = new MinHasher2(numHashFunctions, simThreshold);

            Console.BufferHeight = Int16.MaxValue - 1; // ***** Alters the BufferHeight *****
            Stopwatch sw = new Stopwatch();
            //Create a collection of n docuents with a max length of 1000 tokens 
            /*NumberDocumentCreator numDocCreator = new NumberDocumentCreator(10, 10000);
            //Create a single test document 
            int[] testDoc = numDocCreator.createDocument(10000);*/

            //StringDocumentCreator strDocCreator = new StringDocumentCreator(100, 10000);
            //Create a single test document 
            //string[] testDoc = strDocCreator.createDocument(10000);
            /*int testDocIndex = 1;
            string[] testDoc = strDocCreator.documentCollection[testDocIndex];
            double entireCount = testDoc.Length;*/

            FlatInputReader flatInputReader = new FlatInputReader(rdf_flat_file);
            int testDocKey = 771;
            Console.WriteLine("\r\nTest doc key: " + testDocKey);
            int[] testDoc = flatInputReader.vertexLabelList[testDocKey];
            double entireCount = testDoc.Length;

            //Compare the test document to all items in our document collection using Jaccard 
            //similarity and no minhashing 
            Console.WriteLine("Jaccard Similarity for each entire collection:");
            sw.Restart();
            int maxSimDocKey_fromJaccard = -1;
            double maxSim_fromJaccard = -1;
            int index = 1;
            foreach (var document in flatInputReader.vertexLabelList)
            {   //these value have not been minhashed yet, but the jaccard calulation is the same...
                double jaccard = NumberDocumentCreator.calculateJaccard(testDoc, document.Value);
                entireCount += document.Value.Length;
                //Console.WriteLine("Document " + document.Key.ToString() + ": " + jaccard.ToString() + "     Time :" + sw.Elapsed.ToString("mm\\:ss\\.ff"));     //.ToString("P"));
                if (jaccard > maxSim_fromJaccard && document.Key != testDocKey)
                {
                    maxSim_fromJaccard = jaccard;
                    maxSimDocKey_fromJaccard = document.Key;
                }
                index++;
            }
            sw.Stop();
            Console.WriteLine("Regular jaccard Time :" + sw.Elapsed.ToString("mm\\:ss\\.ff"));     //.ToString("P"));
            Console.WriteLine(" ");

            //Now create a MinHasher object to minhash each of the documents created above
            //using 300 unique hashing functions.
            //MinHasher minHasher = new MinHasher(500, 5);
            Console.WriteLine("\r\nGenerating MinHash signatures ... ");
            sw.Restart();
            Dictionary<int, int[]> docMinhashes = minHasher.createMinhashCollection(flatInputReader.vertexLabelList);
            sw.Stop();
            Console.WriteLine("Generated MinHash signatures in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            //Create the test doc minhash signature
            int[] testDocMinhashSignature = minHasher.getMinHashSignature(testDoc);
            double minhashCount = testDocMinhashSignature.Length;

            //Compare the test document minhash signature to all minhash signatures  
            //in our document collection using Jaccard similarity 
            Console.WriteLine("\r\nJaccard Similarity for each Minhashed collection:");
            sw.Restart();
            int maxSimDocKey_fromJustMinHash = -1;
            double maxSim_fromJustMinHash = -1;
            index = 1;
            foreach (var document in docMinhashes)
            {
                double jaccard = MinHasher.calculateJaccard(testDocMinhashSignature, document.Value);
                //Console.WriteLine("Document " + document.Key.ToString() + ": " + jaccard.ToString() + "     Time :" + sw.Elapsed.ToString("mm\\:ss\\.ff"));
                minhashCount += document.Value.Length;
                if (jaccard > maxSim_fromJustMinHash && document.Key != testDocKey)
                {
                    maxSim_fromJustMinHash = jaccard;
                    maxSimDocKey_fromJustMinHash = document.Key;
                }
                index++;
            }
            sw.Stop();
            Console.WriteLine("\r\nMinHash jaccard Time :" + sw.Elapsed.ToString("mm\\:ss\\.ff"));

            sw.Restart();
            Console.WriteLine("\r\nCreating MinHash buckets ... ");
            Dictionary<string, HashSet<int>> m_lshBuckets = minHasher.createBandBuckets(flatInputReader.vertexLabelList, docMinhashes);
            Console.WriteLine("Created MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            sw.Restart();
            Console.WriteLine("\r\nFinding closest using MinHash buckets ... ");
            int maxSimDocKey_fromMinhashBuckets = minHasher.FindClosest(testDocKey, flatInputReader.vertexLabelList, docMinhashes, m_lshBuckets);
            Console.WriteLine("Found closest using MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nComplexity with regular jaccard lookup(estimate): " + Math.Pow(flatInputReader.vertexLabelList.Count, 3) / 5);

            sw.Restart();
            Console.WriteLine("\r\nGenerating vertex pairs using MinHash buckets ... ");
            Dictionary<string, Tuple<int, int, double>> pairsDictionary = minHasher.generateVertexPairs(m_lshBuckets, docMinhashes, flatInputReader.vertexLabelList, exclude_sim_under_threshold, pair_output_filename);
            Console.WriteLine("Generated vertex pairs using MinHash buckets in Time : " + sw.Elapsed.ToString("mm\\:ss\\.ff"));
            sw.Stop();

            Console.WriteLine("\r\nBucket pairsDictionary size: " + pairsDictionary.Count);

            Console.WriteLine("\r\nTest doc key: " + testDocKey);
            Console.WriteLine("\r\nMaxSimDocKey from Jaccard: " + maxSimDocKey_fromJaccard + "\t" + maxSim_fromJaccard);
            Console.WriteLine("MaxSimDocKey from just Minhash: " + maxSimDocKey_fromJustMinHash + "\t" + maxSim_fromJustMinHash);
            Console.WriteLine("MaxSimDocKey from Minhash buckets: " + maxSimDocKey_fromMinhashBuckets + "\t" + maxSim_fromJustMinHash);

            Console.WriteLine("\r\nEntire Integer Count: " + entireCount);
            Console.WriteLine("\r\nMinhash Integer Count: " + minhashCount);
            Console.ReadKey();
        }
    }
}
