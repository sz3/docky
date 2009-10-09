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

using GLib;

using Docky.Items;
using Docky.Services;

namespace Mounter
{
	
	public class MountProvider : AbstractDockItemProvider
	{
		
		#region AbstractDockItemProvider implementation
		
		public override string Name {
			get {
				return "Mount";
			}
		}
		
		public override IEnumerable<AbstractDockItem> Items {
			get {
				//yield return Computer;
				foreach (MountItem item in Mounts)
					yield return item;
			}
		}
		
		public override void Dispose ()
		{
			foreach (Mount m in Mounts)
				OnItemsChanged (null, (m as AbstractDockItem).AsSingle ());
		}
		
		#endregion
		
		List<MountItem> Mounts;
		public VolumeMonitor Monitor { get; private set; }
		ComputerItem Computer;
		
		public MountProvider ()
		{
			//Computer = new ComputerItem ();
			
			GLib.GType.Init ();

			Mounts = new List<MountItem> ();
			
			Monitor = VolumeMonitor.Default;

			foreach (Mount m in Monitor.Mounts) {
				Mounts.Add ( new MountItem (m));
				Log<MountProvider>.Debug ("Adding {0}.", m.Name);
			}
			
			Monitor.MountAdded += HandleMountAdded;
			Monitor.MountRemoved += HandleMountRemoved;
			
		}

		void HandleMountAdded (object o, MountAddedArgs args)
		{
			Console.WriteLine ("Mount added..");
			//FIXME: due to a bug in GIO#, this will crash when trying to get args.Mount
			//Mount m = args.Mount;
			Mount m = NewOrRemovedMount;
			
			MountItem newMnt = new MountItem (m);
			Mounts.Add (newMnt);
			OnItemsChanged ((newMnt as AbstractDockItem).AsSingle (), null);
			Log<MountProvider>.Info ("{0} mounted.", m.Name);
		}		
		
		void HandleMountRemoved (object o, MountRemovedArgs args)
		{
			Console.WriteLine ("Mount removed..");
			//Mount m = args.Mount;
			Mount m = NewOrRemovedMount;
			
			if (Mounts.Any (d => d.UniqueID () == m.Uuid)) {
				MountItem mntToRemove = Mounts.First (d => d.UniqueID () == m.Uuid);
				Mounts.Remove (mntToRemove);
				OnItemsChanged (null, (mntToRemove as AbstractDockItem).AsSingle ());
				mntToRemove.Dispose ();
				Log<MountProvider>.Info ("{0} unmounted.", m.Name);
			}
		}
		
		// A hack of a workaround because GIO# currently fails on args.Mount for Mount*Args
		// trust me, I know it looks asinine to compare the UUIDs, but because GLib.Mount
		// *DOESN'T* inherit from System.Object (and therefore, no .Equals () or .GetHashCode ()),
		// I can't use List<Mount> .Except (List<Mount>)
		Mount NewOrRemovedMount {
			get {
				List<string> oldMounts = new List<string> ();
				List<string> currentMounts = new List<string> ();
				
				Mounts.ForEach ( m => oldMounts.Add (m.Mnt.Uuid));
				foreach (Mount m in Monitor.Mounts)
					currentMounts.Add (m.Uuid);
				
				Console.WriteLine ("current mounts: {0}", currentMounts.Count ());
				foreach (string s in currentMounts)
					Console.WriteLine (s);
				Console.WriteLine ("old mounts: {0}", oldMounts.Count ());
				foreach (string s in oldMounts)
					Console.WriteLine (s);
				
				IEnumerable<string> difference = new List<string> ();

				Mount ret;
				
				if (currentMounts.Count () > oldMounts.Count ()) {
					difference = currentMounts.Except (oldMounts);
					ret = Monitor.Mounts.First (m => m.Uuid == difference.First ());
				}
				else {
					difference = oldMounts.Except (currentMounts);
					ret = Mounts.First (m => m.Mnt.Uuid == difference.First ()).Mnt;
				}
				
				Console.WriteLine ("difference: {0}", difference.Count ());
				foreach (string s in difference)
					Console.WriteLine (s);
				
				return ret;
			}
		}
	}
}
