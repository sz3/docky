
using System;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;


namespace NetworkManagerDocklet
{
	
	public class DBusObject<T>
	{
		
		public DBusObject(string busName, string objectPath)
		{
			this.ObjectPath = objectPath;
			this.BusName = busName;
			
			try {
				this.BusObject = Bus.System.GetObject<T>
					(BusName, new ObjectPath (ObjectPath));
			} catch (Exception e) {
				Console.WriteLine (e.ToString ());
			}
		}

		public string BusName { get; private set; }
		public string ObjectPath { get; private set; }
		public T BusObject { get; private set; }
		
	}
}
