//  
//  Copyright (C) 2009 Chris Szikszoy
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

using Mono.Unix;

namespace Docky.Widgets
{

	public abstract class AbstractTileObject
	{
		public event EventHandler IconUpdated;
		
		void OnIconUpdated ()
		{
			if (IconUpdated != null)
				IconUpdated (this, EventArgs.Empty);
		}

		string icon;
		public virtual string Icon {
			get {
				if (icon == null)
					icon = "";
				return icon;
			}
			protected set {
				if (icon == value)
					return;
				icon = value;
				OnIconUpdated ();
			}
		}
		
		int? shift;
		public virtual int HueShift {
			get {
				if (!shift.HasValue)
				    shift = 0;
				return shift.Value;
			}
			protected set {
				if (shift.HasValue && shift.Value == value)
					return;
				shift = value;
				OnIconUpdated ();
			}
		}
		
		string desc;
		public virtual string Description {
			get { 
				if (desc == null)
					desc = "";
				return desc;
			}
			protected set {
				if (desc == value)
					return;
				desc = value;
			}
		}		
		
		string name;
		public virtual string Name {
			get { 
				if (name == null)
					name = "";
				return name;
			}
			protected set {
				if (name == value)
					return;
				name = value;
			}
		}		
		
		public virtual void OnActiveChanged ()
		{
		}
		
		string sub_desc_title;
		public virtual string SubDescriptionTitle {
			get { 
				if (sub_desc_title == null)
					sub_desc_title = "";
				return sub_desc_title;
			}
			protected set {
				if (sub_desc_title == value)
					return;
				sub_desc_title = value;
			}
		}
		
		string sub_desc_text;
		public virtual string SubDescriptionText {
			get { 
				if (sub_desc_text == null)
					sub_desc_text = "";
				return sub_desc_text;
			}
			protected set {
				if (sub_desc_text == value)
					return;
				sub_desc_text = value;
			}
		}
		
		string enabled_text;
		public virtual string ButtonStateEnabledText {
			get { 
				if (enabled_text == null)
					enabled_text = Catalog.GetString ("_Remove");
				return enabled_text;
			}
			protected set {
				if (enabled_text == value)
					return;
				enabled_text = value;
			}
		}
		
		string disabled_text;
		public virtual string ButtonStateDisabledText {
			get { 
				if (disabled_text == null)
					disabled_text = Catalog.GetString ("_Add");
				return disabled_text;
			}
			protected set {
				if (disabled_text == value)
					return;
				disabled_text = value;
			}
		}
		
		bool? show_button;
		public virtual bool ShowActionButton {
			get { 
				if (!show_button.HasValue)
					show_button = true;
				return show_button.Value;
			}
			protected set {
				if (show_button.HasValue && show_button.Value == value)
					return;
				show_button = value;
			}
		}
		
		bool? enabled;
		public virtual bool Enabled {
			get { 
				if (!enabled.HasValue)
					enabled = false;
				return enabled.Value;
			}
			set {
				if (enabled.HasValue && enabled.Value == value)
					return;
				enabled = value;
			}
		}
	}
}
