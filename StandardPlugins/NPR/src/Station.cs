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
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

using Mono.Unix;

namespace NPR
{

	public class Station : IconDockItem
	{
		public string Name { get; private set; }
		public uint ID { get; private set; }
		public uint Signal { get; private set; }
		public string TagLine { get; private set; }
		public string Logo { get; private set; }
		XElement StationElement { get; set; }
		
		public Station (XElement stationElement)
		{
			State |= ItemState.Wait;
			
			StationElement = stationElement;
			Name = StationElement.Element ("name").Value;
			ID = uint.Parse (StationElement.Attribute ("id").Value);
			
			// when looking up a station by ID, there is no signal property
			if (StationElement.Elements ("signal").Any ())
				Signal = uint.Parse (StationElement.Element ("signal").Attribute ("strength").Value);
			else 
				Signal = 0;
			TagLine = StationElement.Element ("tagline").Value;
			
			WebClient cl = new WebClient ();
			string logo = StationElement.Elements ("image").First (el => el.Attribute ("type").Value == "logo").Value;
			string logoFile = System.IO.Path.GetTempFileName ();
			cl.DownloadFileAsync (new Uri (logo), logoFile);
			cl.DownloadFileCompleted += delegate {
				State ^= ItemState.Wait;
				Icon = logoFile;
			};
			
			string hover = (string.IsNullOrEmpty (TagLine)) ? Name : string.Format ("{0} : {1}", Name, TagLine);
			
			HoverText = hover;
		}
		
		public IEnumerable<StationUrl> StationUrls {
			get {
				return StationElement.Elements ("url").Select (u => new StationUrl (u));
			}
		}
		
		#region IconDockItem
		
		public override string UniqueID ()
		{
			return Name + TagLine;
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				DockServices.System.Open (StationUrls.First (u => u.UrlType == StationUrlType.OrgHomePage).Target);
				
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
						
			List<StationUrl> urls = StationUrls.ToList ();
			
			if (urls.Any (u => u.UrlType == StationUrlType.OrgHomePage)) {
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Home Page"),
						Gtk.Stock.Home,
						delegate {
							Clicked (1, Gdk.ModifierType.None, 0, 0);
						}));
			}
			if (urls.Any (u => u.UrlType == StationUrlType.ProgramSchedule)) {
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Program Schedule"),
						"gnome-calendar",
						delegate {
							DockServices.System.Open (urls.First (u => u.UrlType == StationUrlType.ProgramSchedule).Target);
						}));
			}
			
			list[MenuListContainer.Actions].Add (new SeparatorMenuItem ());
			
			urls.Where (u => u.UrlType >= StationUrlType.AudioMP3Stream).ToList ().ForEach (url => {
				string format = "", icon = "";
				switch (url.UrlType) {
				case StationUrlType.AudioMP3Stream:
					format = "MP3";
					icon = "audio-x-mpeg";
					break;
				case StationUrlType.AudioRAMStream:
					format = "Real Audio";
					icon = "audio-x-generic";
					break;
				case StationUrlType.AudioWMAStream:
					format = "Windows Media";
					icon = "audio-x-ms-wma";
					break;
				}
				
				list[MenuListContainer.Actions].Add (new MenuItem (string.Format ("{0} ({1})", url.Title, format),
					icon,
					delegate {
						DockServices.System.RunOnThread (() => {
							WebClient cl = new WebClient ();
							string tempFile = System.IO.Path.GetTempFileName ();
							cl.DownloadFile (url.Target, tempFile);
							GLib.File f = FileFactory.NewForPath (tempFile);
							DockServices.System.Open (f);
						});
					}));
			});
			
			list[MenuListContainer.Actions].Add (new SeparatorMenuItem ());
			
			if (urls.Any (u => u.UrlType == StationUrlType.PledgePage)) {
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Donate"),
						"emblem-money",
						delegate {
							DockServices.System.Open (urls.First (u => u.UrlType == StationUrlType.PledgePage).Target);
						}));
			}
			
			/*
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Donate"),
					"emblem-money",
					delegate {
						if (GMailConfigurationDialog.instance == null)
							GMailConfigurationDialog.instance = new GMailConfigurationDialog ();
						GMailConfigurationDialog.instance.Show ();
					}));
			*/
			return list;
		}

		#endregion
		
	}
}
