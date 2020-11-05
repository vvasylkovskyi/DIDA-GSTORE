using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;

namespace DataStoreServer.Domain
{
    public class DataStore
    {
        private Dictionary<DataStoreKey, DataStoreValue> dataStore = new Dictionary<DataStoreKey, DataStoreValue>();
        private List<DataStoreKey> readQueue = new List<DataStoreKey>();

        public DataStoreKey getCorrectKey(DataStoreKey key)
        {
            foreach (DataStoreKey objectkey in dataStore.Keys)
            {
                if (objectkey.Equals(key))
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
                    while (keyCorrect.isLocked || !readQueue[0].Equals(key))
                    {
                        Monitor.Wait(this);
                    }
                    DataStoreValue result = dataStore[keyCorrect];
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
            if (objectExists(key))
            {
                key = getCorrectKey(key);
                dataStore[key] = value;
            }
            else
            {
                dataStore.Add(key, value);
            }

        }


        public bool objectExists(DataStoreKey key)
        {
            key = getCorrectKey(key);
            if (key != null)
                return true;
            return false;
        }


        public void SetLockObject(DataStoreKey key, bool objectLock)
        {
            if (objectExists(key))
            {
                key = getCorrectKey(key);
                key.isLocked = objectLock;
                return;
            }
        }
    }


}
