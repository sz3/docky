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


	public class DockyDBus : IDockyDBus
	{
		#region IDockyDBus implementation
		public event ItemAddedHandler ItemAdded;
		
		public string[] DockItemPaths ()
		{
			return DBusManager.Default.Items
				.Select (adi => DBusManager.Default.PathForItem (adi))
				.ToArray ();
		}

		public string DockItemPathForDesktopID (string id)
		{
			return DBusManager.Default.Items
				.OfType<ApplicationDockItem> ()
				.Where (adi => adi.OwnedItem.DesktopID == id)
				.Select (adi => DBusManager.Default.PathForItem (adi))
				.DefaultIfEmpty ("")
				.FirstOrDefault ();
		}

		public string DockItemPathForDesktopFile (string path)
		{
			return DBusManager.Default.Items
				.OfType<ApplicationDockItem> ()
				.Where (adi => adi.OwnedItem.Location == path)
				.Select (adi => DBusManager.Default.PathForItem (adi))
				.DefaultIfEmpty ("")
				.FirstOrDefault ();
		}
		
		public string DockItemPathForWindowXID (uint xid)
		{
			return DBusManager.Default.Items
				.OfType<WnckDockItem> ()
				.Where (wdi => wdi.Windows.Any (w => (uint) w.Xid == xid))
				.Select (wdi => DBusManager.Default.PathForItem (wdi))
				.DefaultIfEmpty ("")
				.FirstOrDefault ();
		}
		
		
		public void ShowAbout ()
		{
		}
		
		
		public void ShowSettings ()
		{
		}
		
		
		public void Quit ()
		{
			// fixme
			System.Environment.Exit (0);
		}
		
		#endregion

		public DockyDBus ()
		{
		}
		
		public void OnItemAdded (string path)
		{
			if (ItemAdded != null)
				ItemAdded (path);
		}
	}
}
