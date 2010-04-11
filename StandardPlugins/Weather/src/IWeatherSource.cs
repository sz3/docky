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

namespace WeatherDocklet
{
	/// <summary>
	/// A weather source provides information about the weather data and
	/// allows requesting updates to that data.
	/// </summary>
	public interface IWeatherSource
	{
		/// <value>
		/// The number of days forecast available from this weather source.
		/// </value>
		int ForecastDays { get; }
		
		/// <value>
		/// The current city, obtained from the weather source.
		/// </value>
		string City { get; }
		
		/// <value>
		/// The Location's latitude.
		/// </value>
		double Latitude { get; }
		
		/// <value>
		/// The Location's longitude.
		/// </value>
		double Longitude { get; }
		
		/// <value>
		/// The Location's sunrise time.
		/// </value>
		DateTime SunRise { get; }
		
		/// <value>
		/// The Location's sunset time.
		/// </value>
		DateTime SunSet { get; }
		
		/// <value>
		/// The current temperature obtained from the weather source.
		/// </value>
		int Temp { get; }
		
		/// <value>
		/// The current 'feels like' temperature obtained from the weather source.
		/// </value>
		int FeelsLike { get; }
		
		/// <value>
		/// The current conditions obtained from the weather source.
		/// </value>
		string Condition { get; }
		
		/// <value>
		/// The current wind speed obtained from the weather source.
		/// </value>
		int Wind { get; }
		
		/// <value>
		/// The current wind direction obtained from the weather source.
		/// </value>
		string WindDirection { get; }
		
		/// <value>
		/// The current humidity obtained from the weather source.
		/// </value>
		string Humidity { get; }
		
		/// <value>
		/// An icon name for the current conditions obtained from the weather source.
		/// </value>
		string Image { get; }
		
		/// <value>
		/// An array of the current forecasts obtained from the weather source.
		/// </value>
		WeatherForecast[] Forecasts { get; }
		
		/// <value>
		/// The displayed name for this weather source.
		/// </value>
		string Name { get; }
		
		/// <value>
		/// A description for this weather source.
		/// </value>
		string About { get; }

		/// <value>
		/// Indicates if the source supports heat index/wind chill.
		/// </value>
		bool SupportsFeelsLike { get; }
		
		/// <summary>
		/// Indicates the weather source is currently reloading the weather data.
		/// </summary>
		event Action WeatherReloading;
		
		/// <summary>
		/// Indicates there was an error reloading the weather data.
		/// </summary>
		event EventHandler<WeatherErrorArgs> WeatherError;
		
		/// <summary>
		/// Indicates the weather was successfully updated.
		/// </summary>
		event Action WeatherUpdated;
		
		/// <summary>
		/// Reloads the weather data using the weather source.
		/// </summary>
		void ReloadWeatherData ();
		
		void StopReload ();
		
		/// <summary>
		/// Displays the radar in the default browser using this weather source's specified URL.
		/// </summary>
		void ShowRadar ();
		
		/// <summary>
		/// Displays the forecast in the default browser using this weather source's specified URL.
		/// </summary>
		/// <param name="day">
		/// A <see cref="System.Int32"/> representing how many days away to show the forecast for.
		/// </param>
		void ShowForecast (int day);
		
		/// <summary>
		/// Returns if it is currently night.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/> indicating if it is night.
		/// </returns>
		bool IsNight ();
		
		/// <summary>
		/// Searches for a location code.
		/// </summary>
		/// <param name="location">
		/// A <see cref="System.String"/> location to search for.
		/// </param>
		/// <returns>
		/// A <see cref="IEnumerable"/> to iterate over pairs of strings, which represent
		/// a location name and location code.
		/// </returns>
		IEnumerable<string> SearchLocation (string location);
	}
}
