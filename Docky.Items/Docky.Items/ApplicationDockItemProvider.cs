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


	public class ApplicationDockItemProvider : IDockItemProvider
	{
		public static ApplicationDockItemProvider WindowManager;
		static List<ApplicationDockItemProvider> Providers = new List<ApplicationDockItemProvider> ();
		
		List<AbstractDockItem> items;
		
		public bool InsertItem (string desktop_file)
		{
			return InsertItemAt (desktop_file, 0);
		}
		
		public bool InsertItemAt (string desktop_file, int position)
		{
			if (desktop_file == null)
				throw new ArgumentNullException ("desktop file");
			
			ApplicationDockItem item = ApplicationDockItem.NewFromFilename (desktop_file);
			if (item == null) return false;
			
			items.Insert (position, item);
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

		public ApplicationDockItemProvider ()
		{
			items = new List<AbstractDockItem> ();
			
			Providers.Add (this);
		}
		
		~ApplicationDockItemProvider ()
		{
			Providers.Remove (this);
		}
		
		public void Dispose ()
		{
			foreach (ApplicationDockItem item in items)
				item.Dispose ();
		}
	}
}
