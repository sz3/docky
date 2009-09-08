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
using System.ComponentModel;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;
using Wnck;

using Docky;
using Docky.CairoHelper;
using Docky.Services;

namespace Docky.Items
{


	public abstract class WnckDockItem : IconDockItem
	{
		public abstract IEnumerable<Wnck.Window> Windows { get; }
		
		protected IEnumerable<Wnck.Window> ManagedWindows {
			get {
				return Windows.Where (w => !w.IsSkipTasklist);
			}
		}
		
		public sealed override void SetScreenRegion (Gdk.Screen screen, Gdk.Rectangle region)
		{
			foreach (Wnck.Window w in ManagedWindows) {
				w.SetIconGeometry (region.X, region.Y, region.Width, region.Height);
			}
		}
		
		protected override ClickAnimation OnClicked (uint button, ModifierType mod, double xPercent, double yPercent)
		{
			return ClickAnimation.Bounce;
		}
	}
}
