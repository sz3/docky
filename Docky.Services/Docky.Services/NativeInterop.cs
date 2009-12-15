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

using GLib;

namespace Docky.Services
{
	
	public class NativeInterop
	{		
		[DllImport ("gio-2.0")]
		private static extern IntPtr g_file_get_uri (IntPtr fileHandle);
		
		[DllImport("libc")]
		private static extern int prctl (int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
		
		
		// these next 4 methods are not yet in GIO#.  The methods in GIO# (Unmount, Eject, UnmountFinish, EjectFinish)
		// have been marked as deprecated since 2.22.  Once GIO# gets these methods we can remove these.
		[DllImport("gio-2.0")]
		private static extern void g_mount_unmount_with_operation (IntPtr mount, int flags, IntPtr mount_operation, 
			IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative callback, IntPtr user_data);
		
		[DllImport("gio-2.0")]
		private static extern void g_mount_eject_with_operation (IntPtr mount, int flags, IntPtr mount_operation, 
			IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative callback, IntPtr user_data);
		
		[DllImport("gio-2.0")]
		private static extern bool g_mount_unmount_with_operation_finish (IntPtr mount, IntPtr result, out IntPtr error);
		
		[DllImport("gio-2.0")]
		private static extern bool g_mount_eject_with_operation_finish (IntPtr mount, IntPtr result, out IntPtr error);

		public static string StrUri (File file)
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
				return prctl (option, System.Text.Encoding.ASCII.GetBytes (arg2 + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not find libc, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return -1;
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to set process name: {0}", e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return -1;
			}
		}
		
		public static void UnmountWithOperation (Mount mount, MountUnmountFlags flags, MountOperation op, 
			Cancellable cancellable, AsyncReadyCallback cb)
		{
			try {
				GLibSharp.AsyncReadyCallbackWrapper cb_wrapper = new GLibSharp.AsyncReadyCallbackWrapper (cb);
				g_mount_unmount_with_operation (mount.Handle, (int) flags, op == null ? IntPtr.Zero : op.Handle, 
				cancellable == null ? IntPtr.Zero : cancellable.Handle, cb_wrapper.NativeDelegate, IntPtr.Zero);
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not find gio-2.0, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return;
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to unmount with operation name: {0}", e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return;
			}
		}
		
		public static void EjectWithOperation (Mount mount, MountUnmountFlags flags, MountOperation op, 
			Cancellable cancellable, AsyncReadyCallback cb)
		{
			try {
				GLibSharp.AsyncReadyCallbackWrapper cb_wrapper = new GLibSharp.AsyncReadyCallbackWrapper (cb);
				g_mount_eject_with_operation (mount.Handle, (int) flags, op == null ? IntPtr.Zero : op.Handle, 
				cancellable == null ? IntPtr.Zero : cancellable.Handle, cb_wrapper.NativeDelegate, IntPtr.Zero);
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not find gio-2.0, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return;
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to eject with operation name: {0}", e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return;
			}
		}
		
		public static bool EjectWithOperationFinish (Mount mount, AsyncResult result)
		{
			try {
				IntPtr error = IntPtr.Zero;
				bool success = g_mount_eject_with_operation_finish (mount.Handle, result == null ? IntPtr.Zero : 
				((result is GLib.Object) ? (result as GLib.Object).Handle : (result as GLib.AsyncResultAdapter).Handle), out error);
				if (error != IntPtr.Zero)
					throw new GLib.GException (error);
				return success;
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not find gio-2.0, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return false;
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to eject with operation finish name: {0}", e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return false;
			}
		}
		
		public static bool UnmountWithOperation (Mount mount, AsyncResult result)
		{
			try {
				IntPtr error = IntPtr.Zero;
				bool success = g_mount_unmount_with_operation_finish (mount.Handle, result == null ? IntPtr.Zero : ((result is GLib.Object) ? (result as GLib.Object).Handle : (result as GLib.AsyncResultAdapter).Handle), out error);
				if (error != IntPtr.Zero)
					throw new GLib.GException (error);
				return success;
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal ("Could not find gio-2.0, please report immediately.");
				Log<NativeInterop>.Info (e.StackTrace);
				return false;
			} catch (Exception e) {
				Log<NativeInterop>.Error ("Failed to unmount with operation finish name: {0}", e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return false;
			}
		}
	}
}
