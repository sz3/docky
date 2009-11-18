
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface ("org.freedesktop.NetworkManager.Device.Wireless")]
	public interface IWirelessDevice : org.freedesktop.DBus.Properties
	{
		ObjectPath[] GetAccessPoints ();
		event AccessPointAddRemoveHandler AccessPointAdded;
		event AccessPointAddRemoveHandler AccessPointRemoved;
	}
	
	public delegate void AccessPointAddRemoveHandler(string objectPath);
}
