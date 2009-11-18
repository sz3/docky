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
using System.Threading;

using Cairo;
using Gdk;
using Wnck;

using Docky.Menus;
using Docky.Services;
using Docky.Windowing;
using Docky.Zeitgeist;

namespace Docky.Items
{
	public class ApplicationDockItem : WnckDockItem
	{
		public static ApplicationDockItem NewFromUri (string uri)
		{
			DesktopItem desktopItem;
			string filename = Gnome.Vfs.Global.GetLocalPathFromUri (uri);
			
			try {
				desktopItem = new DesktopItem (filename);
			} catch (Exception e) {
				Log<ApplicationDockItem>.Error (e.Message);
				Log<ApplicationDockItem>.Debug (e.StackTrace);
				return null;
			}
			
			if (desktopItem == null)
				return null;
			
			return new ApplicationDockItem (desktopItem);
		}
		
		ZeitgeistResult[] related_uris = new ZeitgeistResult[0];
		object related_lock = new Object ();
		
		uint timer;
		IEnumerable<string> mimes;
		
		public DesktopItem OwnedItem { get; protected set; }
	
		public override string ShortName {
			get {
				// FIXME
				return HoverText;
			}
		}
		
		private ApplicationDockItem (DesktopItem item)
		{
			OwnedItem = item;
			
			if (item.HasAttribute ("Icon")) {
				Icon = item.GetString ("Icon");
				Gtk.IconInfo info = Gtk.IconTheme.Default.LookupIcon (Icon, 48, Gtk.IconLookupFlags.ForceSvg);
				if (info.Filename.EndsWith (".svg")) {
					Icon = info.Filename;
				}
				info.Dispose ();
			}
			
			if (item.HasAttribute ("Name")) {
				HoverText = item.GetLocaleString ("Name");
				if (HoverText == null)
					HoverText = item.GetString ("Name");
			} else {
				HoverText = System.IO.Path.GetFileNameWithoutExtension (item.Location);
			}
			
			if (item.HasAttribute ("MimeType")) {
				mimes = item.GetStrings ("MimeType");
			} else {
				mimes = Enumerable.Empty<string> ();
			}
			
			UpdateWindows ();
			UpdateRelated ();
			
			timer = GLib.Timeout.Add (10 * 60 * 1000, delegate {
				UpdateRelated ();
				return true;
			});
			
			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
			Wnck.Screen.Default.ActiveWindowChanged += WnckScreenDefaultWindowChanged;
		}
		
		public override string UniqueID ()
		{
			return OwnedItem.Location;
		}
		
		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			UpdateWindows ();
			OnPaintNeeded ();
		}

		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			UpdateWindows ();
			OnPaintNeeded ();
		}
		
		void WnckScreenDefaultWindowChanged (object o, ActiveWindowChangedArgs args)
		{
			UpdateWindows ();
			OnPaintNeeded ();
		}

		void UpdateWindows ()
		{
			Windows = WindowMatcher.Default.WindowsForDesktopFile (OwnedItem.Location);
		}
		
		void UpdateRelated ()
		{
			if (mimes.Any ()) {
				Thread th = new Thread ((ThreadStart) delegate {
					Zeitgeist.ZeitgeistFilter filter = new Zeitgeist.ZeitgeistFilter ();
					filter.MimeTypes.AddRange (mimes);
					
					ZeitgeistResult[] uris = ZeitgeistProxy.Default.FindEvents (
						DateTime.Now.AddDays (-14), 
						DateTime.Now, 
						8, 
						false, 
						"mostused",
						filter.AsSingle ())
						.Where (res => res.Uri.Contains ("://") &&
									(!res.Uri.StartsWith ("file://") || System.IO.File.Exists (new Uri (res.Uri).LocalPath)))
						.Take (4)
						.ToArray ();
					
					lock (related_lock) {
						related_uris = uris;
					}
				});
				th.Priority = ThreadPriority.BelowNormal;
				th.Start ();
			}
		}
		
		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
			if (ManagedWindows.Any ())
				list[MenuListContainer.Actions].Insert (0, new MenuItem ("New Window", RunIcon, (o, a) => Launch ()));
			else
				list[MenuListContainer.Actions].Insert (0, new MenuItem ("Open", RunIcon, (o, a) => Launch ()));

			if (related_uris.Any ()) {
				lock (related_lock) {
					foreach (ZeitgeistResult result in related_uris) {
						RelatedFileMenuItem item = new RelatedFileMenuItem (result.Uri);
						if (!string.IsNullOrEmpty (result.Text))
							item.Text = result.Text;
						item.Clicked += ItemClicked;
						list[MenuListContainer.RelatedItems].Add (item);
					}
				}
			}
			
			return list;
		}

		void ItemClicked (object sender, EventArgs e)
		{
			RelatedFileMenuItem item = sender as RelatedFileMenuItem;
			if (item == null)
				return;
			
			LaunchWithFiles (item.Uri.AsSingle ());
		}
		
		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			if (uris == null)
				return false;
			
			foreach (string uri in uris) {
				GLib.FileInfo info = GLib.FileFactory.NewForUri (uri).QueryInfo ("standard::content-type", GLib.FileQueryInfoFlags.None, null);
				string mime = info.ContentType;
				if (mimes.Any (m => GLib.Content.TypeIsA (mime, m) || GLib.Content.TypeEquals (mime, m)))
					return true;
			}
			
			return base.OnCanAcceptDrop (uris);
		}
		
		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			LaunchWithFiles (uris);
			return true;
		}


		protected override ClickAnimation OnClicked (uint button, ModifierType mod, double xPercent, double yPercent)
		{
			if ((!ManagedWindows.Any () && button == 1) || button == 2) {
				Launch ();
				return ClickAnimation.Bounce;
			}
			return base.OnClicked (button, mod, xPercent, yPercent);
		}
		
		void Launch ()
		{
			LaunchWithFiles (Enumerable.Empty<string> ());
		}
		
		public void LaunchWithFiles (IEnumerable<string> files)
		{
			OwnedItem.Launch (files);
		}
		
		public override void Dispose ()
		{
			GLib.Source.Remove (timer);
			Wnck.Screen.Default.WindowOpened -= WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed -= WnckScreenDefaultWindowClosed;
			base.Dispose ();
		}
	}
}
