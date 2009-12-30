	//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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

using Docky.CairoHelper;

namespace Docky.Services
{
	public class DrawingService
	{
		const string MissingIconIcon = "application-default-icon;;application-x-executable";
		
		internal DrawingService ()
		{
		}
		
		/// <summary>
		/// An empty pixbuf
		/// </summary>
		public Pixbuf EmptyPixbuf {
			get {
				Pixbuf pb = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);
				pb.Fill (0x00000000);
				return pb;
			}
		}
		
		/// <summary>
		/// Returns a pango layout consistent with the current theme.
		/// </summary>
		/// <returns>
		/// A <see cref="Pango.Layout"/>
		/// </returns>
		public Pango.Layout ThemedPangoLayout ()
		{
			Pango.Context context = Gdk.PangoHelper.ContextGetForScreen (Gdk.Screen.Default);
			return new Pango.Layout (context);
		}
		
		/// <summary>
		/// Determines whether or not the icon is light or dark.
		/// </summary>
		/// <param name="icon">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool IsIconLight (string icon) {
			int light = 0;
			using (Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (icon)) {
				unsafe {
					byte* pixelPtr = (byte*) pixbuf.Pixels;
					for (int i = 0; i < pixbuf.Height; i++) {
						for (int j = 0; j < pixbuf.Width; j++) {
							byte max = Math.Max (pixelPtr[0], Math.Max (pixelPtr[1], pixelPtr[2]));
							
							if (pixelPtr[3] > 0) {
								if (max > byte.MaxValue / 2)
									light++;
								else
									light--;
							}
							
							pixelPtr += 4;
						}
						pixelPtr += pixbuf.Rowstride - pixbuf.Width * 4;
					}
				}
			}
			
			return light > 0;
		}
		
		/// <summary>
		/// Returns a monochrome version of the supplied pixbuf.
		/// </summary>
		/// <param name="pbuf">
		/// A <see cref="Gdk.Pixbuf"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Pixbuf"/>
		/// </returns>
		public Gdk.Pixbuf MonochromePixbuf (Gdk.Pixbuf source)
		{
			try {
				Gdk.Pixbuf monochrome = source;
				unsafe {
					double a, r, g, b;
					byte* pixels = (byte*) monochrome.Pixels;
					for (int i = 0; i < monochrome.Height * monochrome.Width; i++) {
						r = (double) pixels[0];
						g = (double) pixels[1];
						b = (double) pixels[2];
						a = (double) pixels[3];
						
						Cairo.Color color = new Cairo.Color (r / byte.MaxValue, 
						                                     g / byte.MaxValue, 
						                                     b / byte.MaxValue,
						                                     a / byte.MaxValue);
						
						color = color.DarkenBySaturation (0.5).SetSaturation (0);
						
						pixels[0] = (byte) (color.R * byte.MaxValue);
						pixels[1] = (byte) (color.G * byte.MaxValue);
						pixels[2] = (byte) (color.B * byte.MaxValue);
						pixels[3] = (byte) (color.A * byte.MaxValue);
						
						pixels += 4;
					}
				}
				source = monochrome.Copy ();
				monochrome.Dispose ();
			} catch (Exception e) {
				Log<DrawingService>.Error ("Error monochroming pixbuf: {0}", e.Message);
				Log<DrawingService>.Debug (e.StackTrace);
			}
			
			return source;
		}
		
		/// <summary>
		/// Adds a hue shift to the supplpied pixbuf.
		/// </summary>
		/// <param name="source">
		/// A <see cref="Gdk.Pixbuf"/>
		/// </param>
		/// <param name="shift">
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Pixbuf"/>
		/// </returns>
		public Gdk.Pixbuf AddHueShift (Gdk.Pixbuf source, double shift)
		{
			try {
				Gdk.Pixbuf shifted = source;
				if (shift != 0) {
					unsafe {
						double a, r, g, b;
						byte* pixels = (byte*) shifted.Pixels;
						for (int i = 0; i < shifted.Height * shifted.Width; i++) {
							r = (double) pixels[0];
							g = (double) pixels[1];
							b = (double) pixels[2];
							a = (double) pixels[3];
							
							Cairo.Color color = new Cairo.Color (r / byte.MaxValue, 
								g / byte.MaxValue, 
								b / byte.MaxValue,
								a / byte.MaxValue);
							color = color.AddHue (shift);
							
							pixels[0] = (byte) (color.R * byte.MaxValue);
							pixels[1] = (byte) (color.G * byte.MaxValue);
							pixels[2] = (byte) (color.B * byte.MaxValue);
							pixels[3] = (byte) (color.A * byte.MaxValue);
							
							pixels += 4;
						}
					}
				}
				source = shifted.Copy ();
				shifted.Dispose ();
			} catch (Exception e) {
				Log<DrawingService>.Error ("Error adding hue shift: {0}", e.Message);
				Log<DrawingService>.Debug (e.StackTrace);
			}
			
			return source;
		}
		
		/// <summary>
		/// Load an icon specifying the width and height.
		/// </summary>
		/// <param name="names">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="width">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="height">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Pixbuf"/>
		/// </returns>
		public Gdk.Pixbuf LoadIcon (string names, int width, int height)
		{
			if (names == null)
				names = "";
			
			List<string> iconNames = names.Split (new [] { ";;"}, StringSplitOptions.RemoveEmptyEntries).ToList ();
			// add the MissingIconIcon as a last resort icon.
			iconNames = iconNames.Union (MissingIconIcon.Split (new [] { ";;"}, StringSplitOptions.RemoveEmptyEntries)).ToList ();
			
			Gdk.Pixbuf pixbuf = null;
			
			foreach (string name in iconNames) {
				// The icon can be loaded from a loaded assembly if the icon has
				// the format: "resource@assemblyname".
				if (IconIsEmbeddedResource (name)) {
					pixbuf = IconFromEmbeddedResource (name, width, height);
					if (pixbuf != null)
						break;
				}
				
				if (IconIsFile (name)) {
					pixbuf = IconFromFile (name, width, height);
					if (pixbuf != null)
						break;
				}
				
				if (width <= 0 || height <= 0)
					throw new ArgumentException ("Width / Height must be greater than 0 if icon is not a file or embedded resource");
				
				// Try to load icon from defaul theme.
				pixbuf = IconFromTheme (name, Math.Max (width, height), IconTheme.Default);
				if (pixbuf != null)
					break;
				
				// Try to load a generic file icon.
				if (name.StartsWith ("gnome-mime")) {
					pixbuf = GenericFileIcon (Math.Max (width, height));
					if (pixbuf != null)
						break;
				}
				
				// After this point, we assume that the caller's icon cannot be found,
				// so we warn and continue, trying the next in the list
				
				if (name != iconNames.Last ())
					Log<DrawingService>.Info ("Could not find '{0}', using fallback of '{1}'.", name, iconNames[ iconNames.IndexOf (name) + 1]);
			}
			
			if (pixbuf != null) {
				if (width != -1 && height != -1 && (width != pixbuf.Width || height != pixbuf.Height))
					pixbuf = ARScale (width, height, pixbuf);
				return pixbuf;
			}		
			
			// If all else fails, use an empty pixbuf.
			return EmptyPixbuf;
		}

		/// <summary>
		/// Load an icon specifying only the size.  Size will be  used for both width and height.
		/// </summary>
		/// <param name="names">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Pixbuf"/>
		/// </returns>
		public Gdk.Pixbuf LoadIcon (string names, int size)
		{
			return LoadIcon (names, size, size);
		}
		
		/// <summary>
		/// Load an icon at its native size.  Note when this is used on icons that are not files or resources 
		/// an exception will be thrown.
		/// </summary>
		/// <param name="names">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Pixbuf"/>
		/// </returns>
		public Gdk.Pixbuf LoadIcon (string names)
		{
			return LoadIcon (names, -1);
		}
		
		/// <summary>
		/// Scale a pixbuf to the desired width or height maintaining the aspect ratio of the supplied pixbuf.
		/// Note that due to maintaining the aspect ratio, the returned pixbuf may not have the exact width AND height as is specified.
		/// Though it is guaranteed that one of these measurements will be correct.
		/// </summary>
		/// <param name="width">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="height">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="pixbuf">
		/// A <see cref="Pixbuf"/>
		/// </param>
		/// <returns>
		/// A <see cref="Pixbuf"/>
		/// </returns>
		public Pixbuf ARScale (int width, int height, Pixbuf pixbuf)
		{			
			double xScale = (double) width / (double) pixbuf.Width;
			double yScale = (double) height / (double) pixbuf.Height;
			double scale = Math.Min (xScale, yScale);
			
			if (scale == 1) return pixbuf;
			
			Pixbuf temp = pixbuf;
			pixbuf = temp.ScaleSimple ((int) (temp.Width * scale),
			                           (int) (temp.Height * scale),
			                           InterpType.Hyper);
			temp.Dispose ();
			
			return pixbuf;
		}
		
		/// <summary>
		/// Returns the string name of the supplied icon.
		/// </summary>
		/// <param name="icon">
		/// A <see cref="GLib.Icon"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
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
		
		/// <summary>
		/// Returns a new color which is the result of blending the two supplied colors.
		/// </summary>
		/// <param name="a">
		/// A <see cref="Gdk.Color"/>
		/// </param>
		/// <param name="b">
		/// A <see cref="Gdk.Color"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Color"/>
		/// </returns>
		public Gdk.Color ColorBlend (Gdk.Color a, Gdk.Color b)
        {
            // at some point, might be nice to allow any blend?
            double blend = 0.5;

            if (blend < 0.0 || blend > 1.0) {
                throw new ApplicationException ("blend < 0.0 || blend > 1.0");
            }
            
            double blendRatio = 1.0 - blend;

            int aR = a.Red >> 8;
            int aG = a.Green >> 8;
            int aB = a.Blue >> 8;

            int bR = b.Red >> 8;
            int bG = b.Green >> 8;
            int bB = b.Blue >> 8;

            double mR = aR + bR;
            double mG = aG + bG;
            double mB = aB + bB;

            double blR = mR * blendRatio;
            double blG = mG * blendRatio;
            double blB = mB * blendRatio;

            Gdk.Color color = new Gdk.Color ((byte)blR, (byte)blG, (byte)blB);
            Gdk.Colormap.System.AllocColor (ref color, true, true);
            return color;
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
			GLib.File iconFile = (name.StartsWith ("/")) ? FileFactory.NewForPath (name) : FileFactory.NewForUri (name);
			try {
				if (width <= 0 || height <= 0)
					pixbuf = new Pixbuf (iconFile.Path);
				else
					pixbuf = new Pixbuf (iconFile.Path, width, height, true);
			} catch (Exception e) {
				Log<DrawingService>.Warn ("Error loading icon from file '" + iconFile.Path + "': " + e.Message);
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
