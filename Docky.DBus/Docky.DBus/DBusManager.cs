//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using org.freedesktop.DBus;
using NDesk.DBus;

using Docky.Items;
using Docky.Services;

namespace Docky.DBus
{
	public class DBusManager
	{
		const string BusName        = "org.gnome.Docky";
		const string DockyPath      = "/org/gnome/Docky";
		const string ItemsPath      = "/org/gnome/Docky/Items";
		
		public event Action QuitCalled;
		public event Action SettingsCalled;
		public event Action AboutCalled;
		
		static DBusManager manager;
		public static DBusManager Default {
			get {
				return manager;
			}
		}
		
		static DBusManager ()
		{
			manager = new DBusManager ();
		}

		DockyDBus docky;
		Dictionary<AbstractDockItem, DockyDBusItem> item_dict;
		
		internal IEnumerable<AbstractDockItem> Items {
			get {
				return item_dict.Keys;
			}
		}
		
		private DBusManager ()
		{
		}
		
		public void Initialize ()
		{
			Bus bus = Bus.Session;
			
			if (bus.RequestName (BusName) != RequestNameReply.PrimaryOwner) {
				Log<DBusManager>.Error ("Bus Name is already owned");
				return;
			}
			
			item_dict = new Dictionary<AbstractDockItem, DockyDBusItem> ();
			
			ObjectPath dockyPath = new ObjectPath (DockyPath);
			docky = new DockyDBus ();
			docky.QuitCalled += HandleQuitCalled;
			docky.SettingsCalled += HandleSettingsCalled;
			docky.AboutCalled += HandleAboutCalled;
			
			bus.Register (dockyPath, docky);
			
			DockServices.Helpers.HelperStatusChanged += delegate(object sender, HelperStatusChangedEventArgs e) {
				// if a script has stopped running, trigger a refresh
				if (!e.IsRunning)
					ForceRefresh ();
			};
		}
		
		public void ForceRefresh ()
		{
			foreach (DockyDBusItem item in item_dict.Values)
				item.TriggerConfirmation ();
		}
		
		public void Shutdown ()
		{
			docky.Shutdown ();
		}
		
		public void RegisterItem (AbstractDockItem item)
		{
			if (item_dict.ContainsKey (item))
				return;
			
			string path = PathForItem (item);
			DockyDBusItem dbusitem = new DockyDBusItem (item);
			
			item_dict[item] = dbusitem;
			Bus.Session.Register (new ObjectPath (path), dbusitem);
			
			docky.OnItemAdded (path);
		}
		
		public void UnregisterItem (AbstractDockItem item)
		{
			if (!item_dict.ContainsKey (item))
				return;
			
			item_dict[item].Dispose ();
			item_dict.Remove (item);
			
			ObjectPath path = new ObjectPath (PathForItem (item));
			
			try {
				Bus.Session.Unregister (path);
			} catch (Exception e) {
				Log<DBusManager>.Error ("Could not unregister: " + path);
				Log<DBusManager>.Debug (e.StackTrace);
				return;
			}
			
			docky.OnItemRemoved (PathForItem (item));
		}
		
		internal string PathForItem (AbstractDockItem item)
		{
			
			return ItemsPath + "/" + Math.Abs (item.UniqueID ().GetHashCode ());
		}
		
		public void HandleAboutCalled ()
		{
			if (AboutCalled != null)
				AboutCalled ();
		}
		
		public void HandleSettingsCalled ()
		{
			if (SettingsCalled != null)
				SettingsCalled ();
		}
		
		public void HandleQuitCalled ()
		{
			if (QuitCalled != null)
				QuitCalled ();
		}
	}
}
