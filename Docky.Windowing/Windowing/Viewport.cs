//  
//  Copyright (C) 2009 GNOME Do
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
using System.Runtime.InteropServices;
using System.Linq;

using Gdk;
using Wnck;

using Docky.Windowing;
using Docky.Xlib;

namespace Docky.Windowing
{
	public class Viewport
	{
		Workspace parent;
		Rectangle area;
		
		public string Name { get; private set; }
		
		public Rectangle Area {
			get { return area; }
		}
		
		public bool IsActive {
			get {
				if (!parent.IsVirtual)
					return Wnck.Screen.Default.ActiveWorkspace == parent;
				else
					return Wnck.Screen.Default.ActiveWorkspace.ViewportX == area.X && Wnck.Screen.Default.ActiveWorkspace.ViewportY == area.Y;
			}
		}
		
		internal Viewport(string name, Rectangle area, Workspace parent)
		{
			this.area = area;
			this.parent = parent;
			Name = name;
		}
		
		public void Present ()
		{
			parent.Screen.MoveViewport (area.X, area.Y);
		}
	}
}
