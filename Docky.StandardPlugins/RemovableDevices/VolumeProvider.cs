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

using Gnome.Vfs;

using Docky.Items;

namespace RemovableDevices
{
	
	public class VolumeProvider : AbstractDockItemProvider
	{
		
		#region AbstractDockItemProvider implementation
		
		public override string Name {
			get {
				return "Removable Devices";
			}
		}
		
		public override IEnumerable<AbstractDockItem> Items {
			get {
				yield return Computer;
				foreach (VolumeItem item in Volumes)
					yield return item;
			}
		}
		
		public override void Dispose ()
		{
			foreach (VolumeItem v in Volumes)
				OnItemsChanged (null, (v as AbstractDockItem).AsSingle ());
			
			Vfs.Shutdown ();
		}
		
		#endregion
		
		List<VolumeItem> Volumes;
		public VolumeMonitor Monitor { get; private set; }
		ComputerItem Computer;
		
		public VolumeProvider ()
		{
			Computer = new ComputerItem ();
			
			// initialize VFS in case it isn't already
			if (!Vfs.Initialized)
				Vfs.Initialize ();
			
			Volumes = new List<VolumeItem> ();
			
			Monitor = VolumeMonitor.Get ();

			foreach (Volume v in Monitor.MountedVolumes) {
				if (v.IsUserVisible) {
					// Console.WriteLine ("adding {0}", v.DisplayName);
					Volumes.Add ( new VolumeItem (v));
				}
			}
			
			Monitor.VolumeMounted += HandleVolumeMounted;;
			Monitor.VolumeUnmounted += HandleVolumeUnmounted;; 
		}

		void HandleVolumeUnmounted(object o, VolumeUnmountedArgs args)
		{
			if (Volumes.Any ( d => d.VfsVolume == args.Volume)) {
				// Console.WriteLine ("Removing {0}", args.Volume.DisplayName);
				VolumeItem volToRemove = Volumes.First ( d => d.VfsVolume == args.Volume);
				Volumes.Remove (volToRemove);
				OnItemsChanged (null, (volToRemove as AbstractDockItem).AsSingle ());
				volToRemove.Dispose ();
			}
		}

		void HandleVolumeMounted(object o, VolumeMountedArgs args)
		{
			// Console.WriteLine ("Adding {0}", args.Volume.DisplayName);
			VolumeItem newVol = new VolumeItem (args.Volume);
			Volumes.Add (newVol);
			OnItemsChanged ((newVol as AbstractDockItem).AsSingle (), null);
		}
	}
}
