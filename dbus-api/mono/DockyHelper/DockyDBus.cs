//  
//  Copyright (C) 2010 Chris Szikszoy
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
using System.Linq;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

using GLib;

namespace DockyHelper
{

	public static class DockyDBus
	{
		public static string DockyBusName = "org.gnome.Docky";
		public static string DockyPath = "/org/gnome/Docky";
		static string SessionBusName = "org.freedesktop.DBus";
		static string SessionBusPath = "/org/freedesktop/DBus";
		
		public static List<IDockyItem> Items { get; private set; }
		
		public static event EventHandler Quit;
		
		static IDocky Docky;
		static IBus SessionBus;
		
		static DockyDBus ()
		{
			Items = new List<IDockyItem> ();
			
			try {
				Docky = Bus.Session.GetObject<IDocky> (DockyBusName, new ObjectPath (DockyPath));
				SessionBus = Bus.Session.GetObject<IBus> (SessionBusName, new ObjectPath (SessionBusPath));
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				Environment.Exit (-1);
			}
			
			SessionBus.NameOwnerChanged += delegate(string name, string old_owner, string new_owner) {
				if (name != DockyBusName)
					return;
				OnQuit ();
			};
			
			Docky.ItemAdded += delegate(string path) {
				UpdateDockyItems ();
			};
			
			Docky.ItemRemoved += delegate(string path) {
				UpdateDockyItems ();
			};
			
			UpdateDockyItems ();
		}

		static void UpdateDockyItems ()
		{
			try {
				Console.WriteLine ("Updating items.");
				lock (Items) {
					Items.Clear ();
					//get a list of the current item paths, associate with the .desktop file
					foreach (string path in Docky.DockItemPaths ()) {		
						try {
							IDockyItem item = Bus.Session.GetObject<IDockyItem> (DockyBusName, new ObjectPath (path));
							Items.Add (item);
						} catch {
							continue;
						}	
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("Error updating items... {0}", e.Message);
			}
		}
		
		static void OnQuit ()
		{
			if (Quit != null)
				Quit (null, EventArgs.Empty);
			Helper.Quit ();
		}
	}
}

