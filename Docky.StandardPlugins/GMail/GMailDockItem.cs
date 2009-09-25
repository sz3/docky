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
			return "GMailDockItem";
		}
		
		public GMailDockItem ()
		{
			GMailAtom.GMailChecked += GMailCheckedHandler;
			GMailAtom.GMailChecking += GMailCheckingHandler;
			GMailAtom.GMailFailed += GMailFailedHandler;
			
			GMailAtom.ResetTimer ();
		}
		
		static int old_count = 0;
		public void GMailCheckedHandler (object obj, EventArgs e)
		{
			string status = "";
			if (old_count < GMailAtom.NewCount)
				UpdateAttention (true);
			old_count = GMailAtom.NewCount;
			if (GMailAtom.UnreadCount == 0)
				status = Catalog.GetString ("No unread mail");
			else if (GMailAtom.UnreadCount == 1)
				status = Catalog.GetString ("1 unread message");
			else
				status = GMailAtom.UnreadCount + Catalog.GetString (" unread messages");
			HoverText = GMailAtom.CurrentLabel + " - " + status;
			
			QueueRedraw ();
		}
		
		void UpdateAttention (bool status)
		{
			if (!GMailPreferences.NeedsAttention)
				return;
			
			State = status ? ItemState.Urgent : ItemState.Wait;
			Indicator = status ? ActivityIndicator.Single : ActivityIndicator.None;
		}
		
		public void GMailCheckingHandler (object obj, EventArgs e)
		{
			UpdateAttention (false);
			
			HoverText = Catalog.GetString ("Checking mail...");
			QueueRedraw ();
		}
		
		public void GMailFailedHandler (object obj, GMailErrorArgs e)
		{
			UpdateAttention (false);
			
			HoverText = e.Error;
			QueueRedraw ();
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			int size = Math.Min (surface.Width, surface.Height);
			Context cr = surface.Context;
			
			string icon = "gmail-logo.png@";
			if (GMailAtom.State == GMailState.ManualReload || GMailAtom.State == GMailState.Error)
				icon = "gmail-logo-dark.png@";
			
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (icon + GetType ().Assembly.FullName, size))
			{
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, 0, 0);
				if (GMailAtom.State == GMailState.ManualReload || !GMailAtom.HasUnread)
					cr.PaintWithAlpha (.5);
				else
					cr.Paint ();
			}
			
			if (GMailAtom.HasUnread)
			{
				using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("badge-yellow.svg@" + GetType ().Assembly.FullName, size / 2))
				{
					Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, size / 2, 0);
					cr.PaintWithAlpha (0.9);
				}
			
				Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (size / 2);

				layout.SetText ("" + GMailAtom.UnreadCount);

				if (GMailAtom.UnreadCount < 100)
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (size / 4);
				else
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (size / 5);

				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (size / 2 + (size / 2 - inkRect.Width) / 2, (size / 2 - logicalRect.Height) / 2);

				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2;
				cr.Color = new Cairo.Color (0, 0, 0, 0.4);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 1);
				cr.Fill ();
			}
		}
		
		void OpenInbox ()
		{
			string label = GMailAtom.CurrentLabel;
			string username = GMailPreferences.User;
			string[] login = username.Split (new char[] {'@'});
			string domain = login.Length > 1 ? login [1] : "gmail.com";
			string url = "https://mail.google.com/{0}/#{1}";
			
			if (label != "Inbox")
				label = String.Format ("label/{0}", HttpUtility.UrlEncode (label));
			
			if (domain == "gmail.com" || domain == "googlemail.com")
				url = String.Format (url, "mail", label);
			else
				url = String.Format (url, "a/" + domain, label);
			
			DockServices.System.Open (url);
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
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			UpdateAttention (false);
			
			if (GMailPreferences.Labels.Length == 0)
				return;
			
			switch (direction) {
			case Gdk.ScrollDirection.Up:
			case Gdk.ScrollDirection.Left:
				GMailPreferences.CurrentLabel--;
				break;
			case Gdk.ScrollDirection.Down:
			case Gdk.ScrollDirection.Right:
				GMailPreferences.CurrentLabel++;
				break;
			}
			
			if (GMailPreferences.CurrentLabel < -1)
				GMailPreferences.CurrentLabel = GMailPreferences.Labels.Length - 1;
			if (GMailPreferences.CurrentLabel >= GMailPreferences.Labels.Length)
				GMailPreferences.CurrentLabel = -1;
			
			GMailAtom.ResetTimer (true);
			base.OnScrolled (direction, mod);
		}
		
		public override void Dispose ()
		{
			GMailAtom.GMailChecked -= GMailCheckedHandler;
			GMailAtom.GMailChecking -= GMailCheckingHandler;
			GMailAtom.GMailFailed -= GMailFailedHandler;

			base.Dispose ();
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			UpdateAttention (false);
			
			yield return new MenuItem (Catalog.GetString ("View ") + GMailAtom.CurrentLabel,
					"gmail-logo.png@" + GetType ().Assembly.FullName,
					delegate {
						Clicked (1, Gdk.ModifierType.None, 0, 0);
					});
			yield return new MenuItem (Catalog.GetString ("Compose Mail"),
					"mail-message-new",
					delegate {
						DockServices.System.Open ("https://mail.google.com/mail/#compose");
					});
			
			yield return new SeparatorMenuItem ();

			if (GMailAtom.HasUnread) {
				foreach (UnreadMessage message in GMailAtom.Messages.Take (10))
					yield return new GMailMenuItem (message);
				
				yield return new SeparatorMenuItem ();
			}
			
			yield return new MenuItem (Catalog.GetString ("Check Now"),
					Gtk.Stock.Refresh,
					delegate {
						GMailAtom.ResetTimer (true);
						QueueRedraw ();
					});
			
			yield return new MenuItem (Catalog.GetString ("Settings"),
					Gtk.Stock.Preferences,
					delegate {
						GMailConfigurationDialog dlg = new GMailConfigurationDialog ();
						dlg.Show ();
					});
		}
	}
}
