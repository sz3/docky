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
		
		List<AbstractDockItem> items;
		List<string> uris;
		
		public FileApplicationProvider ()
		{
			items = new List<AbstractDockItem> ();
			uris = new List<string> ();
			
			Providers.Add (this);
		}
		
		public bool InsertItem (string uri)
		{
			return InsertItemAt (uri, 0);
		}
		
		public bool InsertItemAt (string uri, int position)
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");
			
			if (uris.Contains (uri))
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
			
			items.Insert (position, item);
			uris.Add (uri);
			
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
		
		public bool ItemCanBeMoved (AbstractDockItem item)
		{
			return true;
		}
		
		public bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return true;
		}
		
		public bool MoveItem (AbstractDockItem item, int position)
		{
			if (!items.Contains (item))
				return false;
			
			items.Remove (item);
			items.Insert (position, item);
			
			return true;
		}
		
		public bool RemoveItem (AbstractDockItem item)
		{
			return false;
		}
		
		public IEnumerable<AbstractDockItem> Items {
			get {
				return items.AsEnumerable ();
			}
		}
		#endregion

		~FileApplicationProvider ()
		{
			Providers.Remove (this);
		}
		
		public void Dispose ()
		{
			foreach (AbstractDockItem item in items)
				item.Dispose ();
		}
	}
}
