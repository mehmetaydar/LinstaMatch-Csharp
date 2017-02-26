//*********************************************************************************************************
//TabInputReader - reads the tabbed file - example: Corpora news input
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace LinstaMatch
{
    //T1 = key type, T = value type
    public class SepInputReader<T1, T>
    {
        private string fileName;
        public int Count; //number of vertexes(nodes) in - products - news
        public Dictionary<T1, T[]> wordList = new Dictionary<T1, T[]>(); //shingles
        public Dictionary<T1, T[]> wordList_actual = new Dictionary<T1, T[]>();
        public Dictionary<T1, string> groundTruth = new Dictionary<T1, string>();
        public T1 selectedTestDocKey = default(T1);
        private string sep;
        private int id_index = 0;
        private int text_index = 1;
        private int category_index = 2;
        
        private int shingle_size = 2;
        private int limit = -1;
        private bool use_shingles = true;
        public SepInputReader(string FileName, int[]index_locations, string sep = @"\t", bool shingles = true, int limit = -1)
        {
            fileName = FileName;
            this.sep = sep;
            id_index = index_locations[0];
            text_index = index_locations[1];
            category_index = index_locations[2];
            this.limit = limit;
            this.use_shingles = shingles;
            ReadFile();
        }

        public void ReadFile()
        {
            Console.WriteLine("Reading tabbed input file ..");
            int skip_till = 0;
            int i = 0;
            int index = 0;
            String line;
            StreamReader inn = new StreamReader(this.fileName);
            String[] input;
            T1 id;
            T val = default(T);
            string val_str;
            string[] val_str_array;
            while ((line = inn.ReadLine()) != null)
            {
                input = Regex.Split(line, sep );
                id = Convert<T1>(input[id_index]);
                if (index == 0)
                    selectedTestDocKey = id;
                if (wordList.ContainsKey(id))
                    continue;
                
                //adding actual
                val_str = input[text_index].Trim().Replace("  ", " ");
                val_str_array = val_str.Split(' ');
                T[] vals = new T[val_str_array.Length];
                for (i=0; i< val_str_array.Length;i++)
                {
                    vals[i] = Convert<T>(val_str_array[i]);
                }
                wordList_actual.Add(id, vals);

                //adding shingles
                if (use_shingles)
                {
                    val_str = input[text_index].Trim().Replace(" ", "").ToLower();
                    string substr;
                    Dictionary<string, int> shingles_added = new Dictionary<string, int>();
                    for (int j = 0; j < val_str.Length && j + shingle_size <= val_str.Length; j++)
                    {
                        substr = val_str.Substring(j, shingle_size);
                        if (substr.Length == shingle_size && !shingles_added.ContainsKey(substr))
                            shingles_added[substr] = 1;
                    }
                    T[] vals_shingles = new T[shingles_added.Count];
                    i = 0;
                    foreach (KeyValuePair<string, int> entry in shingles_added)
                    {
                        vals_shingles[i++] = Convert<T>(entry.Key);
                    }
                    wordList.Add(id, vals_shingles);
                }
                else
                {
                    wordList = wordList_actual;
                }
                if (!groundTruth.ContainsKey(id))
                    groundTruth.Add(id, input[category_index]);
                index++;
                
                if (limit > 0)
                {
                    if (index >= limit)
                        break;
                }
            }
            Console.WriteLine("Finished reading file.");
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
