//  
//  Copyright (C) 2009 Robert Dyer
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
using System.Threading;

using Gtk;

namespace WeatherDocklet
{
	[System.ComponentModel.Category("File")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WeatherConfiguration : Bin
	{
		TreeStore locationTreeStore = new TreeStore (typeof (string));
		TreeStore searchTreeStore = new TreeStore (typeof (string), typeof (string));

		public WeatherConfiguration ()
		{			
			Build ();
			
			locationTreeView.Model = locationTreeStore;
			locationTreeView.Selection.Changed += OnLocationSelectionChanged;
			locationTreeView.AppendColumn ("Locations", new CellRendererText (), "text", 0);
			UpdateLocations ();
			
			searchTreeView.Model = searchTreeStore;
			searchTreeView.Selection.Changed += OnSearchSelectionChanged;
			searchTreeView.AppendColumn ("Search Results", new CellRendererText (), "text", 0);
			
			timeout.Value = WeatherPreferences.Timeout;
			metric.Active = WeatherPreferences.Metric;
			aboutLabel.Text = WeatherController.Weather.About;
			
			int selected = 0;
			foreach (AbstractWeatherSource aws in WeatherController.Service.WeatherSources)
			{
				source.AppendText (aws.Name);
				if (aws.Name.Equals(WeatherPreferences.Source))
					source.Active = selected;
				selected++;
			}
		}
		
		void UpdateLocations ()
		{
			locationTreeStore.Clear ();
			for (int i = 0; i < WeatherPreferences.Location.Length; i++)
				locationTreeStore.AppendValues (WeatherPreferences.Location [i]);
		}
		
		protected virtual void OnTimeoutFocusOut (object o, Gtk.FocusOutEventArgs args)
		{
			WeatherPreferences.Timeout = (uint) timeout.ValueAsInt;
		}
		
		protected virtual void OnMetricFocusOut (object o, Gtk.FocusOutEventArgs args)
		{
			WeatherPreferences.Metric = metric.Active;
		}
		
		protected virtual void OnSourceChanged (object sender, System.EventArgs e)
		{
			WeatherPreferences.Source = source.ActiveText;
			aboutLabel.Text = WeatherController.Weather.About;
			searchTreeStore.Clear ();
		}
		
		protected virtual void OnSearchClicked (object sender, System.EventArgs e)
		{
			searchTreeStore.Clear ();
			searchTreeStore.AppendValues ("Currently searching...", "");
			
			new Thread(() => {
				try {
					List<string> vals = new List<string> ();
		
					foreach (string s in WeatherController.Weather.SearchLocation (location.Text))
						vals.Add (s);
					
					Gtk.Application.Invoke (delegate {
						searchTreeStore.Clear ();
						
						if (vals.Count > 0)
							for (int i = 0; i < vals.Count; i += 2)
								searchTreeStore.AppendValues (vals [i], vals [i + 1]);
						else
							searchTreeStore.AppendValues ("No results found", "");
					});
				} catch {}
			}).Start ();
		}

		protected virtual void OnLocationSelectionChanged (object o, System.EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			
			if (((TreeSelection)o).GetSelected (out model, out iter))
			{
				string selected = (string) model.GetValue (iter, 0);
				int index = FindLocationIndex (selected);
				
				if (index != -1) {
					location.Text = selected;
					btnMoveUp.Sensitive = index > 0;
					btnMoveDown.Sensitive = index < WeatherPreferences.Location.Length - 1;
					btnRemove.Sensitive = true;
					return;
				}
			}
			
			btnMoveUp.Sensitive = false;
			btnMoveDown.Sensitive = false;
			btnRemove.Sensitive = false;
		}
		
		protected virtual void OnSearchSelectionChanged (object o, System.EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			
			if (((TreeSelection)o).GetSelected (out model, out iter))
			{
				string val = (string) model.GetValue (iter, 1);
				location.Text = val;
			}
		}

		protected virtual void OnAddClicked (object sender, System.EventArgs e)
		{
			if (location.Text.Trim ().Length == 0 || FindLocationIndex (location.Text) != -1)
				return;
			
			string[] locations = new string [WeatherPreferences.Location.Length + 1];
			Array.Copy (WeatherPreferences.Location, 0, locations, 0, WeatherPreferences.Location.Length);
			locations [WeatherPreferences.Location.Length] = location.Text.Trim ();
			WeatherPreferences.Location = locations;
			locationTreeStore.AppendValues (new string[] { location.Text.Trim () });
		}

		int FindLocationIndex (string location)
		{
			for (int i = 0; i < WeatherPreferences.Location.Length; i++)
				if (WeatherPreferences.Location [i].Equals (location))
					return i;
			
			return -1;
		}

		protected virtual void OnRemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			
			if (locationTreeView.Selection.GetSelected (out model, out iter))
			{
				string removedLocation = (string) model.GetValue (iter, 0);
				int index = FindLocationIndex (removedLocation);
				
				string[] locations = new string [WeatherPreferences.Location.Length - 1];
				Array.Copy (WeatherPreferences.Location, 0, locations, 0, index);
				Array.Copy (WeatherPreferences.Location, index + 1, locations, index, WeatherPreferences.Location.Length - index - 1);
				
				WeatherPreferences.Location = locations;
				locationTreeStore.Remove (ref iter);
				
				if (removedLocation.Equals (WeatherController.CurrentLocation))
					WeatherController.PreviousLocation ();
			}
		}

		protected virtual void OnMoveDownClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			
			if (locationTreeView.Selection.GetSelected (out model, out iter))
			{
				TreePath[] paths = locationTreeView.Selection.GetSelectedRows ();
				int index = FindLocationIndex ((string) model.GetValue (iter, 0));
				
				string[] locations = WeatherPreferences.Location;
				string temp = locations [index];
				locations [index] = locations [index + 1];
				locations [index + 1] = temp;
				
				WeatherPreferences.Location = locations;
				UpdateLocations ();
				paths [0].Next ();
				locationTreeView.Selection.SelectPath (paths [0]);
				locationTreeView.ScrollToCell (paths [0], null, false, 0, 0);
			}
		}

		protected virtual void OnMoveUpClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			
			if (locationTreeView.Selection.GetSelected (out model, out iter))
			{
				TreePath[] paths = locationTreeView.Selection.GetSelectedRows ();
				int index = FindLocationIndex ((string) model.GetValue (iter, 0));
				
				string[] locations = WeatherPreferences.Location;
				string temp = locations [index];
				locations [index] = locations [index - 1];
				locations [index - 1] = temp;
				
				WeatherPreferences.Location = locations;
				UpdateLocations ();
				paths [0].Prev ();
				locationTreeView.Selection.SelectPath (paths [0]);
				locationTreeView.ScrollToCell (paths [0], null, false, 0, 0);
			}
		}
	}
}
