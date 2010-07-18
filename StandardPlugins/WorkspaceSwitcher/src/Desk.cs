//  
//  Copyright (C) 2010 Rico Tzschichholz
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
using System.Collections.ObjectModel;
using System.Linq;

using Gdk;
using Wnck;

namespace WorkspaceSwitcher
{
	internal class Desk
	{
		public Wnck.Workspace Parent { get; private set; }
		public string Name { get; private set; }
		public int Number { get; private set; }
		public Gdk.Rectangle Area { get; private set; }

		Dictionary<Wnck.MotionDirection, Desk> neighbors;
		
		static Wnck.MotionDirection OppositeDirection (Wnck.MotionDirection direction)
		{
			switch (direction) {
			case MotionDirection.Down: return MotionDirection.Up;
			case MotionDirection.Up: return MotionDirection.Down;
			case MotionDirection.Left: return MotionDirection.Right;
			case MotionDirection.Right: default: return MotionDirection.Left;
			}
		}
		
		public bool IsVirtual {
			get {
				return Parent.IsVirtual;
			}
		}
		
		public bool IsActive {
			get {
				if (!Parent.IsVirtual)
					return Wnck.Screen.Default.ActiveWorkspace == Parent;
				else
					return Wnck.Screen.Default.ActiveWorkspace.ViewportX == Area.X && Wnck.Screen.Default.ActiveWorkspace.ViewportY == Area.Y;
			}
		}
		
		Desk GetUpperLeftDesk ()
		{
			Desk upperleft, next;
			upperleft = this;
			while ((next = upperleft.GetNeighbor (Wnck.MotionDirection.Up)) != null)
				upperleft = next;
			while ((next = upperleft.GetNeighbor (Wnck.MotionDirection.Left)) != null)
				upperleft = next;
			return upperleft;
		}
		
		Gdk.Point GetDeskGridSize ()
		{
			int cols = 1; 
			int rows = 1; 
			Desk bottomright, next;
			next = GetUpperLeftDesk ();
			while ((bottomright = next.GetNeighbor (Wnck.MotionDirection.Down)) != null) {
				next = bottomright;
				rows++;
			}
			while ((bottomright = next.GetNeighbor (Wnck.MotionDirection.Right)) != null) {
				next = bottomright;
				cols++;
			}
			return new Gdk.Point (cols, rows);
		}
		
		public Desk [,] GetDeskGridLayout ()
		{
			Desk next, desk = GetUpperLeftDesk ();
			Gdk.Point gridsize = GetDeskGridSize ();
			Desk [,] grid = new Desk [gridsize.X, gridsize.Y];
			grid [0, 0] = desk;
			int x = 0;
			for (int y = 0; y < gridsize.Y; y++) {
				x = 0;
				while ((next = desk.GetNeighbor (Wnck.MotionDirection.Right)) != null) {
					desk = next;
					x++;
					if (gridsize.X - 1 < x)
						break;
					grid [x, y] = desk;
				}
				if (gridsize.Y - 1 > y) {
					desk = grid [0, y].GetNeighbor (Wnck.MotionDirection.Down);
					grid [0, y+1] = desk;
				}
			}
			return grid;
		}
		
		public void Activate ()
		{
			if (Parent.Screen.ActiveWorkspace != Parent)
				Parent.Activate (Gtk.Global.CurrentEventTime);
			if (Parent.IsVirtual)
				Parent.Screen.MoveViewport (Area.X, Area.Y);
		}
		
		public void SetNeighbor (Wnck.MotionDirection direction, Desk newneighbor)
		{
			Desk oldneighbor = GetNeighbor (direction);
			if (oldneighbor != null && oldneighbor != newneighbor) {
				oldneighbor.SetNeighbor (OppositeDirection (direction), null);
				neighbors.Remove (direction);
			}
			if (oldneighbor != newneighbor && newneighbor != null) {
				neighbors.Add (direction, newneighbor);
				newneighbor.SetNeighbor (OppositeDirection (direction), this);
			}
		}
		
		public Desk GetNeighbor (Wnck.MotionDirection direction)
		{
			Desk desk;
			neighbors.TryGetValue (direction, out desk);
			return desk;
		}
		
		public Desk (string name, int number, Gdk.Rectangle area, Workspace parent)
		{
			Parent = parent;
			Area = area;
			Name = name;
			Number = number;
			neighbors = new Dictionary<MotionDirection, Desk> ();
		}
		
		public Desk (Workspace parent) : this (parent.Name, parent.Number, new Gdk.Rectangle (0, 0, parent.Width, parent.Height), parent)
		{
		}
		
		public void Dispose ()
		{
			neighbors.Clear ();
		}
	}
}
