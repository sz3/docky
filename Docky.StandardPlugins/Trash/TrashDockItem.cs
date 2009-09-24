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
using System.IO;
using System.Linq;
using System.Text;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Trash
{


	public class TrashDockItem : IconDockItem
	{

		public TrashDockItem ()
		{
			Icon = "user-trash";
			HoverText = "Recycle Bin";
		}
		
		public override string UniqueID ()
		{
			return "TrashCan";
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				OpenTrash ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem ("Open Recycling Bin", "user-trash", (o, a) => OpenTrash ());
			yield return new MenuItem ("Empty Trash", "gtk-delete", (o, a) => EmptyTrash ());
		}
		
		void OpenTrash ()
		{
			DockServices.System.Open ("trash://");
		}
		
		void EmptyTrash ()
		{
			// fixme
		}
	}
}
