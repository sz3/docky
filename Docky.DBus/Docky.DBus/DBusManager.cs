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

using org.freedesktop.DBus;
using NDesk.DBus;

using Docky.Services;

namespace Docky.DBus
{
	
	public class DBusManager
	{
		const string BusName        = "org.gnome.Docky";
		const string DockyPath      = "/org/gnome/Docky";
		const string MenusPath      = "/org/gnome/Docky/Menus";
		
		static DBusManager manager;
		public static DBusManager Default {
			get {
				if (manager == null)
					manager = new DBusManager ();
				return manager;
			}
		}

		DockyDBus      docky;
		DockyDBusMenus menus;
		
		public IEnumerable<RemoteMenuEntry> RemoteEntries {
			get {
				return menus.MenuEntries;
			}
		}
		
		private DBusManager ()
		{
		}
		
		public void Initialize ()
		{
			Bus bus = Bus.Session;
			
			if (bus.RequestName (BusName) != RequestNameReply.PrimaryOwner) {
				Log<DBusManager>.Error ("BusName is already owned");
				return;
			}
			
			ObjectPath dockyPath = new ObjectPath (DockyPath);
			ObjectPath menusPath = new ObjectPath (MenusPath);
			
			docky = new DockyDBus ();
			menus = new DockyDBusMenus ();
			
			bus.Register (dockyPath, docky);
			bus.Register (menusPath, menus);
		}
	}
}
