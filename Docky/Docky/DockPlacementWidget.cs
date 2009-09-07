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

using Docky.Interface;

namespace Docky
{


	public class DockPlacementWidget : Gtk.DrawingArea
	{
		const int Width = 260;
		const int Height = 175;

		Gdk.Rectangle allocation;
		
		public event EventHandler ActiveDockChanged;
		
		public Dock ActiveDock {
			get; private set;
		}
		
		public DockPlacementWidget ()
		{
			SetSizeRequest (Width, Height);
		}
		
		// Is there really not an easier way to get this?
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			this.allocation = allocation;
			
			base.OnSizeAllocated (allocation);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return true;
			
			bool result = base.OnExposeEvent (evnt);
			
			int x = (allocation.Width - Width) / 2;
			int y = (allocation.Height - Height) / 2;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.RoundedRectangle (x + .5, 
				                     y + .5, 
				                     Width - 1, 
				                     Height - 1,
				                     5);
				
				LinearGradient lg = new LinearGradient (x, y, x + Width, y + Width);
				lg.AddColorStop (0, new Cairo.Color (0.6, 0.9, 1.0, 0.7));
				lg.AddColorStop (1, new Cairo.Color (0.3, 0.6, 0.9, 0.7));
				cr.Pattern = lg;
				cr.FillPreserve ();
				
				lg.Destroy ();
				
				cr.Color = new Cairo.Color (0, 0, 0);
				cr.LineWidth = 1;
				cr.Stroke ();
			}
			
			return result;
		}

	}
}
