
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface ("org.freedesktop.NetworkManager")]
	public interface INetManager : org.freedesktop.DBus.Properties
	{
		ObjectPath[] GetDevices ();
		ObjectPath ActivateConnection (string serviceName, ObjectPath connection, ObjectPath device, ObjectPath specificObject);
		uint state ();
	}
}
