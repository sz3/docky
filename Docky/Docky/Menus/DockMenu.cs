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
using Wnck;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Interface;
using Docky.Services;

namespace Docky.Menus
{
	public class DockMenu : Gtk.Window
	{
		const int Padding   = 10;
		const int TailSize  = 20;
		const int TailWidth = 30;
		const int SliceSize = 18;
		const int SvgWidth  = 100;
		const int SvgHeight = 120;
		
		enum Slice {
			Top,
			Left,
			Right,
			Tail,
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight,
			TailLeft,
			TailRight,
			Center,
		}
		
		protected static bool IsLight { get; private set; }
		
		static DockySurface [] menu_slices;
		
		static DockySurface[] GetSlices (DockySurface model)
		{
			if (menu_slices != null)
				return menu_slices;
			
			DockySurface main = new DockySurface (SvgWidth, SvgHeight, model);
			
			using (Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (Docky.Controller.MenuSvg, -1)) {
				int dark = 0;
				int light = 0;
				unsafe {
					byte* pixelPtr = (byte*) pixbuf.Pixels;
					for (int i = 0; i < pixbuf.Height; i++) {
						for (int j = 0; j < pixbuf.Width; j++) {
							byte max = Math.Max (pixelPtr[0], Math.Max (pixelPtr[1], pixelPtr[2]));
							
							if (pixelPtr[3] > 0) {
								if (max > byte.MaxValue / 2)
									light++;
								else
									dark++;
							}
							
							pixelPtr += 4;
						}
						pixelPtr += pixbuf.Rowstride - pixbuf.Width * 4;
					}
				}
				IsLight = light > dark;
				
				Gdk.CairoHelper.SetSourcePixbuf (main.Context, pixbuf, 0, 0);
				main.Context.Paint ();
			}
			
			int middleWidth = SvgWidth - 2 * SliceSize;
			int middleHeight = SvgHeight - 2 * SliceSize - TailSize;
			int tailSliceSize = TailSize + SliceSize;
			int tailSideSize = (middleWidth - TailWidth) / 2;
			
			DockySurface[] results = new DockySurface[11];
			results[(int) Slice.TopLeft] = CreateSlice (main, new Gdk.Rectangle (
					0, 
					0, 
					SliceSize, 
					SliceSize));
			
			results[(int) Slice.Top] = CreateSlice (main, new Gdk.Rectangle (
					SliceSize, 
					0, 
					middleWidth, 
					SliceSize));
			
			results[(int) Slice.TopRight] = CreateSlice (main, new Gdk.Rectangle (
					SvgWidth - SliceSize, 
					0, 
					SliceSize, 
					SliceSize));
			
			results[(int) Slice.Left] = CreateSlice (main, new Gdk.Rectangle (
					0, 
					SliceSize, 
					SliceSize, 
					middleHeight));
			
			results[(int) Slice.Center] = CreateSlice (main, new Gdk.Rectangle (
					SliceSize, 
					SliceSize, 
					middleWidth, 
					middleHeight));
			
			results[(int) Slice.Right] = CreateSlice (main, new Gdk.Rectangle (
					SvgWidth - SliceSize, 
					SliceSize, 
					SliceSize, 
					middleHeight));
			
			results[(int) Slice.BottomLeft] = CreateSlice (main, new Gdk.Rectangle (
					0, 
					SvgHeight - tailSliceSize, 
					SliceSize, 
					tailSliceSize));
			
			results[(int) Slice.TailLeft] = CreateSlice (main, new Gdk.Rectangle (
					SliceSize, 
					SvgHeight - tailSliceSize, 
					tailSideSize, 
					tailSliceSize));
			
			results[(int) Slice.Tail] = CreateSlice (main, new Gdk.Rectangle (
					SliceSize + tailSideSize,
					SvgHeight - tailSliceSize,
					TailWidth,
					tailSliceSize));
				
			results[(int) Slice.TailRight] = CreateSlice (main, new Gdk.Rectangle (
					SliceSize + middleWidth - tailSideSize,
					SvgHeight - tailSliceSize,
					tailSideSize,
					tailSliceSize));
			
			results[(int) Slice.BottomRight] = CreateSlice (main, new Gdk.Rectangle (
					SliceSize + middleWidth,
					SvgHeight - tailSliceSize,
					SliceSize,
					tailSliceSize));
			
			menu_slices = results;
			
			main.Dispose ();
			
			return menu_slices;
		}
		
		static DockySurface CreateSlice (DockySurface original, Gdk.Rectangle area)
		{
			DockySurface result = new DockySurface (area.Width, area.Height, original);
			
			original.Internal.Show (result.Context, 0 - area.X, 0 - area.Y);
			
			return result;
		}
		
		DockySurface background_buffer;
		Gdk.Rectangle allocation;
		DockPosition orientation;
		DateTime show_time;
		
		protected Gtk.Bin Container { get; private set; }
		
		public Gdk.Point Anchor { get; set; }
		
		public new bool Visible { get; set; }
		
		public DockPosition Orientation {
			get { return orientation; }
			set {
				if (orientation == value)
					return;
				orientation = value;
				SetPadding ();
				ResetBackgroundBuffer ();
			} 
		}
		
		public DockMenu (Gtk.Window parent) : base(Gtk.WindowType.Popup)
		{
			AcceptFocus = false;
			Decorated = false;
			KeepAbove = true;
			AppPaintable = true;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			Resizable = false;
			Modal = true;
			TypeHint = WindowTypeHint.PopupMenu;
			
			AddEvents ((int) Gdk.EventMask.AllEventsMask);
			
			this.SetCompositeColormap ();
			
			Container = new Gtk.Alignment (0.5f, 0.5f, 0, 0);
			Container.Show ();
			
			Add (Container);
			
			SetPadding ();
			
			Docky.Controller.ThemeChanged += DockyControllerThemeChanged;
		}

		void DockyControllerThemeChanged (object sender, EventArgs e)
		{
			if (menu_slices != null) {
				foreach (DockySurface s in menu_slices) {
					s.Dispose ();
				}
				menu_slices = null;
			}
			
			if (background_buffer != null) {
				background_buffer.Dispose ();
				background_buffer = null;
			}
		}
		
		void SetPadding ()
		{
			(Container as Alignment).LeftPadding   = Orientation == DockPosition.Left   ? (uint) (TailSize + Padding) : (uint) Padding;
			(Container as Alignment).RightPadding  = Orientation == DockPosition.Right  ? (uint) (TailSize + Padding) : (uint) Padding;
			(Container as Alignment).TopPadding    = Orientation == DockPosition.Top    ? (uint) (TailSize + Padding) : (uint) Padding;
			(Container as Alignment).BottomPadding = Orientation == DockPosition.Bottom ? (uint) (TailSize + Padding) : (uint) Padding;
		}
		
		void Reposition ()
		{
			switch (Orientation) {
			case DockPosition.Bottom:
				Move (Anchor.X - allocation.Width / 2, Anchor.Y - allocation.Height);
				break;
			case DockPosition.Top:
				Move (Anchor.X - allocation.Width / 2, Anchor.Y);
				break;
			case DockPosition.Left:
				Move (Anchor.X, Anchor.Y - allocation.Height / 2);
				break;
			case DockPosition.Right:
				Move (Anchor.X - allocation.Width, Anchor.Y - allocation.Height / 2);
				break;
			}
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			this.allocation = allocation;
			ResetBackgroundBuffer ();
			Reposition ();
			base.OnSizeAllocated (allocation);
		}
		
		protected override void OnShown ()
		{
			Visible = true;
			show_time = DateTime.UtcNow;
			Reposition ();

			CursorTracker.ForDisplay (Display).Enabled = false;
			
			GLib.Timeout.Add (10, delegate {
				Gdk.GrabStatus status = Gdk.Pointer.Grab (
					GdkWindow, 
					true, 
					Gdk.EventMask.ButtonPressMask | 
					Gdk.EventMask.ButtonReleaseMask, 
					null, 
					null, 
					Gtk.Global.CurrentEventTime);
				
				if (status == GrabStatus.AlreadyGrabbed || status == GrabStatus.Success) {
					Gtk.Grab.Add (this);
					return false;
				}
				return true;
			
			});
			
			base.OnShown ();
		}
		
		protected override void OnHidden ()
		{
			Visible = false;
			CursorTracker.ForDisplay (Display).Enabled = true;
			base.OnHidden ();
		}

		
		void ResetBackgroundBuffer ()
		{
			if (background_buffer != null) {
				background_buffer.Dispose ();
				background_buffer = null;
			}
		}
		
		void DrawBackground (DockySurface surface)
		{
			DockySurface[] slices = GetSlices (surface);
			
			int middleWidth = surface.Width - 2 * SliceSize;
			int middleHeight = surface.Height - 2 * SliceSize - TailSize;
			int tailSliceSize = TailSize + SliceSize;
			int tailSideSize = (middleWidth - TailWidth) / 2;
			
			DrawSlice (surface, slices[(int) Slice.TopLeft], new Gdk.Rectangle (
					0, 
					0, 
					SliceSize, 
					SliceSize));
			
			DrawSlice (surface, slices[(int) Slice.Top], new Gdk.Rectangle (
					SliceSize, 
					0, 
					middleWidth, 
					SliceSize));
			
			DrawSlice (surface, slices[(int) Slice.TopRight], new Gdk.Rectangle (
					surface.Width - SliceSize, 
					0, 
					SliceSize, 
					SliceSize));
			
			DrawSlice (surface, slices[(int) Slice.Left], new Gdk.Rectangle (
					0, 
					SliceSize, 
					SliceSize, 
					middleHeight));
			
			DrawSlice (surface, slices[(int) Slice.Center], new Gdk.Rectangle (
					SliceSize, 
					SliceSize, 
					middleWidth, 
					middleHeight));
			
			DrawSlice (surface, slices[(int) Slice.Right], new Gdk.Rectangle (
					surface.Width - SliceSize, 
					SliceSize, 
					SliceSize, 
					middleHeight));
			
			DrawSlice (surface, slices[(int) Slice.BottomLeft], new Gdk.Rectangle (
					0, 
					surface.Height - tailSliceSize, 
					SliceSize, 
					tailSliceSize));
			
			if (tailSideSize > 0) {
				DrawSlice (surface, slices[(int) Slice.TailLeft], new Gdk.Rectangle (
						SliceSize, 
						surface.Height - tailSliceSize, 
						tailSideSize, 
						tailSliceSize));
			}
			
			DrawSlice (surface, slices[(int) Slice.Tail], new Gdk.Rectangle (
					SliceSize + tailSideSize,
					surface.Height - tailSliceSize,
					TailWidth,
					tailSliceSize));
				
			if (tailSideSize > 0) {
				DrawSlice (surface, slices[(int) Slice.TailRight], new Gdk.Rectangle (
						SliceSize + middleWidth - tailSideSize,
						surface.Height - tailSliceSize,
						tailSideSize,
						tailSliceSize));
			}
			
			DrawSlice (surface, slices[(int) Slice.BottomRight], new Gdk.Rectangle (
					SliceSize + middleWidth,
					surface.Height - tailSliceSize,
					SliceSize,
					tailSliceSize));
		}
		
		void DrawSlice (DockySurface target, DockySurface slice, Gdk.Rectangle area)
		{
			// simple case goes here
			if (area.Width == slice.Width && area.Height == slice.Height) {
				slice.Internal.Show (target.Context, area.X, area.Y);
				return;
			}
			
			int columns = (area.Width / slice.Width) + 1;
			int rows = (area.Height / slice.Height) + 1;
			
			target.Context.Rectangle (area.X, area.Y, area.Width, area.Height);
			target.Context.Clip ();
			
			for (int c = 0; c < columns; c++) {
				for (int r = 0; r < rows; r++) {
					int x = area.X + c * slice.Width;
					int y = area.Y + r * slice.Height;
					
					target.Context.SetSource (slice.Internal, x, y);
					target.Context.Rectangle (x, y, slice.Width, slice.Height);
					target.Context.Fill ();
				}
			}
			
			target.Context.ResetClip ();
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				if (background_buffer == null) {
					if (Orientation == DockPosition.Bottom || Orientation == DockPosition.Top) {
						background_buffer = new DockySurface (allocation.Width, allocation.Height, cr.Target);
					} else {
						// switch width and height so we can rotate it later
						background_buffer = new DockySurface (allocation.Height, allocation.Width, cr.Target);
					}
					DrawBackground (background_buffer);
				}
				
				switch (Orientation) {
				case DockPosition.Top:
					cr.Scale (1, -1);
					cr.Translate (0, 0 - background_buffer.Height);
					break;
				case DockPosition.Left:
					cr.Rotate (Math.PI * .5);
					cr.Translate (0, 0 - background_buffer.Height);
					break;
				case DockPosition.Right:
					cr.Rotate (Math.PI * -0.5);
					cr.Translate (0 - background_buffer.Width, 0);
					break;
				}
				
				cr.Operator = Operator.Source;
				background_buffer.Internal.Show (cr, 0, 0);
			}
			
			return base.OnExposeEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button != 3 || (DateTime.UtcNow - show_time).TotalMilliseconds > 500) {
				Hide ();
			}
			return base.OnButtonReleaseEvent (evnt);
		}
	}
}
