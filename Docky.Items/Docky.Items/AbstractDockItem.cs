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
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

using Docky;
using Docky.CairoHelper;
using Docky.Painters;
using Docky.Services;

namespace Docky.Items
{


	public abstract class AbstractDockItem : IDisposable
	{
		string hover_text;
		bool redraw;
		DockySurface text_buffer;
		DockySurface[] icon_buffers;
		Cairo.Color? average_color;
		ActivityIndicator indicator;
		ItemState state;
		Dictionary<ItemState, DateTime> state_times;
		
		public event EventHandler HoverTextChanged;
		public event EventHandler<PaintNeededEventArgs> PaintNeeded;
		public event EventHandler<PainterRequestEventArgs> PainterRequest;
		
		public ActivityIndicator Indicator {
			get {
				return indicator; 
			}
			protected set {
				if (value == indicator)
					return;
				indicator = value;
				OnPaintNeeded ();
			}
		}
		
		public ItemState State {
			get { return state; }
			protected set {
				if (state == value)
					return;
				
				ItemState difference = value ^ state;
				
				SetStateTime (difference, DateTime.UtcNow);
				state = value;
				OnPaintNeeded ();
			}
		}
		
		public DateTime AddTime {
			get; internal set;
		}
		
		public DateTime LastClick {
			get; private set;
		}
		
		public ClickAnimation ClickAnimation {
			get; private set;
		}
		
		public virtual bool RotateWithDock {
			get { return false; }
		}
		
		public virtual bool Square {
			get { return true; }
		}
		
		public virtual bool Zoom {
			get { return true; }
		}
		
		public string HoverText {
			get {
				return hover_text;
			}
			protected set {
				if (hover_text == value)
					return;
				
				hover_text = value;
				text_buffer = ResetBuffer (text_buffer);
				OnHoverTextChanged ();
			}
		}
		
		public virtual string ShortName {
			get {
				return HoverText;
			}
		}
		
		public int Position {
			get;
			set;
		}
		
		public AbstractDockItemProvider Owner {
			get;
			internal set;
		}
		
		public AbstractDockItem ()
		{
			icon_buffers = new DockySurface[2];
			state_times = new Dictionary<ItemState, DateTime> ();
			Gtk.IconTheme.Default.Changed += HandleIconThemeChanged;
			
			AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
		}

		void HandleProcessExit (object sender, EventArgs e)
		{
			Dispose ();
		}
		
		public DateTime StateSetTime (ItemState state)
		{
			if (!state_times.ContainsKey (state))
				return new DateTime (0);
			return state_times [state];
		}
		
		void SetStateTime (ItemState state, DateTime time)
		{
			foreach (ItemState i in Enum.GetValues (typeof(ItemState)).Cast<ItemState> ()) {
				if ((state & i) == i)
					state_times[i] = time;
			}
		}
		
		public abstract string UniqueID ();
		
		public Cairo.Color AverageColor ()
		{
			if (icon_buffers[0] == null)
				return new Cairo.Color (1, 1, 1, 1);
			
			if (average_color.HasValue)
				return average_color.Value;
				
			ImageSurface sr = new ImageSurface (Format.ARGB32, icon_buffers[0].Width, icon_buffers[0].Height);
			using (Context cr = new Context (sr)) {
				cr.Operator = Operator.Source;
				icon_buffers[0].Internal.Show (cr, 0, 0);
			}
			
			sr.Flush ();
			
			byte [] data;
			try {
				data = sr.Data;
			} catch {
				return new Cairo.Color (1, 1, 1, 1);
			}
			byte r, g, b;
			
			double rTotal = 0;
			double gTotal = 0;
			double bTotal = 0;
			
			unsafe {
				fixed (byte* dataSrc = data) {
					byte* dataPtr = dataSrc;
					
					for (int i = 0; i < data.Length - 3; i += 4) {
						b = dataPtr [0];
						g = dataPtr [1];
						r = dataPtr [2];
						
						byte max = Math.Max (r, Math.Max (g, b));
						byte min = Math.Min (r, Math.Min (g, b));
						double delta = max - min;
						
						double sat;
						if (delta == 0) {
							sat = 0;
						} else {
							sat = delta / max;
						}
						double score = .2 + .8 * sat;
						
						rTotal += r * score;
						gTotal += g * score;
						bTotal += b * score;
						
						dataPtr += 4;
					}
				}
			}
			
			double pixelCount = icon_buffers[0].Width * icon_buffers[0].Height * byte.MaxValue;
			
			sr.Destroy ();
			
			average_color = new Cairo.Color (rTotal / pixelCount, 
			                                 gTotal / pixelCount, 
			                                 bTotal / pixelCount)
				.SetValue (.8)
				.MultiplySaturation (1.15);
			
			return average_color.Value;
		}
		
		#region Drop Handling
		public bool CanAcceptDrop (IEnumerable<string> uris)
		{
			bool result = false;
			try {
				result = OnCanAcceptDrop (uris);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			return result;
		}
		
		public bool AcceptDrop (IEnumerable<string> uris)
		{
			bool result = false;
			try {
				result = OnAcceptDrop (uris);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			return result;
		}
		
		public bool CanAcceptDrop (AbstractDockItem item)
		{
			bool result = false;
			try {
				result = OnCanAcceptDrop (item);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			return result;
		}
		
		public bool AcceptDrop (AbstractDockItem item)
		{
			bool result = false;
			try {
				result = OnAcceptDrop (item);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			return result;
		}
		
		protected virtual bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			return false;
		}
		
		protected virtual bool OnAcceptDrop (IEnumerable<string> uris)
		{
			return false;
		}
		
		protected virtual bool OnCanAcceptDrop (AbstractDockItem item)
		{
			return false;
		}
		
		protected virtual bool OnAcceptDrop (AbstractDockItem item)
		{
			return false;
		}
		#endregion
		
		#region Input Handling
		public void Clicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			try {
				ClickAnimation = OnClicked (button, mod, xPercent, yPercent);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
				ClickAnimation = ClickAnimation.Darken;
			}
			
			LastClick = DateTime.UtcNow;
		}
		
		protected virtual ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			return ClickAnimation.None;
		}
		
		public void Scrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			try {
				OnScrolled (direction, mod);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
		}
		#endregion
		
		#region Buffer Handling
		public DockySurface IconSurface (DockySurface model, int size)
		{
			if (!redraw) {
				// look for something nice to return
				foreach (DockySurface surface in icon_buffers) {
					if (surface == null)
						continue;
					if (surface.Width == size || surface.Height == size)
						return surface;
				}
			}
			
			int i = -1;
			for (int x = 0; x < icon_buffers.Length; x++) {
				if (icon_buffers[x] != null && (icon_buffers[x].Width == size || icon_buffers[x].Height == size)) {
					i = x;
					break;
				}
				if (i == -1 && icon_buffers[x] == null)
					i = x;
			}
			
			i = Math.Max (i, 0);
			
			if (icon_buffers[i] == null || (icon_buffers[i].Width != size && icon_buffers[i].Height != size)) {
				if (icon_buffers[i] != null)
					icon_buffers[i] = ResetBuffer (icon_buffers[i]);
					
				try {
					icon_buffers[i] = CreateIconBuffer (model, size);
				} catch (Exception e) {
					Log<AbstractDockItem>.Error (e.Message);
					Log<AbstractDockItem>.Debug (e.StackTrace);
					icon_buffers[i] = new DockySurface (size, size, model);
				}
			}
			
			average_color = null;
			
			icon_buffers[i].Clear ();
			icon_buffers[i].ResetContext ();
			
			try {
				PaintIconSurface (icon_buffers[i]);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			
			redraw = false;
			
			return icon_buffers[i];
		}
		
		protected virtual DockySurface CreateIconBuffer (DockySurface model, int size)
		{
			return new DockySurface (size, size, model);
		}
		
		protected abstract void PaintIconSurface (DockySurface surface);
		
		public DockySurface HoverTextSurface (DockySurface model, Style style)
		{
			if (string.IsNullOrEmpty (HoverText))
				return null;
			
			if (text_buffer == null) {
			
				Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
				
				layout.FontDescription = style.FontDescription;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (10);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.Width = Pango.Units.FromPixels (500);
				
				layout.SetText (HoverText);
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				int textWidth = inkRect.Width;
				int textHeight = logicalRect.Height;
				int buffer = 12;
				text_buffer = new DockySurface (textWidth + buffer, textHeight + buffer, model);
				
				Cairo.Context cr = text_buffer.Context;
				cr.RoundedRectangle (.5, .5, text_buffer.Width - 1, text_buffer.Height - 1, buffer / 2);
				cr.Color = new Cairo.Color (0, 0, 0, .8);
				cr.FillPreserve ();
				
				cr.Color = new Cairo.Color (1, 1, 1, .3);
				cr.LineWidth = 1;
				cr.Stroke ();
				
				cr.MoveTo (buffer / 2, buffer / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Color = new Cairo.Color (1, 1, 1);
				cr.Fill ();
				
				layout.Dispose ();
			}
			
			return text_buffer;
		}
		
		public void ResetBuffers ()
		{
			for (int i = 0; i < icon_buffers.Length; i++)
				icon_buffers[i] = ResetBuffer (icon_buffers[i]);
			text_buffer = ResetBuffer (text_buffer);
		}
		
		protected void QueueRedraw ()
		{
			// try to ensure we dont clobber the buffers
			// during an already in-progress repaint
			GLib.Idle.Add (delegate {
				if (!Square) {
					ResetBuffers ();
				}
				redraw = true;
				OnPaintNeeded ();
				return false;
			});
		}
		
		DockySurface ResetBuffer (DockySurface buffer)
		{
			if (buffer != null) {
				buffer.Dispose ();
			}
			
			return null;
		}
		#endregion
		
		public virtual Docky.Menus.MenuList GetMenuItems ()
		{
			return new Docky.Menus.MenuList ();
		}
		
		public void SetScreenRegion (Gdk.Screen screen, Gdk.Rectangle region)
		{
			try {
				OnSetScreenRegion (screen, region);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnSetScreenRegion (Gdk.Screen screen, Gdk.Rectangle region)
		{
			return;
		}
		
		void OnHoverTextChanged ()
		{
			if (HoverTextChanged != null)
				HoverTextChanged (this, EventArgs.Empty);
		}
		
		void HandleIconThemeChanged (object o, EventArgs e)
		{
			QueueRedraw ();
		}
		
		protected void OnPaintNeeded ()
		{
			if (PaintNeeded != null) {
				PaintNeededEventArgs args = new PaintNeededEventArgs { DrawLength = TimeSpan.MinValue };
				
				PaintNeeded (this, args);
			}
		}
		
		protected void ShowPainter (AbstractDockPainter painter)
		{
			if (PainterRequest != null && painter != null) {
				PainterRequest (this, new PainterRequestEventArgs (this, painter, ShowHideType.Show));
			}
		}
		
		protected void HidePainter (AbstractDockPainter painter)
		{
			if (PainterRequest != null && painter != null) {
				PainterRequest (this, new PainterRequestEventArgs (this, painter, ShowHideType.Hide));
			}
		}

		#region IDisposable implementation
		public virtual void Dispose ()
		{
			AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
			Gtk.IconTheme.Default.Changed -= HandleIconThemeChanged;
			ResetBuffers ();
		}

		#endregion

	}
}
