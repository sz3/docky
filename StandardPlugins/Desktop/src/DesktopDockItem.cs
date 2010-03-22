//  
//  Copyright (C) 2010 Rico Tzschichholz
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

using Mono.Unix;

using Cairo;
using GLib;
using Gdk;
using Wnck;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Desktop
{
	public class DesktopDockItem : IconDockItem
	{
		
		public DesktopDockItem ()
		{
			HoverText = Catalog.GetString ("Show Desktop");
			Icon = "desktop";
		}

		public override string UniqueID ()
		{
			return "Desktop";
		}
		
		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			return false;
		}
		
		protected override bool OnCanAcceptDrop (AbstractDockItem item)
		{
			return false;
		}
		
		protected override bool OnAcceptDrop (AbstractDockItem item)
		{
			return false;
		}
		
		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			return false;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				ToggleShowDesktop ();
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
		
		void ToggleShowDesktop ()
		{
			Wnck.Screen.Default.ToggleShowingDesktop (!Wnck.Screen.Default.ShowingDesktop);
		}

		#region IDisposable implementation
		public override void Dispose ()
		{
			base.Dispose ();
		}

		#endregion
		
	}
}
