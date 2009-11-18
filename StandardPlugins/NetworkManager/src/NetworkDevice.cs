
using System;
using System.Net;
using System.Linq;

using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{

	public class NetworkDevice : DBusObject<INetworkDevice>
	{
		
		const string NMBusName = "org.freedesktop.NetworkManager";
		
		public delegate void DeviceStateChangedHandler (object o, DeviceStateChangedArgs args);
		
		public event DeviceStateChangedHandler StateChanged;
		
		public NetworkDevice(string objectPath) : base (NMBusName, objectPath)
		{
			BusObject.StateChanged += OnStateChanged;
			SetIPs ();
		}

		void OnStateChanged (DeviceState new_state, DeviceState old_state, uint reason)
		{
			if (new_state == DeviceState.Active)
				SetIPs ();
			DeviceStateChangedHandler handler = StateChanged;
			if (handler != null) {
				DeviceStateChangedArgs args = new DeviceStateChangedArgs ();
				args.NewState = new_state;
				args.OldState = old_state;
				args.Reason = reason;
				handler (this, args);
			}
		}

		public IPAddress IP4Address { get; private set; }
		public IPAddress PrimaryDNS { get; private set; }
		public IPAddress Gateway { get; private set; }
		public IPAddress SubnetMask { get; private set; }
		
		private DBusObject<IIP4Config> IP4Config { get; set; }
		
		public ConnectionType ConType { get; private set; }
		
		public DeviceType DType {
			get { return (DeviceType) Enum.ToObject (typeof (DeviceType), BusObject.Get (BusName, "DeviceType")); }
		}
		
		public DeviceState State {
			get	{ return (DeviceState) Enum.ToObject (typeof (DeviceType), BusObject.Get (BusName, "State")); }
		}
		
		private void SetIPs ()
		{
			if (this.State == DeviceState.Active)
			{
				if (BusObject.Get (BusName, "Dhcp4Config").ToString () != "/")
					this.ConType = ConnectionType.Manaul;
				else
					this.ConType = ConnectionType.DHCP;
				this.IP4Config = new DBusObject<IIP4Config> (NMBusName, BusObject.Get (BusName, "Ip4Config").ToString ());
				this.IP4Address = new IPAddress (long.Parse (BusObject.Get (BusName, "Ip4Address").ToString ()));
				uint[][] Addresses = (uint[][]) IP4Config.BusObject.Get (IP4Config.BusName, "Addresses");
				this.Gateway = new IPAddress (Addresses[0][2]);
				this.SubnetMask = ConvPrefixToIp ((int) Addresses[0][1]);
				uint[] NameServers = (uint[]) IP4Config.BusObject.Get (IP4Config.BusName, "Nameservers");
				if (NameServers.Length > 0)
					this.PrimaryDNS = new IPAddress (NameServers[0]);
				else
					this.PrimaryDNS = null;
			} else {
				this.IP4Config = null;
				this.ConType = ConnectionType.Unknown;
				this.IP4Address = null;
				this.PrimaryDNS = null;
				this.Gateway = null;
				this.SubnetMask = null;
			}
		}
		
		private IPAddress ConvPrefixToIp (int prefix)
		{
			uint IP = 0;
			while (prefix > 0) {
				prefix--;
				IP += (uint) (1 << prefix);
			}
			return new IPAddress (IP);
		}
	}
}
