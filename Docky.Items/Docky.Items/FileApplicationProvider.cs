//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using Wnck;

using Docky.Menus;
using Docky.Windowing;

namespace Docky.Items
{
	public class FileApplicationProvider : AbstractDockItemProvider
	{
		public static FileApplicationProvider WindowManager;
		static List<FileApplicationProvider> Providers = new List<FileApplicationProvider> ();
		
		internal static IEnumerable<Wnck.Window> ManagedWindows {
			get {
				return Providers
					.SelectMany (p => p.PermanentItems)
					.Where (i => i is WnckDockItem)
					.Cast<WnckDockItem> ()
					.SelectMany (w => w.Windows);
			}
		}
		
		static IEnumerable<Wnck.Window> UnmanagedWindows {
			get {
				IEnumerable<Wnck.Window> managed = ManagedWindows.ToArray ();
				return Wnck.Screen.Default.Windows
					.Where (w => !w.IsSkipPager && !managed.Contains (w));
			}
		}
		
		public event EventHandler WindowManagerChanged;
		
		Dictionary<string, AbstractDockItem> items;
		List<AbstractDockItem> transient_items;
		
		public IEnumerable<string> Uris {
			get { return items.Keys.AsEnumerable (); }
		}
		
		public bool IsWindowManager {
			get { return WindowManager == this; }
		}
		
		IEnumerable<AbstractDockItem> PermanentItems {
			get {
				return items.Values.AsEnumerable ();
			}
		}
		
		IEnumerable<AbstractDockItem> InternalItems {
			get {
				return items.Values.Concat (transient_items);
			}
		}
		
		public FileApplicationProvider ()
		{
			items = new Dictionary<string, AbstractDockItem> ();
			transient_items = new List<AbstractDockItem> ();
			
			Providers.Add (this);
			
			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
		}

		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			if (args.Window.IsSkipTasklist)
				return;
			
			if (!WindowMatcher.Default.WindowIsReadyForMatch (args.Window)) {
				int i = 0;
				// try to give open office enough time to open and set its title
				GLib.Timeout.Add (150, delegate {
					if (!WindowMatcher.Default.WindowIsReadyForMatch (args.Window) && i < 20) {
						i++;
						return true;
					}
					UpdateTransientItems ();
					return false;
				});
			} else {
				// ensure we run last (more or less) so that all icons can update first
				GLib.Timeout.Add (150, delegate {
					UpdateTransientItems ();
					return false;
				});
			}
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			// we dont need to delay in this case as icons owning extra windows
			// is a non-event
			UpdateTransientItems ();
		}
		
		public void UpdateTransientItems ()
		{
			if (!IsWindowManager) {
				if (transient_items.Any ()) {
					List<AbstractDockItem> old_transient_items = transient_items;
					
					transient_items = new List<AbstractDockItem> ();
					
					Items = InternalItems;
					foreach (AbstractDockItem adi in old_transient_items)
						adi.Dispose ();
				}
				return;
			}
			
			// we will need a list of these bad boys we can mess with
			List<Wnck.Window> windows = UnmanagedWindows.ToList ();
			
			string desktopFile;
			WnckDockItem item;
			foreach (Wnck.Window window in windows) {
				if (transient_items.Where (adi => adi is WnckDockItem)
					.Cast<WnckDockItem> ()
					.SelectMany (wdi => wdi.Windows)
					.Contains (window))
					continue;
				
				desktopFile = WindowMatcher.Default.DesktopFileForWindow (window);
				
				if (!string.IsNullOrEmpty (desktopFile)) {
					item = ApplicationDockItem.NewFromUri (new Uri (desktopFile).AbsoluteUri);
				} else {
					item = new WindowDockItem (window);
				}
				
				if (!item.ManagedWindows.Any ()) {
					item.Dispose ();
					continue;
				}
				
				transient_items.Add (item);
				item.WindowsChanged += HandleTransientWindowsChanged;
				
				Items = InternalItems;
			}
			
			// remove old transient items
			List<AbstractDockItem> removed_transient_items = new List<AbstractDockItem> ();
			
			foreach (WnckDockItem wdi in transient_items.Where (adi => adi is WnckDockItem).Cast<WnckDockItem> ()) {
				foreach (Wnck.Window window in ManagedWindows)
					if (wdi.Windows.Contains (window)) {
						removed_transient_items.Add (wdi);
						continue;
					}
			}
			
			foreach (AbstractDockItem adi in removed_transient_items)
				transient_items.Remove (adi);
			Items = InternalItems;
			foreach (AbstractDockItem adi in removed_transient_items)
				adi.Dispose();
		}

		void HandleTransientWindowsChanged (object sender, EventArgs e)
		{
			if (!(sender is WnckDockItem))
				return;
			
			WnckDockItem item = sender as WnckDockItem;
			if (!item.ManagedWindows.Any ()) {
				transient_items.Remove (item);
				Items = InternalItems;
				item.Dispose ();
			}
		}
		
		protected override bool OnCanAcceptDrop (string uri)
		{
			return true;
		}

		protected override AbstractDockItem OnAcceptDrop (string uri)
		{
			return Insert (uri);
		}
		
		public bool InsertItem (string uri)
		{
			return Insert (uri) != null;
		}
		
		AbstractDockItem Insert (string uri)
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");
			
			if (items.ContainsKey (uri))
				return null;
			
			AbstractDockItem item;
			
			try {
				if (uri.EndsWith (".desktop")) {
					item = ApplicationDockItem.NewFromUri (uri);
				} else {
					item = FileDockItem.NewFromUri (uri);
				}
			} catch {
				item = null;
			}
			
			if (item == null)
				return null;
			
			items[uri] = item;
			
			
			Items = InternalItems;
			UpdateTransientItems ();
			
			return item;
		}
		
		public void PinToDock (ApplicationDockItem item)
		{
			transient_items.Remove (item);
			items.Add (new Uri (item.OwnedItem.Location).AbsoluteUri, item);
		}
		
		public void SetWindowManager ()
		{
			if (WindowManager == this)
				return;
			
			if (WindowManager != null)
				WindowManager.UnsetWindowManager ();
			
			WindowManager = this;
			OnWindowManagerChanged ();
		}
		
		public void UnsetWindowManager ()
		{
			if (WindowManager != this)
				return;
			
			WindowManager = null;
			OnWindowManagerChanged ();
		}
		
		void OnWindowManagerChanged ()
		{
			UpdateTransientItems ();
			if (WindowManagerChanged != null)
				WindowManagerChanged (this, EventArgs.Empty);
		}
		
		#region IDockItemProvider implementation
		public override string Name { get { return "File Application Provider"; } }
		
		public override string Icon { get { return "gtk-delete"; } }
		
		public override bool Separated { get { return true; } }
		
		public override bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return items.ContainsValue (item);
		}
		
		public override bool RemoveItem (AbstractDockItem item)
		{
			if (!items.ContainsValue (item))
				return false;
			
			string key = null;
			foreach (KeyValuePair<string, AbstractDockItem> kvp in items) {
				if (kvp.Value == item) {
					key = kvp.Key;
					break;
				}
			}
			
			// this should never happen...
			if (key == null)
				return false;
			
			items.Remove (key);
			
			Items = InternalItems;
			
			item.Dispose ();
			
			// this is so if the launcher has open windows and we manage those...
			UpdateTransientItems ();
			return true;
		}
		
		public override MenuList GetMenuItems (AbstractDockItem item)
		{
			MenuList list = base.GetMenuItems (item);
			
			if (item is ApplicationDockItem && !items.ContainsValue (item)) {
				list[MenuListContainer.Actions].Insert (0, 
					new MenuItem ("Pin to Dock", "pin.svg@" + GetType ().Assembly.FullName, (o, a) => PinToDock (item as ApplicationDockItem)));
			}
			
			return list;
		}
		
		public override void Dispose ()
		{
			IEnumerable<AbstractDockItem> old_items = Items;
			
			items = new Dictionary<string, AbstractDockItem> ();
			transient_items = new List<AbstractDockItem> ();
			
			Items = Enumerable.Empty<AbstractDockItem> ();
			foreach (AbstractDockItem adi in old_items)
				adi.Dispose ();
		}
		
		#endregion

		~FileApplicationProvider ()
		{
			Providers.Remove (this);
		}
	}
}
