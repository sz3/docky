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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Cairo;
using Gdk;
using Gtk;
using Wnck;

using Docky.Items;
using Docky.CairoHelper;
using Docky.Menus;
using Docky.Painters;
using Docky.Services;
using Docky.Xlib;

namespace Docky.Interface
{


	public class DockWindow : Gtk.Window
	{
		struct DrawValue 
		{
			public PointD Center;
			public PointD StaticCenter;
			public Gdk.Rectangle HoverArea;
			public double Zoom;
			
			public DrawValue MoveIn (DockPosition position, double amount)
			{
				DrawValue result = new DrawValue {
					Center = Center,
					StaticCenter = StaticCenter,
					HoverArea = HoverArea,
					Zoom = Zoom
				};
				
				switch (position) {
				case DockPosition.Top:
					result.Center.Y += amount;
					result.StaticCenter.Y += amount;
					break;
				case DockPosition.Left:
					result.Center.X += amount;
					result.StaticCenter.X += amount;
					break;
				case DockPosition.Right:
					result.Center.X -= amount;
					result.StaticCenter.X -= amount;
					break;
				case DockPosition.Bottom:
					result.Center.Y -= amount;
					result.StaticCenter.Y -= amount;
					break;
				}
				
				return result;
			}
			
			public DrawValue MoveRight (DockPosition position, double amount)
			{
				DrawValue result = new DrawValue {
					Center = Center,
					StaticCenter = StaticCenter,
					HoverArea = HoverArea,
					Zoom = Zoom
				};
				
				switch (position) {
				case DockPosition.Top:
					result.Center.X += amount;
					result.StaticCenter.X += amount;
					break;
				case DockPosition.Left:
					result.Center.Y += amount;
					result.StaticCenter.Y += amount;
					break;
				case DockPosition.Right:
					result.Center.Y -= amount;
					result.StaticCenter.Y -= amount;
					break;
				case DockPosition.Bottom:
					result.Center.X -= amount;
					result.StaticCenter.X -= amount;
					break;
				}
				
				return result;
			}
		}
		
		static DateTime UpdateTimeStamp (DateTime lastStamp, TimeSpan animationLength)
		{
			TimeSpan delta = DateTime.UtcNow - lastStamp;
			if (delta < animationLength)
				return DateTime.UtcNow.Subtract (animationLength - delta);
			return DateTime.UtcNow;
		}
		
		/*******************************************
		 * Note to reader:
		 * All values labeled X or width reference x or width as thought of from a horizontally positioned dock.
		 * This is because as the dock rotates, the math is largely unchanged, however there needs to be a consistent
		 * name for these directions regardless of orientation. The catch is that when speaking to cairo, x/y are
		 * normal
		 * *****************************************/
		
		public event EventHandler<HoveredItemChangedArgs> HoveredItemChanged;
		
		const int UrgentBounceHeight  = 80;
		const int LaunchBounceHeight  = 30;
		const int DockHeightBuffer    = 7;
		const int DockWidthBuffer     = 5;
		const int BackgroundWidth     = 1000;
		const int BackgroundHeight    = 150;
		const int NormalIndicatorSize = 20;
		const int UrgentIndicatorSize = 26;
		const int GlowSize            = 30;
		
		readonly TimeSpan BaseAnimationTime = new TimeSpan (0, 0, 0, 0, 150);
		readonly TimeSpan BounceTime = new TimeSpan (0, 0, 0, 0, 600);
		
		DateTime hidden_change_time;
		DateTime dock_hovered_change_time;
		DateTime render_time;
		DateTime items_change_time;
		DateTime remove_time;
		
		IDockPreferences preferences;
		DockySurface main_buffer, background_buffer, icon_buffer, painter_buffer;
		DockySurface normal_indicator_buffer, urgent_indicator_buffer;
		AbstractDockItem hoveredItem;
		AbstractDockPainter painter;
		
		Gdk.Rectangle monitor_geo;
		Gdk.Rectangle current_mask_area;
		Gdk.Rectangle painter_area;
		Gdk.Point window_position;
		
		double? zoom_in_buffer;
		bool rendering;
		bool update_screen_regions;
		bool repaint_painter;
		bool active_glow;
		bool config_mode;
		
		/// <summary>
		/// Used as a decimal representation of the "index" of where the old item used to be
		/// </summary>
		double remove_index;
		int remove_size;
		
		uint animation_timer;
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		int MaxIconSize { get; set; }
		
		bool ExternalDragActive { get { return DragTracker.ExternalDragActive; } }
		
		bool InternalDragActive { get { return DragTracker.InternalDragActive; } }
		
		bool HoveredAcceptsDrop { get; set; }
		
		internal AutohideManager AutohideManager { get; private set; }
		
		internal CursorTracker CursorTracker { get; private set; }
		
		internal DockDragTracker DragTracker { get; private set; }
		
		internal HoverTextManager TextManager { get; private set; }
		
		AnimationState AnimationState { get; set; }
		
		DockItemMenu Menu { get; set; }
		
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
				
				// Initialize value
				MaxIconSize = preferences.IconSize;
			}
		}
		
		public bool ActiveGlow {
			get {
				return active_glow;
			}
			set {
				if (active_glow == value)
					return;
				active_glow = value;
				AnimatedDraw ();
			}
		}
		
		public bool ConfigurationMode {
			get {
				return config_mode;
			}
			set {
				if (config_mode == value)
					return;
				config_mode = value;
				DragTracker.RepositionMode = config_mode;
				update_screen_regions = true;
				
				SetTooltipVisibility ();
				AnimatedDraw ();
			}
		}
		
		List<AbstractDockItem> collection_backend;
		ReadOnlyCollection<AbstractDockItem> collection_frontend;
		
		/// <summary>
		/// Provides a list of all items to be displayed on the dock. Nulls are
		/// inserted where separators should go.
		/// </summary>
		public ReadOnlyCollection<AbstractDockItem> Items {
			get {
				if (collection_backend.Count == 0) {
					update_screen_regions = true;
					if (Preferences.DefaultProvider.IsWindowManager)
						collection_backend.Add (new DockyItem ());
					
					bool priorItems = false;
					bool separatorNeeded = false;
					foreach (AbstractDockItemProvider provider in ItemProviders) {
						if (!provider.Items.Any ())
							continue;
						
						if (provider.Separated && priorItems || separatorNeeded)
							collection_backend.Add (new SeparatorItem ());
					
						collection_backend.AddRange (provider.Items.OrderBy (i => i.Position));
						priorItems = true;
						
						separatorNeeded = provider.Separated;
					}
					
					for (int i = collection_backend.Count; i < 2; i++) {
						collection_backend.Add (new SpacingItem ());
					}
				}
				return collection_frontend;
			}
		}
		
		#region Shortcuts
		AutohideType Autohide {
			get { return Preferences.Autohide; }
		}
		
		internal bool DockHovered {
			get { return AutohideManager.DockHovered; }
		}
		
		internal AbstractDockItem HoveredItem {
			get {
				if (!DockHovered)
					return null;
				return hoveredItem;
			}
			private set {
				if (hoveredItem == value)
					return;
				AbstractDockItem last = hoveredItem;
				hoveredItem = value;
				SetHoveredAcceptsDrop ();
				OnHoveredItemChanged (last);
				
				UpdateHoverText ();
				SetTooltipVisibility ();
			}
		}
		
		void UpdateHoverText ()
		{
			if (hoveredItem != null && background_buffer != null) {
				DrawValue loc = DrawValues[hoveredItem].MoveIn (Position, IconSize * (ZoomPercent + .1) - IconSize / 2);
				
				Gdk.Point point = new Gdk.Point ((int) loc.StaticCenter.X, (int) loc.StaticCenter.Y);
				point.X += window_position.X;
				point.Y += window_position.Y;
				
				TextManager.Gravity = Position; // FIXME
				TextManager.SetSurfaceAtPoint (hoveredItem.HoverTextSurface (background_buffer, Style), point); 
			}
		}
		
		internal AbstractDockItem ClosestItem {
			get {
				return Items
					.Where (adi => !(adi is INonPersistedItem) && DrawValues.ContainsKey (adi))
					.OrderBy (adi => Math.Abs (VerticalDock ? DrawValues[adi].Center.Y - LocalCursor.Y : DrawValues[adi].Center.X - LocalCursor.X))
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			}
		}
		
		internal AbstractDockItem RightMostItem {
			get {
				return Items
					.Where (adi => !(adi is INonPersistedItem) && DrawValues.ContainsKey (adi))
					.Where (adi => (VerticalDock ? DrawValues[adi].Center.Y - LocalCursor.Y : DrawValues[adi].Center.X - LocalCursor.X) > 0)
					.OrderBy (adi => Math.Abs (VerticalDock ? DrawValues[adi].Center.Y - LocalCursor.Y : DrawValues[adi].Center.X - LocalCursor.X))
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			}
		}
		
		internal AbstractDockItemProvider HoveredProvider {
			get {
				if (!DockHovered)
					return null;
				
				AbstractDockItem closest = HoveredItem ?? ClosestItem;
				if (closest != null && closest.Owner != null) {
					return closest.Owner;
				}
				
				return Preferences.DefaultProvider;
			}
		}
		
		internal IEnumerable<AbstractDockItemProvider> ItemProviders {
			get { return Preferences.ItemProviders; }
		}

		AbstractDockPainter Painter {
			get { return painter; }
			set {
				painter = value;
			}
		}
		
		int IconSize {
			get { return Math.Min (MaxIconSize, Preferences.IconSize); }
		}
		
		int Monitor {
			get { return Preferences.MonitorNumber; }
		}
		
		internal DockPosition Position {
			get { return Preferences.Position; }
		}
		
		bool ZoomEnabled {
			get { return Preferences.ZoomEnabled; }
		}
		
		double ZoomPercent {
			get {
				if (!ZoomEnabled)
					return 1;
				return (Preferences.IconSize * Preferences.ZoomPercent) / MaxIconSize;
			}
		}
		#endregion
		
		#region Internal Properties
		Gdk.Point Cursor {
			get {
				if (Screen != CursorTracker.Screen) {
					return new Gdk.Point (-1000, -1000);
				}
				return CursorTracker.Cursor; 
			}
		}
		
		Gdk.Point LocalCursor {
			get { return new Gdk.Point (Cursor.X - window_position.X, Cursor.Y - window_position.Y); }
		}
		
		int DockHeight {
			get { return IconSize + 2 * DockHeightBuffer; }
		}
		
		int DockWidth {
			get; 
			set;
		}
		
		int DynamicDockWidth {
			get {
				if (GdkWindow == null)
					return 0;
				
				int dockWidth = Items.Sum (adi => (int) ((adi.Square ? IconSize : adi.IconSurface (background_buffer, IconSize).Width) * 
						Math.Min (1, (DateTime.UtcNow - adi.AddTime).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds)));
				dockWidth += 2 * DockWidthBuffer + (Items.Count - 1) * ItemWidthBuffer;
				if (remove_index != 0) {
					dockWidth += (int) ((ItemWidthBuffer + remove_size) *
						(1 - Math.Min (1, (DateTime.UtcNow - remove_time).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds)));
				}
				
				return dockWidth;
			}
		}
		
		int ItemWidthBuffer {
			get { return (int) (0.08 * IconSize); }
		}
		
		bool VerticalDock {
			get { return Position == DockPosition.Left || Position == DockPosition.Right; }
		}
		
		/// <summary>
		/// The int size a fully zoomed icon will display at.
		/// </summary>
		internal int ZoomedIconSize {
			get { 
				return ZoomEnabled ? (int) (IconSize * ZoomPercent) : IconSize; 
			}
		}
		
		int ZoomedDockHeight {
			get { return ZoomedIconSize + 2 * DockHeightBuffer; }
		}
		
		double HideOffset {
			get {
				if (Painter != null || ConfigurationMode)
					return 0;
				double progress = Math.Min (1, (render_time - hidden_change_time).TotalMilliseconds / 
				                            BaseAnimationTime.TotalMilliseconds);
				if (AutohideManager.Hidden)
					return progress;
				return 1 - progress;
			}
		}
		
		double DockOpacity {
			get {
				return Math.Min (1, (1 - HideOffset) + Preferences.FadeOpacity);
			}
		}
		
		double ZoomIn {
			get {
				// we buffer this value during renders since it will be checked many times and we dont need to 
				// recalculate it each time
				if (zoom_in_buffer.HasValue && rendering) {
					return zoom_in_buffer.Value;
				}
				
				double zoom = Math.Min (1, (render_time - dock_hovered_change_time).TotalMilliseconds / 
				                        BaseAnimationTime.TotalMilliseconds);
				if (!DockHovered) {
					zoom = 1 - zoom;
				}
				
				// FIXME: Very harsh
				if (Painter != null || ConfigurationMode)
					zoom = 0;
				
				if (rendering)
					zoom_in_buffer = zoom;
				
				return zoom;
			}
		}
		
		int ZoomSize {
			get { 
				// 330 chosen for its pleasant (to me) look
				return (int) (330 * (IconSize / 64.0)); 
			}
		}

		#endregion
	
		public DockWindow () : base(Gtk.WindowType.Toplevel)
		{
			DrawValues = new Dictionary<AbstractDockItem, DrawValue> ();
			Menu = new DockItemMenu (this);
			Menu.Shown += HandleMenuShown;
			Menu.Hidden += HandleMenuHidden;
			
			TextManager = new HoverTextManager ();
			DragTracker = new DockDragTracker (this);
			AnimationState = new AnimationState ();
			BuildAnimationEngine ();
			
			collection_backend = new List<AbstractDockItem> ();
			collection_frontend = collection_backend.AsReadOnly ();
			
			AppPaintable = true;
			AcceptFocus = false;
			Decorated = false;
			DoubleBuffered = false;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			Resizable = false;
			CanFocus = false;
			TypeHint = WindowTypeHint.Dock;
			
			this.SetCompositeColormap ();
			Stick ();
			
			AddEvents ((int) (Gdk.EventMask.ButtonPressMask |
			                  Gdk.EventMask.ButtonReleaseMask |
			                  Gdk.EventMask.EnterNotifyMask |
			                  Gdk.EventMask.LeaveNotifyMask |
					          Gdk.EventMask.PointerMotionMask |
			                  Gdk.EventMask.ScrollMask));
			
			Realized += HandleRealized;
			Docky.Controller.ThemeChanged += DockyControllerThemeChanged;
		}


		#region Event Handling
		void BuildAnimationEngine ()
		{
			AnimationState.AddCondition (Animations.DockHoveredChanged, 
			                             () => (DockHovered && ZoomIn != 1) || (!DockHovered && ZoomIn != 0));
			AnimationState.AddCondition (Animations.HideChanged,
			                             () => ((DateTime.UtcNow - hidden_change_time) < BaseAnimationTime));
			AnimationState.AddCondition (Animations.ItemsChanged,
				                         () => ((DateTime.UtcNow - items_change_time) < BaseAnimationTime));
			AnimationState.AddCondition (Animations.Bounce, BouncingItems);
		}
		
		bool BouncingItems ()
		{
			DateTime now = DateTime.UtcNow;
			
			foreach (AbstractDockItem adi in Items) {
				if ((now - adi.LastClick) < BounceTime || (now - adi.StateSetTime (ItemState.Urgent)) < BounceTime)
					return true;
			}
			return false;
		}

		void HandleMenuHidden (object sender, EventArgs e)
		{
			SetTooltipVisibility ();
		}

		void HandleMenuShown (object sender, EventArgs e)
		{
			AnimatedDraw ();
			SetTooltipVisibility ();
		}

		void DockyControllerThemeChanged (object sender, EventArgs e)
		{
			ResetBuffers ();
			AnimatedDraw ();
		}
		
		void HandleRealized (object sender, EventArgs e)
		{
			GdkWindow.SetBackPixmap (null, false);
			
			CursorTracker = CursorTracker.ForDisplay (Display);
			CursorTracker.CursorPositionChanged += HandleCursorPositionChanged;	
			
			AutohideManager = new AutohideManager (Screen);
			AutohideManager.Behavior = Preferences.Autohide;
			
			AutohideManager.HiddenChanged += HandleHiddenChanged;
			AutohideManager.DockHoveredChanged += HandleDockHoveredChanged;
			
			Screen.SizeChanged += ScreenSizeChanged;
			
			SetSizeRequest ();
			UpdateDockWidth ();
		}

		void ScreenSizeChanged (object sender, EventArgs e)
		{
			Reconfigure ();
		}

		void HandleDockHoveredChanged (object sender, EventArgs e)
		{
			dock_hovered_change_time = UpdateTimeStamp (dock_hovered_change_time, BaseAnimationTime);
			
			if (DockHovered)
				CursorTracker.RequestHighResolution (this);
			else
				CursorTracker.CancelHighResolution (this);
			
			DragTracker.EnsureDragAndDropProxy ();
			AnimatedDraw ();
		}

		void HandleHiddenChanged (object sender, EventArgs e)
		{
			hidden_change_time = UpdateTimeStamp (hidden_change_time, BaseAnimationTime);
			AnimatedDraw ();
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			if (DockHovered && e.LastPosition != Cursor)
				AnimatedDraw ();
			DragTracker.EnsureDragAndDropProxy ();
		}
		
		void RegisterItemProvider (AbstractDockItemProvider provider)
		{
			provider.ItemsChanged += ProviderItemsChanged;
			
			foreach (AbstractDockItem item in provider.Items)
				RegisterItem (item);
		}
		
		void UnregisterItemProvider (AbstractDockItemProvider provider)
		{
			provider.ItemsChanged -= ProviderItemsChanged;
			
			foreach (AbstractDockItem item in provider.Items)
				UnregisterItem (item);
		}
		
		void ProviderItemsChanged (object sender, ItemsChangedArgs args)
		{
			
			foreach (AbstractDockItem item in args.AddedItems) {
				RegisterItem (item);
			}
			
			foreach (AbstractDockItem item in args.RemovedItems) {
				remove_time = DateTime.UtcNow;
				UnregisterItem (item);
				
				remove_index = Items.IndexOf (item) - .5;
				remove_size = IconSize; //FIXME
			}
			
			UpdateCollectionBuffer ();
			
			AnimatedDraw ();
		}
		
		void RegisterItem (AbstractDockItem item)
		{
			item.HoverTextChanged += ItemHoverTextChanged;
			item.PaintNeeded += ItemPaintNeeded;
			item.PainterRequest += ItemPainterRequest;
		}

		void UnregisterItem (AbstractDockItem item)
		{
			item.HoverTextChanged -= ItemHoverTextChanged;
			item.PaintNeeded -= ItemPaintNeeded;
			item.PainterRequest -= ItemPainterRequest;
			DrawValues.Remove (item);
		}
		
		void ItemHoverTextChanged (object sender, EventArgs e)
		{
			if ((sender as AbstractDockItem) == HoveredItem)
				UpdateHoverText ();
			AnimatedDraw ();
		}
		
		void ItemPaintNeeded (object sender, PaintNeededEventArgs e)
		{
			AnimatedDraw ();
		}

		void ItemPainterRequest (object sender, PainterRequestEventArgs e)
		{
			AbstractDockItem owner = sender as AbstractDockItem;
			
			if (!Items.Contains (owner) || e.Painter == null)
				return;
			
			if (e.Type == ShowHideType.Show) {
				ShowPainter (owner, e.Painter);
			} else if (e.Type == ShowHideType.Hide && Painter == e.Painter) {
				HidePainter ();
			}
		}
		
		void RegisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged += PreferencesAutohideChanged;
			preferences.IconSizeChanged += PreferencesIconSizeChanged;
			preferences.PositionChanged += PreferencesPositionChanged;
			preferences.ZoomEnabledChanged += PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged += PreferencesZoomPercentChanged;
			
			preferences.ItemProvidersChanged += PreferencesItemProvidersChanged;
			
			foreach (AbstractDockItemProvider provider in preferences.ItemProviders)
				RegisterItemProvider (provider);
		}

		void UnregisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged -= PreferencesAutohideChanged;
			preferences.IconSizeChanged -= PreferencesIconSizeChanged;
			preferences.PositionChanged -= PreferencesPositionChanged;
			preferences.ZoomEnabledChanged -= PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged -= PreferencesZoomPercentChanged;
			
			preferences.ItemProvidersChanged -= PreferencesItemProvidersChanged;
			foreach (AbstractDockItemProvider provider in preferences.ItemProviders)
				UnregisterItemProvider (provider);
		}
		
		void PreferencesItemProvidersChanged (object sender, ItemProvidersChangedEventArgs e)
		{
			foreach (AbstractDockItemProvider provider in e.AddedProviders)
				RegisterItemProvider (provider);
			foreach (AbstractDockItemProvider provider in e.RemovedProviders)
				UnregisterItemProvider (provider);
			UpdateCollectionBuffer ();
			AnimatedDraw ();
		}

		void PreferencesZoomPercentChanged (object sender, EventArgs e)
		{
			SetSizeRequest ();
			AnimatedDraw ();
		}

		void PreferencesZoomEnabledChanged (object sender, EventArgs e)
		{
			SetSizeRequest ();
			AnimatedDraw ();
		}

		void PreferencesPositionChanged (object sender, EventArgs e)
		{
			Reconfigure ();
		}

		void PreferencesIconSizeChanged (object sender, EventArgs e)
		{
			UpdateDockWidth ();
			AnimatedDraw ();
		}

		void PreferencesAutohideChanged (object sender, EventArgs e)
		{
			AutohideManager.Behavior = Autohide;
			SetStruts ();
		}
		
		void OnHoveredItemChanged (AbstractDockItem lastItem)
		{
			if (HoveredItemChanged != null)
				HoveredItemChanged (this, new HoveredItemChangedArgs (lastItem));
		}
		#endregion
		
		#region Input Handling
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (!ConfigurationMode)
				CursorTracker.SendManualUpdate (evnt);
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			if (!ConfigurationMode)
				CursorTracker.SendManualUpdate (evnt);
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (!ConfigurationMode)
				CursorTracker.SendManualUpdate (evnt);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (InternalDragActive || ConfigurationMode)
				return base.OnButtonPressEvent (evnt);
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				Painter.ButtonPressed (x, y, evnt.State);
			} else if (HoveredItem != null && evnt.Button == 3) {
				MenuList list;
				
				if (HoveredItem.Owner != null)
					list = HoveredItem.Owner.GetMenuItems (HoveredItem);
				else
					list = HoveredItem.GetMenuItems ();
				
				if (list.Any ()) {
					DrawValue val = DrawValues[HoveredItem];
					val = val.MoveIn (Position, ZoomedIconSize / 2.15);
					Menu.Anchor = new Gdk.Point ((int) val.Center.X + window_position.X, (int) val.Center.Y + window_position.Y);
					Menu.Orientation = Position;
					Menu.SetItems (list);
					Menu.Show ();
				}
			}
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			// This event gets fired before the drag end event, in this case
			// we ignore it.
			if (InternalDragActive || ConfigurationMode)
				return base.OnButtonPressEvent (evnt);
			
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				Painter.ButtonReleased (x, y, evnt.State);
			} else if (HoveredItem != null) {
				double x, y;
				Gdk.Rectangle region = DrawRegionForItem (HoveredItem);
					
				x = ((Cursor.X + window_position.X) - region.X) / (double) region.Height;
				y = ((Cursor.Y + window_position.Y) - region.Y) / (double) region.Width;
				
				HoveredItem.Clicked (evnt.Button, evnt.State, x, y);
				AnimatedDraw ();
			}
			
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			if (InternalDragActive || ConfigurationMode)
				return base.OnScrollEvent (evnt);
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				Painter.Scrolled (evnt.Direction, x, y, evnt.State);
			} else if (HoveredItem != null) {
				HoveredItem.Scrolled (evnt.Direction, evnt.State);
			}
			
			return base.OnScrollEvent (evnt);
		}
		#endregion
		
		#region Misc.
		void Reconfigure ()
		{
			SetSizeRequest ();
			Reposition ();
			ResetBuffers ();
			UpdateDockWidth ();
			AnimatedDraw ();
		}
		
		void SetTooltipVisibility ()
		{
			bool visible = HoveredItem != null && 
				!InternalDragActive && 
				!Menu.Visible && 
				!ConfigurationMode && 
				Painter == null;
			
			if (visible)
				TextManager.Show ();
			else
				TextManager.Hide ();
		}
		
		internal void SetHoveredAcceptsDrop ()
		{
			HoveredAcceptsDrop = false;
			if (HoveredItem != null && ExternalDragActive) {
				if (DragTracker.DragData != null && HoveredItem.CanAcceptDrop (DragTracker.DragData)) {
					HoveredAcceptsDrop = true;
				}
			}
		}
		
		internal void UpdateCollectionBuffer ()
		{
			if (rendering) {
				// resetting a durring a render is bad. Complete the render then reset.
				GLib.Idle.Add (delegate {
					// dispose of our separators as we made them ourselves,
					// this could be a bit more elegant
					foreach (AbstractDockItem item in Items.Where (adi => adi is INonPersistedItem))
						item.Dispose ();
					
					collection_backend.Clear ();
					UpdateDockWidth ();
					return false;
				});
			} else {
				foreach (AbstractDockItem item in Items.Where (adi => adi is INonPersistedItem))
					item.Dispose ();
				
				collection_backend.Clear ();
				UpdateDockWidth ();
			}
			
			items_change_time = DateTime.UtcNow;
		}
		
		void ResetBuffers ()
		{
			if (main_buffer != null) {
				main_buffer.Dispose ();
				main_buffer = null;
			}
			
			if (painter_buffer != null) {
				painter_buffer.Dispose ();
				painter_buffer = null;
			}
			
			if (background_buffer != null) {
				background_buffer.Dispose ();
				background_buffer = null;
			}
			
			if (icon_buffer != null) {
				icon_buffer.Dispose ();
				icon_buffer = null;
			}
			
			if (normal_indicator_buffer != null) {
				normal_indicator_buffer.Dispose ();
				normal_indicator_buffer = null;
			}
			
			if (urgent_indicator_buffer != null) {
				urgent_indicator_buffer.Dispose ();
				urgent_indicator_buffer = null;
			}
		}
		#endregion
		
		#region Painters
		void ShowPainter (AbstractDockItem owner, AbstractDockPainter painter)
		{
			if (Painter != null || owner == null || painter == null || (!painter.SupportsVertical && VerticalDock))
				return;
			
			Painter = painter;
			Painter.HideRequest += HandlePainterHideRequest;
			Painter.PaintNeeded += HandlePainterPaintNeeded;
			
			repaint_painter = true;
			update_screen_regions = true;
			DragTracker.DragDisabled = true;
			Painter.SetAllocation (new Gdk.Rectangle (0, 0, DockWidth - 100, DockHeight));
			Painter.SetStyle (Style);
			Painter.Shown ();
			
			SetTooltipVisibility ();
		}
		
		void HidePainter ()
		{
			if (Painter == null)
				return;
			
			Painter.HideRequest -= HandlePainterHideRequest;
			Painter.PaintNeeded -= HandlePainterPaintNeeded;
			
			DragTracker.DragDisabled = false;
			update_screen_regions = true;
			Painter.Hidden ();
			Painter = null;
			
			SetTooltipVisibility ();
		}
		
		void HandlePainterPaintNeeded (object sender, EventArgs e)
		{
			repaint_painter = true;
			AnimatedDraw ();
		}

		void HandlePainterHideRequest (object sender, EventArgs e)
		{
			if (Painter == sender as AbstractDockPainter)
				HidePainter ();
		}
		#endregion
		
		#region Size and Position
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			ResetBuffers ();
			Reposition ();
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			Reposition ();
		}
		
		protected override bool OnConfigureEvent (EventConfigure evnt)
		{
			window_position.X = evnt.X;
			window_position.Y = evnt.Y;
			
			return base.OnConfigureEvent (evnt);
		}
		
		void Reposition ()
		{
			UpdateMonitorGeometry ();
			
			switch (Position) {
			default:
			case DockPosition.Top:
				Move (monitor_geo.X + (monitor_geo.Width - Width) / 2, monitor_geo.Y);
				break;
			case DockPosition.Left:
				Move (monitor_geo.X, monitor_geo.Y + (monitor_geo.Height - Height) / 2);
				break;
			case DockPosition.Right:
				Move (monitor_geo.X + monitor_geo.Width - Width, monitor_geo.Y + (monitor_geo.Height - Height) / 2);
				break;
			case DockPosition.Bottom:
				Move (monitor_geo.X + (monitor_geo.Width - Width) / 2, monitor_geo.Y + monitor_geo.Height - Height);
				break;
			}
			
			SetStruts ();
		}
		
		void UpdateDockWidth ()
		{
			if (GdkWindow == null)
				return;
			
			MaxIconSize = Preferences.IconSize;
			DockWidth = Items.Sum (adi => adi.Square ? MaxIconSize : adi.IconSurface (background_buffer, MaxIconSize).Width);
			DockWidth += 2 * DockWidthBuffer + (Items.Count - 1) * ItemWidthBuffer;
			
			while (DockWidth > (VerticalDock ? Height : Width)) {
				MaxIconSize--;
				DockWidth = Items.Sum (adi => adi.Square ? MaxIconSize : adi.IconSurface (background_buffer, MaxIconSize).Width);
				DockWidth += 2 * DockWidthBuffer + (Items.Count - 1) * ItemWidthBuffer;
			}
		}
		
		void UpdateMonitorGeometry ()
		{
			monitor_geo = Screen.GetMonitorGeometry (Monitor);
		}
		
		void SetSizeRequest ()
		{
			UpdateDockWidth ();
			UpdateMonitorGeometry ();
			
			if (VerticalDock) {
				Height = Math.Min (Docky.CommandLinePreferences.MaxSize, monitor_geo.Height);
				Width = DockHeightBuffer + ZoomedIconSize + UrgentBounceHeight;
			} else {
				Width = Math.Min (Docky.CommandLinePreferences.MaxSize, monitor_geo.Width);
				Height = DockHeightBuffer + ZoomedIconSize + UrgentBounceHeight;
			}
			
			if (Docky.CommandLinePreferences.NetbookHackMode) {
				// Currently the intel i945 series of cards (used on netbooks frequently) will 
				// for some mystical reason get terrible drawing performance if the window is
				// between 1009 pixels and 1024 pixels in width OR height. We just pad it out an extra
				// pixel
				if (Width >= 1009 && Width <= 1024)
					Width = 1026;
		
				if (Height >= 1009 && Height <= 1024)
					Height = 1026;
				
			}
			SetSizeRequest (Width, Height);
		}
		#endregion
		
		#region Drawing
		internal void AnimatedDraw ()
		{
			if (0 < animation_timer) {
				return;
			}
			
			// the presense of this queue draw has caused some confusion, so I will explain.
			// first its here to draw the "first frame".  Without it, we have a 16ms delay till that happens,
			// however minor that is.
			QueueDraw ();
			
			if (AnimationState.AnimationNeeded)
				animation_timer = GLib.Timeout.Add (1000/60, OnDrawTimeoutElapsed);
		}
		
		bool OnDrawTimeoutElapsed ()
		{
			QueueDraw ();
			
			if (AnimationState.AnimationNeeded)
				return true;
			
			//reset the timer to 0 so that the next time AnimatedDraw is called we fall back into
			//the draw loop.
			animation_timer = 0;
			return false;
		}
		
		Gdk.Rectangle DrawRegionForItem (AbstractDockItem item)
		{
			if (!DrawValues.ContainsKey (item))
				return Gdk.Rectangle.Zero;
			
			return DrawRegionForItemValue (item, DrawValues[item]);
		}
		
		Gdk.Rectangle DrawRegionForItemValue (AbstractDockItem item, DrawValue val)
		{
			if (item.Square) {
				return new Gdk.Rectangle ((int) (val.Center.X - (IconSize * val.Zoom / 2)),
					(int) (val.Center.Y - (IconSize * val.Zoom / 2)),
					(int) (IconSize * val.Zoom),
					(int) (IconSize * val.Zoom));
			} else {
				DockySurface surface = item.IconSurface (main_buffer, IconSize);
				
				int width = surface.Width;
				int height = surface.Height;
				
				if (item.RotateWithDock && VerticalDock) {
					int tmp = width;
					width = height;
					height = tmp;
				}
				
				return new Gdk.Rectangle ((int) (val.Center.X - (width * val.Zoom / 2)),
					(int) (val.Center.Y - (height * val.Zoom / 2)),
					(int) (width * val.Zoom),
					(int) (height * val.Zoom));
			}
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
			bool hoveredItemSet = false;
			
			// our width and height switch around if we have a veritcal dock
			if (!VerticalDock) {
				width = surface.Width;
				height = surface.Height;
			} else {
				width = surface.Height;
				height = surface.Width;
			}
			
			Gdk.Point cursor = LocalCursor;
			Gdk.Point localCursor = cursor;
			
			// screen shift sucks
			cursor.X -= monitor_geo.X;
			cursor.Y -= monitor_geo.Y;
			
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
				cursor.X = (Width - 1) - cursor.X;
				cursor.Y = cursor.X;
				cursor.X = width - (width - tmpY);
				break;
			case DockPosition.Bottom:
				cursor.Y = (Height - 1) - cursor.Y;
				break;
			}
			
			// the line along the dock width about which the center of unzoomed icons sit
			int midline = DockHeight / 2;
			
			// the left most edge of the first dock item
			int startX = ((width - DynamicDockWidth) / 2) + DockWidthBuffer;
			
			Gdk.Point center = new Gdk.Point (startX, midline);
			
			int index = 0;
			foreach (AbstractDockItem adi in Items) {
				
				// used to handle remove animation
				if (remove_index != 0 && remove_index < index && remove_index > index - 1) {
					double removePercent = 1 - Math.Min (1, (DateTime.UtcNow - remove_time).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds);
					if (removePercent == 0) {
						remove_index = 0;
					} else {
						center.X += (int) ((remove_size + ItemWidthBuffer) * removePercent);
					}
				}
				
				DrawValue val = new DrawValue ();
				int iconSize = IconSize;
				
				// div by 2 may result in rounding errors? Will this render OK? Shorts WidthBuffer by 1?
				double halfSize;
				if (adi.Square) {
					halfSize = iconSize / 2.0;
				} else {
					DockySurface icon = adi.IconSurface (surface, iconSize);
					
					// yeah I am pretty sure...
					if (adi.RotateWithDock || !VerticalDock) {
						halfSize = icon.Width / 2.0;
					} else {
						halfSize = icon.Height / 2.0;
					}
				}
				
				halfSize *= Math.Min (1, (DateTime.UtcNow - adi.AddTime).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds);
				
				// center now represents our midpoint
				center.X += (int) Math.Floor (halfSize);
				val.StaticCenter = new PointD (center.X, center.Y);
				
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
					
					double offsetPercent;
					if (ExternalDragActive) {
						// Provide space for dropping between items
						offset += ZoomedIconSize * (offset / (ZoomSize / 2.0));
						offsetPercent = Math.Min (1, offset / (ZoomSize / 2.0 + ZoomedIconSize));
					} else {
						offsetPercent = offset / (ZoomSize / 2.0);
					}
					// zoom is calculated as 1 through target_zoom (default 2).  
					// The larger your offset, the smaller your zoom
					
					// First we get the point on our curve that defines out current zoom
					// offset is always going to fall on a point on the curve >= 0
					zoom = 1 - Math.Pow (offsetPercent, 2);
					
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
						val.Center = new Cairo.PointD ((int) centerPosition, center.Y);
					} else {
						double zoomedCenterHeight = DockHeightBuffer + (iconSize * zoom / 2.0);
						
						if (zoom == 1)
							centerPosition = Math.Round (centerPosition);
						
						val.Center = new Cairo.PointD (centerPosition, zoomedCenterHeight);
						val.Zoom = zoom;
					}
				} else {
					val.Zoom = 1;
					val.Center = new PointD (center.X, center.Y);
				}
				
				// now we undo our transforms to the point
				switch (Position) {
				case DockPosition.Top:
					;
					break;
				case DockPosition.Left:
					double tmpY = val.Center.Y;
					val.Center.Y = val.Center.X;
					val.Center.X = width - (width - tmpY);
					
					tmpY = val.StaticCenter.Y;
					val.StaticCenter.Y = val.StaticCenter.X;
					val.StaticCenter.X = width - (width - tmpY);
					break;
				case DockPosition.Right:
					tmpY = val.Center.Y;
					val.Center.Y = val.Center.X;
					val.Center.X = width - (width - tmpY);
					val.Center.X = (height - 1) - val.Center.X;
					
					tmpY = val.StaticCenter.Y;
					val.StaticCenter.Y = val.StaticCenter.X;
					val.StaticCenter.X = width - (width - tmpY);
					val.StaticCenter.X = (height - 1) - val.StaticCenter.X;
					break;
				case DockPosition.Bottom:
					val.Center.Y = (height - 1) - val.Center.Y;
					val.StaticCenter.Y = (height - 1) - val.StaticCenter.Y;
					break;
				}
				
				Gdk.Rectangle hoverArea = DrawRegionForItemValue (adi, val);
				
				if (VerticalDock) {
					hoverArea.Inflate ((int) (ZoomedDockHeight * .3), ItemWidthBuffer / 2);
				} else {
					hoverArea.Inflate (ItemWidthBuffer / 2, (int) (ZoomedDockHeight * .3));
				}
				
				val.HoverArea = hoverArea;
				DrawValues[adi] = val;

				if (hoverArea.Contains (localCursor)) {
					HoveredItem = adi;
					hoveredItemSet = true;
				}
				
				if (update_screen_regions) {
					Gdk.Rectangle region = hoverArea;
					region.X += window_position.X;
					region.Y += window_position.Y;
					if (ConfigurationMode || Painter != null)
						adi.SetScreenRegion (Screen, new Gdk.Rectangle (0, 0, 0, 0));
					else
						adi.SetScreenRegion (Screen, region);
				}
				
				// move past midpoint to end of icon
				center.X += (int) Math.Ceiling (halfSize) + ItemWidthBuffer;
				index++;
			}
			
			update_screen_regions = false;
			
			if (!hoveredItemSet)
				HoveredItem = null;
		}
		
		Gdk.Rectangle StaticDockArea (DockySurface surface)
		{
			switch (Position) {
			case DockPosition.Top:
				return new Gdk.Rectangle ((surface.Width - DockWidth) / 2, 0, DockWidth, DockHeight);
			case DockPosition.Left:
				return new Gdk.Rectangle (0, (surface.Height - DockWidth) / 2, DockHeight, DockWidth);
			case DockPosition.Right:
				return new Gdk.Rectangle (surface.Width - DockHeight, (surface.Height - DockWidth) / 2, DockHeight, DockWidth);
			case DockPosition.Bottom:
				return new Gdk.Rectangle ((surface.Width - DockWidth) / 2, surface.Height - DockHeight, DockWidth, DockHeight);
			}
			
			return Gdk.Rectangle.Zero;
		}
		
		void GetDockAreaOnSurface (DockySurface surface, out Gdk.Rectangle dockArea, out Gdk.Rectangle cursorArea)
		{
			Gdk.Rectangle first, last, staticArea;
			
			first = DrawRegionForItem (Items[0]);
			last = DrawRegionForItem (Items[Items.Count - 1]);
			
			dockArea = new Gdk.Rectangle (0, 0, 0, 0);
			staticArea = StaticDockArea (surface);
			
			int hotAreaSize;
			if ((!Preferences.FadeOnHide || Preferences.FadeOpacity == 0) && AutohideManager.Hidden && !ConfigurationMode) {
				hotAreaSize = 1;
			} else if (DockHovered && !ConfigurationMode) {
				hotAreaSize = (int) (ZoomedDockHeight * 1.3);
			} else {
				hotAreaSize = DockHeight;
			}
			
			switch (Position) {
			case DockPosition.Top:
				dockArea.X = first.X - DockWidthBuffer;
				dockArea.Y = 0;
				dockArea.Width = (last.X + last.Width + DockWidthBuffer) - dockArea.X;
				dockArea.Height = DockHeight;
				
				cursorArea = new Gdk.Rectangle (staticArea.X,
				                                dockArea.Y,
				                                staticArea.Width,
				                                hotAreaSize);
				break;
			case DockPosition.Left:
				dockArea.X = 0;
				dockArea.Y = first.Y - DockWidthBuffer;
				dockArea.Width = DockHeight;
				dockArea.Height = (last.Y + last.Height + DockWidthBuffer) - dockArea.Y;
				
				cursorArea = new Gdk.Rectangle (dockArea.X,
				                                staticArea.Y,
				                                hotAreaSize,
				                                staticArea.Height);
				break;
			case DockPosition.Right:
				dockArea.X = surface.Width - DockHeight;
				dockArea.Y = first.Y - DockWidthBuffer;
				dockArea.Width = DockHeight;
				dockArea.Height = (last.Y + last.Height + DockWidthBuffer) - dockArea.Y;
				
				cursorArea = new Gdk.Rectangle (dockArea.X + dockArea.Width - hotAreaSize,
				                                staticArea.Y,
				                                hotAreaSize,
				                                staticArea.Height);
				break;
			default:
			case DockPosition.Bottom:
				dockArea.X = first.X - DockWidthBuffer;
				dockArea.Y = surface.Height - DockHeight;
				dockArea.Width = (last.X + last.Width + DockWidthBuffer) - dockArea.X;
				dockArea.Height = DockHeight;
				
				cursorArea = new Gdk.Rectangle (staticArea.X,
				                                dockArea.Y + dockArea.Height - hotAreaSize,
				                                staticArea.Width,
				                                hotAreaSize);
				break;
			}
		}
		
		void DrawDock (DockySurface surface)
		{
			surface.Clear ();
			UpdateDrawRegionsForSurface (surface);
			
			Gdk.Rectangle dockArea, cursorArea;
			GetDockAreaOnSurface (surface, out dockArea, out cursorArea);
			
			DrawDockBackground (surface, dockArea);
			
			if (Painter == null) {
			
				if (icon_buffer == null || icon_buffer.Width != surface.Width || icon_buffer.Height != surface.Height) {
					if (icon_buffer != null)
						icon_buffer.Dispose ();
					icon_buffer = new DockySurface (surface.Width, surface.Height, surface);
				}
				
				icon_buffer.Clear ();
				foreach (AbstractDockItem adi in Items) {
					DrawItem (icon_buffer, dockArea, adi);
				}
			
				icon_buffer.Internal.Show (surface.Context, 0, 0);
			
			} else {
				DrawPainter (surface, dockArea);
			}
			
			if (ActiveGlow) {
				Gdk.Color color = Style.BaseColors[(int) Gtk.StateType.Selected];
				
				Gdk.Rectangle extents;
				using (DockySurface tmp = surface.CreateMask (0, out extents)) {
					extents.Inflate (GlowSize * 2, GlowSize * 2);
					tmp.ExponentialBlur (GlowSize, extents);
					tmp.Context.Color = new Cairo.Color (
						(double) color.Red / ushort.MaxValue, 
						(double) color.Green / ushort.MaxValue, 
						(double) color.Blue / ushort.MaxValue, 
						.90).SetValue (1).MultiplySaturation (4);
					tmp.Context.Operator = Operator.Atop;
					tmp.Context.Paint ();
				
					surface.Context.Operator = Operator.DestOver;
					surface.Context.SetSource (tmp.Internal);
					surface.Context.Paint ();
					surface.Context.Operator = Operator.Over;
				}
			}
			
			if (DockOpacity < 1)
				SetDockOpacity (surface);
			
			SetInputMask (cursorArea);
			
			dockArea.Intersect (monitor_geo);
			dockArea.X += window_position.X;
			dockArea.Y += window_position.Y;
			AutohideManager.SetIntersectArea (dockArea);
			
			cursorArea.Intersect (monitor_geo);
			cursorArea.X += window_position.X;
			cursorArea.Y += window_position.Y;
			AutohideManager.SetCursorArea (cursorArea);
		}
		
		void DrawPainter (DockySurface surface, Gdk.Rectangle dockArea)
		{
			if (painter_buffer == null || painter_buffer.Width != surface.Width || painter_buffer.Height != surface.Height) {
				if (painter_buffer != null)
					painter_buffer.Dispose ();
				painter_buffer = new DockySurface (surface.Width, surface.Height, surface);
				repaint_painter = true;
			}
			
			if (repaint_painter) {
				painter_buffer.Clear ();
				DockySurface painterSurface = Painter.GetSurface (surface);
			
				painter_area = new Gdk.Rectangle (dockArea.X + (dockArea.Width - painterSurface.Width) / 2,
					dockArea.Y + (dockArea.Height - painterSurface.Height) / 2,
					painterSurface.Width,
					painterSurface.Height);
			
				painterSurface.Internal.Show (painter_buffer.Context, painter_area.X, painter_area.Y);
				repaint_painter = false;
			}
			
			painter_buffer.Internal.Show (surface.Context, 0, 0);
		}
		
		void SetDockOpacity (DockySurface surface)
		{
			if (!Preferences.FadeOnHide)
				return;
			surface.Context.Save ();
			
			surface.Context.Color = new Cairo.Color (0, 0, 0, 0);
			surface.Context.Operator = Operator.Source;
			surface.Context.PaintWithAlpha (1 - DockOpacity);
			
			surface.Context.Restore ();
		}
		
		void DrawItem (DockySurface surface, Gdk.Rectangle dockArea, AbstractDockItem item)
		{
			if (DragTracker.DragItem == item)
				return;
			
			double zoomOffset = ZoomedIconSize / (double) IconSize;
			
			DrawValue val = DrawValues [item];
			DrawValue center = val;
			
			double clickAnimationProgress = 0;
			double lighten = 0;
			double darken = 0;
			
			if ((render_time - item.LastClick) < BounceTime) {
				clickAnimationProgress = (render_time - item.LastClick).TotalMilliseconds / BounceTime.TotalMilliseconds;
			
				switch (item.ClickAnimation) {
				case ClickAnimation.Bounce:
					double move = Math.Abs (Math.Sin (2 * Math.PI * clickAnimationProgress) * LaunchBounceHeight);
					center = center.MoveIn (Position, move);
					break;
				case ClickAnimation.Darken:
					darken = Math.Max (0, Math.Sin (Math.PI * 2 * clickAnimationProgress)) * .5;
					break;
				case ClickAnimation.Lighten:
					lighten = Math.Max (0, Math.Sin (Math.PI * 2 * clickAnimationProgress)) * .5;
					break;
				}
			}
			
			if (HoveredAcceptsDrop && HoveredItem == item && ExternalDragActive) {
				lighten += .4;
			}
			
			if ((item.State & ItemState.Urgent) == ItemState.Urgent && 
				(render_time - item.StateSetTime (ItemState.Urgent)) < BounceTime) {
				double urgentProgress = (render_time - item.StateSetTime (ItemState.Urgent)).TotalMilliseconds / BounceTime.TotalMilliseconds;
				
				double move = Math.Abs (Math.Sin (Math.PI * urgentProgress) * UrgentBounceHeight);
				center = center.MoveIn (Position, move);
			}
			
			double opacity = Math.Min (1, (render_time - item.AddTime).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds);
			opacity = Math.Pow (opacity, 2);
			DockySurface icon;
			if (item.Zoom) {
				icon = item.IconSurface (surface, ZoomedIconSize);
				icon.ShowAtPointAndZoom (surface, center.Center, center.Zoom / zoomOffset, opacity);
			} else {
				double rotation = 0;
				
				if (item.RotateWithDock) {
					switch (Position) {
					case DockPosition.Top:
						rotation = Math.PI;
						break;
					case DockPosition.Left:
						rotation = Math.PI * 1.5;
						break;
					case DockPosition.Right:
						rotation = Math.PI * .5;
						break;
					case DockPosition.Bottom:
						rotation = 0;
						break;
					}
				}
				
				icon = item.IconSurface (surface, IconSize);
				icon.ShowAtPointAndRotation (surface, center.Center, rotation, opacity);
			}
			
			if (darken > 0 || lighten > 0) {
				Gdk.Rectangle area = DrawRegionForItemValue (item, center);
				surface.Context.Rectangle (area.X, area.Y, area.Height, area.Width);
				
				if (darken > 0) {
					surface.Context.Color = new Cairo.Color (0, 0, 0, darken);
				} else if (lighten > 0) {
					surface.Context.Color = new Cairo.Color (1, 1, 1, lighten);
				}
				
				surface.Context.Operator = Operator.Atop;
				surface.Context.Fill ();
				surface.Context.Operator = Operator.Over;
			}
			
			if ((item.State & ItemState.Active) == ItemState.Active) {
				Gdk.Rectangle area;
				
				if (VerticalDock) {
					area = new Gdk.Rectangle (
						dockArea.X, 
						(int) (val.Center.Y - (IconSize * val.Zoom) / 2) - ItemWidthBuffer / 2,
						DockHeight,
						(int) (IconSize * val.Zoom) + ItemWidthBuffer);
				} else {
					area = new Gdk.Rectangle (
						(int) (val.Center.X - (IconSize * val.Zoom) / 2) - ItemWidthBuffer / 2,
						dockArea.Y, 
						(int) (IconSize * val.Zoom) + ItemWidthBuffer,
						DockHeight);
				}
				
				surface.Context.Operator = Operator.DestOver;
				DrawActiveIndicator (surface, area, item.AverageColor (), opacity);
				surface.Context.Operator = Operator.Over;
			}
			
			if (item.Indicator != ActivityIndicator.None) {
				if (normal_indicator_buffer == null)
					normal_indicator_buffer = CreateNormalIndicatorBuffer ();
				if (urgent_indicator_buffer == null)
					urgent_indicator_buffer = CreateUrgentIndicatorBuffer ();
				
				DrawValue loc = val.MoveIn (Position, 1 - IconSize * val.Zoom / 2 - DockHeightBuffer);
				
				DockySurface indicator;
				if ((item.State & ItemState.Urgent) == ItemState.Urgent) {
					indicator = urgent_indicator_buffer;
				} else {
					indicator = normal_indicator_buffer;
				}
				
				if (item.Indicator == ActivityIndicator.Single || !Preferences.IndicateMultipleWindows) {
					indicator.ShowAtPointAndZoom (surface, loc.Center, 1);
				} else {
					indicator.ShowAtPointAndZoom (surface, loc.MoveRight (Position, 3).Center, 1);
					indicator.ShowAtPointAndZoom (surface, loc.MoveRight (Position, -3).Center, 1);
				}
			}
		}
		
		void DrawActiveIndicator (DockySurface surface, Gdk.Rectangle area, Cairo.Color color, double opacity)
		{
			surface.Context.Rectangle (area.X, area.Y, area.Width, area.Height);
			LinearGradient lg;
			
			switch (Position) {
			case DockPosition.Top:
				lg = new LinearGradient (0, area.Y, 0, area.Y + area.Height);
				break;
			case DockPosition.Left:
				lg = new LinearGradient (area.X, 0, area.X + area.Width, 0);
				break;
			case DockPosition.Right:
				lg = new LinearGradient (area.X + area.Width, 0, area.X, 0);
				break;
			default:
			case DockPosition.Bottom:
				lg = new LinearGradient (0, area.Y + area.Height, 0, area.Y);
				break;
			}
			lg.AddColorStop (0, color.SetAlpha (0.6 * opacity));
			lg.AddColorStop (1, color.SetAlpha (0.0));
			
			surface.Context.Pattern = lg;
			surface.Context.Fill ();
			
			lg.Destroy ();
		}
		
		DockySurface CreateNormalIndicatorBuffer ()
		{
			return CreateIndicatorBuffer (NormalIndicatorSize, new Cairo.Color (.4, .7, 1));
		}
		
		DockySurface CreateUrgentIndicatorBuffer ()
		{
			return CreateIndicatorBuffer (UrgentIndicatorSize, new Cairo.Color (1, .2, .2));
		}
		
		DockySurface CreateIndicatorBuffer (int size, Cairo.Color color)
		{
			DockySurface surface = new DockySurface (size, size, background_buffer);
			surface.Clear ();
			
			Cairo.Context cr = surface.Context;
			
			double x = size / 2;
			double y = x;
				
			cr.MoveTo (x, y);
			cr.Arc (x, y, size / 2, 0, Math.PI * 2);
				
			RadialGradient rg = new RadialGradient (x, y, 0, x, y, size / 2);
			rg.AddColorStop (0, new Cairo.Color (1, 1, 1, 1));
			rg.AddColorStop (.10, color.SetAlpha (1.0));
			rg.AddColorStop (.20, color.SetAlpha (.60));
			rg.AddColorStop (.25, color.SetAlpha (.25));
			rg.AddColorStop (.50, color.SetAlpha (.15));
			rg.AddColorStop (1.0, color.SetAlpha (0.0));
			
			cr.Pattern = rg;
			cr.Fill ();
			rg.Destroy ();
			
			return surface;
		}
		
		void DrawDockBackground (DockySurface surface, Gdk.Rectangle backgroundArea)
		{
			if (background_buffer == null) {
				if (VerticalDock) {
					background_buffer = new DockySurface (BackgroundHeight, BackgroundWidth, surface);
				} else {
					background_buffer = new DockySurface (BackgroundWidth, BackgroundHeight, surface);
				}
				
				Gdk.Pixbuf background = DockServices.Drawing.LoadIcon (Docky.Controller.BackgroundSvg, -1);
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
				
				background.Dispose ();
			}
			
			background_buffer.TileOntoSurface (surface, backgroundArea, 50, Position);
		}
		
		protected override void OnStyleSet (Style previous_style)
		{
			if (GdkWindow != null)
				GdkWindow.SetBackPixmap (null, false);
			base.OnStyleSet (previous_style);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized || !Items.Any ()) {
				SetInputMask (new Gdk.Rectangle (0, 0, 1, 1));
				return true;
			}
			
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
				
				if (Preferences.FadeOnHide) {
					cr.SetSource (main_buffer.Internal);
				} else {
					switch (Position) {
					case DockPosition.Top:
						cr.SetSource (main_buffer.Internal, 0, 0 - HideOffset * DockHeight);
						break;
					case DockPosition.Left:
						cr.SetSource (main_buffer.Internal, 0 - HideOffset * DockHeight, 0);
						break;
					case DockPosition.Right:
						cr.SetSource (main_buffer.Internal, HideOffset * DockHeight, 0);
						break;
					case DockPosition.Bottom:
						cr.SetSource (main_buffer.Internal, 0, HideOffset * DockHeight);
						break;
					}
				}
				
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

			using (Gdk.Pixmap pixmap = new Gdk.Pixmap (null, area.Width, area.Height, 1))
			using (Context cr = Gdk.CairoHelper.Create (pixmap)) {
			
				cr.Color = new Cairo.Color (0, 0, 0, 1);
				cr.Paint ();
				
				InputShapeCombineMask (pixmap, area.X, area.Y);
			}
		}
		
		void SetStruts ()
		{
			if (!IsRealized) return;
			
			X11Atoms atoms = X11Atoms.Instance;
			
			IntPtr [] struts = new IntPtr [12];
			
			if (Autohide == AutohideType.None) {
				switch (Position) {
				case DockPosition.Top:
					struts [(int) Struts.Top] = (IntPtr) (DockHeight + monitor_geo.Y);
					struts [(int) Struts.TopStart] = (IntPtr) monitor_geo.X;
					struts [(int) Struts.TopEnd] = (IntPtr) (monitor_geo.X + monitor_geo.Width - 1);
					break;
				case DockPosition.Left:
					struts [(int) Struts.Left] = (IntPtr) (monitor_geo.X + DockHeight);
					struts [(int) Struts.LeftStart] = (IntPtr) monitor_geo.Y;
					struts [(int) Struts.LeftEnd] = (IntPtr) (monitor_geo.Y + monitor_geo.Height - 1);
					break;
				case DockPosition.Right:
					struts [(int) Struts.Right] = (IntPtr) (DockHeight + (Screen.Width - (monitor_geo.X + monitor_geo.Width)));
					struts [(int) Struts.RightStart] = (IntPtr) monitor_geo.Y;
					struts [(int) Struts.RightEnd] = (IntPtr) (monitor_geo.Y + monitor_geo.Height - 1);
					break;
				case DockPosition.Bottom:
					struts [(int) Struts.Bottom] = (IntPtr) (DockHeight + (Screen.Height - (monitor_geo.Y + monitor_geo.Height)));
					struts [(int) Struts.BottomStart] = (IntPtr) monitor_geo.X;
					struts [(int) Struts.BottomEnd] = (IntPtr) (monitor_geo.X + monitor_geo.Width - 1);
					break;
				}
			}
			
			IntPtr [] first_struts = new [] { struts [0], struts [1], struts [2], struts [3] };

			Xlib.Xlib.XChangeProperty (GdkWindow, atoms._NET_WM_STRUT_PARTIAL, atoms.XA_CARDINAL,
			                      (int) PropertyMode.PropModeReplace, struts);
			
			Xlib.Xlib.XChangeProperty (GdkWindow, atoms._NET_WM_STRUT, atoms.XA_CARDINAL, 
			                      (int) PropertyMode.PropModeReplace, first_struts);
		}
		#endregion
		
		public override void Dispose ()
		{
			if (Menu != null)
				Menu.Dispose ();
			
			AutohideManager.Dispose ();
			UnregisterPreferencesEvents (Preferences);
			
			TextManager.Dispose ();
			DragTracker.Dispose ();
			
			CursorTracker.CursorPositionChanged -= HandleCursorPositionChanged;
			AutohideManager.HiddenChanged -= HandleHiddenChanged;
			AutohideManager.DockHoveredChanged -= HandleDockHoveredChanged;
			Screen.SizeChanged -= ScreenSizeChanged;
			Docky.Controller.ThemeChanged -= DockyControllerThemeChanged;
			
			if (animation_timer > 0)
				GLib.Source.Remove (animation_timer);
			
			// clear out our separators
			foreach (AbstractDockItem adi in Items.Where (adi => adi is INonPersistedItem))
				adi.Dispose ();
			
			ResetBuffers ();
			
			Hide ();
			Destroy ();
			base.Dispose ();
		}
	}
}
