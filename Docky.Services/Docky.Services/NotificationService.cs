/* NotificationHelper.cs
 *
 * GNOME Do is the legal property of its developers. Please refer to the
 * COPYRIGHT file distributed with this source distribution.
 *  
 * This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Gdk;
using LibNotify = Notifications;
	
namespace Docky.Services
{	
	public enum NotificationCapability {
		actions,
		append,
		body,
		body_hyperlinks,
		body_images,
		body_markup,
		icon_multi,
		icon_static,
		image_svg,
		max,
		positioning, // not an official capability
		scaling, // not an official capability
		sound
	}
	
	public class NotificationService
	{
		const string DefaultIconName = "docky";
		
		const int IconSize = 48;
		const int LettersPerWord = 7;
		const int MillisecondsPerWord = 350;
		const int MinNotifyShow = 5000;
		const int MaxNotifyShow = 10000;

		Pixbuf DefaultIcon { get; set; }
		
		public NotificationService ()
		{
			DefaultIcon = DockServices.Drawing.LoadIcon (DefaultIconName, IconSize);
		}

		static int ReadableDurationForMessage (string title, string message)
		{
			int t = (title.Length + message.Length) / LettersPerWord * MillisecondsPerWord;	
			return Math.Min (Math.Max (t, MinNotifyShow), MaxNotifyShow);
		}

		public void Notify (string title, string message, string icon)
		{
			Notify (title, message, icon, Screen.Default, 0, 0);
		}
		
		public void Notify (string title, string message, string icon, Screen screen, int x, int y)
		{
			LibNotify.Notification notify = ToNotify (title, message, icon);
			notify.SetGeometryHints (screen, x, y);
			notify.Show ();
		}
		
		public bool SupportsCapability (NotificationCapability capability)
		{
			// positioning and scaling are not actual capabilities, i just know for a fact most other servers
			// support geo. hints, and notify-osd is the only that auto scales images
			if (capability == NotificationCapability.positioning)
				return LibNotify.Global.ServerInformation.Name != "notify-osd";
			else if (capability == NotificationCapability.scaling)
				return LibNotify.Global.ServerInformation.Name == "notify-osd";
			
			return Array.IndexOf (LibNotify.Global.Capabilities, Enum.GetName (typeof (NotificationCapability), capability)) > -1;
		}

		LibNotify.Notification ToNotify (string title, string message, string icon)
		{
			LibNotify.Notification notify = new LibNotify.Notification ();
			notify.Body = GLib.Markup.EscapeText (message);
			notify.Summary = GLib.Markup.EscapeText (title);
			notify.Timeout = ReadableDurationForMessage (title, message);
			
			if (SupportsCapability (NotificationCapability.scaling) && !icon.Contains ("@")) {
				notify.IconName = string.IsNullOrEmpty (icon)
					? DefaultIconName
					: icon;
			} else {
				notify.Icon = string.IsNullOrEmpty (icon)
					? DefaultIcon
					: DockServices.Drawing.LoadIcon (icon, IconSize);
			}

			return notify;
		}
	}
}
	
