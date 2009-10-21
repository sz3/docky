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
		
		public MountProvider ()
		{
			GLib.GType.Init ();

			Mounts = new List<MountItem> ();
			
			Monitor = VolumeMonitor.Default;

			foreach (Mount m in Monitor.Mounts) {
				if (IsTrash (m))
					continue;
				Mounts.Add (new MountItem (m));
				Log<MountProvider>.Debug ("Adding {0}.", m.Name);
			}
			
			Monitor.MountAdded += HandleMountAdded;
			Monitor.MountRemoved += HandleMountRemoved;
			
		}
		
		void HandleMountAdded (object o, MountAddedArgs args)
		{
			// FIXME: due to a bug in GIO#, this will crash when trying to get args.Mount
			Mount m = MountAdapter.GetObject (args.Args[0] as GLib.Object);
			
			if (IsTrash (m))
				return;
			
			MountItem newMnt = new MountItem (m);
			Mounts.Add (newMnt);
			OnItemsChanged ((newMnt as AbstractDockItem).AsSingle (), null);
			Log<MountProvider>.Info ("{0} mounted.", m.Name);
		}		
		
		void HandleMountRemoved (object o, MountRemovedArgs args)
		{
			// FIXME: due to a bug in GIO#, this will crash when trying to get args.Mount
			Mount m = MountAdapter.GetObject (args.Args[0] as GLib.Object);
			
			if (Mounts.Any (d => d.Mnt.Handle == m.Handle)) {
				MountItem mntToRemove = Mounts.First (d => d.Mnt.Handle == m.Handle);
				Mounts.Remove (mntToRemove);
				OnItemsChanged (null, (mntToRemove as AbstractDockItem).AsSingle ());
				mntToRemove.Dispose ();
				Log<MountProvider>.Info ("{0} unmounted.", m.Name);
			}
		}
		
		// determine if the mount should be handled or not
		bool IsTrash (Mount m)
		{
			return m.Volume == null;
		}
		
		public override bool ItemCanBeRemoved (AbstractDockItem item)
		{
			if (item is MountItem)
				return true;
			return false;
		}
		
		public override bool RemoveItem (AbstractDockItem item)
		{
			Mounts.Remove (item as MountItem);
			
			OnItemsChanged (null, item.AsSingle ());
			
			(item as MountItem).UnMount ();
			
			return false;
		}
	}
}
