//  
//  Copyright (C) 2010 Robert Dyer
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

using Gdk;
using Gtk;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Clippy
{
	public class ClippyItem : IconDockItem
	{
		List<string> clips = new List<string> ();
		int curPos = -1;

		public override string UniqueID ()
		{
			return "Clippy";
		}

		Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Selection.Clipboard);

		uint timer;
		
		public ClippyItem ()
		{
			Icon = "edit-cut";

			timer = GLib.Timeout.Add (500, CheckClipboard);
		}

		bool CheckClipboard ()
		{
			clipboard.RequestText ((cb, text) => {
				if (text == null)
					return;
				if (clips.Count == 0 || !clips[clips.Count - 1].Equals(text)) {
					clips.Add (text);
					curPos = clips.Count;
					Updated ();
				}
			});
			return true;
		}

		string GetClipboardAt (int pos)
		{
			return clips [pos - 1].Replace ("\n", "");
		}

		void Updated ()
		{
			if (curPos == -1 && clips.Count > 0)
				HoverText = GetClipboardAt (clips.Count);
			else if (curPos > clips.Count)
				HoverText = Catalog.GetString ("Clipboard is currently empty.");
			else
				HoverText = GetClipboardAt (curPos);
		}

		void CopyEntry (int pos)
		{
			if (pos > clips.Count)
				return;

			clipboard.Text = clips[pos - 1];
			clips.RemoveAt (pos - 1);

			Updated ();
		}

		void CopyEntry ()
		{
			CopyEntry (curPos);
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (direction == Gdk.ScrollDirection.Up)
				curPos++;
			else
				curPos--;

			if (curPos < 1)
				curPos = clips.Count;
			else if (curPos > clips.Count)
				curPos = 1;

			Updated ();
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				CopyEntry ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			List<Docky.Menus.MenuItem> items = new List<Docky.Menus.MenuItem> ();
			
			for (int i = clips.Count; i > 0; i--)
				items.Add (new Docky.Menus.MenuItem (GetClipboardAt (i), Gtk.Stock.Cut,
						delegate {
							CopyEntry (i);
						}));
			
			MenuList list = base.OnGetMenuItems ();
			
			if (items.Count > 0)
				list[MenuListContainer.Actions].InsertRange (0, items);
			
			return list;
		}
		
		public override void Dispose ()
		{
			if (timer > 0)
				GLib.Source.Remove (timer);
			base.Dispose ();
		}
	}
}
