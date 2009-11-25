
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface ("org.freedesktop.NetworkManager.IP4Config")]
	public interface IIP4Config : org.freedesktop.DBus.Properties
	{
	}
}
