
using System;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	public class NetworkConnection : DBusObject<INetworkConnection>
	{
		
		public class NetworkConnectionRemovedArgs : EventArgs
		{
			public string ConnectionName;
		}
		
		public delegate void ConnectionRemovedHandler (object o, NetworkConnectionRemovedArgs args);
		
		public event ConnectionRemovedHandler ConnectionRemoved;		
		
		public NetworkConnection(string busName, string objectPath, ConnectionOwner owner) : base (busName, objectPath)
		{
			this.Owner = owner;
			//Workaround for bad signals from NM
			//BusObject.Removed += OnDeviceRemoved;
		}

		void OnDeviceRemoved()
		{
			ConnectionRemovedHandler handler = ConnectionRemoved;
			if (handler != null) {
				NetworkConnectionRemovedArgs args = new NetworkConnectionRemovedArgs ();
				args.ConnectionName = this.ConnectionName;
				handler (this, args);
			}
		}
		
		public ConnectionOwner Owner { get; private set; }
		
		public IDictionary<string, IDictionary<string, object>> Settings {
			get { return BusObject.GetSettings (); }
		}
		
		private IDictionary<string, object> Connection {
			get { return this.Settings["connection"]; }
		}
		
		public string ConnectionName {
			get { return this.Connection["id"].ToString (); }
		}
	}
}
