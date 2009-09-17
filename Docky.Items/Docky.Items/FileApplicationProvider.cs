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

using Cairo;
using Gdk;
using Gtk;

namespace Docky.Items
{


	public class FileApplicationProvider : IDockItemProvider
	{
		public static FileApplicationProvider WindowManager;
		static List<FileApplicationProvider> Providers = new List<FileApplicationProvider> ();
		
		Dictionary<string, AbstractDockItem> items;
		
		public IEnumerable<string> Uris {
			get { return items.Keys.AsEnumerable (); }
		}
		
		public FileApplicationProvider ()
		{
			items = new Dictionary<string, AbstractDockItem> ();
			
			Providers.Add (this);
		}
		
		public bool InsertItem (string uri)
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");
			
			if (items.ContainsKey (uri))
				return false;
			
			AbstractDockItem item;
			
			try {
				if (uri.EndsWith (".desktop")) {
					item = ApplicationDockItem.NewFromUri (uri);
				} else {
					item = FileDockItem.NewFromUri (uri);
				}
			} catch (Exception e) {
				item = null;
			}
			
			if (item == null)
				return false;
			
			item.Owner = this;
			items[uri] = item;
			
			if (ItemsChanged != null) {
				ItemsChanged (this, new ItemsChangedArgs (item, AddRemoveChangeType.Add));
			}
			
			return true;
		}
		
		public bool SetWindowManager ()
		{
			if (WindowManager != null)
				WindowManager.UnsetWindowManager ();
			
			WindowManager = this;
			return true;
		}
		
		public bool UnsetWindowManager ()
		{
			WindowManager = null;
			return true;
		}
		
		#region IDockItemProvider implementation
		public event EventHandler<ItemsChangedArgs> ItemsChanged;
		
		public bool Separated { get { return true; } }
		
		public bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return true;
		}
		
		public bool RemoveItem (AbstractDockItem item)
		{
			if (!items.ContainsValue (item))
				return false;
			
			string key = null;
			foreach (KeyValuePair<string, AbstractDockItem> kvp in items) {
				if (kvp.Value == item) {
					key = kvp.Key;
					break;
				}
			}
			
			// this should never happen...
			if (key == null)
				return false;
			
			items.Remove (key);
			
			OnItemsChanged (item, AddRemoveChangeType.Remove);
			
			item.Dispose ();
			return true;
		}
		
		public IEnumerable<AbstractDockItem> Items {
			get {
				return items.Values.AsEnumerable ();
			}
		}
		#endregion
		
		void OnItemsChanged (AbstractDockItem item, AddRemoveChangeType type)
		{
			if (ItemsChanged != null) {
				ItemsChanged (this, new ItemsChangedArgs (item, type));
			}
		}

		~FileApplicationProvider ()
		{
			Providers.Remove (this);
		}
		
		public void Dispose ()
		{
			foreach (AbstractDockItem item in items.Values)
				item.Dispose ();
		}
	}
}
