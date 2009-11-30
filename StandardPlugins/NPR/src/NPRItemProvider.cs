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
		
		public override string Icon { get { return "trashcan_full"; } }
		
		public override void Dispose ()
		{
			foreach (AbstractDockItem adi in items)
				adi.Dispose ();
		}
		
		#endregion
		
		List<AbstractDockItem> items = new List<AbstractDockItem> ();
		
		public NPRItemProvider ()
		{
			ReloadStations ();
			NPR.StationsUpdated += delegate {
				ReloadStations ();
			};
		}
				
		public void ReloadStations ()
		{
			items.Clear ();
			foreach (int stationID in NPR.MyStations)
			{
				items.Add (new Station (stationID));
				Items = items;
			}
			
			if (NPR.MyStations.Count () == 0) {
				items.Add (new NullStation ());
				Items = items;
			}
		}
	}
}
