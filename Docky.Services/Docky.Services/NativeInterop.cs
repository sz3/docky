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
using System.Runtime.InteropServices;

namespace Docky.Services
{
	
	public class NativeInterop
	{		
		[DllImport ("gio-2.0")]
		private static extern IntPtr g_file_get_uri (IntPtr fileHandle);
		
		[DllImport("libc")]
		private static extern int prctl (int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
		
		public static string StrUri (GLib.File file)
		{
			try {
				return Marshal.PtrToStringAuto (g_file_get_uri (file.Handle));
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not find gio-2.0, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return "";
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to retrieve uri for file '{0}': {1}", file.Basename, e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return "";
			}
		}

		public static int prctl (int option, string arg2)
		{
			try {
				return prctl (option, Encoding.ASCII.GetBytes (arg2 + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not load libc, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return -1;
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to set process name: {0}", e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return -1;
			}
		}
	}
}
