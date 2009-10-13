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
		
		readonly TimeSpan BaseAnimationTime = new TimeSpan (0, 0, 0, 0, 150);
		readonly TimeSpan BounceTime = new TimeSpan (0, 0, 0, 0, 600);
		
		DateTime hidden_change_time;
		DateTime dock_hovered_change_time;
		DateTime render_time;
		
		IDockPreferences preferences;
		DockySurface main_buffer, background_buffer, icon_buffer, normal_indicator_buffer, urgent_indicator_buffer;
		AbstractDockItem hoveredItem;
		
		Gdk.Rectangle monitor_geo;
		Gdk.Rectangle current_mask_area;
		Gdk.Point window_position;
		Gdk.Window proxy_window;
		
		double? zoom_in_buffer;
		bool rendering;
		bool update_screen_regions;
		
		uint animation_timer;
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		bool HoveredAcceptsDrop { get; set; }
		
		AutohideManager AutohideManager { get; set; }
		
		CursorTracker CursorTracker { get; set; }
		
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
		
		bool DockHovered {
			get { return AutohideManager.DockHovered; }
		}
		
		
		AbstractDockItem HoveredItem {
			get {
				if (!DockHovered)
					return null;
				return hoveredItem;
			}
			set {
				if (hoveredItem == value)
					return;
				AbstractDockItem last = hoveredItem;
				hoveredItem = value;
				SetHoveredAcceptsDrop ();
				OnHoveredItemChanged (last);
			}
		}

		
		IEnumerable<AbstractDockItemProvider> ItemProviders {
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
			get { return ZoomEnabled ? Preferences.ZoomPercent : 1; }
		}
		#endregion
		
		#region Internal Properties
		Gdk.Point Cursor {
			get { return CursorTracker.Cursor; }
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
		
		int ItemWidthBuffer {
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
		
		int ZoomedDockHeight {
			get { return ZoomedIconSize + 2 * DockHeightBuffer; }
		}
		
		double HideOffset {
			get {
				if (Preferences.FadeOnHide)
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
				if (!Preferences.FadeOnHide)
					return 1;
				double progress = Math.Min (1, (render_time - hidden_change_time).TotalMilliseconds / 
									BaseAnimationTime.TotalMilliseconds);
				progress = (1 - Preferences.FadeOpacity) * progress;
				if (!AutohideManager.Hidden)
					return Preferences.FadeOpacity + progress;
				return 1 - progress;
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
			Menu.Shown += (o, a) => AnimatedDraw ();
			
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
			HoveredItemChanged += HandleHoveredItemChanged;
			
			EnableDragTo ();
			EnableDragFrom ();
			RegisterDragEvents ();
		}

		void SetHoveredAcceptsDrop ()
		{
			HoveredAcceptsDrop = false;
			if (HoveredItem != null && drag_known) {
				if (HoveredItem.CanAcceptDrop (drag_data)) {
					HoveredAcceptsDrop = true;
				}
			}
		}

		#region Event Handling
		void BuildAnimationEngine ()
		{
			AnimationState.AddCondition (Animations.DockHoveredChanged, 
			                             () => (DockHovered && ZoomIn != 1) || (!DockHovered && ZoomIn != 0));
			AnimationState.AddCondition (Animations.HideChanged,
			                             () => ((DateTime.UtcNow - hidden_change_time) < BaseAnimationTime));
			AnimationState.AddCondition (Animations.Bounce,
			                             () => Items.Any (i => (DateTime.UtcNow - i.LastClick) < BounceTime ||
					                                            (DateTime.UtcNow - i.StateSetTime (ItemState.Urgent)) < BounceTime));
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
			SetSizeRequest ();
		}

		void HandleDockHoveredChanged (object sender, EventArgs e)
		{
			dock_hovered_change_time = DateTime.UtcNow;
			
			if (DockHovered)
				CursorTracker.RequestHighResolution (this);
			else
				CursorTracker.CancelHighResolution (this);
			
			EnsureDragAndDropProxy ();
			AnimatedDraw ();
		}

		void HandleHiddenChanged (object sender, EventArgs e)
		{
			if ((DateTime.UtcNow - hidden_change_time) > BaseAnimationTime) {
				hidden_change_time = DateTime.UtcNow;
			} else {
				hidden_change_time = DateTime.UtcNow - (BaseAnimationTime - (DateTime.UtcNow - hidden_change_time));
			}
			
			AnimatedDraw ();
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			if (DockHovered && e.LastPosition != Cursor)
				AnimatedDraw ();
			EnsureDragAndDropProxy ();
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
			UpdateCollectionBuffer ();
			
			foreach (AbstractDockItem item in args.AddedItems)
				RegisterItem (item);
			foreach (AbstractDockItem item in args.RemovedItems)
				UnregisterItem (item);
			
			AnimatedDraw ();
		}
		
		void RegisterItem (AbstractDockItem item)
		{
			item.HoverTextChanged += ItemHoverTextChanged;
			item.PaintNeeded += ItemPaintNeeded;
		}

		void UnregisterItem (AbstractDockItem item)
		{
			item.HoverTextChanged -= ItemHoverTextChanged;
			item.PaintNeeded -= ItemPaintNeeded;
			DrawValues.Remove (item);
		}

		void ItemHoverTextChanged (object sender, EventArgs e)
		{
			AnimatedDraw ();
		}
		
		void ItemPaintNeeded (object sender, PaintNeededEventArgs e)
		{
			AnimatedDraw ();
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
			Reposition ();
			SetSizeRequest ();
			ResetBuffers ();
			AnimatedDraw ();
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
			CursorTracker.SendManualUpdate (evnt);
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			CursorTracker.SendManualUpdate (evnt);
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			CursorTracker.SendManualUpdate (evnt);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (drag_began)
				return base.OnButtonPressEvent (evnt);
			
			if (HoveredItem != null && evnt.Button == 3) {
				IEnumerable<Menus.MenuItem> items;
				
				if (HoveredItem.Owner != null)
					items = HoveredItem.Owner.GetMenuItems (HoveredItem).ToArray ();
				else
					items = HoveredItem.GetMenuItems ().ToArray ();
				
				if (items.Any ()) {
					DrawValue val = DrawValues[HoveredItem];
					val = val.MoveIn (Position, ZoomedIconSize / 2.15);
					Menu.Anchor = new Gdk.Point ((int) val.Center.X + window_position.X, (int) val.Center.Y + window_position.Y);
					Menu.Orientation = Position;
					Menu.SetItems (items);
					Menu.Show ();
				}
			}
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			// This event gets fired before the drag end event, in this case
			// we ignore it.
			if (drag_began)
				return base.OnButtonPressEvent (evnt);
			
			double x, y;
			if (HoveredItem != null) {
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
			if (HoveredItem != null) {
				HoveredItem.Scrolled (evnt.Direction, evnt.State);
			}
			
			return base.OnScrollEvent (evnt);
		}
		#endregion
		
		#region Drag Handling
		bool drag_known;
		bool drag_data_requested;
		bool drag_is_desktop_file;
		bool drag_began;
		int marker = 0;
		
		AbstractDockItem drag_item;
		
		IEnumerable<string> drag_data;
		
		void RegisterDragEvents ()
		{
			DragMotion       += HandleDragMotion;
			DragBegin        += HandleDragBegin;
			DragDataReceived += HandleDragDataReceived;
			DragDrop         += HandleDragDrop;
			DragEnd          += HandleDragEnd;
			DragLeave        += HandleDragLeave;
			DragFailed       += HandleDragFailed;
		}


		/// <summary>
		/// Emitted on the drag source when drag is started
		/// </summary>
		void HandleDragBegin (object o, DragBeginArgs args)
		{
			drag_began = true;
			
			if (proxy_window != null) {
				EnableDragTo ();
				proxy_window = null;
			}
			
			Gdk.Pixbuf pbuf;
			drag_item = HoveredItem;
			
			if (drag_item is INonPersistedItem)
				drag_item = null;
			
			if (drag_item != null) {
				pbuf = HoveredItem.IconSurface (background_buffer, ZoomedIconSize).LoadToPixbuf ();
			} else {
				pbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 1, 1);
			}
			
			Gtk.Drag.SetIconPixbuf (args.Context, pbuf, pbuf.Width / 2, pbuf.Height / 2);
			pbuf.Dispose ();
		}

		/// <summary>
		/// Emitted on the drop site. If the data was recieved to preview the data, call
		/// Gdk.Drag.Status (), else call Gdk.Drag.Finish () 
		/// RetVal = true on success
		/// </summary>
		void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			if (drag_data_requested) {
				SelectionData data = args.SelectionData;
				
				string uris = Encoding.UTF8.GetString (data.Data);
				
				drag_data = Regex.Split (uris, "\r\n")
					.Where (uri => uri.StartsWith ("file://"));
				
				drag_data_requested = false;
				drag_is_desktop_file = drag_data.Any (d => d.EndsWith (".desktop"));
				SetHoveredAcceptsDrop ();
			}
			
			Gdk.Drag.Status (args.Context, DragAction.Copy, Gtk.Global.CurrentEventTime);
			args.RetVal = true;
		}

		/// <summary>
		/// Emitted on the drop site when the user drops data on the widget.
		/// </summary>
		void HandleDragDrop (object o, DragDropArgs args)
		{
			args.RetVal = true;
			Gtk.Drag.Finish (args.Context, true, false, args.Time);
			
			if (drag_data == null)
				return;
			
			AbstractDockItem item = HoveredItem;
			
			if (!drag_is_desktop_file && item != null && item.CanAcceptDrop (drag_data)) {
				item.AcceptDrop (drag_data);
			} else {
				foreach (string s in drag_data)
					Preferences.DefaultProvider.InsertItem (s);
			}
			
			drag_known = false;
			drag_data = null;
			drag_data_requested = false;
			drag_is_desktop_file = false;
		}

		/// <summary>
		/// Emitted on the drag source when the drag finishes
		/// </summary>
		void HandleDragEnd (object o, DragEndArgs args)
		{
			if (drag_item != null) {
				if (!DockHovered) {
					AbstractDockItemProvider provider = ProviderForItem (drag_item);
					if (provider != null && provider.ItemCanBeRemoved (drag_item)) {
						provider.RemoveItem (drag_item);
					}
				} else {
					AbstractDockItem item = HoveredItem;
					if (item != null && item.CanAcceptDrop (drag_item))
						item.AcceptDrop (drag_item);
				}
			}
			
			drag_began = false;
			drag_item = null;
			
			AnimatedDraw ();
		}

		/// <summary>
		/// Emitted on drop site when drag leaves widget
		/// </summary>
		void HandleDragLeave (object o, DragLeaveArgs args)
		{
			drag_known = false;
		}
		
		/// <summary>
		/// Emitted on drag source. Return true to disable drag failed animation
		/// </summary>
		void HandleDragFailed (object o, DragFailedArgs args)
		{
			args.RetVal = true;
		}

		/// <summary>
		/// Emitted on drop site.
		/// Set RetVal == cursor is over drop zone
		/// if (RetVal) Gdk.Drag.Status, unless the decision cannot be made, in which case it may be defered by
		/// a get data call
		/// </summary>
		void HandleDragMotion (object o, DragMotionArgs args)
		{
			if (marker != args.Context.GetHashCode ()) {
				marker = args.Context.GetHashCode ();
				drag_known = false;
			}
			
			// we own the drag if drag_began is true, lets not be silly
			if (!drag_known && !drag_began) {
				drag_known = true;
				Gdk.Atom atom = Gtk.Drag.DestFindTarget (this, args.Context, null);
				Gtk.Drag.GetData (this, args.Context, atom, args.Time);
				drag_data_requested = true;
			} else {
				Gdk.Drag.Status (args.Context, DragAction.Copy, args.Time);
			}
			args.RetVal = true;
		}
		
		Gdk.Window BestProxyWindow ()
		{
			try {
				int pid = System.Diagnostics.Process.GetCurrentProcess ().Id;
				IEnumerable<ulong> xids = Wnck.Screen.Default.WindowsStacked
					.Reverse () // top to bottom order
					.Where (wnk => wnk.IsVisibleOnWorkspace (Wnck.Screen.Default.ActiveWorkspace) && 
							                                 wnk.Pid != pid &&
							                                 wnk.EasyGeometry ().Contains (Cursor))
					.Select (wnk => wnk.Xid);
				
				if (!xids.Any ())
					return null;
				
				return Gdk.Window.ForeignNew ((uint) xids.First ());
			} catch {
				return null;
			}
		}
		
		void HandleHoveredItemChanged (object sender, HoveredItemChangedArgs e)
		{
			if (drag_began && DragItemsCanInteract (drag_item, HoveredItem)) {
				
				int tmp = drag_item.Position;
				drag_item.Position = HoveredItem.Position;
				HoveredItem.Position = tmp;
				
				UpdateCollectionBuffer ();
				Preferences.SyncPreferences ();
			}
		}
		
		AbstractDockItemProvider ProviderForItem (AbstractDockItem item)
		{
			return ItemProviders
				.DefaultIfEmpty (null)
				.Where (p => p.Items.Contains (item))
				.FirstOrDefault ();
		}
		
		bool DragItemsCanInteract (AbstractDockItem dragItem, AbstractDockItem hoveredItem)
		{
			return dragItem != hoveredItem &&
				   ProviderForItem (dragItem) == ProviderForItem (hoveredItem) && 
				   ProviderForItem (dragItem) != null;
		}
		
		void EnsureDragAndDropProxy ()
		{
			// having a proxy window here is VERY bad ju-ju
			if (drag_began) {
				return;
			}
			
			if (DockHovered) {
				if (proxy_window == null)
					return;
				proxy_window = null;
				EnableDragTo ();
			} else if ((CursorTracker.Modifier & ModifierType.Button1Mask) == ModifierType.Button1Mask) {
				Gdk.Window bestProxy = BestProxyWindow ();
				if (proxy_window != bestProxy) {
					proxy_window = bestProxy;
					Gtk.Drag.DestSetProxy (this, proxy_window, DragProtocol.Xdnd, true);
				}
			}
		}

		void EnableDragTo ()
		{
			TargetEntry dest_te = new TargetEntry ("text/uri-list", 0, 0);
			Gtk.Drag.DestSet (this, 0, new [] {dest_te}, Gdk.DragAction.Copy);
		}
		
		void EnableDragFrom ()
		{
			// we dont really want to offer the drag to anything, merely pretend to, so we set a mimetype nothing takes
			TargetEntry te = new TargetEntry ("text/uri-list", TargetFlags.App, 0);
			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask, new[] { te }, DragAction.Copy);
		}
		#endregion
		
		#region Misc.
		void UpdateCollectionBuffer ()
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
		}
		
		void ResetBuffers ()
		{
			if (main_buffer != null) {
				main_buffer.Dispose ();
				main_buffer = null;
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
			monitor_geo = Screen.GetMonitorGeometry (Monitor);
			
			switch (Position) {
			default:
			case DockPosition.Top:
			case DockPosition.Left:
				Move (monitor_geo.X, monitor_geo.Y);
				break;
			case DockPosition.Right:
				Move (monitor_geo.X + monitor_geo.Width - Width, monitor_geo.Y);
				break;
			case DockPosition.Bottom:
				Move (monitor_geo.X, monitor_geo.Y + monitor_geo.Height - Height);
				break;
			}
			
			SetStruts ();
		}
		
		void UpdateDockWidth ()
		{
			if (GdkWindow == null)
				return;
			
			DockySurface model;
			if (background_buffer != null) {
				model = main_buffer;
			} else {
				using (Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow)) {
					model = new DockySurface (0, 0, cr.Target);
				}
			}
			
			DockWidth = Items.Sum (adi => adi.Square ? IconSize : adi.IconSurface (model, IconSize).Width);
			DockWidth += 2 * DockWidthBuffer + (Items.Count - 1) * ItemWidthBuffer;
		}
		
		void SetSizeRequest ()
		{
			if (VerticalDock) {
				Height = monitor_geo.Height;
				Width = ZoomedIconSize + 2 * DockHeightBuffer + 250;
			} else {
				Width = monitor_geo.Width;
				Height = ZoomedIconSize + 2 * DockHeightBuffer + UrgentBounceHeight;
				Height = Math.Max (150, Height);
			}
			SetSizeRequest (Width, Height);
		}
		#endregion
		
		#region Drawing
		void AnimatedDraw ()
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
			
			Gdk.Point cursor = Cursor;
			
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
				cursor.X = (monitor_geo.Width - 1) - cursor.X;
				cursor.Y = cursor.X;
				cursor.X = width - (width - tmpY);
				break;
			case DockPosition.Bottom:
				cursor.Y = (monitor_geo.Height - 1) - cursor.Y;
				break;
			}
			
			// the line along the dock width about which the center of unzoomed icons sit
			int midline = DockHeight / 2;
			
			// the left most edge of the first dock item
			int startX = ((width - DockWidth) / 2) + DockWidthBuffer;
			
			Gdk.Point center = new Gdk.Point (startX, midline);
			
			foreach (AbstractDockItem adi in Items) {
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
					
					double offsetPercent = offset / (ZoomSize / 2.0);
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

				if (hoverArea.Contains (LocalCursor)) {
					HoveredItem = adi;
					hoveredItemSet = true;
				}
				
				if (update_screen_regions) {
					Gdk.Rectangle region = hoverArea;
					region.X += window_position.X;
					region.Y += window_position.Y;
					adi.SetScreenRegion (Screen, region);
				}
				
				// move past midpoint to end of icon
				center.X += (int) Math.Ceiling (halfSize) + ItemWidthBuffer;
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
			if ((!Preferences.FadeOnHide || Preferences.FadeOpacity == 0) && AutohideManager.Hidden) {
				hotAreaSize = 1;
			} else if (DockHovered) {
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
			
			if (DockOpacity < 1)
				SetDockOpacity (surface);
			
			SetInputMask (cursorArea);

			
			dockArea.X += window_position.X;
			dockArea.Y += window_position.Y;
			AutohideManager.SetIntersectArea (dockArea);
			
			cursorArea.X += window_position.X;
			cursorArea.Y += window_position.Y;
			AutohideManager.SetCursorArea (cursorArea);
		}
		
		void SetDockOpacity (DockySurface surface)
		{
			surface.Context.Save ();
			
			surface.Context.Color = new Cairo.Color (0, 0, 0, 0);
			surface.Context.Operator = Operator.Source;
			surface.Context.PaintWithAlpha (1 - DockOpacity);
			
			surface.Context.Restore ();
		}
		
		void DrawItem (DockySurface surface, Gdk.Rectangle dockArea, AbstractDockItem item)
		{
			if (drag_item == item)
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
			
			if (HoveredAcceptsDrop && HoveredItem == item && drag_known) {
				lighten += .4;
			}
			
			if ((item.State & ItemState.Urgent) == ItemState.Urgent && 
				(render_time - item.StateSetTime (ItemState.Urgent)) < BounceTime) {
				double urgentProgress = (render_time - item.StateSetTime (ItemState.Urgent)).TotalMilliseconds / BounceTime.TotalMilliseconds;
				
				double move = Math.Abs (Math.Sin (Math.PI * urgentProgress) * UrgentBounceHeight);
				center = center.MoveIn (Position, move);
			}
			
			DockySurface icon;
			if (item.Zoom) {
				icon = item.IconSurface (surface, ZoomedIconSize);
				icon.ShowAtPointAndZoom (surface, center.Center, center.Zoom / zoomOffset);
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
				icon.ShowAtPointAndRotation (surface, center.Center, rotation);
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
				DrawActiveIndicator (surface, area, item.AverageColor ());
				surface.Context.Operator = Operator.Over;
			}
			
			if (HoveredItem == item && !drag_began && !Menu.Visible) {
				DrawValue loc = val.MoveIn (Position, IconSize * (ZoomPercent + .1) - IconSize / 2);
				
				DockySurface text = item.HoverTextSurface (surface, Style);
				if (text != null)
					text.ShowAtEdge (surface, loc.StaticCenter, Position);
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
		
		void DrawActiveIndicator (DockySurface surface, Gdk.Rectangle area, Cairo.Color color)
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
			lg.AddColorStop (0, color.SetAlpha (0.6));
			lg.AddColorStop (1, color.SetAlpha (0.0));
			
			surface.Context.Pattern = lg;
			surface.Context.Fill ();
			
			lg.Destroy ();
		}
		
		DockySurface CreateNormalIndicatorBuffer ()
		{
			return CreateIndicatorBuffer (NormalIndicatorSize, new Cairo.Color (.3, .65, 1));
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
				
				background.Dispose ();
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
			AutohideManager.Dispose ();
			UnregisterPreferencesEvents (Preferences);
			Preferences.FreeProviders ();
			
			CursorTracker.CursorPositionChanged -= HandleCursorPositionChanged;
			AutohideManager.HiddenChanged -= HandleHiddenChanged;
			AutohideManager.DockHoveredChanged -= HandleDockHoveredChanged;
			Screen.SizeChanged -= ScreenSizeChanged;
			
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
