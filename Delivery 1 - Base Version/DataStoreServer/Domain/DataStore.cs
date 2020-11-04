using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DataStoreServer.Domain
{
    class DataStore
    {
        private Dictionary<DataStoreKey, DataStoreValue> data = new Dictionary<DataStoreKey, DataStoreValue>();
        private List<DataStoreKey> readQueue = new List<DataStoreKey>();

        private DataStoreKey getCorrectKey(DataStoreKey key)
        {
            foreach (DataStoreKey objectkey in data.Keys)
            {
                if (key.object_id == objectkey.object_id && key.partition_id == objectkey.partition_id)
                {
                    return objectkey;
                }
            }
            return null;
        }

        public DataStoreValue getObject(DataStoreKey key)
        {
            DataStoreKey keyCorrect = getCorrectKey(key);
            if (keyCorrect != null)
            {
                lock (this)
                {
                    readQueue.Add(key);
                    while (keyCorrect.IsLocked || !readQueue[0].Equals(key))
                    {
                        Monitor.Wait(this);
                    }
                    DataStoreValue result = data[keyCorrect];
                    readQueue.RemoveAt(0);
                    Monitor.PulseAll(this);
                    return result;
                }
            }
            else
                throw new Exception("Object does not exist");
        }

        public void CreateNewOrUpdateExisting(DataStoreKey key, DataStoreValue value)
        {
            key = getCorrectKey(key);
            if (key != null)
                data[key] = value;
            else
            {
                DataStoreKey newKey = new DataStoreKey(key.partition_id, key.object_id, true);
                data.Add(newKey, value);
            }

        }

        public bool objectExists(DataStoreKey key)
        {
            key = getCorrectKey(key);
            if (key != null)
                return true;
            return false;
        }


        public void setLockObject(DataStoreKey key, bool objectLock)
        {
            foreach (DataStoreKey objectkey in data.Keys)
            {
                if (key.object_id == objectkey.object_id && key.partition_id == objectkey.partition_id)
                {
                    objectkey.IsLocked = objectLock;
                    return;
                }
            }
        }
    }
}
