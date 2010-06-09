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
using System.Text;

using Mono.Unix;

using Cairo;
using GLib;
using Gdk;
using Gtk;
using Wnck;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace WorkspaceSwitcher
{
	public class WorkspaceSwitcherDockItem : IconDockItem
	{
		class Desk
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
			
			public bool IsVirual {
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
			
			internal Desk (string name, int number, Gdk.Rectangle area, Workspace parent)
			{
				Parent = parent;
				Area = area;
				Name = name;
				Number = number;
				neighbors = new Dictionary<MotionDirection, Desk> ();
			}
			
			internal Desk (Workspace parent) : this (parent.Name, parent.Number, new Gdk.Rectangle (0, 0, parent.Width, parent.Height), parent)
			{
			}
			
			public void Dispose ()
			{
				neighbors.Clear ();
			}
		}
		
		public event EventHandler DesksChanged;
		
		List<Desk> Desks = new List<Desk> ();
		Desk[,] DeskGrid = null;
		
		bool UseRotatedDeskGrid {
			get {
				// True if the layout has only one column and multiple rows
				return (DeskGrid != null && DeskGrid.GetLength (0) == 1 && DeskGrid.GetLength (1) > 1);
				//return (DeskGrid != null && DeskGrid.GetLength (0) < DeskGrid.GetLength (1));
			}
		}
		
		bool AreMultipleDesksAvailable {
			get {
				return Desks.Count () > 1;
			}
		}
		
		Desk ActiveDesk {
			get {
				return Desks.Find (d => d.IsActive);
			}
		}
		
		public override bool RotateWithDock {
			get { return true; }
		}
		
		public override bool Square {
			get { return false; }
		}
		
		public WorkspaceSwitcherDockItem ()
		{
			HoverText = Catalog.GetString ("Switch Desks");
			Icon = "workspace-switcher";
			
			UpdateDesks ();
			UpdateItem ();
			
			Wnck.Screen.Default.ActiveWorkspaceChanged += HandleWnckScreenDefaultActiveWorkspaceChanged;;
			Wnck.Screen.Default.ViewportsChanged += HandleWnckScreenDefaultViewportsChanged;
			Wnck.Screen.Default.WorkspaceCreated += HandleWnckScreenDefaultWorkspaceCreated;
			Wnck.Screen.Default.WorkspaceDestroyed += HandleWnckScreenDefaultWorkspaceDestroyed;
		}

		public override string UniqueID ()
		{
			return "WorkspaceSwitcher";
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (!AreMultipleDesksAvailable || DeskGrid == null)
				return ClickAnimation.None;

			//Console.WriteLine ("{0} , {1}", xPercent, yPercent);
			if (button == 1 && DeskGrid != null) {
				for (int row=0; row < DeskGrid.GetLength (0); row++) {
					for (int col=0; col < DeskGrid.GetLength (1); col++) {
						Cairo.Rectangle area = DeskAreaOnIcon (1, 1, row, col);
						if (xPercent >= area.X && xPercent < area.X + area.Width
							    && yPercent >= area.Y && yPercent < area.Y + area.Height) {
							DeskGrid [row,col].Activate ();
							break;
						}
					}
				}
			}
			return ClickAnimation.None;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (!AreMultipleDesksAvailable || DeskGrid == null)
				return;
			
			mod &= ~ModifierType.Mod2Mask; // Ignore NumLock
			mod &= ~ModifierType.LockMask; // Ignore CapsLock
			
			switch (direction) {
			case ScrollDirection.Down:
				if (mod == ModifierType.ShiftMask || DeskGrid.GetLength (1) == 1)
					SwitchDesk (Wnck.MotionDirection.Right);
				else
					SwitchDesk (Wnck.MotionDirection.Down);
				break;
			case ScrollDirection.Right:
				SwitchDesk (Wnck.MotionDirection.Right);
				break;
			case ScrollDirection.Up:
				if (mod == ModifierType.ShiftMask || DeskGrid.GetLength (1) == 1)
					SwitchDesk (Wnck.MotionDirection.Left);
				else
					SwitchDesk (Wnck.MotionDirection.Up);
				break;
			case ScrollDirection.Left:
				SwitchDesk (Wnck.MotionDirection.Left);
				break;
			}
		}

		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = new MenuList ();
			
			Desks.ForEach (d => {
				Desk desk = d;
				list[MenuListContainer.Actions].Add (new Docky.Menus.MenuItem (desk.Name, (desk.IsActive ? "desktop" : ""), (o, a) => desk.Activate ()));
			});

			return list;
		}		

		bool SwitchDesk (Wnck.MotionDirection direction)
		{
			Desk activedesk = Desks.Find (desk => desk.IsActive);

			Desk nextdesk = activedesk.GetNeighbor (direction);
			if (nextdesk != null) {
				nextdesk.Activate ();
				return true;
			}
			
			return false;
		}
		
		#region Update Switcher
		void UpdateDesks ()
		{
			lock (Desks)
			{
				DeskGrid = null;
				Desks.ForEach (desk => desk.Dispose());
				Desks.Clear ();
				
				string DeskNameFormatString;
				
				if (Wnck.Screen.Default.WorkspaceCount > 1)
					DeskNameFormatString = Catalog.GetString ("Desk") + " {0}";
				else
					DeskNameFormatString = Catalog.GetString ("Virtual Desk") + " {0}";
				
				foreach (Wnck.Workspace workspace in Wnck.Screen.Default.Workspaces) {
					if (workspace.IsVirtual) {
						int deskWidth = workspace.Screen.Width;
						int deskHeight = workspace.Screen.Height;
						int rows = workspace.Height / deskHeight;
						int columns = workspace.Width / deskWidth;
						
						for (int row = 0; row < rows; row++) {
							for (int col = 0; col < columns; col++) {
								int desknumber = (int) (columns * row + col + 1);
								Gdk.Rectangle area = new Gdk.Rectangle (col * deskWidth, row * deskHeight, deskWidth, deskHeight);
								
								Desk desk = new Desk (string.Format (DeskNameFormatString, desknumber), desknumber, area, workspace);
								desk.SetNeighbor (Wnck.MotionDirection.Down, Desks.Find (d => d.Area.X == area.X && (d.Area.Y - deskHeight == area.Y)));
								desk.SetNeighbor (Wnck.MotionDirection.Up, Desks.Find (d => d.Area.X == area.X && (d.Area.Y + deskHeight == area.Y)));
								desk.SetNeighbor (Wnck.MotionDirection.Right, Desks.Find (d => (d.Area.X - deskWidth == area.X) && d.Area.Y == area.Y));
								desk.SetNeighbor (Wnck.MotionDirection.Left, Desks.Find (d => (d.Area.X + deskWidth == area.X) && d.Area.Y == area.Y));
								Desks.Add (desk);
							}
						}
					} else {
						Desk desk = new Desk (workspace);
						desk.SetNeighbor (Wnck.MotionDirection.Down, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Down)));
						desk.SetNeighbor (Wnck.MotionDirection.Up, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Up)));
						desk.SetNeighbor (Wnck.MotionDirection.Right, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Right)));
						desk.SetNeighbor (Wnck.MotionDirection.Left, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Left)));
						Desks.Add (desk);
					}
				}

				//Console.WriteLine ("{0} Desks", Desks.Count ());
				
				Desk activedesk = Desks.Find (d => d.IsActive);
				if (activedesk != null)
					DeskGrid = activedesk.GetDeskGridLayout ();
			}
			
			if (DesksChanged != null)
				DesksChanged (new object (), EventArgs.Empty);
		}
		
		void UpdateItem ()
		{
			Desk activedesk = Desks.Find (desk => desk.IsActive);

			if (activedesk != null) {
				HoverText = activedesk.Name;
			} else {
				HoverText = Catalog.GetString ("Switch Desks");
			}
		}

		#endregion
		
		void HandleWnckScreenDefaultWorkspaceCreated (object o, WorkspaceCreatedArgs args)
		{
			UpdateDesks ();
			UpdateItem ();
			
			QueueRedraw ();
		}
		
		void HandleWnckScreenDefaultWorkspaceDestroyed (object o, WorkspaceDestroyedArgs args)
		{
			UpdateDesks ();
			UpdateItem ();
			
			QueueRedraw ();
		}
		
		void HandleWnckScreenDefaultActiveWorkspaceChanged (object o, ActiveWorkspaceChangedArgs args)
		{
			if (ActiveDesk.Parent != args.PreviousWorkspace)
				DeskGrid = ActiveDesk.GetDeskGridLayout ();
			UpdateItem ();
			
			QueueRedraw ();
		}

		void HandleWnckScreenDefaultViewportsChanged (object sender, EventArgs e)
		{
			UpdateDesks ();
			UpdateItem ();
			
			QueueRedraw ();
		}

		#region Drawing
		protected override DockySurface CreateIconBuffer (DockySurface model, int size)
		{
			if (DeskGrid == null) 
				return new DockySurface (size, size, model);
			
			//double ratio = (double) Wnck.Screen.Default.Width / Wnck.Screen.Default.Height;
			int width;
			
			if (UseRotatedDeskGrid)
				width = Math.Min (IconSize * 4,	(int)(size * DeskGrid.GetLength (1)));
			else
				width = Math.Min (IconSize * 4,	(int)(size * DeskGrid.GetLength (0)));
			
			return new DockySurface (width, size, model);
		}		
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			if (DeskGrid == null)
				return;
			
			int cols = DeskGrid.GetLength (0);
			int rows = DeskGrid.GetLength (1);
			
			Gdk.Color gdkColor = Style.Backgrounds [(int) Gtk.StateType.Selected];
			Cairo.Color selection_color = new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											0.5);
			Context cr = surface.Context;

			cr.AlphaPaint ();
			
			LinearGradient lg = new LinearGradient (0, 0, 0, surface.Height);
			lg.AddColorStop (0, new Cairo.Color (.35, .35, .35, .6));
			lg.AddColorStop (1, new Cairo.Color (.05, .05, .05, .7));
			
			for (int x = 0; x < cols; x++) {
				for (int y = 0; y < rows; y++) {
					Cairo.Rectangle area = DeskAreaOnIcon (surface.Width, surface.Height, x, y);
					cr.Rectangle (area.X, area.Y, area.Width, area.Height);
					cr.Pattern = lg;
					cr.FillPreserve ();
					if (DeskGrid[x,y].IsActive) {
						cr.Color = selection_color;
						cr.Fill ();
						if (area.Width >= 16 && area.Height >= 16) {
							using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("desktop", (int) area.Width, (int) area.Height)) {
								Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (int) area.X + (area.Width - pbuf.Width) / 2, (int) area.Y + (area.Height - pbuf.Height) / 2);
								cr.Paint ();
							}
						}
					}
					cr.NewPath ();
				}
			}
			
			lg.Destroy ();
			
			for (int x = 0; x < cols; x++) {
				for (int y = 0; y < rows; y++) {
					Cairo.Rectangle area = DeskAreaOnIcon (surface.Width, surface.Height, x, y);
					cr.Rectangle (area.X, area.Y, area.Width, area.Height);
				}
			}
			
			lg = new LinearGradient (0, 0, 0, surface.Height);
			lg.AddColorStop (0, new Cairo.Color (.95, .95, .95, .7));
			lg.AddColorStop (1, new Cairo.Color (.5, .5, .5, .7));
			cr.Pattern = lg;
			cr.StrokePreserve ();
			lg.Destroy ();
		}
		
		Cairo.Rectangle DeskAreaOnIcon (int width, int height, int column, int row)
		{
			double BorderPercent = 0.05;
			double Border = height * BorderPercent;
			double boxWidth, boxHeight;
			Cairo.Rectangle area;
			
			if (UseRotatedDeskGrid) {
				boxWidth = (width - 2 * Border) / DeskGrid.GetLength (1);
				boxHeight = (height - 2 * Border) / DeskGrid.GetLength (0);
				area = new Cairo.Rectangle (boxWidth * row + Border, boxHeight * column + Border, boxWidth, boxHeight);
			} else {
				boxWidth = (width - 2 * Border) / DeskGrid.GetLength (0);
				boxHeight = (height - 2 * Border) / DeskGrid.GetLength (1);
				area = new Cairo.Rectangle (boxWidth * column + Border, boxHeight * row + Border, boxWidth, boxHeight);
			}
			
			return area;
		}
		#endregion
		
		#region IDisposable implementation
		public override void Dispose ()
		{
			Wnck.Screen.Default.ActiveWorkspaceChanged -= HandleWnckScreenDefaultActiveWorkspaceChanged;
			Wnck.Screen.Default.ViewportsChanged -= HandleWnckScreenDefaultViewportsChanged;
			Wnck.Screen.Default.WorkspaceCreated -= HandleWnckScreenDefaultWorkspaceCreated;
			Wnck.Screen.Default.WorkspaceDestroyed -= HandleWnckScreenDefaultWorkspaceDestroyed;
			
			DeskGrid = null;
			Desks.ForEach (desk => desk.Dispose ());
			Desks.Clear ();
			
			base.Dispose ();
		}

		#endregion
		
	}
}
