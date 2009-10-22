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

namespace WeatherDocklet
{
	/// <summary>
	/// Helper class that contains labels for all units and conversion functions.
	/// </summary>
	public class WeatherUnits
	{
		/// <value>
		/// The current unit for temperature values (F, C, etc).
		/// </value>
		public static string TempUnit { get; set; }
		
		/// <value>
		/// The current unit for wind values (Mph, KM/h, etc).
		/// </value>
		public static string WindUnit { get; set; }
		
		/// <summary>
		/// Converts a temperature in degrees Farenheit to degrees Celsius.
		/// </summary>
		/// <param name="F">
		/// A <see cref="System.Int32"/> representing a temperature in degrees Farenheit.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> value representing the F argument converted to degrees Celsius.
		/// </returns>
		public static int ConvertFtoC (int F)
		{
			return (int) Math.Round ((double) (F - 32) * 5 / 9);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Mph">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> value representing the Mph argument converted to Km/h.
		/// </returns>
		public static int ConvertMphToKmh (int Mph)
		{
			return (int) Math.Round ((double) Mph * 1.609344);
		}
	}
}
