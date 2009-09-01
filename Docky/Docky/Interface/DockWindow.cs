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

using Docky.Items;
using Docky.CairoHelper;

namespace Docky.Interface
{


	public class DockWindow : Gtk.Window
	{
		const int    UrgentBounceHeight   = 80;
		const int    LaunchBounceHeight   = 30;
		const int    DockHeightBuffer   = 7;
		const int    DockWidthBuffer      = 5;
		
		DateTime hidden_change_time;
		DateTime cursor_over_dock_area_change_time;
		
		IDockPreferences preferences;
		
		DockySurface main_buffer, background_buffer, icons_buffer;
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		Gdk.Point WindowPosition;
		
		AutohideManager AutohideManager { get; set; }
		
		CursorTracker CursorTracker { get; set; }
		
		DockAnimationState AnimationState { get; set; }
		
		Dictionary<AbstractDockItem, Gdk.Rectangle> DrawRegions { get; set; }
		
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
							collection_backend.Add (null);
					
						collection_backend.AddRange (provider.Items);
						
						if (provider.Separated && provider != ItemProviders.Last ())
							collection_backend.Add (null);
					}
				}
				return collection_frontend;
			}
		}
		
		#region Preferences Shortcuts
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
		
		int ItemHorizontalBuffer {
			get { return (int) (0.08 * IconSize); }
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
		
		int ZoomedDockSize {
			get { return ZoomedIconSize + 2 * DockHeightBuffer; }
		}
		
		public DockWindow () : base (Gtk.WindowType.Toplevel)
		{
			DrawRegions = new Dictionary<AbstractDockItem, Gdk.Rectangle> ();
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
			AutohideManager.HiddenChanged += AutohideManagerHiddenChanged;
			AutohideManager.CursorIsOverDockAreaChanged += AutohideManagerCursorIsOverDockAreaChanged;
			
			Screen.SizeChanged += ScreenSizeChanged;
			
			SetSizeRequest ();
		}

		void ScreenSizeChanged (object sender, EventArgs e)
		{
			SetSizeRequest ();
		}

		void AutohideManagerCursorIsOverDockAreaChanged (object sender, EventArgs e)
		{
			cursor_over_dock_area_change_time = DateTime.UtcNow;
			
			if (AutohideManager.CursorIsOverDockArea)
				CursorTracker.RequestHighResolution (this);
			else
				CursorTracker.CancelHighResolution (this);
		}

		void AutohideManagerHiddenChanged (object sender, EventArgs e)
		{
			hidden_change_time = DateTime.UtcNow;
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			
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
			collection_backend.Clear ();
			
			if (args.Type == AddRemoveChangeType.Remove)
				DrawRegions.Remove (args.Item);
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
			collection_backend.Clear ();
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
			
		}

		void PreferencesAutohideChanged (object sender, EventArgs e)
		{
			
		}
		#endregion
		
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
		
		void SetSizeRequest ()
		{
			Gdk.Rectangle geo;
			geo = Screen.GetMonitorGeometry (Monitor);
			
			Width = geo.Width;
			Height = ZoomedIconSize + 2 * DockHeightBuffer + UrgentBounceHeight;
			Height = Math.Max (150, Height);
			
			SetSizeRequest (Width, Height);
		}
		#endregion
		
		#region Drawing
		void AnimatedDraw ()
		{
			
		}
		
		/// <summary>
		/// Updates all draw regions
		/// </summary>
		void UpdateDrawRegions ()
		{
			
			foreach (AbstractDockItem adi in Items) {
				if (adi == null) {
					// separator code here;
					continue;
				}
			}
		}
		
		void DrawDock (DockySurface surface)
		{
			UpdateDrawRegions ();
			
			Gdk.Rectangle first, last;
			
			// whilst not immediately obvious, the first item in the enumeration should always
			// be the first dock item, and the last should be the last. The proper data structure
			// is perhaps not a dictionary. Future consideration needed. Also note that Items may
			// not start or end with a null.
			first = DrawRegions [Items.First ()];
			last = DrawRegions [Items.Last ()];
			
			Gdk.Rectangle dockArea = new Gdk.Rectangle (0, 0, 0, 0);
			Gdk.Rectangle hotArea = new Gdk.Rectangle (0, 0, 0, 0);
			
			int hotAreaSize = (AutohideManager.Hidden) ? 1 : ZoomedDockSize;
			
			switch (Position) {
			case DockPosition.Top:
				dockArea.X = first.X - DockWidthBuffer;
				dockArea.Y = 0;
				dockArea.Width = last.X + DockWidthBuffer - dockArea.X;
				dockArea.Height = DockHeight;
				
				hotArea = dockArea;
				hotArea.Height = hotAreaSize;
				break;
			case DockPosition.Left:
				dockArea.X = 0;
				dockArea.Y = first.Y - DockWidthBuffer;
				dockArea.Width = DockHeight;
				dockArea.Height = last.Y + hotAreaSize - dockArea.Y;
				
				hotArea = dockArea;
				hotArea.Width = hotAreaSize;
				break;
			case DockPosition.Right:
				dockArea.X = surface.Width - DockHeight;
				dockArea.Y = first.Y - DockWidthBuffer;
				dockArea.Width = DockHeight;
				dockArea.Height = last.Y + DockWidthBuffer - dockArea.Y;
				
				hotArea = dockArea;
				hotArea.X = dockArea.X + dockArea.Width - hotAreaSize;
				hotArea.Width = hotAreaSize;
				break;
			case DockPosition.Bottom:
				dockArea.X = first.X - DockWidthBuffer;
				dockArea.Y = surface.Height - DockHeight;
				dockArea.Width = last.X + DockWidthBuffer - dockArea.X;
				dockArea.Height = DockHeight;
				
				hotArea = dockArea;
				hotArea.Y = dockArea.Y + dockArea.Height - hotAreaSize;
				hotArea.Height = hotAreaSize;
				break;
			}
			
			AutohideManager.SetDockArea (hotArea);
		}
		
		void DrawDockBackground (DockySurface surface, Gdk.Rectangle backgroundArea)
		{
			
		}
		
		protected override void OnStyleSet (Style previous_style)
		{
			if (GdkWindow != null)
				GdkWindow.SetBackPixmap (null, false);
			base.OnStyleSet (previous_style);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return true;
			
			return false;
		}
		#endregion
		
		public override void Dispose ()
		{
			UnregisterPreferencesEvents (Preferences);
			
			base.Dispose ();
		}
	}
}
