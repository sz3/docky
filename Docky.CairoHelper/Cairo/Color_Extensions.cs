//  
//  Copyright (C) 2009 GNOME Do
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

namespace Cairo
{


	public static class CairoColor_Extensions
	{
		public static Gdk.Color ToGdkColor (this Color self)
		{
			return new Gdk.Color ((byte) (byte.MaxValue * self.R), (byte) (byte.MaxValue * self.G), (byte) (byte.MaxValue * self.B));
		}
		
		public static Cairo.Color SetHue (this Color self, double hue)
		{
			if (hue < 0 || hue > 360) throw new ArgumentOutOfRangeException ("Hue", "Hue must be between 0 and 360");
			
			double h, s, v, r, g, b;
			RGBToHSV (self.R, self.G, self.B, out h, out s, out v);
			h = hue;
			HSVToRGB (h, s, v, out r, out g, out b);
			
			return new Cairo.Color (r, g, b, self.A);
		}
		
		public static Cairo.Color SetValue (this Color self, double val)
		{
			if (val < 0 || val > 1) throw new ArgumentOutOfRangeException ("Value", "Value must be between 0 and 1");
			
			double h, s, v, r, g, b;
			RGBToHSV (self.R, self.G, self.B, out h, out s, out v);
			v = val;
			HSVToRGB (h, s, v, out r, out g, out b);
			
			return new Cairo.Color (r, g, b, self.A);
		}
		
		public static Cairo.Color SetSaturation (this Color self, double sat)
		{
			if (sat < 0 || sat > 1) throw new ArgumentOutOfRangeException ("Saturation", "Saturation must be between 0 and 1");
			
			double h, s, v, r, g, b;
			RGBToHSV (self.R, self.G, self.B, out h, out s, out v);
			s = sat;
			HSVToRGB (h, s, v, out r, out g, out b);
			
			return new Cairo.Color (r, g, b, self.A);
		}
		
		public static Cairo.Color BrightenValue (this Color self, double amount)
		{
			if (amount < 0 || amount > 1) throw new ArgumentOutOfRangeException ("Brighten Amount", "Brighten amount must be between 0 and 1");
			
			double h, s, v, r, g, b;
			RGBToHSV (self.R, self.G, self.B, out h, out s, out v);
			v += (1 - v) * amount;
			HSVToRGB (h, s, v, out r, out g, out b);
			
			return new Cairo.Color (r, g, b, self.A);
		}
		
		public static Cairo.Color MultiplySaturation (this Color self, double amount)
		{
			if (amount < 0 || amount > 1) throw new ArgumentOutOfRangeException ("Brighten Amount", "Brighten amount must be between 0 and 1");
			
			double h, s, v, r, g, b;
			RGBToHSV (self.R, self.G, self.B, out h, out s, out v);
			s = Math.Min (1, s * amount);
			HSVToRGB (h, s, v, out r, out g, out b);
			
			return new Cairo.Color (r, g, b, self.A);
		}
		
		static void RGBToHSV (double r, double g, double b, out double h, out double s, out double v)
		{
			if (r < 0 || r > 1) throw new ArgumentOutOfRangeException ("r");
			if (g < 0 || g > 1) throw new ArgumentOutOfRangeException ("g");
			if (b < 0 || b > 1) throw new ArgumentOutOfRangeException ("b");
			
			double min = Math.Min (r, Math.Min (g, b));
			double max = Math.Max (r, Math.Max (g, b));
			
			v = max;
			if (v == 0) {
				h = 0;
				s = 0;
				return;
			}
			
			// normalize value to 1
			r /= v;
			g /= v;
			b /= v;
			
			min = Math.Min (r, Math.Min (g, b));
			max = Math.Max (r, Math.Max (g, b));
			
			double delta = max - min;
			s = delta;
			if (s == 0) {
				h = 0;
				return;
			}
			
			// normalize saturation to 1
			r = (r - min) / delta;
			g = (g - min) / delta;
			b = (b - min) / delta;
			
			if (max == r) {
				h = 0 + 60 * (g - b);
				if (h < 0) {
					h += 360;
				}
			} else if (max == g) {
				h = 120 + 60 * (b - r);
			} else {
				h = 240 + 60 * (r - g);
			}
		}
		
		static void HSVToRGB (double h, double s, double v, out double r, out double g, out double b)
		{
			if (h < 0 || h > 360) throw new ArgumentOutOfRangeException ("h");
			if (s < 0 || s > 1) throw new ArgumentOutOfRangeException ("s");
			if (v < 0 || v > 1) throw new ArgumentOutOfRangeException ("v");
			
			r = 0; 
			g = 0; 
			b = 0;

			if (s == 0) {
				r = v;
				g = v;
				b = v;
			} else {
				int secNum;
				double fracSec;
				double p, q, t;
				
				secNum = (int) Math.Floor(h / 60);
				fracSec = h/60 - secNum;

				p = v * (1 - s);
				q = v * (1 - s*fracSec);
				t = v * (1 - s*(1 - fracSec));
				
				switch (secNum) {
					case 0:
						r = v;
						g = t;
						b = p;
						break;
					case 1:
						r = q;
						g = v;
						b = p;
						break;
					case 2:
						r = p;
						g = v;
						b = t;
						break;
					case 3:
						r = p;
						g = q;
						b = v;
						break;
					case 4:
						r = t;
						g = p;
						b = v;
						break;
					case 5:
						r = v;
						g = p;
						b = q;
						break;
				}
			}
		}
//		
//		static void RGBToHSL (double r, double g, double b, out double h, out double s, out double l)
//		{
//			if (r < 0 || r > 1) throw new ArgumentOutOfRangeException ("r");
//			if (g < 0 || g > 1) throw new ArgumentOutOfRangeException ("g");
//			if (b < 0 || b > 1) throw new ArgumentOutOfRangeException ("b");
//			
//		}
//		
//		static void HSVToRGB (double h, double s, double v, out double r, out double g, out double b)
//		{
//			if (h < 0 || h > 360) throw new ArgumentOutOfRangeException ("h");
//			if (s < 0 || s > 1) throw new ArgumentOutOfRangeException ("s");
//			if (l < 0 || l > 1) throw new ArgumentOutOfRangeException ("l");
//		}
	}
}
