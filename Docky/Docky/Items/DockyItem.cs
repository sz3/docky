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
		int ThemeHue { get; set; }
		
		public DockyItem ()
		{
			Indicator = ActivityIndicator.Single;
			HoverText = "Docky";
			Icon = "docky";
			
			Docky.Controller.DockHueChanged += delegate {
				SetHue ();
			};
		}
		
		void SetHue ()
		{
			HueShift = (Docky.Controller.DockyHueShift == 0) ? ThemeHue : Docky.Controller.DockyHueShift;
			OnIconUpdated ();
			QueueRedraw ();
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
		
		protected override void OnStyleSet (Gtk.Style style)
		{
			Gdk.Color gdkColor = Style.Backgrounds [(int) Gtk.StateType.Selected];
			int hue = (int) new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0).GetHue ();
			ThemeHue = (((hue - 202) % 360) + 360) % 360;
			SetHue ();
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
				Docky.Config.Show ();
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			// intentionally dont inherit
			MenuList list = new MenuList ();
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Settings"), PrefsIcon, (o, a) => Docky.Config.Show ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_About"), AboutIcon, (o, a) => Docky.ShowAbout ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Quit Docky"), CloseIcon, (o, a) => Docky.Quit ()));
			return list;
		}

	}
}
