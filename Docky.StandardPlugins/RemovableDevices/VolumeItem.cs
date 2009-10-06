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
			string removeLabel = (VfsVolume.Drive.NeedsEject ()) ? "Eject" : "Unmount";
			yield return new MenuItem (removeLabel, "media-eject", (o, a) => UnMount ());
		}

	}
}
