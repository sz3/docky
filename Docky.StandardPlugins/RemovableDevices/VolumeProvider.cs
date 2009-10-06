
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
		
		public VolumeProvider ()
		{
			
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
