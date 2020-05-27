
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Util
{
    // Sorted Dictionary class

    [Serializable]
    public class SortedDic<Tkey, Tvalue> : Dictionary<Tkey, Tvalue>
    {
        public SortedDic()
        {
        }

        protected SortedDic(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public Tvalue[] Sort(string sort = "")
        {
            // Sort the dictionnary and return sorted items

            int nbItems = this.Count;
            Tvalue[] arrayTvalue = new Tvalue[nbItems - 1 + 1];
            int numItem = 0;
            foreach (KeyValuePair<Tkey, Tvalue> line in this)
            {
                arrayTvalue[numItem] = line.Value;
                numItem += 1;
            }

            // If no sort is passed, simply return unordered items
            if (sort.Length == 0) return arrayTvalue;

            // Sorting items
            UniversalComparer<Tvalue> comp = new UniversalComparer<Tvalue>(sort);
            Array.Sort<Tvalue>(arrayTvalue, comp);
            return arrayTvalue;
        }
    }
}
