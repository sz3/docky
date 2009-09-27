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
using Wnck;

using Docky.Menus;
using Docky.Services;
using Docky.Windowing;

namespace Docky.Items
{

	public class ApplicationDockItem : WnckDockItem
	{
		public static ApplicationDockItem NewFromUri (string uri)
		{
			Gnome.DesktopItem desktopItem;
			string filename = new Uri (uri).LocalPath;
			
			try {
				desktopItem = Gnome.DesktopItem.NewFromFile (filename, 0);
			} catch (Exception e) {
				Console.Error.WriteLine (e.Message);
				return null;
			}
			
			if (desktopItem == null)
				return null;
			
			return new ApplicationDockItem (desktopItem);
		}
		
		Gnome.DesktopItem desktop_item;
		string path;
	
		public override string ShortName {
			get {
				return HoverText; // fixme
			}
		}
		
		private ApplicationDockItem (Gnome.DesktopItem item)
		{
			desktop_item = item;
			if (item.AttrExists ("Icon"))
				Icon = item.GetString ("Icon");
			
			if (item.AttrExists ("Name")) {
				try {
					HoverText = item.GetLocalestring ("Name");
				} catch {
					HoverText = item.GetString ("Name");
				}
			} else {
				HoverText = System.IO.Path.GetFileNameWithoutExtension (path);
			}
			
			path = new Uri (item.Location).LocalPath;
			
			UpdateWindows ();
			
			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
		}

		public override string UniqueID ()
		{
			return path;
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
		
		void UpdateWindows ()
		{
			Windows = WindowMatcher.Default.WindowsForDesktopFile (path);
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			if (desktop_item.AttrExists ("MimeType")) {
				string[] mimes = desktop_item.GetString ("MimeType").Split (';');
				
				foreach (string s in Zeitgeist.ZeitgeistProxy.Default.RelevantFilesForMimeTypes (mimes))
					Console.WriteLine (s);
			}
			
			if (ManagedWindows.Any ())
				yield return new MenuItem ("New Instance", RunIcon, (o, a) => Launch ());
			else
				yield return new MenuItem ("Open", RunIcon, (o, a) => Launch ());
			
			foreach (MenuItem item in base.GetMenuItems ()) {
				yield return item;
			}
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
			if (files.Any ()) {
				GLib.List glist = new GLib.List (files.ToArray () as object[], typeof(string), false, true);
				desktop_item.Launch (glist, Gnome.DesktopItemLaunchFlags.OnlyOne);
			} else {
				desktop_item.Launch (null, 0);
			}
		}
	}
}
