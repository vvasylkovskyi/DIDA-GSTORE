using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Domain
{
    public class DataStoreValue
    {
        private static int max_size = 255;
        private string _val;
        public string val
        {
            get { return _val; }
            set
            {
                if (value.Length > max_size)
                {
                    throw new ArgumentException("Could not create Data Store Value. String length exceeds maximum possible length.");
                }
                else
                {
                    _val = value;
                }
            }
        }

    }
}
