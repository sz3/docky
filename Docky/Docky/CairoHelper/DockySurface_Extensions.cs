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

namespace Docky.CairoHelper
{


	public static class DockySurface_Extensions
	{

		public static void ShowAtPointAndZoom (this DockySurface self, DockySurface target, PointD point, double zoom)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			if (zoom != 1)
				cr.Scale (zoom, zoom);
			
			cr.SetSource (self.Internal, 
			              (point.X - zoom * self.Width / 2) / zoom,
			              (point.Y - zoom * self.Height / 2) / zoom);
			cr.Paint ();
			
			if (zoom != 1)
				cr.IdentityMatrix ();
			
		}
		
		public static void ShowAtPointAndRotation (this DockySurface self, DockySurface target, PointD point, double rotation)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			double cos, sin;
			cos = Math.Cos (rotation);
			sin = Math.Sin (rotation);
			Matrix m = new Matrix (cos, sin, 0 - sin, cos, point.X, point.Y);
			cr.Transform (m);
			cr.SetSource (self.Internal, 0 - self.Width / 2, 0 - self.Height / 2);
			cr.Paint ();
			
			cr.IdentityMatrix ();
		}
		
		public static void ShowAtEdge (this DockySurface self, DockySurface target, PointD point, DockPosition position)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			double x = point.X;
			double y = point.Y;
			
			switch (position) {
			case DockPosition.Top:
				x -= self.Width / 2;
				break;
			case DockPosition.Left:
				y -= self.Height / 2;
				break;
			case DockPosition.Right:
				x -= self.Width;
				y -= self.Height / 2;
				break;
			case DockPosition.Bottom:
				x -= self.Width / 2;
				y -= self.Height;
				break;
			}
			
			cr.SetSource (self.Internal, (int) x, (int) y);
			cr.Paint ();
		}
		
		public static Gdk.Pixbuf LoadToPixbuf (this DockySurface self)
		{
			Gdk.Pixbuf pbuf;
			string tmp = System.IO.Path.GetTempFileName ();
			self.Internal.WriteToPng (tmp);
			pbuf = new Pixbuf (tmp);
			System.IO.File.Delete (tmp);

			return pbuf;
		}
	}
}
