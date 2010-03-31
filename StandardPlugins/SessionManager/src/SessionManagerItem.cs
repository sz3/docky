//  
//  Copyright (C) 2010 Claudio Melis, Rico Tzschichholz
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
using System.Diagnostics;

using NDesk.DBus;
using org.freedesktop.DBus;

using Gdk;
using GLib;

using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace SessionManager
{
	class SessionManagerEntry
	{
		public string icon, hover_text;
		public Action action;

		public SessionManagerEntry (string icon, string hover_text, Action action)
		{
			this.icon = icon;
			this.hover_text = hover_text;
			this.action = action;
		}
	}

	public class SessionManagerItem : IconDockItem
	{
		int current_index = 0;

		SystemManager system_manager = SystemManager.GetInstance ();

		List<SessionManagerEntry> SessionMenuItems;
		List<SessionManagerEntry> SessionDockItems;
		
		SessionManagerEntry lockscreen;
		SessionManagerEntry logout;
		SessionManagerEntry suspend;
		SessionManagerEntry hibernate;
		SessionManagerEntry restart;
		SessionManagerEntry shutdown;

		public SessionManagerItem ()
		{
			BuildMenuEntries ();
			GenerateSessionItems ();
			
			system_manager.CapabilitiesChanged += HandlePowermanagerCapabilitiesChanged;
			system_manager.RebootRequired += HandleRebootRequired;
			
			HoverText = SessionDockItems[current_index].hover_text;
			Icon = SessionDockItems[current_index].icon;
		}

		void HandleRebootRequired (object sender, EventArgs e)
		{
			if (system_manager.CanRestart ()) {
				HoverText = restart.hover_text;
				Icon = restart.icon;
				QueueRedraw ();
				State &= ~ItemState.Urgent;
				State |= ItemState.Urgent;
			}
		}
		
		void HandlePowermanagerCapabilitiesChanged (object sender, EventArgs e)
		{
			GenerateSessionItems ();
			
			HoverText = SessionDockItems[current_index].hover_text;
			Icon = SessionDockItems[current_index].icon;

			QueueRedraw ();
		}

		void GenerateSessionItems () 
		{
			SessionMenuItems = new List<SessionManagerEntry> ();
			SessionDockItems = new List<SessionManagerEntry> ();
			
			current_index = 0;
			
			SessionMenuItems.Add (lockscreen);
			SessionDockItems.Add (lockscreen);

			SessionMenuItems.Add (logout);
			SessionDockItems.Add (logout);
			
			if (system_manager.CanSuspend ()) {
				SessionMenuItems.Add (suspend);
			}
			
			if (system_manager.CanHibernate ()) {
				SessionMenuItems.Add (hibernate);
			}
			
			if (system_manager.CanRestart ()) {
				SessionMenuItems.Add (restart);
				SessionDockItems.Add (restart);
			}
			
			if (system_manager.CanStop ()) {
				SessionMenuItems.Add (shutdown);
				SessionDockItems.Add (shutdown);
			}
		}
		
		void BuildMenuEntries () 
		{
			lockscreen = new SessionManagerEntry ("system-lock-screen", Catalog.GetString ("Lock Screen"), () => { 
				system_manager.LockScreen ();
			});
			
			logout = new SessionManagerEntry ("system-log-out", Catalog.GetString ("Log Out..."), () => { 
				ShowConfirmationDialog (Catalog.GetString ("Log Out"), 
				                        Catalog.GetString ("Are you sure you want to close all programs and log out of the computer?"), 
				                        "system-log-out", 
				                        system_manager.LogOut);
			});

			suspend = new SessionManagerEntry ("system-suspend", Catalog.GetString ("Suspend"), () => { 
				system_manager.LockScreen ();
				system_manager.Suspend (); 
			});
			
			hibernate = new SessionManagerEntry ("system-hibernate", Catalog.GetString ("Hibernate"), () => { 
				system_manager.LockScreen ();
				system_manager.Hibernate (); 
			});
			

			restart = new SessionManagerEntry ("system-restart", Catalog.GetString ("Restart..."), () => { 
				ShowConfirmationDialog (Catalog.GetString ("Restart"), 
				                        Catalog.GetString ("Are you sure you want to close all programs and restart the computer?"), 
				                        "system-restart", 
				                        () => system_manager.Restart ());
			});
			
			shutdown = new SessionManagerEntry ("system-shutdown", Catalog.GetString ("Shut Down..."), () => { 
				ShowConfirmationDialog (Catalog.GetString ("Shut Down"), 
				                        Catalog.GetString ("Are you sure you want to close all programs and shut down the computer?"), 
				                        "system-shutdown", 
				                        () => system_manager.Stop ());
			});
		}

		void ShowConfirmationDialog (string title, string text, string icon_name, Action action)
		{
				Gtk.MessageDialog md = new Gtk.MessageDialog (null, 0, Gtk.MessageType.Question, Gtk.ButtonsType.None, text);
				
				md.Title = title;
				md.Image = Gtk.Image.NewFromIconName (icon_name, Gtk.IconSize.Dialog);
				md.Image.Visible = true;
				md.Image.Show ();
				
				md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
				md.AddButton (title, Gtk.ResponseType.Ok);
				md.DefaultResponse = Gtk.ResponseType.Ok;
				
				md.Response += (o, args) => { 
					if (args.ResponseId == Gtk.ResponseType.Ok)
						action.Invoke ();
					md.Destroy ();
				};
				
				md.Show ();
		}
		
		public override string UniqueID ()
		{
			return "SessionManager";
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				SessionDockItems[current_index].action.Invoke ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}

		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = new MenuList ();
			
			foreach (SessionManagerEntry item in SessionMenuItems) {
				SessionManagerEntry entry = item;
				list[MenuListContainer.Actions].Add (new MenuItem (entry.hover_text, entry.icon, (o, a) => entry.action ()));
			}
			
			return list;
		}

		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (direction == Gdk.ScrollDirection.Up || direction == Gdk.ScrollDirection.Left) {
				if (current_index == 0)
					current_index = SessionDockItems.Count;
				current_index = (current_index - 1) % SessionDockItems.Count;
			} else {
				current_index = (current_index + 1) % SessionDockItems.Count;
			}
			
			HoverText = SessionDockItems[current_index].hover_text;
			Icon = SessionDockItems[current_index].icon;
			
			QueueRedraw ();
		}

		#region IDisposable implementation
		public override void Dispose ()
		{
			system_manager.CapabilitiesChanged -= HandlePowermanagerCapabilitiesChanged;
			system_manager.RebootRequired -= HandleRebootRequired;
			
			base.Dispose ();
		}
		
		#endregion
	}
}
