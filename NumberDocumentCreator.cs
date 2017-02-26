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
    class NumberDocumentCreator
    {
        public int documentMaxTokens;
        public int documentCount;
        public Dictionary<int, int[]> documentCollection = new Dictionary<int, int[]>();
        public Dictionary<int, List<int>> documentCollectionList = new Dictionary<int, List<int>>();

        public NumberDocumentCreator(int DocumentsToCreate, int DocumentMaxTokens)
        { 
            documentMaxTokens = DocumentMaxTokens;
            documentCount = DocumentsToCreate;
            fillDocumentCollection(DocumentsToCreate);
        }

        public void fillDocumentCollection(int documentCount)
        {
            for (int i = 1; i <= documentCount; i++)
            {   //select a random number between 25% and 100% of the documentMaxTokens (simulate documents of different sizes)
                int[] doc = createDocument(documentMaxTokens);
                documentCollection.Add(i, doc);
                documentCollectionList.Add(i, doc.ToList());
            }
        }
        

        private Random r = new Random();

        public int[] createDocument(int documentMaxTokens)
        {
            
            int minTokens = (int)(documentMaxTokens * 0.25);
            int tokenCount = r.Next(minTokens, documentMaxTokens);

            int[] tokens = new int[tokenCount];

            //create random tokens for our document
            for (int i = 0; i < tokenCount; i++)
            {
                int token = r.Next(0,documentMaxTokens);
                tokens[i] = token;    
            }
            return tokens;
        }

        public static double calculateJaccard(int[] setA, int[] setB)
        {
            double intersection = setA.Intersect(setB).Count();
            double union = setA.Union(setB).Count();
            return (double)intersection / (double)union;
        }
    }
}
