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
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;

using Cairo;
using GLib;
using Gdk;
using Gtk;

namespace Docky.Services
{


	public class DrawingService
	{
		const string MissingIconIcon = "application-default-icon";
		
		internal DrawingService ()
		{
		}
		
		public Pango.Layout ThemedPangoLayout ()
		{
			Pango.Context context = Gdk.PangoHelper.ContextGetForScreen (Gdk.Screen.Default);
			return new Pango.Layout (context);
		}
		
		// load an icon specifying all arguments
		public Gdk.Pixbuf LoadIcon (string name, int width, int height, string fallback)
		{
			if (string.IsNullOrEmpty (name))
				name = MissingIconIcon;
			if (string.IsNullOrEmpty (fallback))
				fallback = MissingIconIcon;
			
			Gdk.Pixbuf pixbuf;
			
			// The icon can be loaded from a loaded assembly if the icon has
			// the format: "resource@assemblyname".
			if (IconIsEmbeddedResource (name)) {
				pixbuf = IconFromEmbeddedResource (name, width, height);
				if (pixbuf != null)
					return pixbuf;
			}
			
			if (IconIsFile (name)) {
				pixbuf = IconFromFile (name, width, height);
				if (pixbuf != null)
					return pixbuf;
			}
			
			if (width <= 0 || height <= 0)
				throw new ArgumentException ("Width / Height must be greater than 0 if icon is not a file or embedded resource");
			
			// Try to load icon from defaul theme.
			pixbuf = IconFromTheme (name, Math.Max (width, height), IconTheme.Default);
			if (pixbuf != null) return pixbuf;

			// Try to load a generic file icon.
			if (name.StartsWith ("gnome-mime")) {
				pixbuf = GenericFileIcon (Math.Max (width, height));
				if (pixbuf != null) return pixbuf;
			}
			
			// After this point, we assume that the caller's icon cannot be found,
			// so we attempt to provide a suitable alternative.
			
			// Try to load the fallback icon
			Log<DrawingService>.Info ("Could not find '{0}', using fallback of '{1}'.", name, fallback);
			pixbuf = LoadIcon (MissingIconIcon, width, height);
			if (pixbuf != null) return pixbuf;
			
			// If all else fails, use the UnknownPixbuf.
			return UnknownPixbuf ();
		}
		
		// load an icon specifying the width and height
		public Gdk.Pixbuf LoadIcon (string name, int width, int height)
		{
			return LoadIcon (name, width, height, "");
		}
		
		// load a square icon with the given size
		public Gdk.Pixbuf LoadIcon (string name, int size)
		{
			return LoadIcon (name, size, size);
		}
		
		// load the icon at its native size
		// note that when this is used on icons that are not files or resources
		// an exception will be thrown
		public Gdk.Pixbuf LoadIcon (string name)
		{
			return LoadIcon (name, -1);
		}
		
		public Pixbuf ARScale (int width, int height, Pixbuf pixbuf)
		{			
			double xScale = (double) width / (double) pixbuf.Width;
//			xScale = Math.Min (1, xScale);
			double yScale = (double) height / (double) pixbuf.Height;
//			yScale = Math.Min (1,yScale);
			double scale = Math.Min (xScale, yScale);
			
			Pixbuf temp = pixbuf;
			pixbuf = temp.ScaleSimple ((int) (temp.Width * scale),
			                           (int) (temp.Height * scale),
			                           InterpType.Hyper);
			temp.Dispose ();
			
			return pixbuf;
		}
		
		public string IconFromGIcon (GLib.Icon icon)
		{
			if (icon is ThemedIcon) {
				ThemedIcon themeIcon = new ThemedIcon (icon.Handle);
				
				// if the icon exists in the theme, this will return the relevent ion
				if (themeIcon.Names.Any ())
					return themeIcon.Names.FirstOrDefault (n => IconTheme.Default.HasIcon (n));
			} else if (icon is FileIcon) {
				// in some cases, devices provide their own icon.  This will use the device icon.
				FileIcon iconFile = new FileIcon (icon.Handle);
				
				return iconFile.File.Path;
			}
			return "";
		}
		
		Pixbuf UnknownPixbuf () 
		{
			Pixbuf pb = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);
			pb.Fill (0x00000000);
			return pb;
		}
		
		bool IconIsEmbeddedResource (string name)
		{
			return 0 < name.IndexOf ("@");
		}
		
		bool IconIsFile (string name)
		{
			return name.StartsWith ("/") ||
				   name.StartsWith ("~/") || 
				   name.StartsWith ("file://", StringComparison.OrdinalIgnoreCase);
		}
		
		Pixbuf IconFromEmbeddedResource (string name, int width, int height)
		{
			Pixbuf pixbuf = null;
			string resource = name.Substring (0, name.IndexOf ("@"));
			string assemblyName = name.Substring (resource.Length + 1);
			
			try {
				Assembly asm = AppDomain.CurrentDomain.GetAssemblies ().First (a => a.FullName == assemblyName);
				if (asm == null)
					throw new ArgumentNullException ("Could not find assembly '{0}'.", assemblyName);
				
				pixbuf = new Pixbuf (asm, resource);
				
				// now scale the pixbuf but keep the aspect ratio
				if (width > 0 && height > 0)
					pixbuf = ARScale (width, height, pixbuf);
				
			} catch (Exception e) {
				Log<DrawingService>.Warn ("Failed to load icon resource {0} from assembly {1}: {2}",
				                         resource, assemblyName, e.Message); 
				Log<DrawingService>.Debug (e.StackTrace);
				pixbuf = null;
			}
			return pixbuf;
		}
		
		Pixbuf IconFromFile (string name, int width, int height)
		{
			Pixbuf pixbuf;

			string home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			name = name.Replace ("~", home);
			try {
				if (width <= 0 || height <= 0)
					pixbuf = new Pixbuf (name);
				else
					pixbuf = new Pixbuf (name, width, height, true);
			} catch (Exception e) {
				Log<DrawingService>.Warn ("Error loading icon from file '" + name + "': " + e.Message);
				Log<DrawingService>.Debug (e.StackTrace);
				pixbuf = null;
			}
			return pixbuf;
		}
		
		Pixbuf IconFromTheme (string name, int size, IconTheme theme)
		{
			Pixbuf pixbuf = null;
			string name_noext = name;
			
			// We may have to remove the extension.
			if (name.Contains (".")) {
				name_noext = name.Remove (name.LastIndexOf ("."));
			}
			
			try {
				if (theme.HasIcon (name)) {  
					pixbuf = theme.LoadIcon (name, size, 0);
				} else if (theme.HasIcon (name_noext)) { 
					pixbuf = theme.LoadIcon (name_noext, size, 0);
				} else if (name == "gnome-mime-text-plain" && theme.HasIcon ("gnome-mime-text")) { 
					pixbuf = theme.LoadIcon ("gnome-mime-text", size, 0);
				}
			} catch (Exception e) {
				Log<DrawingService>.Warn ("Error loading themed icon '" + name + "': " + e.Message);
				Log<DrawingService>.Debug (e.StackTrace);
				pixbuf = null;
			}
		
			return pixbuf;
		}
		
		Pixbuf GenericFileIcon (int size)
		{
			Pixbuf pixbuf = null;
			if (IconTheme.Default.HasIcon ("gtk-file")) {
				try {
					pixbuf = IconTheme.Default.LoadIcon ("gtk-file", size, 0);
				} catch (Exception e) {
					Log<DrawingService>.Warn ("Error loading generic icon: " + e.Message);
					Log<DrawingService>.Debug (e.StackTrace);
					pixbuf = null;					
				}
			}
			return pixbuf;
		}
	}
}
