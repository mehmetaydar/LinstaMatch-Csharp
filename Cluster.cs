using System;
using System.Collections.Generic;

namespace LinstaMatch
{
    public class Cluster<T1, T>
    {
        private Dictionary<string, Tuple<T1, T1, double>> pairsDictionary;
        public Dictionary<T1, string> groundTruth = new Dictionary<T1, string>();
        private double min_sim_threshold = -1;
        private List<List<T1>> setClusters = new List<List<T1>>();
        public Dictionary<T1, int> vToClusterMap = new Dictionary<T1, int>();
        private double precision_from_grondTruth;
        private double precision_from_actualSimilarity;
        public Cluster(Dictionary<string, Tuple<T1, T1, double>> pairsDictionary, Dictionary<T1, string> groundTruth, double min_sim_threshold = -1)
        {
            this.pairsDictionary = pairsDictionary;
            this.groundTruth = groundTruth;
            this.min_sim_threshold = min_sim_threshold;
            //generateClusers1();
            int x = 1;
        }

        public double calculatePrecision_fromGroundTruth() //precision from category
        {
            Tuple<T1, T1, double> t;
            T1 i, j;
            int correct_pairs =0;
            foreach (string key in pairsDictionary.Keys)
            {
                t = pairsDictionary[key];
                i = t.Item1;
                j = t.Item2;
                if (!groundTruth.ContainsKey(i) || !groundTruth.ContainsKey(j))
                    throw new Exception("Ground truth for: "+i.ToString() + " or " +j.ToString() + " not found.");
                if( groundTruth[i].ToLower().Equals( groundTruth[j].ToLower()))
                    correct_pairs++;
            }
            this.precision_from_grondTruth = (double) correct_pairs/(double) pairsDictionary.Count;
            Console.WriteLine("Precision percentage(from ground truth) is: "+ precision_from_grondTruth*100 + "%");
            return this.precision_from_grondTruth;
        }
        public double calculatePrecision_fromActualSimilarity(Dictionary<T1, T[]> documents, double threshold) //precision from real jaccard of the pairs
        {
            Tuple<T1, T1, double> t;
            T1 i, j;
            int correct_pairs = 0;
            foreach (string key in pairsDictionary.Keys)
            {
                t = pairsDictionary[key];
                i = t.Item1;
                j = t.Item2;
                if (MinHasher2.calculateJaccard(documents[i], documents[j]) >= threshold)
                    correct_pairs++;
            }
            this.precision_from_actualSimilarity = (double)correct_pairs / (double)pairsDictionary.Count;
            Console.WriteLine("Precision percentage(from actual similarity) is: " + precision_from_actualSimilarity * 100 + "%");
            return this.precision_from_actualSimilarity;
        }

        //RoleSimJaccard simulation - for our purpose not very useful
        public void generateClusers1()
        {
            //if a and b is similar then put them in the same cluster. if b and c are also similar then put(a, b, c) to the same cluster
            Tuple<T1, T1, double> t;
            bool cluster_yes = false;
            bool ci_exists, cj_exists;
            int index, i_index, j_index, b_index, s_index, k;
            List<T1> cs, ci, cj, cb;
            T1 i, j;
            foreach (string key in pairsDictionary.Keys)
            {
                t = pairsDictionary[key];
                if (min_sim_threshold <= 0)
                    cluster_yes = true;
                else if (t.Item3 >= this.min_sim_threshold)
                    cluster_yes = true;
                if(cluster_yes)
                {
                    i = t.Item1;
                    j = t.Item2;
                    ci_exists = vToClusterMap.ContainsKey(i);
                    cj_exists = vToClusterMap.ContainsKey(j);
		            if(ci_exists && !cj_exists){
			            index = vToClusterMap[i];
			            cs = setClusters[index];
			            cs.Add(j);
			            vToClusterMap[j] = index;
		            }
		            else if(cj_exists && !ci_exists)
		            {
			            index = vToClusterMap[j];
			            cs = setClusters[index];
			            cs.Add(i);
			            vToClusterMap[i] = index;
		            }
		            else if(!cj_exists && !ci_exists)
		            {
			            setClusters.Add( new List<T1>() );
			            index = setClusters.Count-1;
			            setClusters[index].Add(i);
			            setClusters[index].Add(j);
			            vToClusterMap[i] = index;
			            vToClusterMap[j] = index;
		            }
		            else//both exists then merge
		            {
			            i_index = vToClusterMap[i];
			            j_index = vToClusterMap[j];
			            if(i_index == j_index)//if they are already in the same cluster dont do anything
				            continue;
			            ci = setClusters[i_index ];
			            cj = setClusters[j_index];
			            if(ci.Count >= cj.Count)
			            {
				            cb = ci;
				            b_index = i_index;
				            s_index = j_index;
				            cs = cj;
			            }
			            else
			            {
				            cb = cj;
				            b_index = j_index;
				            s_index = i_index;
				            cs = ci;
			            }
			            //merge cs into cb. and update the map
			            for(k = 0; k< cs.Count; k++)
			            {
				            T1 vid = cs[k];
				            cb.Add( vid );
				            vToClusterMap[vid] = b_index;
			            }
			            cs.Clear();
			            //delete &cs;
		            }

                }
            }
        }
    
    
    }
}
