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
using System.Reflection;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

using Docky.Items;
using Docky.CairoHelper;

namespace Docky.Interface
{


	public class DockWindow : Gtk.Window
	{
		struct DrawValue 
		{
			public PointD Center;
			public double Zoom;
		}
		
		/*******************************************
		 * Note to reader:
		 * All values labeled X or width reference x or width as thought of from a horizontally positioned dock.
		 * This is because as the dock rotates, the math is largely unchanged, however there needs to be a consistent
		 * name for these directions regardless of orientation. The catch is that when speaking to cairo, x/y are
		 * normal
		 * *****************************************/
		
		const int UrgentBounceHeight = 80;
		const int LaunchBounceHeight = 30;
		const int DockHeightBuffer   = 7;
		const int DockWidthBuffer    = 5;
		const int ItemWidthBuffer    = 2;
		const int BackgroundWidth    = 1000;
		const int BackgroundHeight   = 128;
		
		readonly TimeSpan BaseAnimationTime = new TimeSpan (0, 0, 0, 0, 150);
		
		DateTime hidden_change_time;
		DateTime cursor_over_dock_area_change_time;
		DateTime render_time;
		
		IDockPreferences preferences;
		DockySurface main_buffer, background_buffer, icons_buffer;
		
		Gdk.Rectangle current_mask_area;
		double? zoom_in_buffer;
		bool rendering;
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		Gdk.Point WindowPosition;
		
		AutohideManager AutohideManager { get; set; }
		
		CursorTracker CursorTracker { get; set; }
		
		DockAnimationState AnimationState { get; set; }
		
		Dictionary<AbstractDockItem, DrawValue> DrawValues { get; set; }
		
		public IDockPreferences Preferences { 
			get { return preferences; }
			set {
				if (preferences == value)
					return;
				if (preferences != null)
					UnregisterPreferencesEvents (preferences);
				preferences = value;
				RegisterPreferencesEvents (value);
			}
		}
		
		List<AbstractDockItem> collection_backend;
		ReadOnlyCollection<AbstractDockItem> collection_frontend;
		
		/// <summary>
		/// Provides a list of all items to be displayed on the dock. Nulls are
		/// inserted where separators should go.
		/// </summary>
		ReadOnlyCollection<AbstractDockItem> Items {
			get {
				if (collection_backend.Count == 0) {
					AbstractDockItem last = null;
					foreach (IDockItemProvider provider in ItemProviders) {
						if (provider.Separated && last != null)
							collection_backend.Add (new SeparatorItem ());
					
						collection_backend.AddRange (provider.Items);
						
						if (provider.Separated && provider != ItemProviders.Last ())
							collection_backend.Add (new SeparatorItem ());
					}
				}
				return collection_frontend;
			}
		}
		
		#region Shortcuts
		AutohideType Autohide {
			get { return Preferences.Autohide; }
		}
		
		bool CursorIsOverDockArea {
			get { return AutohideManager.CursorIsOverDockArea; }
		}
		
		IEnumerable<IDockItemProvider> ItemProviders {
			get { return Preferences.ItemProviders; }
		}
		
		int IconSize {
			get { return Preferences.IconSize; }
		}
		
		//fixme
		int Monitor {
			get { return 0; }
		}
		
		
		DockPosition Position {
			get { return Preferences.Position; }
		}
		
		bool ZoomEnabled {
			get { return Preferences.ZoomEnabled; }
		}
		
		double ZoomPercent {
			get { return Preferences.ZoomPercent; }
		}
		#endregion
		
		Gdk.Point Cursor {
			get { return CursorTracker.Cursor; }
		}
		
		Gdk.Point RelativeCursor {
			get {
				return new Gdk.Point (Cursor.X - WindowPosition.X, 
				                      Cursor.Y - WindowPosition.Y);
			}
		}
		
		int DockHeight {
			get { return IconSize + 2 * DockHeightBuffer; }
		}
		
		int DockWidth {
			get; 
			set;
		}
		
		int ItemHorizontalBuffer {
			get { return (int) (0.08 * IconSize); }
		}
		
		int SeparatorSize {
			get { return (int) (.2 * IconSize); }
		}
		
		bool VerticalDock {
			get { return Position == DockPosition.Left || Position == DockPosition.Right; }
		}
		
		/// <summary>
		/// The int size a fully zoomed icon will display at.
		/// </summary>
		int ZoomedIconSize {
			get { 
				return ZoomEnabled ? (int) (IconSize * ZoomPercent) : IconSize; 
			}
		}
		
		int ZoomedDockHeight {
			get { return ZoomedIconSize + 2 * DockHeightBuffer; }
		}
		
		double ZoomIn {
			get {
				
				// we buffer this value during renders since it will be checked many times and we dont need to 
				// recalculate it each time
				if (zoom_in_buffer.HasValue && rendering) {
					return zoom_in_buffer.Value;
				}
				
				double zoom = Math.Min (1, (render_time - cursor_over_dock_area_change_time).TotalMilliseconds / 
				                        BaseAnimationTime.TotalMilliseconds);
				if (!CursorIsOverDockArea) {
					zoom = 1 - zoom;
				}
				
				if (rendering)
					zoom_in_buffer = zoom;
				
				return zoom;
			}
		}
		
		int ZoomSize {
			get { return (int) (330 * (IconSize / 64.0)); }
		}
		
		public DockWindow () : base (Gtk.WindowType.Toplevel)
		{
			DrawValues = new Dictionary<AbstractDockItem, DrawValue> ();
			AnimationState = new DockAnimationState ();
			BuildAnimationEngine ();
			
			collection_backend = new List<AbstractDockItem> ();
			collection_frontend = collection_backend.AsReadOnly ();
			
			AppPaintable    = true;
			AcceptFocus     = false;
			Decorated       = false;
			DoubleBuffered  = false;
			SkipPagerHint   = true;
			SkipTaskbarHint = true;
			Resizable       = false;
			CanFocus        = false;
			TypeHint        = WindowTypeHint.Dock;
			
			SetCompositeColormap ();
			Stick ();
			
			Realized += HandleRealized;	
		}
		
		void BuildAnimationEngine ()
		{
			
		}

		#region Event Handling
		void HandleRealized (object sender, EventArgs e)
		{
			GdkWindow.SetBackPixmap (null, false);
			
			CursorTracker = CursorTracker.ForDisplay (Display);
			CursorTracker.CursorPositionChanged += HandleCursorPositionChanged;	
			
			AutohideManager = new AutohideManager (Screen);
			AutohideManager.Behavior = Preferences.Autohide;
			AutohideManager.HiddenChanged += HandleHiddenChanged;
			AutohideManager.CursorIsOverDockAreaChanged += HandleCursorIsOverDockAreaChanged;
			
			Screen.SizeChanged += ScreenSizeChanged;
			
			SetSizeRequest ();
			UpdateDockWidth ();
		}

		void ScreenSizeChanged (object sender, EventArgs e)
		{
			SetSizeRequest ();
		}

		void HandleCursorIsOverDockAreaChanged (object sender, EventArgs e)
		{
			cursor_over_dock_area_change_time = DateTime.UtcNow;
			
			if (CursorIsOverDockArea)
				CursorTracker.RequestHighResolution (this);
			else
				CursorTracker.CancelHighResolution (this);
		}

		void HandleHiddenChanged (object sender, EventArgs e)
		{
			hidden_change_time = DateTime.UtcNow;
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			QueueDraw ();
		}
		
		void RegisterItemProvider (IDockItemProvider provider)
		{
			provider.ItemsChanged += ProviderItemsChanged;
		}
		
		void UnregisterItemProvider (IDockItemProvider provider)
		{
			provider.ItemsChanged -= ProviderItemsChanged;
		}
		
		void ProviderItemsChanged (object sender, ItemsChangedArgs args)
		{
			UpdateCollectionBuffer ();
			
			if (args.Type == AddRemoveChangeType.Remove)
				DrawValues.Remove (args.Item);
		}
		
		void RegisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged    += PreferencesAutohideChanged;	
			preferences.IconSizeChanged    += PreferencesIconSizeChanged;	
			preferences.PositionChanged    += PreferencesPositionChanged;
			preferences.ZoomEnabledChanged += PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged += PreferencesZoomPercentChanged;
			
			preferences.ItemProvidersChanged += PreferencesItemProvidersChanged;	
			
			foreach (IDockItemProvider provider in preferences.ItemProviders)
				RegisterItemProvider (provider);
		}

		void UnregisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged    -= PreferencesAutohideChanged;	
			preferences.IconSizeChanged    -= PreferencesIconSizeChanged;	
			preferences.PositionChanged    -= PreferencesPositionChanged;
			preferences.ZoomEnabledChanged -= PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged -= PreferencesZoomPercentChanged;
			
			preferences.ItemProvidersChanged -= PreferencesItemProvidersChanged;
			foreach (IDockItemProvider provider in preferences.ItemProviders)
				UnregisterItemProvider (provider);
		}
		
		void PreferencesItemProvidersChanged (object sender, ItemProvidersChangedEventArgs e)
		{
			if (e.Type == AddRemoveChangeType.Add) {
				RegisterItemProvider (e.Provider);
			} else {
				UnregisterItemProvider (e.Provider);
			}
			UpdateCollectionBuffer ();
		}

		void PreferencesZoomPercentChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesZoomEnabledChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesPositionChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesIconSizeChanged (object sender, EventArgs e)
		{
			UpdateDockWidth ();
		}

		void PreferencesAutohideChanged (object sender, EventArgs e)
		{
			
		}
		#endregion
		
		void UpdateCollectionBuffer ()
		{
			// dispose of our separators as we made them ourselves,
			// this could be a bit more elegant
			foreach (AbstractDockItem item in Items.Where (adi => adi is SeparatorItem))
				item.Dispose ();
			
			collection_backend.Clear ();
			UpdateDockWidth ();
		}
		
		void SetCompositeColormap ()
		{
			Gdk.Colormap colormap;

            colormap = Screen.RgbaColormap;
            if (colormap == null) {
                    colormap = Screen.RgbColormap;
                    Console.Error.WriteLine ("No alpha support.");
            }
            
            Colormap = colormap;
            colormap.Dispose ();
		}
		
		#region Size and Position
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Reposition ();
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			Reposition ();
		}
		
		protected override bool OnConfigureEvent (EventConfigure evnt)
		{
			WindowPosition.X = evnt.X;
			WindowPosition.Y = evnt.Y;
			
			return base.OnConfigureEvent (evnt);
		}
		
		void Reposition ()
		{
			Gdk.Rectangle geo;
			geo = Screen.GetMonitorGeometry (Monitor);
			
			switch (Position) {
			default:
			case DockPosition.Top:
			case DockPosition.Left:
				Move (geo.X, geo.Y);
				break;
			case DockPosition.Right:
				Move (geo.X + geo.Width - Width, geo.Y);
				break;
			case DockPosition.Bottom:
				Move (geo.X, geo.Y + geo.Height - Height);
				break;
			}
		}
		
		void UpdateDockWidth ()
		{
			if (GdkWindow == null)
				return;
			
			Surface model;
			if (background_buffer != null) {
				model = main_buffer.Internal;
			} else {
				using (Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow)) {
					model = cr.Target;
				}
			}
			
			DockWidth = Items.Select (adi => adi.IconSurface (model, IconSize))
					         .Sum (s => s.Width);
			DockWidth += 2 * DockWidthBuffer;
		}
		
		void SetSizeRequest ()
		{
			Gdk.Rectangle geo;
			geo = Screen.GetMonitorGeometry (Monitor);
			
			if (VerticalDock) {
				Height = geo.Height;
				Width = ZoomedIconSize + 2 * DockHeightBuffer + UrgentBounceHeight;
				Width = Math.Max (150, Width);
			} else {
				Width = geo.Width;
				Height = ZoomedIconSize + 2 * DockHeightBuffer + UrgentBounceHeight;
				Height = Math.Max (150, Height);
			}
			SetSizeRequest (Width, Height);
		}
		#endregion
		
		#region Drawing
		void AnimatedDraw ()
		{
			
		}
		
		// fixme, this ONLY works for square items
		Gdk.Rectangle DrawValueToRectangle (DrawValue val, int iconSize)
		{
			return new Gdk.Rectangle ((int) (val.Center.X - (iconSize * val.Zoom / 2)),
			                          (int) (val.Center.Y - (iconSize * val.Zoom / 2)),
			                          (int) (iconSize * val.Zoom),
			                          (int) (iconSize * val.Zoom));
		}
		
		/// <summary>
		/// Updates drawing regions for the supplied surface
		/// </summary>
		/// <param name="surface">
		/// The <see cref="DockySurface"/> surface off which the coordinates will be based
		/// </param>
		void UpdateDrawRegionsForSurface (DockySurface surface)
		{
			// first we do the math as if this is a top dock, to do this we need to set
			// up some "pretend" variables. we pretend we are a top dock because 0,0 is
			// at the top.
			int width;
			int height;
			double zoom;
			
			// our width and height switch around if we have a veritcal dock
			if (!VerticalDock) {
				width = surface.Width;
				height = surface.Height;
			} else {
				width = surface.Height;
				height = surface.Width;
			}
			
			Gdk.Point cursor = Cursor;
			
			Gdk.Rectangle geo;
			geo = Screen.GetMonitorGeometry (Monitor);
			
			// screen shift sucks
			cursor.X -= geo.X;
			cursor.Y -= geo.Y;
			
			// "relocate" our cursor to be on the top
			switch (Position) {
			case DockPosition.Top:
				;
				break;
			case DockPosition.Left:
				int tmpY = cursor.Y;
				cursor.Y = cursor.X;
				cursor.X = width - (width - tmpY);
				break;
			case DockPosition.Right:
				tmpY = cursor.Y;
				cursor.X = (geo.Width - 1) - cursor.X;
				cursor.Y = cursor.X;
				cursor.X = width - (width - tmpY);
				break;
			case DockPosition.Bottom:
				cursor.Y = (geo.Height - 1) - cursor.Y;
				break;
			}
			
			// the line along the dock width about which the center of unzoomed icons sit
			int midline = DockHeight / 2;
			
			// the left most edge of the first dock item
			int startX = ((width - DockWidth) / 2) + DockWidthBuffer + ItemWidthBuffer;
			
			Gdk.Point center = new Gdk.Point (startX, midline);
			
			foreach (AbstractDockItem adi in Items) {
				DrawValue val = new DrawValue ();
				
				// div by 2 may result in rounding errors? Will this render OK? Shorts WidthBuffer by 1?
				int halfSize;
				if (adi.Square) {
					halfSize = ItemWidthBuffer + IconSize / 2;
				} else {
					DockySurface icon = adi.IconSurface (surface.Internal, IconSize);
					
					// yeah I am pretty sure...
					if (adi.Square || adi.RotateWidthDock || !VerticalDock) {
						halfSize = ItemWidthBuffer + icon.Width / 2;
					} else {
						halfSize = ItemWidthBuffer + icon.Height / 2;
					}
				}
				// center now represents our midpoint
				center.X += halfSize;
				
				if (ZoomPercent > 1) {
					// get us some handy doubles with fancy names
					double cursorPosition = cursor.X;
					double centerPosition = center.X;
					
					// ZoomPercent is a number greater than 1.  It should never be less than one.
					// ZoomIn is a range of 0 to 1. we need a number that is 1 when ZoomIn is 0, 
					// and ZoomPercent when ZoomIn is 1.  Then we treat this as 
					// if it were the ZoomPercent for the rest of the calculation
					double zoomInPercent = 1 + (ZoomPercent - 1) * ZoomIn;
					
					// offset from the center of the true position, ranged between 0 and half of the zoom range
					double offset = Math.Min (Math.Abs (cursorPosition - centerPosition), ZoomSize / 2);
					
					double offsetPercent = offset / (ZoomSize / 2.0);
					// zoom is calculated as 1 through target_zoom (default 2).  
					// The larger your offset, the smaller your zoom
					
					// First we get the point on our curve that defines out current zoom
					// offset is always going to fall on a point on the curve >= 0
					zoom = 1 - offsetPercent * offsetPercent;
					
					// scale this to match out zoomInPercent
					zoom = 1 + zoom * (zoomInPercent - 1);
					
					// pull in our offset to make things less spaced out
					// explaination since this is a bit tricky...
					// we have three terms, basically offset = f(x) * h(x) * g(x)
					// f(x) == offset identify
					// h(x) == a number from 0 to DockPreference.ZoomPercent - 1.  This is used to get the smooth "zoom in" effect.
					//         additionally serves to "curve" the offset based on the max zoom
					// g(x) == a term used to move the ends of the zoom inward.  Precalculated that the edges should be 66% of the current
					//         value. The center is 100%. (1 - offsetPercent) == 0,1 distance from center
					// The .66 value comes from the area under the curve.  Dont as me to explain it too much because it's too clever for me
					offset = offset * (zoomInPercent - 1) * (1 - offsetPercent / 3);
					
					if (cursorPosition > centerPosition) {
						centerPosition -= offset;
					} else {
						centerPosition += offset;
					}
					
					if (!adi.Zoom) {
						val.Zoom = 1;
						val.Center = new Cairo.PointD (centerPosition, center.Y);
					} else {
						double zoomedCenterHeight = DockHeightBuffer + (IconSize * zoom / 2.0);
						
						if (zoom == 1)
							centerPosition = Math.Round (centerPosition);
						
						val.Center = new Cairo.PointD (centerPosition, zoomedCenterHeight);
						val.Zoom = zoom;
					}
				} else {
					val.Zoom = 1;
					val.Center = new PointD (center.X, center.Y);
				}
				
				// move past midpoint to end of icon
				center.X += halfSize;
				
				// now we undo our transforms to the point
				switch (Position) {
				case DockPosition.Top:
					;
					break;
				case DockPosition.Left:
					double tmpY = val.Center.Y;
					val.Center.Y = val.Center.X;
					val.Center.X = width - (width - tmpY);
					break;
				case DockPosition.Right:
					tmpY = val.Center.Y;
					val.Center.Y = val.Center.X;
					val.Center.X = width - (width - tmpY);
					val.Center.X = (height - 1) - val.Center.X;
					
					break;
				case DockPosition.Bottom:
					val.Center.Y = (height - 1) - val.Center.Y;
					break;
				}
				
				val.Center.X += geo.X;
				val.Center.Y += geo.Y;
				
				Console.WriteLine ("Dock: {0} Width: {1} Height {2}", Position, Width, Height);
				Console.WriteLine ("Point: {0} {1}", val.Center.X, val.Center.Y);
				Console.WriteLine ();
				
				DrawValues [adi] = val;
			}
		}
		
		void GetDockAreaOnSurface (DockySurface surface, out Gdk.Rectangle dockArea, out Gdk.Rectangle cursorArea)
		{
			DrawValue firstDv, lastDv;
			Gdk.Rectangle first, last;
			
			firstDv = DrawValues [Items [0]];
			lastDv  = DrawValues [Items [Items.Count - 1]];
			
			first = DrawValueToRectangle (firstDv, IconSize);
			last  = DrawValueToRectangle (lastDv, IconSize);
			
			dockArea = new Gdk.Rectangle (0, 0, 0, 0);
			cursorArea = new Gdk.Rectangle (0, 0, 0, 0);
			
			int hotAreaSize;
			if (AutohideManager.Hidden) {
				hotAreaSize = 1;
			} else if (CursorIsOverDockArea) {
				hotAreaSize = ZoomedDockHeight;
			} else {
				hotAreaSize = DockHeight;
			}
			
			switch (Position) {
			case DockPosition.Top:
				dockArea.X = first.X - DockWidthBuffer;
				dockArea.Y = 0;
				dockArea.Width = (last.X + last.Width + DockWidthBuffer) - dockArea.X;
				dockArea.Height = DockHeight;
				
				cursorArea = dockArea;
				cursorArea.Height = hotAreaSize;
				break;
			case DockPosition.Left:
				dockArea.X = 0;
				dockArea.Y = first.Y - DockWidthBuffer;
				dockArea.Width = DockHeight;
				dockArea.Height = (last.Y + last.Height + DockWidthBuffer) - dockArea.Y;
				
				cursorArea = dockArea;
				cursorArea.Width = hotAreaSize;
				break;
			case DockPosition.Right:
				dockArea.X = surface.Width - DockHeight;
				dockArea.Y = first.Y - DockWidthBuffer;
				dockArea.Width = DockHeight;
				dockArea.Height = (last.Y + last.Height + DockWidthBuffer) - dockArea.Y;
				
				cursorArea = dockArea;
				cursorArea.X = dockArea.X + dockArea.Width - hotAreaSize;
				cursorArea.Width = hotAreaSize;
				break;
			case DockPosition.Bottom:
				dockArea.X = first.X - DockWidthBuffer;
				dockArea.Y = surface.Height - DockHeight;
				dockArea.Width = (last.X + last.Width + DockWidthBuffer) - dockArea.X;
				dockArea.Height = DockHeight;
				
				cursorArea = dockArea;
				cursorArea.Y = dockArea.Y + dockArea.Height - hotAreaSize;
				cursorArea.Height = hotAreaSize;
				break;
			}
			
//			Console.WriteLine ("Dock Area: {0}", dockArea);
		}
		
		void DrawDock (DockySurface surface)
		{
			surface.Clear ();
			UpdateDrawRegionsForSurface (surface);
			
			Gdk.Rectangle dockArea, cursorArea;
			GetDockAreaOnSurface (surface, out dockArea, out cursorArea);
			
			DrawDockBackground (surface, dockArea);
			
			double zOffset = ZoomedIconSize / (double) IconSize;
			foreach (AbstractDockItem adi in Items) {
				DrawValue val = DrawValues [adi];
				DockySurface icon = adi.IconSurface (surface.Internal, ZoomedIconSize);
				icon.ShowAtPointAndZoom (surface, val.Center, val.Zoom / zOffset);
			}
			
			SetInputMask (cursorArea);
			
			// adjust our cursor area for our position
			cursorArea.X += WindowPosition.X;
			cursorArea.Y += WindowPosition.Y;
			AutohideManager.SetDockArea (cursorArea);
		}
		
		void DrawDockBackground (DockySurface surface, Gdk.Rectangle backgroundArea)
		{
			if (background_buffer == null) {
				if (VerticalDock) {
					background_buffer = new DockySurface (BackgroundHeight, BackgroundWidth, surface.Internal);
				} else {
					background_buffer = new DockySurface (BackgroundWidth, BackgroundHeight, surface.Internal);
				}
					
					
				Gdk.Pixbuf background = new Gdk.Pixbuf (Assembly.GetExecutingAssembly (), "classic.svg");
				
				Gdk.Pixbuf tmp;
				
				switch (Position) {
				case DockPosition.Top:
					tmp = background.RotateSimple (PixbufRotation.Upsidedown);
					background.Dispose ();
					background = tmp;
					break;
				case DockPosition.Left:
					tmp = background.RotateSimple (PixbufRotation.Clockwise);
					background.Dispose ();
					background = tmp;
					break;
				case DockPosition.Right:
					tmp = background.RotateSimple (PixbufRotation.Counterclockwise);
					background.Dispose ();
					background = tmp;
					break;
				case DockPosition.Bottom:
					;
					break;
				}
				
				Gdk.CairoHelper.SetSourcePixbuf (background_buffer.Context, background, 0, 0);
				background_buffer.Context.Paint ();
			}
			
			Cairo.Context context = surface.Context;

			int xOffset = 0;
			int yOffset = 0;
			
			switch (Position) {
			case DockPosition.Left:
				xOffset = background_buffer.Width - backgroundArea.Width;
				break;
			case DockPosition.Top:
				yOffset = background_buffer.Height - backgroundArea.Height;
				break;
			}
			
			if (VerticalDock) {
				context.SetSource (background_buffer.Internal, 
				                   backgroundArea.X - xOffset, 
				                   backgroundArea.Y - yOffset);
				
				context.Rectangle (backgroundArea.X, 
				                   backgroundArea.Y, 
				                   backgroundArea.Width, 
				                   backgroundArea.Height / 2);
				context.Fill ();
				
				context.SetSource (background_buffer.Internal, 
				                   backgroundArea.X - xOffset, 
				                   backgroundArea.Y + backgroundArea.Height - background_buffer.Height - yOffset);
				context.Rectangle (backgroundArea.X, 
				                   backgroundArea.Y + backgroundArea.Height / 2, 
				                   backgroundArea.Width, 
				                   backgroundArea.Height - backgroundArea.Height / 2);
				context.Fill ();
			} else {
				context.SetSource (background_buffer.Internal, 
				                   backgroundArea.X - xOffset, 
				                   backgroundArea.Y - yOffset);
				
				context.Rectangle (backgroundArea.X - xOffset, 
				                   backgroundArea.Y, 
				                   backgroundArea.Width / 2, 
				                   backgroundArea.Height);
				context.Fill ();
				
				context.SetSource (background_buffer.Internal, 
				                   backgroundArea.X + backgroundArea.Width - background_buffer.Width, 
				                   backgroundArea.Y - yOffset);
				context.Rectangle (backgroundArea.X + backgroundArea.Width / 2, 
				                   backgroundArea.Y, 
				                   backgroundArea.Width - backgroundArea.Width / 2, 
				                   backgroundArea.Height);
				context.Fill ();
			}
			
			context.IdentityMatrix ();
		}
		
		protected override void OnStyleSet (Style previous_style)
		{
			if (GdkWindow != null)
				GdkWindow.SetBackPixmap (null, false);
			base.OnStyleSet (previous_style);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized || !Items.Any ())
				return true;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				
				render_time = DateTime.UtcNow;
				rendering = true;
				zoom_in_buffer = null;
				
				if (main_buffer == null || main_buffer.Width != Width || main_buffer.Height != Height) {
					if (main_buffer != null)
						main_buffer.Dispose ();
					main_buffer = new DockySurface (Width, Height, cr.Target);
				}
				
				DrawDock (main_buffer);
				cr.Operator = Operator.Source;
				cr.SetSource (main_buffer.Internal, 0, 0);
				cr.Paint ();
				rendering = false;
			}
			
			return false;
		}

		#endregion
		
		#region XServer Related
		void SetInputMask (Gdk.Rectangle area)
		{
			if (!IsRealized || current_mask_area == area)
				return;

			current_mask_area = area;
			if (area.Width == 0 || area.Height == 0) {
				InputShapeCombineMask (null, 0, 0);
				return;
			}

			Gdk.Pixmap pixmap = new Gdk.Pixmap (null, area.Width, area.Height, 1);
			Context cr = Gdk.CairoHelper.Create (pixmap);
			
			cr.Color = new Cairo.Color (0, 0, 0, 1);
			cr.Paint ();

			InputShapeCombineMask (pixmap, area.X, area.Y);
			
			(cr as IDisposable).Dispose ();
			pixmap.Dispose ();
		}
		#endregion
		
		public override void Dispose ()
		{
			UnregisterPreferencesEvents (Preferences);
			
			// clear out our separators
			foreach (AbstractDockItem adi in Items.Where (adi => adi is SeparatorItem))
				adi.Dispose ();
			
			
			base.Dispose ();
		}
	}
}
