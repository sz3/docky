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
using Docky.Painters;
using Docky.Services;
using Docky.Xlib;

namespace Docky.Interface
{

	internal class DockDragTracker : IDisposable
	{
		Gdk.Window proxy_window;
		
		bool drag_known;
		bool drag_data_requested;
		bool drag_is_desktop_file;
		bool enabled = true;
		int marker = 0;
		
		AbstractDockItem drag_item;
		
		IEnumerable<string> drag_data;
		
		public DockWindow Owner { get; private set; }
		
		public bool ExternalDragActive { get; private set; }

		public bool InternalDragActive { get; private set; }

		public bool HoveredAcceptsDrop { get; private set; }
		
		public bool Enabled {
			get {
				return enabled;
			}
			set {
				if (enabled == value)
					return;
				enabled = value;
				
				if (enabled) {
					EnableDragFrom ();
				} else {
					DisableDragFrom ();
				}
			}
		}
		
		public IEnumerable<string> DragData {
			get { return drag_data; }
		}
		
		public AbstractDockItem DragItem {
			get { return drag_item; }
		}
		
		public DockDragTracker (DockWindow owner)
		{
			Owner = owner;
			RegisterDragEvents ();
			
			EnableDragTo ();
			EnableDragFrom ();
			
			Owner.HoveredItemChanged += HandleHoveredItemChanged;
		}
		
		void RegisterDragEvents ()
		{
			Owner.DragMotion += HandleDragMotion;
			Owner.DragBegin += HandleDragBegin;
			Owner.DragDataReceived += HandleDragDataReceived;
			Owner.DragDataGet += HandleDragDataGet;
			Owner.DragDrop += HandleDragDrop;
			Owner.DragEnd += HandleDragEnd;
			Owner.DragLeave += HandleDragLeave;
			Owner.DragFailed += HandleDragFailed;
			
			Owner.MotionNotifyEvent += HandleOwnerMotionNotifyEvent;
			Owner.EnterNotifyEvent += HandleOwnerEnterNotifyEvent;
			Owner.LeaveNotifyEvent += HandleOwnerLeaveNotifyEvent;
		}

		void HandleOwnerLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
		{
			ExternalDragActive = false;
		}

		void HandleOwnerEnterNotifyEvent (object o, EnterNotifyEventArgs args)
		{
			ExternalDragActive = false;
		}

		void HandleOwnerMotionNotifyEvent (object o, MotionNotifyEventArgs args)
		{
			ExternalDragActive = false;
		}
		
		void UnregisterDragEvents ()
		{
			Owner.DragMotion -= HandleDragMotion;
			Owner.DragBegin -= HandleDragBegin;
			Owner.DragDataReceived -= HandleDragDataReceived;
			Owner.DragDataGet -= HandleDragDataGet;
			Owner.DragDrop -= HandleDragDrop;
			Owner.DragEnd -= HandleDragEnd;
			Owner.DragLeave -= HandleDragLeave;
			Owner.DragFailed -= HandleDragFailed;
			
			Owner.MotionNotifyEvent -= HandleOwnerMotionNotifyEvent;
			Owner.EnterNotifyEvent -= HandleOwnerEnterNotifyEvent;
			Owner.LeaveNotifyEvent -= HandleOwnerLeaveNotifyEvent;
		}

		/// <summary>
		/// Emitted on the drag source to fetch drag data
		/// </summary>
		void HandleDragDataGet (object o, DragDataGetArgs args)
		{
			if (InternalDragActive && drag_item != null && !(drag_item is INonPersistedItem)) {
				string uri = string.Format ("docky://{0}\r\n", drag_item.UniqueID ());
				byte[] data = System.Text.Encoding.UTF8.GetBytes (uri);
				args.SelectionData.Set (args.SelectionData.Target, 8, data, data.Length);
			}
		}

		/// <summary>
		/// Emitted on the drag source when drag is started
		/// </summary>
		void HandleDragBegin (object o, DragBeginArgs args)
		{
			InternalDragActive = true;
			
			if (proxy_window != null) {
				EnableDragTo ();
				proxy_window = null;
			}
			
			Gdk.Pixbuf pbuf;
			drag_item = Owner.HoveredItem;
			
			if (drag_item is INonPersistedItem)
				drag_item = null;
			
			if (drag_item != null) {
				pbuf = Owner.HoveredItem.IconSurface (new DockySurface (1, 1), Owner.ZoomedIconSize).LoadToPixbuf ();
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
				Owner.SetHoveredAcceptsDrop ();
			} else {
				Console.WriteLine ("WTF?");
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
			
			AbstractDockItem item = Owner.HoveredItem;
			
			if (!drag_is_desktop_file && item != null && item.CanAcceptDrop (drag_data)) {
				item.AcceptDrop (drag_data);
			} else {
				AbstractDockItem rightMost = Owner.RightMostItem;
				int newPosition = rightMost != null ? rightMost.Position : 0;
			
				foreach (string s in drag_data) {
					AbstractDockItemProvider provider;
					if (Owner.HoveredProvider != null && Owner.HoveredProvider.CanAcceptDrop (s)) {
						provider = Owner.HoveredProvider;
					} else if (Owner.Preferences.DefaultProvider.CanAcceptDrop (s)) {
						provider = Owner.Preferences.DefaultProvider;
					} else {
						// nothing will take it, continue!
						continue;
					}
					
					provider.AcceptDrop (s, newPosition);
					
					if (FileApplicationProvider.WindowManager != null)
						FileApplicationProvider.WindowManager.UpdateTransientItems ();
				}
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
				if (!Owner.DockHovered) {
					AbstractDockItemProvider provider = ProviderForItem (drag_item);
					if (provider != null && provider.ItemCanBeRemoved (drag_item)) {
						PoofWindow window = new PoofWindow (128);
						window.SetCenterPosition (Owner.CursorTracker.Cursor);
						window.Run ();
						
						provider.RemoveItem (drag_item);
						if (FileApplicationProvider.WindowManager != null)
							FileApplicationProvider.WindowManager.UpdateTransientItems ();
					}
				} else {
					AbstractDockItem item = Owner.HoveredItem;
					if (item != null && item.CanAcceptDrop (drag_item))
						item.AcceptDrop (drag_item);
				}
			}
			
			InternalDragActive = false;
			drag_item = null;
			
			Owner.AnimatedDraw ();
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
			if (!InternalDragActive)
				ExternalDragActive = true;
			
			if (marker != args.Context.GetHashCode ()) {
				marker = args.Context.GetHashCode ();
				drag_known = false;
			}
			
			// we own the drag if InternalDragActive is true, lets not be silly
			if (!drag_known && !InternalDragActive) {
				drag_known = true;
				Gdk.Atom atom = Gtk.Drag.DestFindTarget (Owner, args.Context, null);
				if (atom != null) {
					Gtk.Drag.GetData (Owner, args.Context, atom, args.Time);
					drag_data_requested = true;
				} else {
					Gdk.Drag.Status (args.Context, DragAction.Private, args.Time);
				}
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
							                                 wnk.EasyGeometry ().Contains (Owner.CursorTracker.Cursor))
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
			if (InternalDragActive && DragItemsCanInteract (drag_item, Owner.HoveredItem)) {
				
				int tmp = drag_item.Position;
				drag_item.Position = Owner.HoveredItem.Position;
				Owner.HoveredItem.Position = tmp;
				
				Owner.UpdateCollectionBuffer ();
				Owner.Preferences.SyncPreferences ();
			}
		}
		
		AbstractDockItemProvider ProviderForItem (AbstractDockItem item)
		{
			return Owner.ItemProviders
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
		
		public void EnsureDragAndDropProxy ()
		{
			// having a proxy window here is VERY bad ju-ju
			if (InternalDragActive) {
				return;
			}
			
			if (Owner.DockHovered) {
				if (proxy_window == null)
					return;
				proxy_window = null;
				EnableDragTo ();
			} else if ((Owner.CursorTracker.Modifier & ModifierType.Button1Mask) == ModifierType.Button1Mask) {
				Gdk.Window bestProxy = BestProxyWindow ();
				if (proxy_window != bestProxy) {
					proxy_window = bestProxy;
					Gtk.Drag.DestSetProxy (Owner, proxy_window, DragProtocol.Xdnd, true);
				}
			}
		}

		void EnableDragTo ()
		{
			TargetEntry[] dest = new [] {
				new TargetEntry ("text/uri-list", 0, 0),
				new TargetEntry ("text/docky-uri-list", 0, 0),
			};
			Gtk.Drag.DestSet (Owner, 0, dest, Gdk.DragAction.Copy);
		}
		
		void DisableDragFrom ()
		{
			Gtk.Drag.SourceUnset (Owner);
		}
		
		void EnableDragFrom ()
		{
			// we dont really want to offer the drag to anything, merely pretend to, so we set a mimetype nothing takes
			TargetEntry te = new TargetEntry ("text/docky-uri-list", TargetFlags.App, 0);
			Gtk.Drag.SourceSet (Owner, Gdk.ModifierType.Button1Mask, new[] { te }, DragAction.Private);
		}
		#region IDisposable implementation
		public void Dispose ()
		{
			UnregisterDragEvents ();
			Owner.HoveredItemChanged -= HandleHoveredItemChanged;
		}
		#endregion
	}
}
