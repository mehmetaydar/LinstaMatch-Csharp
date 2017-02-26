//*********************************************************************************************************
//NumberDocumentCreator - generate collections of random number tokens for minhashing.
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace LinstaMatch
{

    /// <summary>
    /// This class creates collections of random numbers which are intended to simulate words / tokens parsed from a document.
    /// </summary>
    class StringDocumentCreator
    {
        public int documentMaxTokens;
        public int documentCount;
        private string[] letters;
        public Dictionary<int, string[]> documentCollection = new Dictionary<int, string[]>();

        public StringDocumentCreator(int DocumentsToCreate, int DocumentMaxTokens)
        { 
            documentMaxTokens = DocumentMaxTokens;
            documentCount = DocumentsToCreate;

            char[] alpha = "ABCDEFGHIJKLMNOPRSTUVYZ".ToCharArray();
            letters = new string[alpha.Count()];
            int i = 0;
            foreach (var c in alpha)
            {
                letters[i++] = c.ToString();
            }
            fillDocumentCollection(DocumentsToCreate);
        }

        public void fillDocumentCollection(int documentCount)
        {
            for (int i = 1; i <= documentCount; i++)
            {   //select a random number between 25% and 100% of the documentMaxTokens (simulate documents of different sizes)
                documentCollection.Add(i, createDocument(documentMaxTokens));
            }
        }

        private Random r = new Random();

        public string[] createDocument(int documentMaxTokens)
        {
            
            int minTokens = (int)(documentMaxTokens * 0.25);
            int tokenCount = r.Next(minTokens, documentMaxTokens);

            string[] tokens = new string[tokenCount];

            //create random tokens for our document
            for (int i = 0; i < tokenCount; i++)
            {
                string str = "";
                int rand_len = 3;
                for (int j = 0; j < rand_len; j++)
                {
                    int token = r.Next(0, letters.Count());
                    str += letters[token];
                }
                tokens[i] = str;
                //Console.WriteLine(str);
            }

            return tokens;
        }

        public static double calculateJaccard(string[] setA, string[] setB)
        {
            double intersection = setA.Intersect(setB).Count();
            double union = setA.Union(setB).Count();
            return (double)intersection / (double)union;
        }
    }
}
