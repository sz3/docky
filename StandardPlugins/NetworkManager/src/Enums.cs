
using System;

namespace NetworkManagerDocklet
{
	public enum DeviceType {
		Unknown = 0,
		Wired,
		Wireless
	}
		
	public enum DeviceState {
		Unknown = 0,
		Unmanaged,
		Unavailable,
		Disconnected,
		Preparing,
		Configuring,
		NeedsAuth,
		IPConfiguring,
		Active,
		Failed
	}

	public enum ConnectionOwner {
		Unknown = 0,
		User,
		System
	}
	
	public enum ConnectionType {
		Unknown = 0,
		Manaul,
		DHCP
	}
}
