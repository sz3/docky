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

using WindowManager.Xlib;

namespace WindowManager.Wink
{
	public class Viewport
	{
		Workspace parent;
		Rectangle area;
		
		public bool IsActive {
			get {
				if (!parent.IsVirtual)
					return Wnck.Screen.Default.ActiveWorkspace == parent;
				else
					return Wnck.Screen.Default.ActiveWorkspace.ViewportX == area.X && Wnck.Screen.Default.ActiveWorkspace.ViewportY == area.Y;
			}
		}
		
		WindowMoveResizeMask MoveResizeMask {
			get {
				return WindowMoveResizeMask.X |
					   WindowMoveResizeMask.Y |
					   WindowMoveResizeMask.Height |
					   WindowMoveResizeMask.Width;
			}
		}
		
		internal Viewport(Rectangle area, Workspace parent)
		{
			this.area = area;
			this.parent = parent;
		}
		
		bool Contains (Gdk.Point point)
		{
			return area.Contains (point);
		}
		
		IEnumerable<Wnck.Window> RawWindows ()
		{
			foreach (Wnck.Window window in ScreenUtils.GetWindows ())
				if (WindowCenterInViewport (window) || window.IsSticky)
					yield return window;
		}
		
		IEnumerable<Wnck.Window> Windows ()
		{
			return RawWindows ().Where (w => !w.IsSkipTasklist && w.WindowType != Wnck.WindowType.Dock);
		}
		
		bool WindowCenterInViewport (Wnck.Window window)
		{
			if (!window.IsOnWorkspace (parent))
				return false;
				
			Rectangle geo = window.EasyGeometry ();
			geo.X += parent.ViewportX;
			geo.Y += parent.ViewportY;
			
			Point center = new Point (geo.X + geo.Width / 2, geo.Y + geo.Height / 2);
			return Contains (center);
		}
		
		public void ShowDesktop ()
		{
			if (!ScreenUtils.DesktopShown (parent.Screen))
				ScreenUtils.ShowDesktop (parent.Screen);
			else
				ScreenUtils.UnshowDesktop (parent.Screen);
		}
		
		public void Cascade ()
		{
			IEnumerable<Wnck.Window> windows = Windows ().Where (w => !w.IsMinimized);
			if (windows.Count () <= 1) return;
			
			Gdk.Rectangle screenGeo = GetScreenGeoMinusStruts ();
			
			int titleBarSize = windows.First ().FrameExtents () [(int) Position.Top];
			int windowHeight = screenGeo.Height - ((windows.Count () - 1) * titleBarSize);
			int windowWidth = screenGeo.Width - ((windows.Count () - 1) * titleBarSize);
			
			int count = 0;
			int x, y;
			foreach (Wnck.Window window in windows) {
				x = screenGeo.X + titleBarSize * count - parent.ViewportX;
				y = screenGeo.Y + titleBarSize * count - parent.ViewportY;
				
				SetTemporaryWindowGeometry (window, new Gdk.Rectangle (x, y, windowWidth, windowHeight));
				count++;
			}
		}
		
		public void Tile ()
		{
			IEnumerable<Wnck.Window> windows = Windows ().Where (w => !w.IsMinimized);
			if (windows.Count () <= 1) return;
			
			Gdk.Rectangle screenGeo = GetScreenGeoMinusStruts ();
			
			int width, height;
			//We are going to tile to a square, so what we want is to find
			//the smallest perfect square all our windows will fit into
			width = (int) Math.Ceiling (Math.Sqrt (windows.Count ()));
			
			//Our height is at least one (e.g. a 2x1)
			height = 1;
			while (width * height < windows.Count ())
				height++;
			
			int windowWidth, windowHeight;
			windowWidth = screenGeo.Width / width;
			windowHeight = screenGeo.Height / height;
			
			int row = 0, column = 0;
			int x, y;
			
			foreach (Wnck.Window window in windows) {
				x = screenGeo.X + (column * windowWidth) - parent.ViewportX;
				y = screenGeo.Y + (row * windowHeight) - parent.ViewportY;
				
				Gdk.Rectangle windowArea = new Gdk.Rectangle (x, y, windowWidth, windowHeight);;
				
				if (window == windows.Last ())
					windowArea.Width *= width - column;
				
				SetTemporaryWindowGeometry (window, windowArea);
				
				column++;
				if (column == width) {
					column = 0;
					row++;
				}
			}
		}
		
		Gdk.Rectangle GetScreenGeoMinusStruts ()
		{
			IEnumerable<int []> struts = RawWindows ()
				.Where (w => w.WindowType == Wnck.WindowType.Dock)
				.Select (w => w.GetCardinalProperty (X11Atoms.Instance._NET_WM_STRUT_PARTIAL));
			
			int [] offsets = new int [4];
			for (int i = 0; i < 4; i++)
				offsets [i] = struts.Max (a => a[i]);
			
			Gdk.Rectangle screenGeo = area;
			screenGeo.Width -= offsets [(int) Position.Left] + offsets [(int) Position.Right];
			screenGeo.Height -= offsets [(int) Position.Top] + offsets [(int) Position.Bottom];
			screenGeo.X += offsets [(int) Position.Left];
			screenGeo.Y += offsets [(int) Position.Top];
			
			return screenGeo;
		}
		
		void SetTemporaryWindowGeometry (Wnck.Window window, Gdk.Rectangle area)
		{
			Gdk.Rectangle oldGeo = window.EasyGeometry ();
			
			oldGeo.X += parent.ViewportX;
			oldGeo.Y += parent.ViewportY;
			
			if (window.IsMaximized)
				window.Unmaximize ();
			
			window.SetWorkaroundGeometry (WindowGravity.Current, MoveResizeMask, area.X, area.Y, area.Width, area.Height);
		}
	}
}
