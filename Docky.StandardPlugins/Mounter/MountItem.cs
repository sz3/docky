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

using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Mounter
{
	
	public class MountItem : IconDockItem
	{
		
		#region IconDockItem implementation
		
		public override string UniqueID ()
		{
			return Mnt.Handle.ToString ();
		}
		
		#endregion
		
		public MountItem (Mount mount)
		{
			Mnt = mount;
			
			SetIconFromGIcon (mount.Icon);
			
			HoverText = Mnt.Name;
		}
		
		public Mount Mnt { get; private set; }
		
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
			DockServices.System.Open (Mnt.Root.ToString ());
		}
		
		public void UnMount ()
		{
			Log<MountItem>.Debug ("Trying to unmount {0}.", Mnt.Name);
			if (Mnt.CanEject ())
				Mnt.Eject (MountUnmountFlags.Force, null, new AsyncReadyCallback (HandleMountFinished));
			else
				Mnt.Unmount (MountUnmountFlags.Force, null, new AsyncReadyCallback (HandleMountFinished));
		}
		
		void HandleMountFinished (GLib.Object sender, AsyncResult result)
		{
			string success = "successful";
			if (!Mnt.UnmountFinish (result))
				success = "failed";
			
			Log<MountItem>.Debug ("Unmount of {0} {1}.", Mnt.Name, success);
			    
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem ("Open", Icon, (o, a) => OpenVolume ());
			if (Mnt.CanEject () || Mnt.CanUnmount) {
				string removeLabel = (Mnt.CanEject ()) ? "Eject" : "Unmount";
				yield return new MenuItem (removeLabel, "media-eject", (o, a) => UnMount ());
			}
		}

	}
}
