// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2006 Novell, Inc. (http://www.novell.com)
//
//

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace WindowManager.Xlib
{
	public static class Xlib
	{
		const string libX11 = "X11";
		const string libGdkX11 = "libgdk-x11-2.0";
		
		[DllImport (libGdkX11)]
        static extern IntPtr gdk_x11_display_get_xdisplay (IntPtr display);
		
		[DllImport (libX11)]
		public extern static int XInternAtoms (IntPtr display, string[] atom_names, int atom_count, bool only_if_exists, IntPtr[] atoms);
		
		[DllImport (libX11)]
		public extern static int XGetWindowProperty (IntPtr display, IntPtr window, IntPtr atom, IntPtr long_offset, 
		                                             IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type, 
		                                             out int actual_format, out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);
		
		public static IntPtr GdkDisplayXDisplay (Gdk.Display display)
		{
			return gdk_x11_display_get_xdisplay (display.Handle);
		}
	}
}
