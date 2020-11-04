using Grpc.Net.Client;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

delegate void ThrWork();

namespace DataStoreServer
{
	class ThrPool
	{
		private CircularBuffer<ThrWork> buf;
		private Thread[] pool;
		public ThrPool(int thrNum, int bufSize)
		{
			buf = new CircularBuffer<ThrWork>(bufSize);
			pool = new Thread[thrNum];
			for (int i = 0; i < thrNum; i++)
			{
				pool[i] = new Thread(new ThreadStart(consomeExec));
				pool[i].Start();
			}
		}

		public void AssyncInvoke(ThrWork action)
		{
			buf.Produce(action);

			Console.WriteLine("Submitted action");
		}

		public void consomeExec()
		{
			while (true)
			{
				ThrWork tw = buf.Consume();
				tw();
			}
		}
	}


}
