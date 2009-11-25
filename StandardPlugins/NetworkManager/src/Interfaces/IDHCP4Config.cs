
using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface ("org.freedesktop.NetworkManager.DHCP4Config")]
	public interface IDHCP4Config : org.freedesktop.DBus.Properties
	{
	}
}