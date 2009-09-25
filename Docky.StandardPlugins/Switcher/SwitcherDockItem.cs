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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Cairo;
using Gdk;
using GLib;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Windowing;

namespace Switcher
{
	public class SwitcherDockItem : AbstractDockItem
	{
		const double BorderPercent = .05;
		int current_size;
		
		int Rows {
			get {
				return Layout.GetUpperBound (0) + 1;
			}
		}
		
		int Columns {
			get {
				return Layout.GetUpperBound (1) + 1;
			}
		}
		
		Viewport[,] Layout {
			get {
				return ScreenUtils.ViewportLayout ();
			}
		}
		
		IEnumerable<Viewport> OrderedViewports {
			get {
				for (int i=0; i < Rows; i++) {
					for (int j=0; j < Columns; j++) {
						yield return Layout [i,j];
					}
				}
			}
		}
		
		public override string UniqueID ()
		{
			return "Switcher";
		}

		public SwitcherDockItem ()
		{
			ScreenUtils.ViewportsChanged += HandleViewportsChanged;
			HoverText = "Workspace Switcher";
		}
		
		void HandleViewportsChanged (object sender, EventArgs args)
		{
			QueueRedraw ();
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				
				Gdk.Point point = new Gdk.Point ((int) (current_size * xPercent), 
				                                 (int) (current_size * yPercent));
				for (int i = 0; i < Rows; i++)
					for (int j = 0; j < Columns; j++)
						if (ViewportAreaOnIcon (i, j).Contains (point))
							Layout [i, j].Present ();
			}
			
			return ClickAnimation.None;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (direction != ScrollDirection.Up && direction != ScrollDirection.Down)
				return;
			
			List<Viewport> viewport_list = OrderedViewports.ToList ();
			Viewport current = ScreenUtils.ActiveViewport;
			int index = viewport_list.IndexOf (current);
			int newIndex;
			if (direction == ScrollDirection.Up)
				newIndex = (index - 1) % viewport_list.Count;
			else
				newIndex = (index + 1) % viewport_list.Count;
			if (newIndex >= 0 && newIndex < viewport_list.Count)
				viewport_list [newIndex].Present ();
			
			base.OnScrolled (direction, mod);
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			int size = Math.Min (surface.Width, surface.Height);
			current_size = size;
			Context cr = surface.Context;
			
			LinearGradient lg = new LinearGradient (0, 0, 0, size);
			lg.AddColorStop (0, new Cairo.Color (.35, .35, .35, .7));
			lg.AddColorStop (1, new Cairo.Color (.05, .05, .05, .8));
			
			for (int i=0; i < Rows; i++) {
				for (int j=0; j < Columns; j++) {
					Gdk.Rectangle area = ViewportAreaOnIcon (i, j);
					cr.Rectangle (area.X, area.Y, area.Width, area.Height);
					cr.Pattern = lg;
					cr.FillPreserve ();
					if (Layout [i,j] == ScreenUtils.ActiveViewport) {
						cr.Color = new Cairo.Color (1, 1, 1, .3);
						cr.Fill ();
					}
					cr.NewPath ();
				}
			}
			
			lg.Destroy ();
			
			for (int i=0; i < Rows; i++) {
				for (int j=0; j < Columns; j++) {
					Gdk.Rectangle area = ViewportAreaOnIcon (i, j);
					cr.Rectangle (area.X, area.Y, area.Width, area.Height);
				}
			}
			
			lg = new LinearGradient (0, 0, 0, size);
			lg.AddColorStop (0, new Cairo.Color (.95, .95, .95, .8));
			lg.AddColorStop (1, new Cairo.Color (.5, .5, .5, .8));
			cr.Pattern = lg;
			cr.StrokePreserve ();
			lg.Destroy ();
		}
		
		Gdk.Rectangle ViewportAreaOnIcon (int row, int column)
		{
			int Border = (int) (current_size * BorderPercent);
				
			int boxHeight = (current_size - 2 * Border) / Rows;
			int boxWidth = (current_size - 2 * Border) / Columns;
			
			double ratio = LayoutUtils.MonitorGeometry ().Height / (double) LayoutUtils.MonitorGeometry ().Width;
			
			if (ratio < 1)
				boxHeight = (int) (Math.Round (boxHeight * ratio));
			else
				boxWidth = (int) (Math.Round (boxWidth / ratio));
			
			return new Gdk.Rectangle (((current_size - boxWidth * Columns) /2) + boxWidth * column, 
			                          ((current_size - boxHeight * Rows) / 2) + boxHeight * row, 
			                          boxWidth, 
			                          boxHeight);
		}

		public override void Dispose ()
		{
			ScreenUtils.ViewportsChanged -= HandleViewportsChanged;
			base.Dispose ();
		}
	}
}
