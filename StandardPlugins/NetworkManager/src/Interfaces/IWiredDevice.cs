
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{

	[Interface ("org.freedesktop.NetworkManager.Device.Wired")]
	public interface IWiredDevice : org.freedesktop.DBus.Properties
	{
		
	}
}
