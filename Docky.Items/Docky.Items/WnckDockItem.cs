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
using Docky.Windowing;

namespace Docky.Items
{


	public abstract class WnckDockItem : IconDockItem
	{
		public event EventHandler WindowsChanged;
		
		IEnumerable<Wnck.Window> windows;
		public IEnumerable<Wnck.Window> Windows {
			get { return windows; }
			protected set {
				windows = value;
				SetIndicator ();
				
				if (WindowsChanged != null)
					WindowsChanged (this, EventArgs.Empty);
			}
		}
		
		protected IEnumerable<Wnck.Window> ManagedWindows {
			get {
				return Windows.Where (w => !w.IsSkipTasklist);
			}
		}
		
		void SetIndicator ()
		{
			int count = ManagedWindows.Count ();
			if (count > 1) {
				Indicator = ActivityIndicator.SinglePlus;
			} else if (count == 1) {
				Indicator = ActivityIndicator.Single;
			} else {
				Indicator = ActivityIndicator.None;
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
			if (!ManagedWindows.Any ())
				return ClickAnimation.None;
			
			List<Wnck.Window> stack = new List<Wnck.Window> (Wnck.Screen.Default.WindowsStacked);
			IEnumerable<Wnck.Window> windows = ManagedWindows.OrderByDescending (w => stack.IndexOf (w));
			
			bool not_in_viewport = !windows.Any (w => !w.IsSkipTasklist && w.IsInViewport (w.Screen.ActiveWorkspace));
			bool urgent = windows.Any (w => w.NeedsAttention ());
			
			if (not_in_viewport || urgent) {
				foreach (Wnck.Window window in windows) {
					if (urgent && !window.NeedsAttention ())
						continue;
					if (!window.IsSkipTasklist) {
						WindowControl.IntelligentFocusOffViewportWindow (window, windows);
						return ClickAnimation.Darken;
					}
				}
			}
			
			if (windows.Any (w => w.IsMinimized && w.IsInViewport (Wnck.Screen.Default.ActiveWorkspace))) {
				WindowControl.RestoreWindows (windows);
			} else if (windows.Any (w => w.IsActive && w.IsInViewport (Wnck.Screen.Default.ActiveWorkspace))) {
				WindowControl.MinimizeWindows (windows);
			} else {
				WindowControl.FocusWindows (windows);
			}
			
			return ClickAnimation.Darken;
		}
	}
}
