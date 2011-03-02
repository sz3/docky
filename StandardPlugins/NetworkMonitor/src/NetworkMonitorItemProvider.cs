//  
//  Copyright (C) 2011 Florian Dorn
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

using Docky.Items;

namespace NetworkMonitorDocklet
{
	public class NetworkMonitorItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "NetworkMonitor";
			}
		}
		
		#endregion

		NetworkMonitorDockItem monitor;
		
		public NetworkMonitorItemProvider ()
		{
			monitor = new NetworkMonitorDockItem ();
			Items = monitor.AsSingle<AbstractDockItem> ();
		}
		
		public override void Dispose ()
		{
			monitor.Dispose ();
			
			base.Dispose ();
		}
	}
}
