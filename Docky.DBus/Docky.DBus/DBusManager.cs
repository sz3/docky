//  
//  Copyright (C) 2009 Jason Smith
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
		
		static DBusManager manager;
		public static DBusManager Default {
			get {
				if (manager == null)
					manager = new DBusManager ();
				return manager;
			}
		}

		DockyDBus docky;
		Dictionary<AbstractDockItem, DockyDBusItem> items;
		
		internal IEnumerable<AbstractDockItem> Items {
			get {
				return items.Keys;
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
			
			items = new Dictionary<AbstractDockItem, DockyDBusItem> ();
			
			ObjectPath dockyPath = new ObjectPath (DockyPath);
			docky = new DockyDBus ();
			
			bus.Register (dockyPath, docky);
		}
		
		public void Shutdown ()
		{
			docky.Shutdown ();
		}
		
		public void RegisterItem (AbstractDockItem item)
		{
			if (items.ContainsKey (item))
				return;
			
			string path = PathForItem (item);
			DockyDBusItem dbusitem = new DockyDBusItem (item);
			
			items[item] = dbusitem;
			Bus.Session.Register (new ObjectPath (path), dbusitem);
			
			docky.OnItemAdded (path);
		}
		
		public void UnregisterItem (AbstractDockItem item)
		{
			if (!items.ContainsKey (item))
				return;
			
			items[item].Dispose ();
			items.Remove (item);
			
			ObjectPath path = new ObjectPath (PathForItem (item));
			Bus.Session.Unregister (path);
			
			docky.OnItemRemoved (PathForItem (item));
		}
		
		internal string PathForItem (AbstractDockItem item)
		{
			
			return ItemsPath + "/" + Math.Abs (item.UniqueID ().GetHashCode ());
		}
	}
}
