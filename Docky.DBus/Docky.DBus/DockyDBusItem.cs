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

using Docky.Items;

namespace Docky.DBus
{

	public class DockyDBusItem : IDockyDBusItem
	{
		
		Dictionary<uint, RemoteMenuEntry> items;
		Dictionary<uint, DateTime> update_time;
		
		List<uint> known_ids;
		
		AbstractDockItem owner;
		
		public DockyDBusItem (AbstractDockItem item)
		{
			owner = item;
			known_ids = new List<uint> ();
			items = new Dictionary<uint, RemoteMenuEntry> ();
			update_time = new Dictionary<uint, DateTime> ();
		}
		
		uint GetRandomID ()
		{
			Random rand = new Random ();
			
			uint number;
			
			do {
				// should we ever get 100,000 items in here, I hope we crash, though we will likely get an infinite loop
				number = (uint) rand.Next (0, 100000);
			} while (known_ids.BinarySearch (number) >= 0);
			
			known_ids.Add (number);
			known_ids.Sort ();
			
			return number;
		}
		
		#region IDockyDBusMenus implementation
		public event MenuItemActivatedHandler MenuItemActivated;
		
		public string Name {
			get {
				return owner.ShortName;
			}
		}
		
		public string Text {
			get {
				return owner.HoverText;
			}
		
		}
		
		public string Icon {
			get {
				if (owner is IconDockItem)
					return (owner as IconDockItem).Icon;
				return "custom";
			}
		}

		public bool OwnsDesktopFile {
			get {
				return (owner is ApplicationDockItem);
			}
		}
		
		public string DesktopFile {
			get {
				if (owner is ApplicationDockItem)
					return (owner as ApplicationDockItem).OwnedItem.Location;
				return "";
			}
		}
		
		public uint AddMenuItem (string name, string icon, string title)
		{
			uint number = GetRandomID ();
			
			RemoteMenuEntry rem = new RemoteMenuEntry (number, name, icon, title);
			rem.Activated += HandleActivated;
			
			items[number] = rem;
			update_time[number] = DateTime.UtcNow;
			
			return number;
		}
		
		public void RemoveItem (uint item)
		{
			if (items.ContainsKey (item))
				items.Remove (item);
			
			known_ids.Remove (item);
		}
		
		
		public void ConfirmItem (uint item)
		{
			update_time[item] = DateTime.UtcNow;
		}
		
		#endregion
			
		void HandleActivated (object sender, EventArgs args)
		{
			if (!(sender is RemoteMenuEntry))
				return;
			
			if (MenuItemActivated != null)
				MenuItemActivated ((sender as RemoteMenuEntry).ID);
		}
	}
}
