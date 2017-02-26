//*********************************************************************************************************
//FlatInputReader - reads the flat file format used in RoleSimJaccard 
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LinstaMatch
{
    public class AmazonJsonInputReader
    {
        private string fileName;
        public int productCount; //number of vertexes(nodes) in - products
        public int wordCount; //number of properties(outgoing labels) - words
        
        public Dictionary<string, string[]> productWordList = new Dictionary<string, string[]>();//shingles
        public Dictionary<string, string[]> productWordList_actual = new Dictionary<string, string[]>();//shingles
        
        public string selectedTestDocKey = "";
        private int shingle_size = 2;
        private int limit = -1;
        private bool use_shingles = true;

        public AmazonJsonInputReader(string FileName, bool shingles = true, int limit = -1)
        {
            fileName = FileName;
            this.limit = limit;
            this.use_shingles = shingles;
            ReadFile();
            int i = 1;
        }

        public void clearProductWordList()//to save from memory
        {
            productWordList.Clear();
            productWordList_actual.Clear();            
        }
        public void ReadFile()
        {
            Console.WriteLine("Reading input amazon json file ..");
            int skip_till = -1;
            if (fileName.ToLower().Contains("amazon-meta.txt.gz"))
                skip_till = -1;
            int index = 0;
            //List<string> words = new List<string>();
            using (GZipStream gzInput = new GZipStream(new FileStream(fileName, FileMode.Open), System.IO.Compression.CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(gzInput, Encoding.UTF8))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if(index<= skip_till) continue;
                        try
                        {
                            line = line.Replace(@"\x", "");
                            //Console.WriteLine(line);
                            JObject o = JObject.Parse(line);

                            string asin = (string) o["asin"];
                            if (index == 1)
                                selectedTestDocKey = asin;
                            string title = (string) o["title"];
                            string desc = (string) o["description"];
                            //Console.WriteLine("asin: " +asin);
                            //Console.WriteLine("title: "+title);
                            //Console.WriteLine("description: " + desc);
                            if (productWordList_actual.ContainsKey(asin))
                                continue;
                            //words.Clear();
                            string terms = title + " " + desc;
                            terms = terms.Trim();
                            terms = terms.Replace(",", "");
                            productWordList_actual.Add(asin, terms.Split(' '));

                            //adding shingles
                            if (use_shingles)
                            {
                                terms = title + " " + desc;
                                terms = terms.Trim();
                                terms = terms.Replace(",", "");
                                string substr;
                                Dictionary<string, int> shingles_added = new Dictionary<string, int>();
                                for (int j = 0; j < terms.Length && j + shingle_size <= terms.Length; j++)
                                {
                                    substr = terms.Substring(j, shingle_size);
                                    if (substr.Length == shingle_size && !shingles_added.ContainsKey(substr))
                                        shingles_added[substr] = 1;
                                }
                                string[] vals_shingles = new string[shingles_added.Count];
                                int i = 0;
                                foreach (KeyValuePair<string, int> entry in shingles_added)
                                {
                                    vals_shingles[i++] = entry.Key;
                                }
                                productWordList.Add(asin, vals_shingles);
                            }
                            else
                            {
                                productWordList = productWordList_actual;
                            }
                            index++;
                        }
                        catch (Exception exxException)
                        {
                            
                        }
                        if(index%1000 == 0)
                            Console.Write(".");
                        if (limit > 0)
                        {
                            if (index >= limit)
                                break;
                        }
                        //int i = 1;
                    }
                }
                Console.WriteLine("Finished reading input amazon json file");
            }
        }
    }
}
