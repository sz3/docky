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
using Docky.Menus;

namespace Docky.DBus
{
	public struct Tuple
	{
		public string Name;
		public string Icon;
		public string Title;
		
		public Tuple (string name, string icon, string title)
		{
			Name = name;
			Icon = icon;
			Title = title;
		}
	}
	
	public class DockyDBusItem : IDockyDBusItem, IDisposable
	{
		
		uint timer;
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
			
			timer = GLib.Timeout.Add (4 * 60 * 1000, delegate {
				foreach (uint i in update_time
					.Where (kvp => (DateTime.UtcNow - kvp.Value).TotalMinutes > 5)
					.Select (kvp => kvp.Key))
					
					RemoveItem (i);
				return true;
			});
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
			set {
				owner.SetRemoteText (value);
			}
		
		}
		
		public string Icon {
			get {
				if (CanSetIcon)
					return (owner as IconDockItem).Icon;
				return "custom";
			}
			set {
				if (!CanSetIcon)
					return;
				
				(owner as IconDockItem).SetRemoteIcon (value);
			}
		}

		public bool CanSetIcon {
			get {
				return owner is IconDockItem;
			}
		}
		
		public bool OwnsDesktopFile {
			get {
				return owner is ApplicationDockItem;
			}
		}
		
		public bool Attention { 
			get { return (owner.State & ItemState.Urgent) == ItemState.Urgent; }
		}
		
		public bool Wait {
			get { return (owner.State & ItemState.Wait) == ItemState.Wait; }
		}
		
		public string DesktopFile {
			get {
				if (owner is ApplicationDockItem)
					return (owner as ApplicationDockItem).OwnedItem.Location;
				return "";
			}
		}
		
		public uint[] Items {
			get {
				return items.Keys.ToArray ();
			}
		}
		
		public uint AddMenuItem (string name, string icon, string title)
		{
			uint number = GetRandomID ();
			
			RemoteMenuEntry rem = new RemoteMenuEntry (number, name, icon, title);
			rem.Clicked += HandleActivated;
			
			items[number] = rem;
			update_time[number] = DateTime.UtcNow;
			
			//Insert items into list... this is stupid but whatever fix later
			foreach (MenuItem item in items.Values)
				owner.RemoteMenuItems.Remove (item);
			
			MenuListContainer container = MenuListContainer.Footer + 1;
			var groupedItems = items.Values
				.GroupBy (rmi => rmi.Title);
			
			foreach (var itemGroup in groupedItems) {
				owner.RemoteMenuItems.SetContainerTitle (container, itemGroup.Key);
				foreach (MenuItem item in itemGroup) {
					owner.RemoteMenuItems[container].Add (item);
				}
				container++;
			}
			
			return number;
		}
		
		public void RemoveItem (uint item)
		{
			
			if (items.ContainsKey (item)) {
				RemoteMenuEntry entry = items[item];
				items.Remove (item);
				
				owner.RemoteMenuItems.Remove (entry);
			}
			
			known_ids.Remove (item);
		}
		
		public void ConfirmItem (uint item)
		{
			update_time[item] = DateTime.UtcNow;
		}
		
		public void SetAttention ()
		{
			owner.State |= ItemState.Urgent;
		}
		
		public void UnsetAttention ()
		{
			owner.State &= ~ItemState.Urgent;
		}

		public void SetWaiting ()
		{
			owner.State |= ItemState.Wait;
		}
		
		public void UnsetWaiting ()
		{
			owner.State &= ~ItemState.Wait;
		}
		
		public void ResetText ()
		{
			owner.SetRemoteText ("");
		}
		
		public void ResetIcon ()
		{
			if (!CanSetIcon)
				return;
			
			(owner as IconDockItem).SetRemoteIcon ("");
		}
		
		public Tuple GetItem (uint item)
		{
			if (!items.ContainsKey (item))
				return new Tuple ("", "", "");
			
			RemoteMenuEntry entry = items[item];
			return new Tuple (entry.Text, entry.Icon, entry.Title);
		}
		
		#endregion
			
		void HandleActivated (object sender, EventArgs args)
		{
			if (!(sender is RemoteMenuEntry))
				return;
			
			if (MenuItemActivated != null)
				MenuItemActivated ((sender as RemoteMenuEntry).ID);
		}
		#region IDisposable implementation
		public void Dispose ()
		{
			GLib.Source.Remove (timer);
		}
		
		#endregion
	}
}
