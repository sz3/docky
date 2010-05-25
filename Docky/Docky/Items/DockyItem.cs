//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer, Chris Szikszoy
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
using Mono.Unix;

using Docky.Menus;
using Docky.Services;

namespace Docky.Items
{
	internal class DockyItem : ColoredIconDockItem, INonPersistedItem
	{
		static IPreferences prefs = DockServices.Preferences.Get <DockyItem> ();
		
		bool? docky_item;
		public bool Show {
			get {
				if (!docky_item.HasValue)
					docky_item = prefs.Get<bool> ("ShowDockyItem", true);
				return docky_item.Value;
			}
		}
		
		bool? show_settings;
		public bool ShowSettings {
			get {
				if (!show_settings.HasValue)
					show_settings = prefs.Get<bool> ("ShowSettings", true);
				return show_settings.Value;
			}
		}
		
		bool? show_quit;
		public bool ShowQuit {
			get {
				if (!show_quit.HasValue)
					show_quit= prefs.Get<bool> ("ShowQuit", true);
				return show_quit.Value;
			}
		}
		
		public DockyItem ()
		{
			Indicator = ActivityIndicator.Single;
			HoverText = prefs.Get<string> ("HoverText", "Docky");
			Icon = "docky";
		}
		
		protected string AboutIcon {
			get {
				return "[monochrome]about.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected string CloseIcon {
			get {
				return "[monochrome]close.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected string PrefsIcon {
			get {
				return "[monochrome]preferences.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected string HelpIcon {
			get {
				return "[monochrome]help.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected override void OnStyleSet (Gtk.Style style)
		{
			Gdk.Color gdkColor = Style.Backgrounds [(int) Gtk.StateType.Selected];
			int hue = (int) new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0).GetHue ();
			if (HueShift >= 0)
				HueShift = (((hue - 202) % 360) + 360) % 360;
		}
		
		public override string UniqueID ()
		{
			return "DockyItem";
		}
		
		protected override void OnScrolled (ScrollDirection direction, ModifierType mod)
		{
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				string command = prefs.Get<string> ("DockyItemCommand", "");
				if (string.IsNullOrEmpty (command))
					ConfigurationWindow.Instance.Show ();
				else
					DockServices.System.Execute (command);
				
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = new MenuList ();
			if (ShowSettings)
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Settings"), PrefsIcon, (o, a) => ConfigurationWindow.Instance.Show ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_About"), AboutIcon, (o, a) => Docky.ShowAbout ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Help"), HelpIcon, (o, a) => DockServices.System.Open ("http://wiki.go-docky.com")));
			if (ShowQuit)
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Quit Docky"), CloseIcon, (o, a) => Docky.Quit ()));
			return list;
		}

	}
}
