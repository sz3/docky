
using System;

namespace NetworkManagerDocklet
{
	
	public class DeviceStateChangedArgs : EventArgs
	{
		public DeviceState NewState;
		public DeviceState OldState;
		public uint Reason;
	}
}
