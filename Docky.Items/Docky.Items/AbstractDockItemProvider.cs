//  
//  Copyright (C) 2009 Robert Dyer
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
using System.ComponentModel;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

using Docky;
using Docky.CairoHelper;
using Docky.Services;

namespace Docky.Items
{
	public abstract class AbstractDockItemProvider
	{
		public event EventHandler<ItemsChangedArgs> ItemsChanged;
		
		public abstract string Name { get; }
		
		public virtual bool Separated {
			get { return false; }
		}
		
		public abstract IEnumerable<AbstractDockItem> Items { get; }
		
		public virtual bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return false;
		}
		
		public virtual bool RemoveItem (AbstractDockItem item)
		{
			return false;
		}
		
		public virtual IEnumerable<Docky.Menus.MenuItem> GetMenuItems (AbstractDockItem item)
		{
			return item.GetMenuItems ();
		}
		
		protected void OnItemsChanged (IEnumerable<AbstractDockItem> added, IEnumerable<AbstractDockItem> removed)
		{
			if (ItemsChanged != null)
				ItemsChanged (this, new ItemsChangedArgs (added, removed));
		}
		
		public abstract void Dispose ();
	}
}
