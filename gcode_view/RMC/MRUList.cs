using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMC
{
    public class MRUList
    {
        public List<string> Items;

        public MRUList()
        {
            Items = new List<string>();
        }

        public void Add (string s)
        {
            if (Items == null)
                Items = new List<string>();

            if (Items.Contains(s))
            {
                Items.Remove(s);
                Items.Insert(0, s);
            }
            else
                Items.Insert(0, s);
        }

    }
}
