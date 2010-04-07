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
using Diag = System.Diagnostics;

using GLib;

namespace Docky.Services
{
	
	public class Helper
	{
		public File File { get; private set; }
		public HelperMetadata Data { get; private set; }
		public bool IsUser { get; private set; }
		
		uint alive_timer = 0;
		
		bool? is_running;
		public bool IsRunning {
			get {
				if (!is_running.HasValue)
					is_running = false;
				return is_running.Value;
			}
			set {
				if (is_running.HasValue && is_running.Value == value)
					return;
				is_running = value;
				OnHelperStatusChanged ();
			}
		}
		public bool Enabled {
			get {
				return prefs.Get<bool> (prefs.SanitizeKey (File.Basename), false);
			}
			set {
				if (Enabled == value)
					return;
				
				if (value)
					Start ();
				else
					Stop ();
				
				prefs.Set<bool> (prefs.SanitizeKey (File.Basename), value);
				OnHelperStatusChanged ();
			}
		}

		static IPreferences prefs;
		Diag.Process Proc { get; set; }
		uint X_PERM = Convert.ToUInt32 ("1001001", 2);
		
		// interested parties should not listen for this event
		// instead use the ScriptStatusChanged event from ScriptService
		public event EventHandler<HelperStatusChangedEventArgs> HelperStatusChanged;
		
		public Helper (File file)
		{
			prefs = DockServices.Preferences.Get<HelperService> ();
			this.File = file;
			this.IsUser = file.Path.StartsWith ("/home/");
			
			GLib.File DataFile;
			if (IsUser)
				DataFile = HelperService.UserMetaDir;
			else
				DataFile = HelperService.SysMetaDir;
			
			DataFile = DataFile.GetChild (File.Basename + ".info");
			
			if (DataFile.Exists)
				Data = new HelperMetadata (DataFile);
			
			if (Enabled)
				Start ();
		}
		
		void OnHelperStatusChanged ()
		{
			if (HelperStatusChanged != null)
				HelperStatusChanged (this, new HelperStatusChangedEventArgs (File, Enabled, IsRunning));
		}
		
		void Start ()
		{	
			// if the execute bits aren't set, try to set
			if (!File.QueryInfo<bool> ("access::can-execute")) {
				Log<Helper>.Debug ("Execute permissions are not currently set for '{0}', attempting to set them.", File.Path);
				uint currentPerm = File.QueryInfo<uint> ("unix::mode");
				try {
					File.SetAttributeUint32 ("unix::mode", currentPerm | X_PERM, 0, null);
				// if we can't log the error, and disable this script
				} catch (Exception e) {
					Log<Helper>.Error ("Failed to set execute permissions for '{0}': {1}", File.Path, e.Message);
					Enabled = false;
					return;
				}
			}
			
			Log<Helper>.Info ("Starting {0}", File.Basename);
			if (Proc == null) {
				Proc = new Diag.Process ();
				Proc.StartInfo.FileName = File.Path;
				Proc.StartInfo.UseShellExecute = false;
				Proc.StartInfo.RedirectStandardError = true;
				Proc.StartInfo.RedirectStandardOutput = true;
				Proc.EnableRaisingEvents = true;
				Proc.ErrorDataReceived += delegate(object sender, Diag.DataReceivedEventArgs e) {
					if (DockServices.Helpers.ShowOutput && !string.IsNullOrEmpty (e.Data))
						Log<Helper>.Error ("{0} :: {1}", File.Basename, e.Data);
				};
				Proc.OutputDataReceived += delegate(object sender, Diag.DataReceivedEventArgs e) {
					if (DockServices.Helpers.ShowOutput && !string.IsNullOrEmpty (e.Data))
						Log<Helper>.Info ("{0} :: {1}", File.Basename, e.Data);
				};
				Proc.Exited += delegate {
					IsRunning = false;
					Log<Helper>.Info ("{0} has exited (Code {1}).", File.Basename, Proc.ExitCode);
				};
			}

			Proc.Start ();
			Proc.BeginOutputReadLine ();
			Proc.BeginOutputReadLine ();
			IsRunning = true;
			
			// check if the process is alive every 10 seconds.  I can't figure out a better way to do this...
			if (alive_timer > 0)
				GLib.Source.Remove (alive_timer);
			alive_timer = GLib.Timeout.Add (1000*10, delegate {
				if (!FileFactory.NewForPath ("/proc").GetChild (Proc.Id.ToString ()).Exists) {
					IsRunning = false;
					return false;
				}
				
				return true;
			});
		}
		
		void Stop ()
		{
			if (alive_timer > 0)
				GLib.Source.Remove (alive_timer);
			
			if (Proc == null || !IsRunning)
				return;
			
			// Use the kill program to send off a sigterm instead of a sigkill
			System.Diagnostics.Process.Start ("kill", Proc.Id.ToString ());
			Log<Helper>.Info ("Stopping {0}", File.Basename);
		}
	}
}
