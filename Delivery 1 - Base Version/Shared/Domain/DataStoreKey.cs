using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Shared.Domain
{
    public class DataStoreKey : IEquatable<DataStoreKey>
    {
        public int partition_id { get; set; }
        public long object_id { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (obj.GetType() != this.GetType()) return false;

            // Call the implementation from IEquatable
            return Equals((DataStoreKey)obj);
        }

        public bool Equals(DataStoreKey other)
        {
            if (this.partition_id != other.partition_id)
                return false;

            if (this.object_id != other.object_id)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            // Constant because equals tests mutable member.
            // This will give poor hash performance, but will prevent bugs.
            return 0;
        }
    }
}
