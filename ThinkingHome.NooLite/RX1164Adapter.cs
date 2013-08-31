﻿
using System.Timers;
using ThinkingHome.NooLite.Common;

namespace ThinkingHome.NooLite
{
	public class RX1164Adapter : BaseAdapter
	{
		private readonly Timer timer = new Timer(100);

		private readonly object lockObject = new object();

		private ReceivedCommandData latestCommandData;

		public RX1164Adapter(bool raiseEvents = false)
		{
			timer.Elapsed += TimerElapsed;

			if (raiseEvents)
			{	
				timer.Start();
			}
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			ReceivedCommandData prev, current;

			lock (lockObject)
			{
				prev = latestCommandData;
				current = latestCommandData = ReadLatestCommand();
			}

			if (!current.Equals(prev))
			{
				System.Console.WriteLine("MOO!!!");
			}
		}

		public override int ProductId
		{
			get { return 0x05DC; }
		}

		public ReceivedCommandData ReadLatestCommand()
		{
			byte[] buf;
			device.ReadFeatureData(out buf);

			return new ReceivedCommandData(buf);
		}

		public void SendCommand(RX1164Command cmd, byte channel = 0)
		{
			var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

			data[1] = (byte)cmd;
			data[2] = channel;

			device.WriteFeatureData(data);
			System.Threading.Thread.Sleep(200);
		}

		public override void Dispose()
		{
			base.Dispose();
			timer.Stop();			
		}
	}
}
