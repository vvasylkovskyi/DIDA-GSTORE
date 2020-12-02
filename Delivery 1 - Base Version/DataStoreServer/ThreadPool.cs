using Grpc.Net.Client;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public delegate void ThrWork();

namespace DataStoreServer
{
	public class ThrPool
	{
		private CircularBuffer<ThrWork> buf;
		private Thread[] pool;
		private bool _isFreeze = false;
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

			Console.WriteLine("Submitted action to thread pool");
		}

		public void consomeExec()
		{
			while (true)
			{
				// instantiate delegate
				ThrWork tw = buf.Consume();
				getPermission();
				// call the delegate
				tw();
			}
		}

		public void getPermission() {
			lock (this) {
				while (_isFreeze) {
					Monitor.Wait(this);
				}
			}
		}

		public void setFreeze(bool f) {
			lock (this) {
				_isFreeze = f;
				Monitor.PulseAll(this);
			}
		}
	}

}
