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

namespace Docky.Menus
{


	public class SeparatorWidget : EventBox
	{

		public SeparatorWidget ()
		{
			HasTooltip = true;
			VisibleWindow = false;
			AboveChild = true;
			
			SetSizeRequest (-1, 3);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.MoveTo (Allocation.X, Allocation.Y + 1.5);
				cr.LineTo (Allocation.X + Allocation.Width, Allocation.Y + 1.5);
				
				LinearGradient lg = new LinearGradient (Allocation.X, 0, Allocation.X + Allocation.Width, 0);
				lg.AddColorStop (0, new Cairo.Color (1, 1, 1, 0));
				lg.AddColorStop (.4, new Cairo.Color (1, 1, 1, .4));
				lg.AddColorStop (.6, new Cairo.Color (1, 1, 1, .4));
				lg.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));
				cr.Pattern = lg;
				
				cr.LineWidth = 1;
				cr.Stroke ();
				
				lg.Destroy ();
			}
			
			return false;
		}
	}
}
