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

using Docky.Items;
using Docky.Services;

namespace NPR
{
	public class NPRItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "NPR";
			}
		}
		
		public override string Icon { get { return "nprlogo.png@" + GetType ().Assembly.FullName; } }
		
		public override void Dispose ()
		{
			items.ForEach (adi => {
				adi.Dispose ();
			});
		}
		
		#endregion
		
		List<AbstractDockItem> items = new List<AbstractDockItem> ();
		
		public NPRItemProvider ()
		{
			ReloadStations ();
			
			NPR.StationsUpdated += delegate (object sender, StationsUpdatedEventArgs args) {
				switch (args.UpdateAction) {
				case StationUpdateAction.Added:
					AddStation (args.StationID);
					break;
				case StationUpdateAction.Removed:
					RemoveStation (args.StationID);
					break;
				}
			};
		}
		
		void AddStation (int stationID)
		{
			items.Add (new Station (stationID));
			
			items.Cast <Station> ().Where (s => s.ID < 0).ToList ().ForEach (station => {
				items.Remove (station);
				station.Dispose ();
			});
			
			Items = items;
		}
		
		void RemoveStation (int stationID)
		{
			Station station = items.Cast <Station> ().Where (s => s.ID == stationID).First ();
			
			items.Remove (station);
			Items = items;
			
			station.Dispose ();
			
			MaybeAddNullStation ();
		}
		
		void MaybeAddNullStation ()
		{
			if (items.Count () == 0)
				items.Add (new Station (-1));
			
			Items = items;
		}
		
		public void ReloadStations ()
		{
			items.Clear ();
			
			NPR.MyStations.ToList ().ForEach (s => {
				items.Add (new Station (s));				
			});
				
			Items = items;
			
			MaybeAddNullStation ();
		}
	}
}
