//  
// Copyright (C) 2009 Robert Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace GMail
{
	/// <summary>
	/// </summary>
	public class GMailDockItem : AbstractDockItem
	{
		public override string UniqueID ()
		{
			return "GMailDockItem#" + Atom.CurrentLabel;
		}
		
		public bool Visible {
			get { return Atom.UnreadCount > 0 || Atom.CurrentLabel == "Inbox"; }
		}
		
		public GMailAtom Atom { get; protected set; }
		
		GMailItemProvider parent;
		
		int shift;
		
		public int HueShift {
			get {
				return shift;
			}
			set {
				if (shift == value)
					return;
				shift = value;
				QueueRedraw ();
			}
		}
		
		static Regex hueRegex = new Regex ("[^a-zA-Z0-9]");
		
		public GMailDockItem (string label, GMailItemProvider parent)
		{
			ScalableRendering = false;
			
			this.parent = parent;
			Atom = new GMailAtom (label);
			
			Atom.GMailChecked += GMailCheckedHandler;
			Atom.GMailChecking += GMailCheckingHandler;
			Atom.GMailFailed += GMailFailedHandler;
			
			HueShift = GMailPreferences.prefs.Get<int> (hueRegex.Replace (UniqueID (), "_"), 0);
		}
		
		
		static int old_count = 0;
		public void GMailCheckedHandler (object obj, EventArgs e)
		{
			if (old_count < Atom.NewCount)
				UpdateAttention (true);
			old_count = Atom.NewCount;
			
			string status = "";
			if (Atom.UnreadCount == 0)
				status = Catalog.GetString ("No unread mail");
			else
				status = string.Format (Catalog.GetPluralString ("{0} unread message", "{0} unread messages", Atom.UnreadCount), Atom.UnreadCount);
			HoverText = Atom.CurrentLabel + " - " + status;
			
			parent.ItemVisibilityChanged (this, Visible);
			State &= ~ItemState.Wait;
			QueueRedraw ();
		}
		
		void UpdateAttention (bool status)
		{
			if (!GMailPreferences.NeedsAttention)
				return;
			
			Indicator = status ? ActivityIndicator.Single : ActivityIndicator.None;
		}
		
		public void GMailCheckingHandler (object obj, EventArgs e)
		{
			UpdateAttention (false);
			
			HoverText = Catalog.GetString ("Checking mail...");
			if (Atom.State == GMailState.ManualReload)
				State |= ItemState.Wait;
			QueueRedraw ();
		}
		
		public void GMailFailedHandler (object obj, GMailErrorArgs e)
		{
			UpdateAttention (false);
			
			HoverText = e.Error;
			State &= ~ItemState.Wait;
			QueueRedraw ();
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			int size = Math.Min (surface.Width, surface.Height);
			Context cr = surface.Context;
			
			string icon = Atom.Icon;
			if (Atom.State == GMailState.Error)
				icon = Atom.DisabledIcon;
			
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (icon, size))
			{
				if (HueShift != 0) {
					unsafe {
						double a, r, g, b;
						byte* pixels = (byte*) pbuf.Pixels;
						for (int i = 0; i < pbuf.Height * pbuf.Width; i++) {
							r = (double) pixels[0];
							g = (double) pixels[1];
							b = (double) pixels[2];
							a = (double) pixels[3];
							
							Cairo.Color color = new Cairo.Color (r / byte.MaxValue, 
								g / byte.MaxValue, 
								b / byte.MaxValue,
								a / byte.MaxValue);
							color = color.AddHue (HueShift);
							
							pixels[0] = (byte) (color.R * byte.MaxValue);
							pixels[1] = (byte) (color.G * byte.MaxValue);
							pixels[2] = (byte) (color.B * byte.MaxValue);
							pixels[3] = (byte) (color.A * byte.MaxValue);
							
							pixels += 4;
						}
					}
				}
				
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, 0, 0);
				if (!Atom.HasUnread)
					cr.PaintWithAlpha (.5);
				else
					cr.Paint ();
			}
			
			BadgeText = "";
			if (Atom.HasUnread)
				BadgeText += Atom.UnreadCount;
		}
		
		void OpenInbox ()
		{
			string[] login = GMailPreferences.User.Split (new char[] {'@'});
			string domain = login.Length > 1 ? login [1] : "gmail.com";
			string url = "https://mail.google.com/";
			
			// add the domain
			if (domain == "gmail.com" || domain == "googlemail.com")
				url += "mail";
			else
				url += "a/" + domain;
			
			url += "/\\#";

			// going to a custom label
			if (Atom.CurrentLabel != "Inbox")
				url += "label/";
			
			DockServices.System.Open (url + HttpUtility.UrlEncode (Atom.CurrentLabel));
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				UpdateAttention (false);
				
				OpenInbox ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		public override void Dispose ()
		{
			Atom.GMailChecked -= GMailCheckedHandler;
			Atom.GMailChecking -= GMailCheckingHandler;
			Atom.GMailFailed -= GMailFailedHandler;
			Atom.Dispose ();

			base.Dispose ();
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (direction == Gdk.ScrollDirection.Up)
				HueShift += 5;
			else if (direction == Gdk.ScrollDirection.Down)
				HueShift -= 5;
			
			if (HueShift < 0)
				HueShift += 360;
			HueShift %= 360;
			
			GMailPreferences.prefs.Set<int> (hueRegex.Replace (UniqueID (), "_"), HueShift);
		}
		
		protected void ResetHue ()
		{
			HueShift = 0;
			GMailPreferences.prefs.Set<int> (hueRegex.Replace (UniqueID (), "_"), HueShift);
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			UpdateAttention (false);
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_View ") + Atom.CurrentLabel,
					Atom.Icon,
					delegate {
						Clicked (1, Gdk.ModifierType.None, 0, 0);
					}));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Compose Mail"),
					"mail-message-new",
					delegate {
						DockServices.System.Open ("https://mail.google.com/mail/#compose");
					}));
			
			list[MenuListContainer.Actions].Add (new SeparatorMenuItem ());
			
			if (Atom.HasUnread) {
				foreach (UnreadMessage message in Atom.Messages.Take (10))
					list[MenuListContainer.Actions].Add (new GMailMenuItem (message, Atom.Icon));
				
				list[MenuListContainer.Actions].Add (new SeparatorMenuItem ());
			}
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Settings"),
					Gtk.Stock.Preferences,
					delegate {
						if (GMailConfigurationDialog.instance == null)
							GMailConfigurationDialog.instance = new GMailConfigurationDialog ();
						GMailConfigurationDialog.instance.Show ();
					}));
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Check _Now"),
					Gtk.Stock.Refresh,
					delegate {
						Atom.ResetTimer (true);
					}));
			
			list[MenuListContainer.Footer].Add (new MenuItem (Catalog.GetString ("_Reset Color"),
					"edit-clear",
					delegate {
						ResetHue ();
					}, HueShift == 0));
			
			return list;
		}
	}
}
