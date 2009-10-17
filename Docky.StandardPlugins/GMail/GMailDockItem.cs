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
			return "GMailDockItem#" + atom.CurrentLabel;
		}
		
		public bool Visible {
			get { return atom.UnreadCount > 0 || atom.CurrentLabel == "Inbox"; }
		}
		
		GMailAtom atom;
		
		public GMailDockItem (string label)
		{
			atom = new GMailAtom (label);
			
			atom.GMailChecked += GMailCheckedHandler;
			atom.GMailChecking += GMailCheckingHandler;
			atom.GMailFailed += GMailFailedHandler;
			
			GLib.Idle.Add (delegate {
				atom.ResetTimer ();
				return false;
			});
		}
		
		static int old_count = 0;
		public void GMailCheckedHandler (object obj, EventArgs e)
		{
			if (old_count < atom.NewCount)
				UpdateAttention (true);
			old_count = atom.NewCount;
			
			string status = "";
			if (atom.UnreadCount == 0)
				status = Catalog.GetString ("No unread mail");
			else if (atom.UnreadCount == 1)
				status = Catalog.GetString ("1 unread message");
			else
				status = atom.UnreadCount + Catalog.GetString (" unread messages");
			HoverText = atom.CurrentLabel + " - " + status;
			
			(Owner as GMailItemProvider).ItemVisibilityChanged (this, Visible);
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
			if (atom.State == GMailState.ManualReload || atom.State == GMailState.Error)
				icon = "gmail-logo-dark.png@";
			
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (icon + GetType ().Assembly.FullName, size))
			{
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, 0, 0);
				if (atom.State == GMailState.ManualReload || !atom.HasUnread)
					cr.PaintWithAlpha (.5);
				else
					cr.Paint ();
			}
			
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			layout.FontDescription = new Gtk.Style().FontDescription;
			layout.FontDescription.Weight = Pango.Weight.Bold;
			layout.Ellipsize = Pango.EllipsizeMode.None;
			
			Pango.Rectangle inkRect, logicalRect;
			
			if (atom.HasUnread)
			{
				using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("badge-yellow.svg@" + GetType ().Assembly.FullName, size / 2))
				{
					Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, size / 2, 0);
					cr.PaintWithAlpha (1);
				}
			
				layout.Width = Pango.Units.FromPixels (size / 2);

				layout.SetText ("" + atom.UnreadCount);

				if (atom.UnreadCount < 100)
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (size / 4);
				else
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (size / 5);

				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (size / 2 + (size / 2 - inkRect.Width) / 2, (size / 2 - logicalRect.Height) / 2);

				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2;
				cr.Color = new Cairo.Color (0, 0, 0, 0.2);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 1);
				cr.Fill ();
			}
			
			// no need to draw the label for the Inbox
			if (atom.CurrentLabel == "Inbox")
				return;

			layout.Width = Pango.Units.FromPixels (size);

			layout.SetText (atom.CurrentLabel);

			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (size / 5);

			layout.GetPixelExtents (out inkRect, out logicalRect);
			cr.MoveTo ((size - inkRect.Width) / 2, size - logicalRect.Height);

			Pango.CairoHelper.LayoutPath (cr, layout);
			cr.LineWidth = 2;
			cr.Color = new Cairo.Color (0, 0, 0, 0.4);
			cr.StrokePreserve ();
			cr.Color = new Cairo.Color (1, 1, 1, 0.6);
			cr.Fill ();
		}
		
		void OpenInbox ()
		{
			string label = atom.CurrentLabel;
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
		
		public override void Dispose ()
		{
			atom.GMailChecked -= GMailCheckedHandler;
			atom.GMailChecking -= GMailCheckingHandler;
			atom.GMailFailed -= GMailFailedHandler;
			atom.Dispose ();

			base.Dispose ();
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			UpdateAttention (false);
			
			yield return new MenuItem (Catalog.GetString ("View ") + atom.CurrentLabel,
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

			if (atom.HasUnread) {
				foreach (UnreadMessage message in atom.Messages.Take (10))
					yield return new GMailMenuItem (message);
				
				yield return new SeparatorMenuItem ();
			}
			
			yield return new MenuItem (Catalog.GetString ("Settings"),
					Gtk.Stock.Preferences,
					delegate {
						GMailConfigurationDialog dlg = new GMailConfigurationDialog ();
						dlg.Show ();
					});
			
			yield return new MenuItem (Catalog.GetString ("Check Now"),
					Gtk.Stock.Refresh,
					delegate {
						atom.ResetTimer (true);
						QueueRedraw ();
					});
		}
	}
}
