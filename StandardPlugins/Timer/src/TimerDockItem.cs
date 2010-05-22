//  
//  Copyright (C) 2010 Robert Dyer
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

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Timer
{
	public class TimerDockItem : AbstractDockItem
	{
		static uint id_counter = 0;
		uint id = 0;
		public override string UniqueID ()
		{
			return "TimerItem#" + id;
		}
		
		public event EventHandler Finished;
		
		uint Length { get; set; }
		
		uint remaining;
		uint Remaining {
			get {
				return remaining;
			}
			set {
				if (remaining == value)
					return;
				
				remaining = value;
				
				UpdateHoverText ();
				QueueRedraw ();
				
				if (remaining == 0)
					OnFinished ();
			}
		}
		
		DateTime LastRender { get; set; }
		
		bool Running { get; set; }
		
		uint timer;
		
		void OnFinished ()
		{
			Log.Notify ("Docky Timer", "clock", string.Format (Catalog.GetString ("A timer set for {0} has expired."), TimerMainDockItem.TimeRemaining (Length)));
			if (Finished != null)
				Finished (this, EventArgs.Empty);
		}
		
		public TimerDockItem ()
		{
			id = id_counter++;
			Remaining = Length = TimerMainDockItem.DefaultTimer;
			LastRender = DateTime.UtcNow;
			Running = false;
			
			if (TimerMainDockItem.AutoStartTimers)
				Toggle ();
		}

		protected override void PaintIconSurface (DockySurface surface)
		{
			Context cr = surface.Context;
			int size = Math.Min (surface.Width, surface.Height);
			double center = size / 2.0;
			
			double percent = (double) Remaining / (double) Length;
			percent -= ((double) (DateTime.UtcNow - LastRender).TotalMilliseconds / 1000.0) * (1 / (double) Length);
			
			cr.Arc (center, center, center, -Math.PI / 2.0, 3.0 * Math.PI / 2.0);
			cr.Color = new Cairo.Color (1, 0, 0, 1 - percent);
			cr.Fill ();
			
			cr.MoveTo (center, center);
			cr.Arc (center, center, center, -Math.PI / 2.0, Math.PI * 2.0 * percent - Math.PI / 2.0);
			cr.LineTo (center, center);
			cr.Color = new Cairo.Color (1, 1, 1, 0.8);
			cr.Fill ();
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			uint amount = 1;
			
			if ((mod & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)
				amount = 60;
			else if ((mod & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
				amount = 3600;
			
			if (direction == Gdk.ScrollDirection.Up || direction == Gdk.ScrollDirection.Right) {
				Remaining += amount;
				Length += amount;
			} else if (Remaining > amount) {
				Remaining -= amount;
				Length -= amount;
			}
			
			UpdateHoverText ();
			QueueRedraw ();
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1)
				Toggle ();
			
			return ClickAnimation.None;
		}
		
		public void Toggle ()
		{
			if (timer != 0)
				GLib.Source.Remove (timer);
			
			Running = !Running;
			
			if (Running) {
				LastRender = DateTime.UtcNow;
				
				timer = GLib.Timeout.Add (200, () => { 
					if (DateTime.UtcNow.Second != LastRender.Second) {
						Remaining--;
						LastRender = DateTime.UtcNow;
					}
					
					QueueRedraw ();
					
					if (Remaining == 0) {
						timer = 0;
						return false;
					}
					
					return true;
				});
			}
			
			UpdateHoverText ();
		}
		
		void UpdateHoverText ()
		{
			String text;
			
			if (Running)
				text = Catalog.GetString ("Time remaining:") + " ";
			else
				text = Catalog.GetString ("Timer paused, time remaining:") + " ";
			
			HoverText = text + TimerMainDockItem.TimeRemaining (remaining);
		}
		
		public override void Dispose ()
		{
			if (Running)
				Toggle ();
			base.Dispose ();
		}
	}
}
