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

using Docky.Windowing;

namespace Docky.Items
{


	public class FileApplicationProvider : IDockItemProvider
	{
		public static FileApplicationProvider WindowManager;
		static List<FileApplicationProvider> Providers = new List<FileApplicationProvider> ();
		
		static IEnumerable<Wnck.Window> ManagedWindows {
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
			// ensure we run last (more or less) so that all icons can update first
			GLib.Timeout.Add (10, delegate {
				UpdateTransientItems ();
				return false;
			});
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			// we dont need to delay in this case as icons owning extra windows
			// is a non-event
			UpdateTransientItems ();
		}
		
		void UpdateTransientItems ()
		{
			if (!IsWindowManager) {
				transient_items.Clear ();
				return;
			}
			// we will need a list of these bad boys we can mess with
			List<Wnck.Window> windows = UnmanagedWindows.ToList ();
			
			string desktopFile;
			AbstractDockItem item;
			foreach (Wnck.Window window in windows) {
				if (transient_items.Where (adi => adi is WnckDockItem)
					.Cast<WnckDockItem> ()
					.SelectMany (wdi => wdi.Windows)
					.Contains (window))
					return;
				
				desktopFile = WindowMatcher.Default.DesktopFileForWindow (window);
				
				if (desktopFile != null) {
					item = ApplicationDockItem.NewFromUri (new Uri (desktopFile).AbsoluteUri);
					transient_items.Add (item);
					
					(item as ApplicationDockItem).WindowsChanged += HandleTransientWindowsChanged;
					
					OnItemsChanged (item, AddRemoveChangeType.Add);
				}
			}
		}

		void HandleTransientWindowsChanged (object sender, EventArgs e)
		{
			if (!(sender is WnckDockItem))
				return;
			
			WnckDockItem item = sender as WnckDockItem;
			if (!item.Windows.Any ()) {
				transient_items.Remove (item);
				OnItemsChanged (item, AddRemoveChangeType.Remove);
				item.Dispose ();
			}
		}
		
		public bool InsertItem (string uri)
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");
			
			if (items.ContainsKey (uri))
				return false;
			
			AbstractDockItem item;
			
			try {
				if (uri.EndsWith (".desktop")) {
					item = ApplicationDockItem.NewFromUri (uri);
				} else {
					item = FileDockItem.NewFromUri (uri);
				}
			} catch (Exception e) {
				item = null;
			}
			
			if (item == null)
				return false;
			
			item.Owner = this;
			items[uri] = item;
			
			if (ItemsChanged != null) {
				ItemsChanged (this, new ItemsChangedArgs (item, AddRemoveChangeType.Add));
			}
			
			return true;
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
			if (WindowManagerChanged != null)
				WindowManagerChanged (this, EventArgs.Empty);
		}
		
		#region IDockItemProvider implementation
		public event EventHandler<ItemsChangedArgs> ItemsChanged;
		
		public bool Separated { get { return true; } }
		
		public bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return true;
		}
		
		public bool RemoveItem (AbstractDockItem item)
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
			
			OnItemsChanged (item, AddRemoveChangeType.Remove);
			
			item.Dispose ();
			return true;
		}
		
		public IEnumerable<AbstractDockItem> Items {
			get {
				return items.Values.Concat (transient_items);
			}
		}
		#endregion
		
		void OnItemsChanged (AbstractDockItem item, AddRemoveChangeType type)
		{
			if (ItemsChanged != null) {
				ItemsChanged (this, new ItemsChangedArgs (item, type));
			}
		}

		~FileApplicationProvider ()
		{
			Providers.Remove (this);
		}
		
		public void Dispose ()
		{
			foreach (AbstractDockItem item in items.Values)
				item.Dispose ();
		}
	}
}
