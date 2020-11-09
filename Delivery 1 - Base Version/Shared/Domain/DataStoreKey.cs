using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Shared.Domain
{
    public class DataStoreKey : IEquatable<DataStoreKey>
    {
        // attributes read-only so we can get a proper custom hash function
        // explanation:
        // the hash needs to be the same over the lifetime of the object, and the
        // simplest way to do that is to make the attributes read-only

        public String partition_id { get; }
        public long object_id { get; }

        public bool isLocked { get; set; }

        public DataStoreKey(String partition_id, long object_id)
        {
            this.partition_id = partition_id;
            this.object_id = object_id;
            this.isLocked = false;
        }

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
            // Making it return 0 will give poor hash performance, but will prevent bugs.
            int hash = 17;
            hash = hash * 23 + this.partition_id.GetHashCode();
            hash = hash * 23 + this.object_id.GetHashCode();
            return hash;
        }
    }
}
