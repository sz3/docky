//  
//  Copyright (C) 2009 Jason Smith
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;

using Mono.Unix;

using Gdk;
using Gtk;

using Docky.DBus;
using Docky.Windowing;
using Docky.Services;

namespace Docky
{

	public static class Docky
	{

		public static UserArgs CommandLinePreferences { get; private set; }
		
		static DockController controller;
		internal static DockController Controller { 
			get {
				if (controller == null)
					controller = new DockController ();
				return controller;
			}
		}
		
		public static void Main (string[] args)
		{
			// output the version number & system info
			Log.DisplayLevel = LogLevel.Info;
			Log.Info ("Docky version: {0} {1}", AssemblyInfo.DisplayVersion, AssemblyInfo.VersionDetails);
			Log.Info ("Kernel version: {0}", System.Environment.OSVersion.Version);
			Log.Info ("CLR version: {0}", System.Environment.Version);
			
			//Init gtk and GLib related
			Catalog.Init ("docky", AssemblyInfo.LocaleDirectory);
			Gdk.Threads.Init ();
			NDesk.DBus.BusG.Init ();
			Gtk.Application.Init ("Docky", ref args);
			Gnome.Vfs.Vfs.Initialize ();
			GLib.GType.Init ();
			
			// process the command line args
			CommandLinePreferences = new UserArgs (args);
			
			Wnck.Global.ClientType = Wnck.ClientType.Pager;
			
			// set process name
			DockServices.System.SetProcessName ("docky");
			
			// check compositing
			CheckComposite ();
			Gdk.Screen.Default.CompositedChanged += delegate {
				CheckComposite ();
			};
			
			DBusManager.Default.Initialize ();
			PluginManager.Initialize ();
			Controller.Initialize ();
			
			Gdk.Threads.Enter ();
			Gtk.Application.Run ();
			Gdk.Threads.Leave ();
			
			Controller.Dispose ();
			DockServices.Dispose ();
			PluginManager.Shutdown ();
			Gnome.Vfs.Vfs.Shutdown ();
		}
		
		static void CheckComposite ()
		{
			GLib.Timeout.Add (2000, delegate {
				if (!Gdk.Screen.Default.IsComposited)
					Log.Notify (Catalog.GetString ("Docky requires compositing to work properly. " +
						"Please enable compositing and restart docky."));
				return false;
			});
		}
		
		public static void ShowAbout ()
		{
			Gtk.AboutDialog about = new Gtk.AboutDialog ();
			about.ProgramName = "Docky";
			about.Version = AssemblyInfo.DisplayVersion + "\n" + AssemblyInfo.VersionDetails;
			about.IconName = "docky";
			about.LogoIconName = "docky";
			about.Website = "http://www.go-docky.com/";
			about.WebsiteLabel = "Website";
			Gtk.AboutDialog.SetUrlHook ((dialog, link) => DockServices.System.Open (link));
			about.Copyright = "Copyright \xa9 2009-2010 Docky Developers";
			about.Comments = "Docky. Simply Powerful.";
			about.Authors = new[] {
				"Jason Smith <jason@go-docky.com>",
				"Robert Dyer <robert@go-docky.com>",
				"Chris Szikszoy <chris@go-docky.com>",
				"Rico Tzschichholz <rtz@go-docky.com>",
				"Seif Lofty <seif@lotfy.com>",
				"Chris Halse Rogers <raof@ubuntu.com>",
				"Alex Launi <alex.launi@gmail.com>"
			};
			about.Artists = new[] { 
				"Daniel Foré <bunny@go-docky.com>",
			};
			about.TranslatorCredits = 
				"Basque\n" +
				" Ibai Oihanguren (https://launchpad.net/~ibai-oihanguren)\n" +
				
				"Bengali\n" +
				" Scio (https://launchpad.net/~scio)\n" +

				"Brazilian Portuguese\n" +
				" André Gondim (https://launchpad.net/~andregondim)\n" +
				" Fabio S Monteiro (https://launchpad.net/~fabiomonteiro)\n" +
				" Glauco Vinicius (https://launchpad.net/~glaucovinicius)\n" +
				" Lindeval (https://launchpad.net/~lindeval)\n" +
				" Thiago Bellini (https://launchpad.net/~roxthiaguin)\n" +
				" Victor Mello (https://launchpad.net/~victormmello)\n" +

				"Catalan\n" +
				" BadChoice (https://launchpad.net/~guitarboy000)\n" +

				"Chinese (Simplified)\n" +
				" Chen Tao (https://launchpad.net/~pro711)\n" +
				" G.S.Alex (https://launchpad.net/~g.s.alex)\n" +
				" fighterlyt (https://launchpad.net/~fighter-lyt)\n" +
				" skatiger (https://launchpad.net/~skatiger)\n" +
				" 冯超 (https://launchpad.net/~rainofchaos)\n" +

				"Croatian\n" +
				" zekopeko (https://launchpad.net/~zekopeko)\n" +

				"English (United Kingdom)\n" +
				" Daniel Bell (https://launchpad.net/~danielbell)\n" +
				" Joel Auterson (https://launchpad.net/~joel-auterson)\n" +
				" SteVe Cook (https://launchpad.net/~yorvyk)\n" +

				"French\n" +
				" Hugo M. (https://launchpad.net/~spirit-power)\n" +
				" Kévin Gomez (https://launchpad.net/~geek63)\n" +
				" Pierre Slamich (https://launchpad.net/~pierre-slamich)\n" +
				" Simon Richard (https://launchpad.net/~saymonz)\n" +
				" alienworkshop (https://launchpad.net/~alienworkshop)\n" +
				" maxime Cheval (https://launchpad.net/~arkahys)\n" +

				"Galician\n" +
				" Indalecio Freiría Santos (https://launchpad.net/~ifreiria)\n" +

				"German\n" +
				" Mark Parigger (https://launchpad.net/~mark-climber)\n" +
				" Martin Lettner (https://launchpad.net/~m.lettner)\n" +
				" augias (https://launchpad.net/~augias)\n" +
				" fiction (https://launchpad.net/~moradin-web)\n" +
				" pheder (https://launchpad.net/~pheder)\n" +
				" tai (https://launchpad.net/~agent00tai)\n" +

				"Hebrew\n" +
				" IsraeliHawk (https://launchpad.net/~uri.shabtay)\n" +

				"Hindi\n" +
				" Bilal Akhtar (https://launchpad.net/~bilalakhtar96)\n" +

				"Hungarian\n" +
				" Bognár András (https://launchpad.net/~bognarandras)\n" +
				" Gabor Kelemen (https://launchpad.net/~kelemeng)\n" +
				" NewPlayer (https://launchpad.net/~newplayer)\n" +

				"Icelandic\n" +
				" Baldur (https://launchpad.net/~baldurpet)\n" +

				"Indonesian\n" +
				" Fakhrul Rijal (https://launchpad.net/~frijal)\n" +
					
				"Italian\n" +
				" Blaster (https://launchpad.net/~dottorblaster)\n" +
				" MastroPino (https://launchpad.net/~mastropino)\n" +
				" Quizzlo (https://launchpad.net/~marcopaolone)\n" +

				"Japanese\n" +
				" kawaji (https://launchpad.net/~jiro-kawada)\n" +

				"Korean\n" +
				" Cedna (https://launchpad.net/~cedna)\n" +

				"Polish\n" +
				" 313 (https://launchpad.net/~tenotoja)\n" +
				" Adrian Grzemski (https://launchpad.net/~adrian-grzemski)\n" +
				" EuGene (https://launchpad.net/~eugenewolfe)\n" +

				"Russian\n" +
				" Sergey Sedov (https://launchpad.net/~serg-sedov)\n" +

				"Spanish\n" +
				" Sebastián Porta (https://launchpad.net/~sebastianporta)\n" +

				"Swedish\n" +
				" Daniel Nylander (https://launchpad.net/~yeager)\n" +

				"Turkish\n" +
				" Yalçın Can (https://launchpad.net/~echza)\n" +

				"Ukrainian\n" +
				" naker.ua (https://launchpad.net/~naker-ua)\n";
			
			
			about.ShowAll ();
			
			about.Response += delegate {
				about.Hide ();
				about.Destroy ();
			};
			
		}
		
		public static void Quit ()
		{
			DBusManager.Default.Shutdown ();
			Gtk.Application.Quit ();
		}
	}
}
