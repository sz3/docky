//  
//  Copyright (C) 2009 Robert Dyer
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

using Gdk;
using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// Indicates what mode the docklet currently is in.
	/// </summary>
	public enum WeatherDockletStatus
	{
		Initializing,
		Normal,
		Reloading,
		ManualReload,
		Error
	}
	
	/// <summary>
	/// A docklet to display the current temp and condition as an icon and
	/// use a painter to display forecast information.
	/// </summary>
	public class WeatherDocklet : AbstractDockItem
	{
		public override string UniqueID ()
		{
			return "WeatherDockItem";
		}
		
		/// <value>
		/// Indicates what mode the docklet currently is in.
		/// </value>
		WeatherDockletStatus Status { get; set; }
		
		WeatherPainter painter;
		
		/// <summary>
		/// Creates a new weather docklet.
		/// </summary>
		public WeatherDocklet ()
		{
			ScalableRendering = false;
			
			painter = new WeatherPainter (this);
			
			Status = WeatherDockletStatus.Initializing;
			State |= ItemState.Wait;
			
			WeatherController.WeatherReloading += HandleWeatherReloading;
			WeatherController.WeatherError += HandleWeatherError;
			WeatherController.WeatherUpdated += HandleWeatherUpdated;
			
			HoverText = Catalog.GetString ("Fetching data...");
		}
		
		/// <summary>
		/// Handles when a weather source reloads data.
		/// </summary>
		void HandleWeatherReloading ()
		{
			Gtk.Application.Invoke (delegate {
				HoverText = Catalog.GetString ("Fetching data...");
				if (Status != WeatherDockletStatus.Initializing && Status != WeatherDockletStatus.ManualReload) {
					Status = WeatherDockletStatus.Reloading;
					State &= ~ItemState.Wait;
				} else {
					State |= ItemState.Wait;
				}
				QueueRedraw ();
			});
		}
		
		/// <summary>
		/// Handles an error with the weather source.
		/// </summary>
		/// <param name="sender">
		/// Ignored
		/// </param>
		/// <param name="e">
		/// A <see cref="WeatherErrorArgs"/> which contains the error message.
		/// </param>
		void HandleWeatherError (object sender, WeatherErrorArgs e)
		{
			Gtk.Application.Invoke (delegate {
				HoverText = e.Error;
				Status = WeatherDockletStatus.Error;
				State &= ~ItemState.Wait;
				QueueRedraw ();
			});
		}
		
		/// <summary>
		/// Handles when a weather source successfully reloads data.
		/// </summary>
		void HandleWeatherUpdated ()
		{
			Gtk.Application.Invoke (delegate {
				IWeatherSource weather = WeatherController.Weather;
				
				string feelsLike = "";
				if (weather.SupportsFeelsLike)
					feelsLike = " (" + weather.FeelsLike + WeatherUnits.TempUnit + ")";
				
				HoverText = weather.Condition + "   " +
					weather.Temp + WeatherUnits.TempUnit + feelsLike +
					"   " + weather.City;
				Status = WeatherDockletStatus.Normal;
				State &= ~ItemState.Wait;
				QueueRedraw ();
			});
		}
		
		/// <summary>
		/// Draws an icon onto a context using the specified offset and size.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Context"/> to draw the icon.
		/// </param>
		/// <param name="icon">
		/// A <see cref="System.String"/> containing the icon name to use.
		/// </param>
		/// <param name="xoffset">
		/// A <see cref="System.Int32"/> indicating the x offset for the icon.
		/// </param>
		/// <param name="yoffset">
		/// A <see cref="System.Int32"/> indicating the y offset for the icon.
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/> indicating the size of the icon.
		/// </param>
		public static void RenderIconOntoContext (Context cr, string icon, int xoffset, int yoffset, int size)
		{
			RenderIconOntoContext (cr, icon, xoffset, yoffset, size, 1);
		}
		
		/// <summary>
		/// Draws an icon onto a context using the specified offset and size.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Context"/> to draw the icon.
		/// </param>
		/// <param name="icon">
		/// A <see cref="System.String"/> containing the icon name to use.
		/// </param>
		/// <param name="xoffset">
		/// A <see cref="System.Int32"/> indicating the x offset for the icon.
		/// </param>
		/// <param name="yoffset">
		/// A <see cref="System.Int32"/> indicating the y offset for the icon.
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/> indicating the size of the icon.
		/// </param>
		/// <param name="alpha">
		/// A <see cref="double"/> indicating the alpha to use.
		/// </param>
		public static void RenderIconOntoContext (Context cr, string icon, int xoffset, int yoffset, int size, double alpha)
		{
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (icon, size))
			{
				CairoHelper.SetSourcePixbuf (cr, pbuf, xoffset, yoffset);
				cr.PaintWithAlpha (alpha);
			}
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			int size = Math.Min (surface.Width, surface.Height);
			Context cr = surface.Context;
			
			switch (Status) {
			case WeatherDockletStatus.Error:
				RenderIconOntoContext (cr, "network-offline", 0, 0, size);
				cr.Fill ();
				break;

			default:
			case WeatherDockletStatus.ManualReload:
			case WeatherDockletStatus.Normal:
			case WeatherDockletStatus.Reloading:
				RenderIconOntoContext (cr, WeatherController.Weather.Image, 0, 0, size, 1);
				
				if (size >= 32) {
					Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
					layout.FontDescription = new Gtk.Style().FontDescription;
					layout.FontDescription.Weight = Pango.Weight.Bold;
					layout.Ellipsize = Pango.EllipsizeMode.None;
					
					Pango.Rectangle inkRect, logicalRect;
					
					layout.Width = Pango.Units.FromPixels (size / 2);
					layout.SetText (WeatherController.Weather.Temp + WeatherUnits.TempUnit);
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (size / 3.5));

					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo ((size - inkRect.Width) / 2, size - logicalRect.Height);

					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.LineWidth = 4;
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();

					cr.Color = new Cairo.Color (1, 1, 1, 0.8);
					cr.Fill ();
				}
				break;

			case WeatherDockletStatus.Initializing:
				break;
			}
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1)
				ShowPainter (painter);
			return ClickAnimation.None;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (WeatherPreferences.Location.Length <= 1)
				return;

			Status = WeatherDockletStatus.ManualReload;
			State |= ItemState.Wait;
			QueueRedraw ();
			
			if (direction == Gdk.ScrollDirection.Up)
				WeatherController.PreviousLocation ();
			else
				WeatherController.NextLocation ();
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			if (WeatherController.Weather.Condition != null)
			{
				list[MenuListContainer.Header].Add (new MenuItem (Catalog.GetString ("Radar _Map"),
						WeatherController.Weather.Image, (o, a) => WeatherController.Weather.ShowRadar ()));
			}
			
			
			list.SetContainerTitle (MenuListContainer.Actions, Mono.Unix.Catalog.GetString ("Forecasts"));
			for (int i = 0; i < WeatherController.Weather.ForecastDays; i++)
				if (WeatherController.Weather.Forecasts[i].dow != null)
				{
					list[MenuListContainer.Actions].Add (new ForecastMenuItem (i,
							string.Format ("{0}", WeatherForecast.DayName (WeatherController.Weather.Forecasts[i].dow)),
							WeatherController.Weather.Forecasts[i].image));
				}
			
			list[MenuListContainer.CustomOne].Add (new MenuItem (Catalog.GetString ("_Settings"), Gtk.Stock.Preferences,
					delegate {
						if (WeatherConfigurationDialog.instance == null)
							WeatherConfigurationDialog.instance = new WeatherConfigurationDialog ();
						WeatherConfigurationDialog.instance.Show ();
					}));
			
			list[MenuListContainer.CustomOne].Add (new MenuItem (Catalog.GetString ("_Reload Weather Data"), Gtk.Stock.Refresh,
					delegate {
						Status = WeatherDockletStatus.ManualReload;
						State |= ItemState.Wait;
						QueueRedraw ();
						WeatherController.ResetTimer ();
					}));
			
			return list;
		}
		
		public override void Dispose ()
		{
			if (WeatherConfigurationDialog.instance == null) {
				WeatherConfigurationDialog.instance.Destroy ();
				WeatherConfigurationDialog.instance = null;
			}
			
			WeatherController.WeatherReloading -= HandleWeatherReloading;
			WeatherController.WeatherError -= HandleWeatherError;
			WeatherController.WeatherUpdated -= HandleWeatherUpdated;
			
			base.Dispose ();
		}
	}
}
