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
using System.Linq;
using System.Collections.Generic;

using GLib;

namespace Docky.Services
{

	public static class GLibFileExtension
	{
		static Dictionary<File, List<Action>> MountActions;

		static GLibFileExtension ()
		{
			// setup the mount actions dict
			MountActions = new Dictionary<File, List<Action>> ();
		}
		
		public static string StringUri (this GLib.File file)
		{
			return NativeInterop.StrUri (file);
		}
		
		public static FileInfo QueryInfoSimple (this GLib.File file, string attribute)
		{
			FileInfo info;
			if (attribute.StartsWith ("filesystem::"))
				info = file.QueryFilesystemInfo (attribute, null);
			else
				info = file.QueryInfo (attribute, FileQueryInfoFlags.None, null);
			return info;
		}
		
		public static GLib.Icon Icon (this GLib.File file)
		{
			return file.QueryInfoSimple ("standard::icon").Icon;
		}
		
		public static string QueryStringAttr (this GLib.File file, string attribute)
		{
			return file.QueryInfoSimple (attribute).GetAttributeAsString (attribute);
		}
		
		public static uint QueryUintAttr (this GLib.File file, string attribute)
		{
			return file.QueryInfoSimple (attribute).GetAttributeUInt (attribute);
		}		
		
		public static bool QueryBoolAttr (this GLib.File file, string attribute)
		{
			return file.QueryInfoSimple (attribute).GetAttributeBoolean (attribute);
		}
		
		public static ulong QueryULongAttr (this GLib.File file, string attribute)
		{
			return file.QueryInfoSimple (attribute).GetAttributeULong (attribute);
		}

		public static FileType QueryFileType (this GLib.File file)
		{
			return file.QueryInfoSimple ("standard::type").FileType;
		}
		
		// Recursively list all of the subdirs for a given directory
		public static IEnumerable<GLib.File> SubDirs (this GLib.File file)
		{
			return file.SubDirs (true);
		}
		
		// list all of the subdirs for a given directory
		public static IEnumerable<GLib.File> SubDirs (this GLib.File file, bool recurse)
		{
			FileEnumerator enumerator = file.EnumerateChildren ("standard::type,standard::name,access::can-read", FileQueryInfoFlags.NofollowSymlinks, null);
			
			if (enumerator == null)
				return Enumerable.Empty<GLib.File> ();
			
			FileInfo info;
			List<GLib.File> dirs = new List<GLib.File> ();
			
			while ((info = enumerator.NextFile ()) != null) {
				File child = file.GetChild (info.Name);
				
				if (info.FileType == FileType.Directory && info.GetAttributeBoolean ("access::can-read")) {
					dirs.Add (child);
					if (recurse)
						dirs = dirs.Union (child.SubDirs ()).ToList ();
				}
			}
			
			if (info != null)
				info.Dispose ();
			enumerator.Close (null);
			return dirs.AsEnumerable ();
		}
		
		public static IEnumerable<GLib.File> GetFiles (this GLib.File file)
		{
			return file.GetFiles ("");
		}
		
		// gets all files under the given GLib.File (directory) with the extension of extension	
		public static IEnumerable<GLib.File> GetFiles (this GLib.File file, string extension)
		{
			FileEnumerator enumerator = file.EnumerateChildren ("standard::type,standard::name", FileQueryInfoFlags.NofollowSymlinks, null);
			
			if (enumerator == null)
				return Enumerable.Empty<GLib.File> ();
			
			FileInfo info;
			List<GLib.File> files = new List<GLib.File> ();
			
			while ((info = enumerator.NextFile ()) != null) {
				File child = file.GetChild (info.Name);
				
				if (info.FileType == FileType.Directory)
					continue;
				
				if (child.Basename.EndsWith (extension))
					files.Add (child);
			}
			
			if (info != null)
				info.Dispose ();
			enumerator.Close (null);
			return files.AsEnumerable ();
		}
		
		// This is the recursive equivalent to GLib.File.Delete ()
		public static void Delete_Recurse (this GLib.File file)
		{
			FileEnumerator enumerator = file.EnumerateChildren ("standard::type,standard::name,access::can-delete", FileQueryInfoFlags.NofollowSymlinks, null);
			
			if (enumerator == null)
				return;
			
			FileInfo info;
			
			while ((info = enumerator.NextFile ()) != null) {
				File child = file.GetChild (info.Name);
				
				if (info.FileType == FileType.Directory)
					Delete_Recurse (child);
				
				if (info.GetAttributeBoolean ("access::can-delete"))
					child.Delete (null);
			}
			
			if (info != null)
				info.Dispose ();
			enumerator.Close (null);
		}
		
		// This is the recursive equivalent of GLib.File.Copy ()
		public static void Copy_Recurse (this GLib.File source, GLib.File dest, FileCopyFlags flags, FileProgressCallback progress_cb)
		{
			long totalBytes = source.GetSize ();
			long copiedBytes = 0;
			
			Recursive_Copy (source, dest, ref copiedBytes, totalBytes, progress_cb);
		}
		
		public static bool DirectoryHasFiles (this GLib.File file)
		{
			System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo (file.Path);
			
			if (dir.GetFiles ().Count () > 0 || dir.GetDirectories ().Count () > 0)
				return true;
			return false;
		}
		
		public static string NewFileName (this GLib.File fileToMove, File dest)
		{
			string name, ext;
			
			if (fileToMove.Basename.Split ('.').Count() > 1) {
				name = fileToMove.Basename.Split ('.').First ();
				ext = fileToMove.Basename.Substring (fileToMove.Basename.IndexOf ('.'));
			} else {
				name = fileToMove.Basename;
				ext = "";
			}
			if (dest.GetChild (fileToMove.Basename).Exists) {
				int i = 1;
				while (dest.GetChild (string.Format ("{0} ({1}){2}", name, i, ext)).Exists) {
					i++;
				}
				return string.Format ("{0} ({1}){2}", name, i, ext);
			} else {
				return fileToMove.Basename;
			}
		}
		
		static void Recursive_Copy (GLib.File source, GLib.File dest, ref long copiedBytes, long totalBytes, FileProgressCallback progress_cb)
		{
			if (source.QueryFileType () != FileType.Directory) {
				source.Copy (dest, FileCopyFlags.AllMetadata | FileCopyFlags.NofollowSymlinks, null, (current, total) => {
					 progress_cb.Invoke (current, totalBytes);
				 });
				return;
			}
			
			FileEnumerator enumerator = source.EnumerateChildren ("standard::type,standard::name,standard::size", FileQueryInfoFlags.NofollowSymlinks, null);
			
			if (enumerator == null)
				return;
			
			FileInfo info;
			
			while ((info = enumerator.NextFile ()) != null) {
				File child = source.GetChild (info.Name);
				
				if (info.FileType == FileType.Directory) {
					// copy all of the children
					Recursive_Copy (child, dest.GetChild (info.Name), ref copiedBytes, totalBytes, progress_cb);
				} else {
					// first create the directory at the destination if it doesn't exist
					if (!dest.Exists)
						dest.MakeDirectoryWithParents (null);
					// this looks crazy making variables here, assigning in the delegate, then reassigning to
					// copiedBytes, but c# won't let me use out or ref vars in a delegate func.
					long copied = copiedBytes;
					// copy
					child.Copy (dest.GetChild (info.Name), FileCopyFlags.AllMetadata | FileCopyFlags.NofollowSymlinks, null, (current, total) => {
						 progress_cb.Invoke (copied + current, totalBytes);
					 });
					copiedBytes += info.Size;
				}
			}
			
			if (info != null)
				info.Dispose ();
			enumerator.Close (null);
		}
		
		// will recurse and get the total size in bytes
		public static long GetSize (this GLib.File file)
		{
			FileInfo fileInfo = file.QueryInfo ("standard::type,standard::size", FileQueryInfoFlags.NofollowSymlinks, null);
			
			if (fileInfo.FileType != FileType.Directory)
				return fileInfo.Size;
			
			long size = 0;
			FileEnumerator enumerator = file.EnumerateChildren ("standard::type,standard::name,standard::size", FileQueryInfoFlags.NofollowSymlinks, null);
			
			if (enumerator == null)
				return 0;
			
			FileInfo info;
			
			while ((info = enumerator.NextFile ()) != null) {
				File child = file.GetChild (info.Name);
				
				if (info.FileType == FileType.Directory)
					size += GetSize (child);
				else
					size += info.Size;
			}
			
			if (info != null)
				info.Dispose ();
			enumerator.Close (null);
			return size;
		}
		
		public static string NewLineString (this GLib.DataInputStream stream)
		{
			switch (stream.NewlineType) {
			case DataStreamNewlineType.Cr:
				return "\r";
			case DataStreamNewlineType.Lf:
				return "\n";
			case DataStreamNewlineType.CrLf:
				return "\r\n";
			// this is a safe default because \n is the common line ending on *nix
			default:
				return "\n";
			}
		}
		
		public static void MountWithActionAndFallback (this GLib.File file, Action success, Action failed)
		{
			// In rare instances creating a Gtk.MountOperation can fail so let's try to create it first
			Gtk.MountOperation op = null;
			try {
				op = new Gtk.MountOperation (null);
			} catch (Exception e) {
				Log.Error ("Unable to create a Gtk.MountOperation. " +
					"This is most likely due to a missing gtk or glib library.  Error message: {0}", e.Message);
				Log.Debug (e.StackTrace);
			}
			file.MountEnclosingVolume (0, op == null ? null : op, null, (o, result) =>
			{
				// wait for the mount to finish
				try {
					if (file.MountEnclosingVolumeFinish (result)) {
						// invoke the supplied success action
						success.Invoke ();
						// if we have any other actions for this file on a successful mount
						// invoke them too
						if (!MountActions.ContainsKey (file))
							return;
						lock (MountActions[file]) {
							foreach (Action act in MountActions[file])
								act.Invoke ();
						}
					}
					// an exception can be thrown here if we are trying to mount an already mounted file
					// in that case, resort to the fallback
				} catch (GLib.GException) {
					try {
						failed.Invoke ();
					} catch {}
				}
			});
		}
		
		public static void AddMountAction (this GLib.File file, Action action)
		{
			if (!MountActions.ContainsKey (file))
				MountActions[file] = new List<Action> ();
			MountActions [file].Add (action);
		}
		
		public static void RemoveAction (this GLib.File file, Action action)
		{
			if (MountActions.ContainsKey (file) && MountActions [file].Contains (action))
				MountActions [file].Remove (action);
		}
	}
}