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
		public int ID { get; private set; }
		public uint Signal { get; private set; }
		public string TagLine { get; private set; }
		public string Logo { get; private set; }
		public string MarketCity { get; private set; }
		public IEnumerable<StationUrl> StationUrls { get; private set; }
		
		public event EventHandler FinishedLoading;
		
		private string DefaultLogo { get; set; }
		private string LogoFile { get; set; }
		private bool IsSetUp { get; set; }
		private bool IsReady {
			get {
				return ((State & ItemState.Wait) != ItemState.Wait) && IsSetUp;
			}
		}
		
		public Station (int id)
		{
			State |= ItemState.Wait;
			Icon = DefaultLogo = "nprlogo.gif@" + GetType ().Assembly.FullName;
			
			if (id > 0) {
				HoverText = Catalog.GetString ("Fetching information...");
				DockServices.System.RunOnThread (() => {
					LoadDataFromXElement (NPR.StationXElement (id));
				});
				// this is how we create our "null" station entry
			} else {
				ID = id;
				HoverText = Catalog.GetString ("Click to add NPR stations");
				State ^= ItemState.Wait;
			}
		}
		
		public Station (XElement stationElement)
		{
			State |= ItemState.Wait;
			
			LoadDataFromXElement (stationElement);
		}
		
		void LoadDataFromXElement (XElement stationElement)
		{
			IsSetUp = false;
			
			Name = stationElement.Element ("name").Value;
			ID = int.Parse (stationElement.Attribute ("id").Value);
			
			Signal = 0;
			// when looking up a station by ID, there is no signal property
			if (stationElement.Elements ("signal").Any ())
				Signal = uint.Parse (stationElement.Element ("signal").Attribute ("strength").Value);
			
			MarketCity = "";
			// or MarketCity property
			if (stationElement.Elements ("marketCity").Any ())
				MarketCity = stationElement.Element ("marketCity").Value;
			
			TagLine = stationElement.Element ("tagline").Value;
			
			StationUrls = stationElement.Elements ("url").Select (u => new StationUrl (u));
			
			WebClient cl = new WebClient ();
			string logo = stationElement.Elements ("image").First (el => el.Attribute ("type").Value == "logo").Value;
			
			LogoFile = System.IO.Path.Combine (System.IO.Path.GetTempPath (), Name.GetHashCode ().ToString ());
			if (System.IO.File.Exists (LogoFile)) {
				SetFinish ();
			} else {
				cl.DownloadFileAsync (new Uri (logo), LogoFile);
				cl.DownloadDataCompleted += delegate {
					DockServices.System.RunOnMainThread (SetFinish);
				};
			}
		}
		
		#region IconDockItem
		
		void SetFinish ()
		{
			State ^= ItemState.Wait;
			IsSetUp = true;
			
			// try loading the file, if this fails, then we use the backup.
			try {
				Gdk.Pixbuf pbuf = new Gdk.Pixbuf (LogoFile);
				pbuf.Dispose ();
				// if we get to this point, the logofile will load just fine
				Icon = LogoFile;
			} catch (Exception e) {
				// delete the bad logofile
				System.IO.File.Delete (LogoFile);
				Icon = DefaultLogo;
			}
			
			string hover = (string.IsNullOrEmpty (TagLine)) ? Name : string.Format ("{0} : {1}", Name, TagLine);
			HoverText = hover;
			
			if (FinishedLoading != null)
				FinishedLoading (this, EventArgs.Empty);
		}
		
		public override string UniqueID ()
		{
			return Name + TagLine;
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{			
			if (button == 1) {
				if (IsReady)
					DockServices.System.Open (StationUrls.First (u => u.UrlType == StationUrlType.OrgHomePage).Target);
				else
					ShowConfig ();
				
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}

		void ShowConfig ()
		{
			if (ConfigDialog.instance == null)
				ConfigDialog.instance = new ConfigDialog ();
			ConfigDialog.instance.Show ();
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			if (!IsReady)
				return list;
			
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
			if (urls.Any (u => u.UrlType == StationUrlType.PledgePage)) {
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Donate"),
						"emblem-money",
						delegate {
							DockServices.System.Open (urls.First (u => u.UrlType == StationUrlType.PledgePage).Target);
						}));
			}

			if (list.Count () > 0)
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
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Settings"),
					Gtk.Stock.Preferences,
					delegate {
						ShowConfig ();
					}));
			return list;
		}

		#endregion
		
	}
}
