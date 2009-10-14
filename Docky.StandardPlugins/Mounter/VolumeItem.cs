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
using System.Text.RegularExpressions;

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
			VfsVolume = volume;
			
			Icon = volume.Icon;
			
			if (StringIsUUID (volume.DisplayName))
				HoverText = string.Format ("{0} ({1})", volume.DeviceType.ToString (), volume.DevicePath);
			else
				HoverText = volume.DisplayName;
		}
		
		bool StringIsUUID (string uuid)
		{
			Regex regex = new Regex ("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}");
			
			return regex.IsMatch (uuid);
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
			VfsVolume.Unmount ( (s,e,d) => {} );
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem ("Open", Icon, (o, a) => OpenVolume ());
			string removeLabel = (VfsVolume.Drive.NeedsEject ()) ? "Eject" : "Unmount";
			yield return new MenuItem (removeLabel, "media-eject", (o, a) => UnMount ());
		}

	}
}
