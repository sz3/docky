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

using GConf;
using Gdk;

namespace Docky.Menus
{

	public class MenuItem : IDisposable
	{
		public event EventHandler DisabledChanged;
		public event EventHandler TextChanged;
		public event EventHandler IconChanged;
		public event EventHandler Clicked;
		
		// date of the docky rev1 on launchpad, what we'll call the DockyEpoch
		static DateTime DockyEpoch = new DateTime (2009, 9, 1, 0, 37, 37);
		
		public bool Bold { get; set;}
		
		static bool defaultShowIcons;
		static MenuItem () {
			object o = new GConf.Client ().Get ("/desktop/gnome/interface/menus_have_icons");
			defaultShowIcons = o != null && o is bool && (bool) o;
		}
		
		bool? show_icons;
		public bool ShowIcons { 
			get {
				if (!show_icons.HasValue)
					show_icons = defaultShowIcons;
				return (ForcePixbuf != null || !string.IsNullOrEmpty (Icon)) && show_icons.Value;
			}
			protected set {
				if (show_icons.HasValue && show_icons.Value == value)
					return;
				show_icons = value;
			}
		}	
		
		bool disabled;
		public bool Disabled {
			get { return disabled; }
			set {
				if (disabled == value)
					return;
				disabled = value;
				if (DisabledChanged != null)
					DisabledChanged (this, EventArgs.Empty);
			}
		}
		
		public char? Mnemonic { get; set; }
		
		string text;
		public string Text {
			get { return text; }
			set {
				if (text == value)
					return;
				text = GLib.Markup.EscapeText (value);
				int pos = text.IndexOf ("_") + 1;
				if (pos > 0 && pos < text.Length)
					Mnemonic = text.ToLower () [pos];
				if (TextChanged != null)
					TextChanged (this, EventArgs.Empty);
			} 
		}
		
		string icon;
		public string Icon {
			get { return icon; }
			set {
				// if we set this, clear the forced pixbuf
				if (forced_pixbuf != null)
					forced_pixbuf = null;
				if (icon == value)
					return;
				icon = value;
				if (IconChanged != null)
					IconChanged (this, EventArgs.Empty);
			}
		}
		
		Pixbuf forced_pixbuf;
		public Pixbuf ForcePixbuf {
			get { return forced_pixbuf; }
			protected set {
				if (forced_pixbuf == value)
					return;
				forced_pixbuf = value;
			}
		}		
		
		string emblem;
		public string Emblem {
			get { return emblem; }
			set {
				if (emblem == value)
					return;
				emblem = value;
				if (IconChanged != null)
					IconChanged (this, EventArgs.Empty);
			}
		}
		
		public uint ID { get; private set; }
		
		public void SendClick ()
		{
			if (Clicked != null)
				Clicked (this, EventArgs.Empty);
		}
		
		public MenuItem ()
		{
		}
		
		public MenuItem (string text, string icon) : this (text, icon, false)
		{
		}
		
		public MenuItem (string text, string icon, bool disabled)
		{
			Bold = false;
			this.icon = icon;
			Text = text;
			this.disabled = disabled;
			this.ID = (uint) (DateTime.Now - DockyEpoch).TotalMilliseconds;
		}
		
		public MenuItem (string text, string icon, EventHandler onClicked) : this(text, icon)
		{
			Clicked += onClicked;
		}
		
		public MenuItem (string text, string icon, EventHandler onClicked, bool disabled) : this(text, icon, disabled)
		{
			Clicked += onClicked;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (forced_pixbuf != null)
				forced_pixbuf.Dispose ();
			forced_pixbuf = null;
			
			Clicked = null;
			IconChanged = null;
			DisabledChanged = null;
			TextChanged = null;
		}
		#endregion
	}
}
