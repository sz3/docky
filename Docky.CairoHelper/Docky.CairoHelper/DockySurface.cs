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

namespace Docky.CairoHelper
{


	public class DockySurface : IDisposable
	{
		bool disposed;
		Surface surface;
		Context context;
		
		public Surface Internal {
			get { 
				if (surface == null && !disposed)
					surface = new ImageSurface (Format.Argb32, Width, Height);
				return surface; 
			}
			private set { surface = value; }
		}
		
		bool HasInternal {
			get { return surface != null; }
		}
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		public Context Context {
			get {
				if (context == null && !disposed)
					context = new Context (Internal);
				return context;
			}
		}
		
		public DockySurface (int width, int height, Surface model) : this(width, height)
		{
			if (model != null)
				EnsureSurfaceModel (model);
		}

		public DockySurface (int width, int height, DockySurface model) : this(width, height)
		{
			if (model != null)
				EnsureSurfaceModel (model.Internal);
		}
		
		public DockySurface (int width, int height)
		{
			Width = width;
			Height = height;
		}

		public void Clear ()
		{
			if (disposed)
				return;
			Context.Save ();
			
			Context.Color = new Cairo.Color (0, 0, 0, 0);
			Context.Operator = Operator.Source;
			Context.Paint ();
			
			Context.Restore ();
		}
		
		public DockySurface DeepCopy ()
		{
			if (disposed)
				return null;
			Surface copy = Internal.CreateSimilar (Content.ColorAlpha, Width, Height);
			using (Cairo.Context cr = new Cairo.Context (copy)) {
				Internal.Show (cr, 0, 0);
			}
			
			DockySurface result = new DockySurface (Width, Height);
			result.Internal = copy;
			
			return result;
		}
		
		public void EnsureSurfaceModel (Surface reference)
		{
			if (disposed)
				return;
			if (reference == null)
				throw new ArgumentNullException ("Reference Surface", "Reference Surface may not be null");
			
			bool hadInternal = HasInternal;
			Surface last = null;
			if (hadInternal)
				last = Internal;
			
			// we dont need to copy to a model we are already on
			if (hadInternal && reference.SurfaceType == Internal.SurfaceType)
				return;
			
			Internal = reference.CreateSimilar (Cairo.Content.ColorAlpha, Width, Height);
			
			if (hadInternal) {
				using (Cairo.Context cr = new Cairo.Context (Internal)) {
					last.Show (cr, 0, 0);
				}
				if (context != null) {
					(context as IDisposable).Dispose ();
					context = null;
				}
				last.Destroy ();
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (disposed)
				return;
			
			disposed = true;
			if (context != null)
				(context as IDisposable).Dispose ();
			if (surface != null)
				surface.Destroy ();
		}
		#endregion

		~DockySurface ()
		{
			// just to be safe
			if (!disposed) {
				Dispose ();
			}
		}
	}
}
