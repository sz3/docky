
using System;
using System.Linq;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	public class ConnectionManager
	{
		
		[Interface ("org.freedesktop.NetworkManagerSettings")]
		public interface IConnectionManager
		{
			string[] ListConnections ();
			event ConnectionAddedHandler NewConnection;
		}
	
		public delegate void ConnectionAddedHandler (string objectPath);
		
		const string SettingsObjectPath = "/org/freedesktop/NetworkManagerSettings";
		const string SystemBus = "org.freedesktop.NetworkManagerSystemSettings";
		const string UserBus = "org.freedesktop.NetworkManagerUserSettings";
		
		public ConnectionManager()
		{
			SystemConnectionManager = new DBusObject<IConnectionManager> (SystemBus,  SettingsObjectPath);
			//SystemConnectionManager.BusObject.NewConnection += OnConnectionAdded;
			UserConnectionManager = new DBusObject<IConnectionManager> (UserBus,  SettingsObjectPath);
			//UserConnectionManager.BusObject.NewConnection += OnConnectionAdded;
			
			UserConnections = new List<NetworkConnection> ();
			SystemConnections = new List<NetworkConnection> ();
			
			UpdateConnections ();
			//this workaround is necessary because NM emits bad signals, multiple times with bad data.
			GLib.Timeout.Add (1000*60*5, delegate { UpdateConnections(); return true; });
		}
		
		DBusObject<IConnectionManager> SystemConnectionManager { get; set; }
		DBusObject<IConnectionManager> UserConnectionManager { get; set; }
		public List<NetworkConnection> UserConnections { get; private set; }
		public List<NetworkConnection> SystemConnections { get; private set; }
		public IEnumerable<NetworkConnection> AllConnections {
			get { return UserConnections.Union (SystemConnections); }
		}
		
		void PrintConnections ()
		{
			Console.WriteLine ("UserConnections:");
			Console.WriteLine ("Total wireless connections {0}", this.UserConnections.OfType<WirelessConnection> ().Count ());
			Console.WriteLine ("Total wired connections {0}", this.UserConnections.OfType<WiredConnection> ().Count ());
			foreach (NetworkConnection con in UserConnections.ToArray ())
				Console.WriteLine (" --> " + con.ConnectionName);
			Console.WriteLine ("System:");
			Console.WriteLine ("Total wireless connections {0}", this.SystemConnections.OfType<WirelessConnection> ().Count ());
			Console.WriteLine ("Total wired connections {0}", this.SystemConnections.OfType<WiredConnection> ().Count ());
			foreach (NetworkConnection con in SystemConnections.ToArray ())
				Console.WriteLine (" --> " + con.ConnectionName);
		}
		
		//Commented because of some oddity in NetworkManager that emits these signals multiple times with erroneous data.
		/*
		void OnConnectionAdded (string objectPath)
		{
			Console.WriteLine ("Connection added: {0}", objectPath);
		}

		void OnNetworkConnectionRemoved (object o, NetworkConnection.NetworkConnectionRemovedArgs args)
		{
			Console.WriteLine ("connection removed: {0}", args.ConnectionName);
		}
		*/
		
		public void UpdateConnections ()
		{
			lock (SystemConnections) {
				SystemConnections.Clear ();
				foreach (string con in SystemConnectionManager.BusObject.ListConnections ())
				{
					NetworkConnection connection = new NetworkConnection (SystemBus, con, ConnectionOwner.System);
					if (connection.Settings.ContainsKey ("802-11-wireless"))
						connection = new WirelessConnection (SystemBus, con, ConnectionOwner.System);
					else if (connection.Settings.ContainsKey ("802-3-ethernet"))
						connection = new WiredConnection (SystemBus, con, ConnectionOwner.System);
					else 
						continue;
					
					Console.WriteLine ("adding {0} as {1}", connection.ConnectionName, connection.GetType ().ToString ());
					//connection.ConnectionRemoved += OnNetworkConnectionRemoved;
					SystemConnections.Add (connection);
				}
			}
			
			lock (UserConnections) {
				UserConnections.Clear ();
				foreach (string con in UserConnectionManager.BusObject.ListConnections ())
				{
					NetworkConnection connection = new NetworkConnection (UserBus, con, ConnectionOwner.User);
					if (connection.Settings.ContainsKey ("802-11-wireless"))
						connection = new WirelessConnection (UserBus, con, ConnectionOwner.User);
					else if (connection.Settings.ContainsKey ("802-3-ethernet"))
						connection = new WiredConnection (UserBus, con, ConnectionOwner.User);
					else 
						continue;
					
					Console.WriteLine ("adding {0} as {1}", connection.ConnectionName, connection.GetType ().ToString ());
					//connection.ConnectionRemoved += OnNetworkConnectionRemoved;
					UserConnections.Add (connection);
				}
			}
		}
	}
}
