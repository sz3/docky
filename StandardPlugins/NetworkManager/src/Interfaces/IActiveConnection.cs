
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface ("org.freedesktop.NetworkManager.Connection.Active")]
	public interface IActiveConnection : org.freedesktop.DBus.Properties
	{
	}
}
