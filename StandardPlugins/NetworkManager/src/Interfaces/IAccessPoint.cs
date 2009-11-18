
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface ("org.freedesktop.NetworkManager.AccessPoint")]
	public interface IAccessPoint : org.freedesktop.DBus.Properties
	{
	}
}

