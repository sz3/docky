//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using Wnck;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Interface;

namespace Docky.Menus
{
	public class DockItemMenu : DockMenu
	{
		public DockItemMenu (Gtk.Window parent) : base (parent)
		{
		}
		
		IEnumerable<MenuItem> ParseMenuList (MenuList list)
		{
			bool separate = false;
			
			List<MenuItem> actions, windows, related;
			actions = list[MenuListContainer.Actions];
			windows = list[MenuListContainer.Windows];
			related = list[MenuListContainer.RelatedItems];
			
			if (actions.Any ()) {
				foreach (MenuItem item in actions)
					yield return item;
				separate = true;
			}
			
			if (windows.Any ()) {
				if (separate)
					yield return new SeparatorMenuItem ();
				
				foreach (MenuItem item in windows)
					yield return item;
				
				separate = true;
			}
			
			if (related.Any ()) {
				if (separate)
					yield return new SeparatorMenuItem ();
				
				foreach (MenuItem item in related)
					yield return item;
			}
		}
		
		public void SetItems (MenuList items)
		{
			if (Container.Child != null) {
				foreach (Gtk.Widget widget in (Container.Child as VBox).Children)
					widget.Destroy ();
				
				Container.Remove (Container.Child);
			}
			
			VBox vbox = new VBox ();
			Container.Add (vbox);
			int width = 1;
			
			foreach (MenuItem item in ParseMenuList (items)) {
				if (item is SeparatorMenuItem) {
					vbox.PackStart (new SeparatorWidget ());
				} else {
					MenuItemWidget menuItem = new MenuItemWidget (item);
					if (IsLight)
						menuItem.TextColor = new Cairo.Color (0.2, 0.2, 0.2);
					else
						menuItem.TextColor = new Cairo.Color (1, 1, 1);
					vbox.PackStart (menuItem, false, false, 0);
					
					width = Math.Max (width, menuItem.TextWidth);
				}
			}
			vbox.SetSizeRequest (width, -1);
			
			Container.ShowAll ();
		}
	}
}
