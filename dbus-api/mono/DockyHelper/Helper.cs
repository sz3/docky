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

using GLib;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace DockyHelper
{

	public abstract class Helper
	{
		#region Static section
		static MainLoop loop;
		
		static Helper ()
		{
			BusG.Init ();
			loop = new MainLoop ();
			DockyDBus.Quit += delegate {
				Quit ();
			};
		}
		
		public static void Run ()
		{
			loop.Run ();
		}
		
		public static void Quit ()
		{
			GLib.Idle.Add (delegate {
				loop.Quit ();
				return false;
			});
		}
		
		public static void Invoke (Action act)
		{
			GLib.Idle.Add (delegate {
				act.Invoke ();
				return false;
			});
		}
		#endregion
		
		#region public instances
		protected IDockyItem DockyItem { get; private set; }
		
		List<BaseMenuItem> MenuItems;
		
		public Helper (IDockyItem item)
		{
			MenuItems = new List<BaseMenuItem> ();
			DockyItem = item;
		}
		
		protected void AddMenuItem (BaseMenuItem item)
		{
			if (MenuItems.Contains (item))
				return;
			
			item.Owner = this.DockyItem;
			MenuItems.Add (item);
			if (item is FileMenuItem)
				item.Handle = DockyItem.AddFileMenuItem ((item as FileMenuItem).Uri, item.Title);
			else if (item is MenuItem)
				item.Handle = DockyItem.AddMenuItem ((item as MenuItem).Name, (item as MenuItem).Icon, item.Title);
			
			item.Init ();
		}
		
		protected void RemoveMenuItem (BaseMenuItem item)
		{
			if (!MenuItems.Contains (item))
				return;
			
			MenuItems.Remove (item);
			DockyItem.RemoveItem (item.Handle);
		}
		
		#endregion
	}
}

