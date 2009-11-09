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

using GLib;

namespace Docky.Services.GUI
{

	public partial class MountOperationDialog : Gtk.Dialog
	{
		
		private AskPasswordFlags AskFlags;
		private PasswordWidget passwordWidget;
		private AnonymousWidget anonWidget;
		private UsernameWidget userWidget;
		private PasswordSaveWidget passwordSaveWidget;
		public string Password {
			get { return passwordWidget.Password; }
		}

		public MountOperationDialog (string message, string defaultUser, AskPasswordFlags flags)
		{
			this.Build ();
			
			TitleLabel.Text = message;
			
			AskFlags = flags;
			
			passwordWidget = new PasswordWidget ();
			userWidget = new UsernameWidget ();
			
			if ((flags & AskPasswordFlags.AnonymousSupported) == AskPasswordFlags.AnonymousSupported) {
				anonWidget = new AnonymousWidget ();
				anonWidget.AnonRadio.Clicked += delegate {
					passwordWidget.PassEntry.Sensitive = false;
					userWidget.UserName.Sensitive = false;
				};
				anonWidget.UserRadio.Clicked += delegate {
					passwordWidget.PassEntry.Sensitive = true;
					userWidget.UserName.Sensitive = true;
				};
				anonWidget.AnonRadio.Activate ();
				ItemsVBox.PackStart (anonWidget);
			}
			
			if ((flags & AskPasswordFlags.NeedUsername) == AskPasswordFlags.NeedUsername) {
				userWidget = new UsernameWidget ();
				userWidget.UserName.Text = defaultUser;
				ItemsVBox.PackStart (userWidget);
			}
			
			ItemsVBox.PackStart (passwordWidget);
			
			if ((flags & AskPasswordFlags.SavingSupported) == AskPasswordFlags.SavingSupported) {
				passwordSaveWidget = new PasswordSaveWidget ();
				ItemsVBox.PackStart (passwordSaveWidget);
			}
			
			ShowAll ();
		}
	}
}
