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
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

using Gtk;

using Docky.Services;

namespace NPR
{

	[System.ComponentModel.ToolboxItem(true)]
	public partial class StationSearchWidget : Gtk.Bin
	{
		
		StationsView view;

		public StationSearchWidget ()
		{
			this.Build ();
			
			view = new StationsView ();
			
			stationsScroll.HscrollbarPolicy = PolicyType.Never;
			stationsScroll.AddWithViewport (view);

		}
		
		public void ClearResults ()
		{
			view.Clear ();
		}
		
		public void ShowMyStations ()
		{
			view.Clear ();
			ZipEntry.Text = "";
			my_stations.Sensitive = false;
			
			DockServices.System.RunOnThread (() => {
				foreach (XElement stationXElement in NPR.MyStations.Select (id => NPR.StationXElement (id))) {
					DockServices.System.RunOnMainThread (() => {
						view.AppendStation (new Station (stationXElement));
					});
				}
			});	
		}
		
		protected virtual void SearchClicked (object sender, System.EventArgs e)
		{
			uint zip;
			if (!uint.TryParse (ZipEntry.Text, out zip))
				return;
			
			my_stations.Sensitive = true;
			
			view.Clear ();
			
			DockServices.System.RunOnMainThread (() => {
				// grab a list of nearby stations, sorted by closeness to the supplied query
				IEnumerable<Station> stations = NPR.SearchStations (zip).OrderByDescending (s => s.Signal);
				DockServices.System.RunOnMainThread (() => {
					if (stations.Count () == 0) {
						view.AppendStation (new Station (-1));
						return;
					}
					foreach (Station s in stations)
						view.AppendStation (s);
				});
			});
		}

		protected virtual void MyStationsClicked (object sender, System.EventArgs e)
		{
			ShowMyStations ();
		}
	}
}
