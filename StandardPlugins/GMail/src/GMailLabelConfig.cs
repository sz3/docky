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
using System.Linq;
using System.Collections.Generic;

using Gdk;
using Gtk;

using Mono.Unix;

namespace GMail
{

	[System.ComponentModel.ToolboxItem(true)]
	public partial class GMailLabelConfig : Gtk.Bin
	{

		public GMailLabelConfig ()
		{
			this.Build ();
			Name = Catalog.GetString ("Labels");
			
			label_list.IconSize = 24;

			label_entry.InnerEntry.KeyPressEvent += OnKeyPressed;
			label_entry.Show ();
			
			Shown += delegate {
				check_interval.Value = GMailPreferences.RefreshRate;
				label_list.Clear ();
				label_list.AppendTile (new GMailLabel (GMailDockItem.DefaultLabel));
				foreach (string label in GMailPreferences.Labels)
					label_list.AppendTile (new GMailLabel (label));				
			};
		}
		
		protected virtual void AddLabelClicked (object sender, System.EventArgs e)
		{			
			// GMail's atom feed doesnt like the '/' character
			// and needs it as '-'
			string newLabel = label_entry.InnerEntry.Text.Replace ("/", "-").Trim ();
			
			label_entry.InnerEntry.Text = "";
			
			if (newLabel.Length == 0 || GMailPreferences.Labels.Contains (newLabel))
				return;
			
			List<string> labels = GMailPreferences.Labels.ToList ();
			labels.Add (newLabel);
			GMailPreferences.Labels = labels.ToArray ();
			
			label_list.AppendTile (new GMailLabel (newLabel));
		}

		protected virtual void OnIntervalValueChanged (object sender, System.EventArgs e)
		{
			GMailPreferences.RefreshRate = (uint) check_interval.ValueAsInt;
		}
		
		[GLib.ConnectBefore]
		protected virtual void OnKeyPressed (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return)
				add_label.Click ();
		}
	}
}
