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
using System.Globalization;

using Cairo;
using Gdk;
using Gtk;

using Docky.CairoHelper;
using Docky.Painters;
using Docky.Services;

namespace Clock
{
	public class CalendarPainter : AbstractDockPainter
	{
		const double lowlight = .35;
		
		int LineHeight { get; set; }
		
		DateTime paint_time;
		
		DateTime startDate;
		public DateTime StartDate
		{
			get {
				return startDate;
			}
			set {
				startDate = value;
				paint_time = DateTime.Now.Date.AddDays (-100);
				QueueRepaint ();
			}
		}
		
		DateTime CalendarStartDate {
			get {
				return StartDate.AddDays ((int) DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek - (int) StartDate.DayOfWeek);
			}
		}
		
		public CalendarPainter () : base ()
		{
			StartDate = DateTime.Today;
		}
		
		#region IDockPainter implementation 

		protected override void OnScrolled (Gdk.ScrollDirection direction, int x, int y, Gdk.ModifierType type)
		{
			if (direction == Gdk.ScrollDirection.Up)
				StartDate = StartDate.AddDays (-7);
			else if (direction == Gdk.ScrollDirection.Down)
				StartDate = StartDate.AddDays (7);
		}
		
		public override int MinimumHeight {
			get {
				return 60;
			}
		}
		
		public override int MinimumWidth {
			get {
				return 630;
			}
		}
		
		protected override void PaintSurface (DockySurface surface)
		{
			if (paint_time.Date == DateTime.Now.Date)
				return;
			
			surface.Clear ();
			
			LineHeight = Math.Max (12, Allocation.Height / 4);
			
			paint_time = DateTime.Now;
			int height = Allocation.Height / LineHeight;
			RenderHeader (surface);
			for (int i = 1; i < height; i++)
				RenderLine (surface, i);
		}
		
		#endregion 
		
		void RenderHeader (DockySurface surface)
		{
			Context cr = surface.Context;
			int centerLine = LineHeight + ((Allocation.Height % LineHeight) / 2);
			int offsetSize = Allocation.Width / 9;
			
			DateTime day = CalendarStartDate;
			
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			
			layout.FontDescription = new Gtk.Style().FontDescription;
			layout.FontDescription.Weight = Pango.Weight.Bold;
			layout.Ellipsize = Pango.EllipsizeMode.None;
			layout.Width = Pango.Units.FromPixels (offsetSize);
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (0.625 * LineHeight));
			
			cr.Color = new Cairo.Color (1, 1, 1, .5);
			for (int i = 1; i < 8; i++) {
				layout.SetText (string.Format ("{0}", day.ToString ("ddd").ToUpper ()));
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (offsetSize * i + (offsetSize - inkRect.Width) / 2, centerLine - logicalRect.Height);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				day = day.AddDays (1);
			}
		}
		
		void RenderLine (DockySurface surface, int line)
		{
			Context cr = surface.Context;
			DateTime lineStart = CalendarStartDate.AddDays ((line - 1) * 7);
			int offsetSize = Allocation.Width / 9;
			int centerLine = LineHeight + LineHeight * line + ((Allocation.Height % LineHeight) / 2);
			int dayOffset = 0;
			
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			Pango.Rectangle inkRect, logicalRect;
			
			layout.FontDescription = new Gtk.Style().FontDescription;
			layout.Ellipsize = Pango.EllipsizeMode.None;
			layout.Width = Pango.Units.FromPixels (offsetSize);
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (0.625 * LineHeight));
			
			for (int i = 0; i < 9; i++) {
				layout.FontDescription.Weight = Pango.Weight.Bold;
				
				if (i == 8) {
					cr.Color = new Cairo.Color (1, 1, 1, lowlight);
					layout.SetText (string.Format ("{0}", lineStart.AddDays (6).ToString ("MMM").ToUpper ()));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (offsetSize * i, centerLine - logicalRect.Height);
				} else if (i == 0) {
					cr.Color = new Cairo.Color (1, 1, 1, lowlight);
					int woy = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear (lineStart.AddDays (6), 
					                                                             DateTimeFormatInfo.CurrentInfo.CalendarWeekRule, 
					                                                             DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek);
					layout.SetText (string.Format ("W{0:00}", woy));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (offsetSize - inkRect.Width, centerLine - logicalRect.Height);
				} else {
					DateTime day = lineStart.AddDays (dayOffset);
					
					if (day.Month == CalendarStartDate.AddDays (6).Month)
						cr.Color = new Cairo.Color (1, 1, 1);
					else
						cr.Color = new Cairo.Color (1, 1, 1, .8);
					
					if (day.Date == DateTime.Today)
					{
						Gdk.Color color = Style.Backgrounds [(int) StateType.Selected].SetMinimumValue (100);
						cr.Color = new Cairo.Color ((double) color.Red / ushort.MaxValue,
													(double) color.Green / ushort.MaxValue,
													(double) color.Blue / ushort.MaxValue,
													1.0);
					} else {
						layout.FontDescription.Weight = Pango.Weight.Normal;
					}
					dayOffset++;
					
					layout.SetText (string.Format ("{0:00}", day.Day));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (offsetSize * i + (offsetSize - inkRect.Width) / 2, centerLine - logicalRect.Height);
				}
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
			}
		}
		
		protected override void OnShown ()
		{
			StartDate = DateTime.Today;
		}
	}
}
