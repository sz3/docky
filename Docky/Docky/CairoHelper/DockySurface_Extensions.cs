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


	/// <summary>
	/// Advanced methods for a DockySurface that are not fit for "general" consumption, usually due to
	/// performance implications or very specific use cases.
	/// </summary>
	public static class DockySurface_Extensions
	{

		public static void ShowAtPointAndZoom (this DockySurface self, DockySurface target, PointD point, double zoom)
		{
			self.ShowAtPointAndZoom (target, point, zoom, 1);
		}
		
		public static void ShowAtPointAndZoom (this DockySurface self, DockySurface target, PointD point, double zoom, double opacity)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			if (zoom != 1)
				cr.Scale (zoom, zoom);
			
			cr.SetSource (self.Internal, 
			              (point.X - zoom * self.Width / 2) / zoom,
			              (point.Y - zoom * self.Height / 2) / zoom);
			cr.PaintWithAlpha (opacity);
			
			if (zoom != 1)
				cr.IdentityMatrix ();
			
		}
		
		public static void ShowAtPointAndRotation (this DockySurface self, DockySurface target, PointD point, double rotation, double opacity)
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
			cr.PaintWithAlpha (opacity);
			
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
		
		public static DockySurface CreateMask (this DockySurface self)
		{
			ImageSurface original = new ImageSurface (Format.Argb32, self.Width, self.Height);
			
			using (Cairo.Context cr = new Cairo.Context (original))
				self.Internal.Show (cr, 0, 0);
			
			byte a;
			int length = original.Data.Length;
			
			unsafe {
				byte* dataPtr = (byte*) original.DataPtr;
				
				for (int i = 0; i < length - 3; i += 4) {
					a = dataPtr[3];
					
					dataPtr[0] = 0;
					dataPtr[1] = 0;
					dataPtr[2] = 0;
					dataPtr[3] = (a > 0) ? byte.MaxValue : (byte) 0;
					
					dataPtr += 4;
				}
			}
			
			DockySurface target = new DockySurface (self.Width, self.Height, self);
			original.Show (target.Context, 0, 0);
			
			original.Destroy ();
			
			return target;
		}
		
		public unsafe static DockySurface GaussianBlur (this DockySurface self, int size)
		{
			// Note: This method is wickedly slow
			
			int gaussWidth = size * 2 + 1;
			double[] kernel = BuildGaussianKernel (gaussWidth);
			
			ImageSurface original = new ImageSurface (Format.Argb32, self.Width, self.Height);
			using (Cairo.Context cr = new Cairo.Context (original))
				self.Internal.Show (cr, 0, 0);
			
			double gaussSum = 0;
			foreach (double d in kernel)
				gaussSum += d;
			
			for (int i = 0; i < kernel.Length; i++)
				kernel[i] = kernel[i] / gaussSum;
			
			int width = self.Width;
			int height = self.Height;
			
			byte[] src = original.Data;
			double[] xbuffer = new double[original.Data.Length];
			double[] ybuffer = new double[original.Data.Length];
			
			int dest, dest2, shift, source;
			
			byte* srcPtr = (byte*) original.DataPtr;
			
			fixed (double* xbufferPtr = xbuffer)
			fixed (double* ybufferPtr = ybuffer) 
			fixed (double* kernelPtr = kernel) {
				// Horizontal Pass
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						dest = y * width + x;
						dest2 = dest * 4;
						
						for (int k = 0; k < gaussWidth; k++) {
							shift = k - size;
							
							source = dest + shift;
							
							if (x + shift <= 0 || x + shift >= width) {
								source = dest;
							}
							
							source = source * 4;
							xbufferPtr[dest2 + 0] = xbufferPtr[dest2 + 0] + (srcPtr[source + 0] * kernelPtr[k]);
							xbufferPtr[dest2 + 1] = xbufferPtr[dest2 + 1] + (srcPtr[source + 1] * kernelPtr[k]);
							xbufferPtr[dest2 + 2] = xbufferPtr[dest2 + 2] + (srcPtr[source + 2] * kernelPtr[k]);
							xbufferPtr[dest2 + 3] = xbufferPtr[dest2 + 3] + (srcPtr[source + 3] * kernelPtr[k]);
						}
					}
				}
			
				// Vertical Pass
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						dest = y * width + x;
						dest2 = dest * 4;
						
						for (int k = 0; k < gaussWidth; k++) {
							shift = k - size;
							
							source = dest + shift * width;
							
							if (y + shift <= 0 || y + shift >= height) {
								source = dest;
							}
							
							source = source * 4;
							ybufferPtr[dest2 + 0] = ybufferPtr[dest2 + 0] + (xbufferPtr[source + 0] * kernelPtr[k]);
							ybufferPtr[dest2 + 1] = ybufferPtr[dest2 + 1] + (xbufferPtr[source + 1] * kernelPtr[k]);
							ybufferPtr[dest2 + 2] = ybufferPtr[dest2 + 2] + (xbufferPtr[source + 2] * kernelPtr[k]);
							ybufferPtr[dest2 + 3] = ybufferPtr[dest2 + 3] + (xbufferPtr[source + 3] * kernelPtr[k]);
						}
					}
				}
				
				for (int i = 0; i < src.Length; i++)
					srcPtr[i] = (byte) ybufferPtr[i];
			}
			
			DockySurface target = new DockySurface (self.Width, self.Height, self);
			original.Show (target.Context, 0, 0);
			original.Destroy ();
			
			return target;
		}
		
		static double[] BuildGaussianKernel (int gaussWidth)
		{
			if (gaussWidth % 2 != 1)
				throw new ArgumentException ("Gaussian Width must be odd");
			
			double[] kernel = new double[gaussWidth];
			
			// Maximum value of curve
			double sd = 255;
			
			// Width of curve
			double range = gaussWidth;
			
			// Average value of curve
			double mean = range / sd;
			
			for (int i = 0; i < gaussWidth / 2 + 1; i++) {
				kernel[i] = Math.Pow (Math.Sin (((i + 1) * (Math.PI / 2) - mean) / range), 2) * sd;
				kernel[gaussWidth - i - 1] = kernel[i];
			}
			
			return kernel;
		}
	}
}
