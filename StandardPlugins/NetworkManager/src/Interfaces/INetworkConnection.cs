
using System;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	[Interface("org.freedesktop.NetworkManagerSettings.Connection")]
	public interface INetworkConnection 
	{
		IDictionary<string, IDictionary<string, object>> GetSettings();
		event ConnectionRemovedHandler Removed;
	}
			
	public delegate void ConnectionRemovedHandler ();
}
