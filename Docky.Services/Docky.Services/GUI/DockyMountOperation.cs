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

	public class DockyMountOperation : MountOperation
	{
		
		MountOperationDialog dlg;

		public DockyMountOperation () : base()
		{
			ExceptionManager.UnhandledException += HandleExceptionManagerUnhandledException;
		}

		void HandleExceptionManagerUnhandledException (UnhandledExceptionArgs args)
		{
			Console.WriteLine ("Some baaaaaad shit happened.... {0}", (args.ExceptionObject as GException).Message);
		}
		
		protected override void OnAskPassword (string message, string default_user, string default_domain, AskPasswordFlags flags)
		{
			dlg = new MountOperationDialog (message, default_user, flags);
			int retVal = dlg.Run ();
			Console.WriteLine (retVal);
			if (retVal == -5) {
				this.Password = dlg.Password;
				this.PasswordSave = PasswordSave.Never;
				EmitReply (MountOperationResult.Handled);
			}
			
			dlg.Hide ();
			base.OnAskPassword (message, default_user, default_domain, flags);
			Console.WriteLine ("done with OnAskPassword");
		}
		
		protected override void OnReply (MountOperationResult result)
		{
			Console.WriteLine (result);
		}
	}
}
