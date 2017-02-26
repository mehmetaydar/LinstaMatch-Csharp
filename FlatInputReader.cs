//*********************************************************************************************************
//FlatInputReader - reads the flat file format used in RoleSimJaccard 
//*********************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;

namespace LinstaMatch
{
    public class FlatInputReader
    {
        private string GraphFileName;
        /*each v is the vertex id and its list of properties in the dictionary */
        public int vertexCount; //number of vertexes(nodes) in the rdf
        public int labelCount; //number of properties(outgoing labels) in the rdf
        public int Triple_Count_From_Triple_File; //number of triples in the RDF
        public Dictionary<int, int[]> vertexLabelList = new Dictionary<int, int[]>();

        public FlatInputReader(string GraphFileName_)
        {
            GraphFileName = GraphFileName_;
            ReadFlatFile();
        }

        /*
         * 	Reads the graph givent a flat file input
	     *  Populates graph data such as vl(vertexes in the graph) with edge_in and edge out maps, ll(properties in the graph)
         */

        public void ReadFlatFile()
        {
            Console.WriteLine("Reading input file ...");
            /*
	        format:
	        graph_for_greach
	        vsize
	        tsize
	        v_id: <l_id= vn1 vn2 ..>#
	        */
            //flattened format
            string buf, buf2, token;
            StreamReader inn = new StreamReader(this.GraphFileName);
            token = buf = inn.ReadLine();
            if (token != "graph_for_greach")
            {
                Console.WriteLine( "BAD FILE FORMAT!");
                return;
            }
            buf = inn.ReadLine();
            vertexCount = int.Parse(buf);
            buf = inn.ReadLine();
            labelCount = int.Parse(buf);
            buf = inn.ReadLine();
            Triple_Count_From_Triple_File = int.Parse(buf);

            string sub, sub2;
            int idx; //pos
            int sid = 0; //vertex id
            int did = 0; // vertex id reached by lid
            int lid = 0; //label id

            List<int> lidList = new List<int>();
            while ( (buf = inn.ReadLine()) != null)
            {
                lidList.Clear();
                //buf = sid: lid= did1 did2 did3: lid= did4 did5 did6#
                idx = buf.IndexOf(":");
                if (idx < 0)
                    continue;
                sub = buf.Substring(0, idx);
                sid = int.Parse(sub);
                //sid | sid
                buf = buf.Remove(0, idx + 2);
                //buf | lid= did1 did2 did3: lid= did4 did5 did6#

                while ((idx = buf.IndexOfAny(":#".ToCharArray())) > -1)
                {
                    sub = buf.Substring(0, idx);
                    //sub | lid= did1 did2 did3
                    buf = buf.Remove(0, Math.Min(idx + 2, buf.Length ) );
                    //buf | lid= did4 did5 did6#
                    idx = sub.IndexOf("=");
                    if (idx < 0)
                        continue;
                    sub2 = sub.Substring(0, idx);
                    //sub2 = lid
                    lid = int.Parse(sub2);
                    //lid = lid
                    lidList.Add(lid);
                    sub = sub.Remove(0, idx + 2);
                    //sub | did1 did2 did3
                    do
                    {
                        idx = sub.IndexOf(" ");
                        if (idx == -1)
                            idx = sub.Length;
                        sub2 = sub.Substring(0, idx);
                        //sub2 = did1
                        did = int.Parse(sub2);
                        //did = did1
                        sub = sub.Remove(0, Math.Min(idx + 1, sub.Length));
                        //sub = did2 did3

                        //addTriple( /*tindex ,*/sid, lid, did); //mehmet,  addEdge is handled inside this function

                    } while (sub != "");
                }

                vertexLabelList.Add(sid, lidList.ToArray());

                ++sid; //we may not need this, we are already parsing it from the text file
            }
        }
    }
}
