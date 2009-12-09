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

namespace Docky.DBus
{
	public class RemoteMenuEntry
	{
		public event EventHandler Activated;
		
		public uint ID { get; private set; }
		public string Target {get; private set;}
		public string Name {get; private set;}
		public string Icon { get; private set; }
		public string Title { get; private set; }
	
		public RemoteMenuEntry (uint id, string target, string name, string icon, string title)
		{
			ID = id;
			Target = target;
			Name = name;
			Icon = icon;
			Title = title;
		}
		
		public void OnActivated ()
		{
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}
	}

	public class DockyDBusMenus : IDockyDBusMenus
	{
		public event MenuItemActivatedHandler MenuItemActivated;
		
		Dictionary<uint, RemoteMenuEntry> entries;
		Dictionary<uint, DateTime> update_time;
		
		uint last = 2500;
		
		#region IDockyDBusMenus implementation
		public uint AddMenuItem (string target, string name, string icon, string title)
		{
			uint number = last++;
			entries[number] = new RemoteMenuEntry (number, target, name, icon, title);
			entries[number].Activated += HandleActivated;
			return number;
		}
		
		public void RemoveMenuItem (uint item)
		{
			if (entries.ContainsKey (item))
				entries.Remove (item);
		}
		
		
		public void ConfirmMenuItem (uint item)
		{
			update_time[item] = DateTime.UtcNow;
		}
		
		#endregion

		public DockyDBusMenus ()
		{
			entries = new Dictionary<uint, RemoteMenuEntry> ();
			update_time = new Dictionary<uint, DateTime> ();
		}
		
		void HandleActivated (object sender, EventArgs args)
		{
			if (!(sender is RemoteMenuEntry))
				return;
			
			if (MenuItemActivated != null)
				MenuItemActivated ((sender as RemoteMenuEntry).ID);
		}
		
		public IEnumerable<RemoteMenuEntry> MenuEntries {
			get {
				return entries.Values.ToArray ();
			}
		}
	}
}
