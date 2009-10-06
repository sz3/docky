
using System;
using System.Collections.Generic;

using Gnome.Vfs;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace RemovableDevices
{
	
	
	public class VolumeItem : IconDockItem
	{
		
		#region IconDockItem implementation
		
		public override string UniqueID ()
		{
			return VfsVolume.ActivationUri;
		}
		
		#endregion
		
		public VolumeItem (Volume volume)
		{
			this.VfsVolume = volume;
			
			this.Icon = volume.Icon;
			
			this.HoverText = volume.DisplayName;
		}
		
		public Volume VfsVolume { get; private set; }
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				OpenVolume ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		void OpenVolume ()
		{
			DockServices.System.Open (VfsVolume.ActivationUri);
		}
		
		void UnMount ()
		{
			this.VfsVolume.Unmount ( (s,e,d) => {} );
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem ("Open", this.Icon, (o, a) => OpenVolume ());
			yield return new MenuItem ("Unmount", "gtk-delete", (o, a) => UnMount ());
		}

	}
}
