//*********************************************************************************************************
//UobmInputReader - read for instance match of two nt formatted rdf files with ground truth provided
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LinstaMatch
{
    public class UobmInputReader
    {
        private string file1;
        private string file2;
        private string file_gt;
        public int Count; //number of vertexes(nodes) in - products - news
        public Dictionary<string, string[]> wordList = new Dictionary<string, string[]>();
        //public Dictionary<string, string[]> wordList2;
        public Dictionary<string, Tuple<string, string, double>> gtPairsDictionary;
        public int possiblePairsCount;

        private int shingle_size = 2;
        private int limit = -1;
        private string prefix1;
        private string prefix2;
        private string sep_prefix;

        public UobmInputReader(string file1, string file2, string file_gt , int limit = -1, string prefix1 = "|first|", string prefix2 = "|second|", string sep_prefix = "-")
        {
            this.file1 = file1;
            this.file2 = file2;
            this.file_gt = file_gt;
            this.limit = limit;
            this.prefix1 = prefix1;
            this.prefix2 = prefix2;
            this.sep_prefix = sep_prefix;
            ReadFiles();
        }

        private void ReadGraph(string file, string key_prefix)
        {
            int index = 0;
            Dictionary<string, string> wstring = new Dictionary<string, string>();
            INode s, p, o;
            IGraph g1 = new Graph();
            FileLoader.Load(g1, file);
            string vals_str;            
            string id;
            string key;
            foreach (Triple t in g1.Triples)
            {
                //Console.WriteLine("\r\n" + t.ToString());
                s = t.Subject;
                p = t.Predicate;
                o = t.Object;
                id = s.ToString().ToLower().Trim();
                key = key_prefix + sep_prefix + id;
                vals_str = wstring.ContainsKey(key) ? wstring[key] : "";
                    
                vals_str +=" "+ p.ToString();
                if (o.NodeType == NodeType.GraphLiteral)
                {
                    throw new Exception("I don't know what to do here: GraphLiteral");
                }
                else if (o.NodeType == NodeType.Literal)
                {
                    vals_str += " " + Util.RemoveSpecialCharacters( o.ToString() ); 
                }
                else if (o.NodeType == NodeType.Uri)
                {
                    vals_str += " " + o.ToString();
                }
                else if (o.NodeType == NodeType.Variable)
                {
                    throw new Exception("I don't know what to do here: Variable");
                }
                wstring[key] = vals_str;
                index++;
                if (limit > 0)
                {
                    if (wstring.Count >= limit)
                        break;
                }
            }
            //.ToLower().Trim().Replace("  ", " ")
            g1.Dispose();
            g1 = null;
            string[] val_str_array;
            foreach (var v in wstring)
            {
                val_str_array = v.Value.Replace("  ", " ").Trim().ToLower().Split(' ').Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                wordList.Add(v.Key, val_str_array);
            }
            wstring.Clear();
            wstring = null;
        }

        private void ReadGroundTruth()
        {
            IGraph g1 = new Graph();
            FileLoader.Load(g1, file_gt);
            string vals_str;            
            string id;
            int i = -100;
            gtPairsDictionary = new Dictionary<string, Tuple<string, string, double>>();
            string key1="", key2="", pair_key="";
            double pair_sim;
            foreach (Triple t in g1.Triples)
            {
                //Console.WriteLine("\r\n" + t.ToString());
                if (t.Object.ToString().ToLower().EndsWith("/alignmentcell") || t.Object.ToString().ToLower().EndsWith("/alignment#cell"))
                {
                    i = 0;
                }
                if(i == 1)//reading first
                    key1 = prefix1 + sep_prefix + t.Object.ToString().ToLower().Trim();
                else if (i == 2)//reading second
                    key2 = prefix2 + sep_prefix + t.Object.ToString().ToLower().Trim();
                else if (i == 3)
                {
                    //reading measure, it is always 1 in here
                    pair_sim = 1.0;
                    pair_key = Util.getKeyFromPair(key1, key2);
                    if(!gtPairsDictionary.ContainsKey(pair_key))
                        gtPairsDictionary.Add(pair_key, new Tuple<string, string, double>(key1, key2, pair_sim));
                }
                i++;
            }
        }
        public void ReadFiles()
        {
            Console.WriteLine("Reading rdf nt files with ground truth...");
            ReadGroundTruth();
            ReadGraph(file1, prefix1);
            int count1 = wordList.Count;
            ReadGraph(file2, prefix2); // the two files are merged to one wordList ( belongs to the object)
            int count2 = wordList.Count;
            this.possiblePairsCount = count1*(count2 - count1); //cartesian
            Console.WriteLine("Finished reading files.");
        }

        public static T Convert<T>(string input)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null)
            {
                //Cast ConvertFromString(string text) : object to (T)
                return (T)converter.ConvertFromString(input);
            }
            return default(T);
        }
    }
}
