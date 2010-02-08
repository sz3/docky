//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Rico Tzschichholz
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
using System.Reflection;
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
	public enum MenuButton {
		None   = 0,
		Left   = 1 << 0,
		Middle = 1 << 1,
		Right  = 1 << 2,
	}
	
	public abstract class AbstractDockItem : IDisposable
	{
		string hover_text, remote_text;
		string badge_text, remote_badge_text;
		bool[] redraw;
		DockySurface text_buffer;
		DockySurface[] icon_buffers;
		Cairo.Color? average_color;
		Cairo.Color[] badgeColors;
		ActivityIndicator indicator;
		ItemState state;
		Dictionary<ItemState, DateTime> state_times;
		
		public event EventHandler HoverTextChanged;
		public event EventHandler<PaintNeededEventArgs> PaintNeeded;
		public event EventHandler<PainterRequestEventArgs> PainterRequest;
		
		/// <summary>
		/// Indicates if the item should have zero, one, or multiple indicate dots below it
		/// </summary>
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
		
		/// <summary>
		/// The current state of the item. Indicates wait, urgency, and active
		/// </summary>
		public ItemState State {
			get { return state; }
			set {
				if (state == value)
					return;
				
				ItemState difference = value ^ state;
				
				SetStateTime (difference, DateTime.UtcNow);
				state = value;
				OnPaintNeeded ();
			}
		}
		
		/// <summary>
		/// The time at which the item was added (and made visible) in the item provider
		/// </summary>
		public DateTime AddTime {
			get; internal set;
		}
		
		/// <summary>
		/// The last time this item was clicked
		/// </summary>
		public DateTime LastClick {
			get; private set;
		}
		
		/// <summary>
		/// Which mouse buttons trigger the menu
		/// </summary>
		public virtual MenuButton MenuButton {
			get { return MenuButton.Right; }
		}
		
		/// <summary>
		/// The animation with which the item wishes to display
		/// </summary>
		public ClickAnimation ClickAnimation {
			get; private set;
		}
		
		/// <summary>
		/// Determines if the item will be rendered at multiple sizes when zoomed. Not advised for icons who
		/// use a themed icon as this may look different at different sizes
		/// </summary>
		public virtual bool ScalableRendering {
			get;
			protected set;
		}
		
		/// <summary>
		/// Causes an items "upward" orientation to always face in if true
		/// </summary>
		public virtual bool RotateWithDock {
			get { return false; }
		}
		
		/// <summary>
		/// Provices a performance optimization if the icon is known to be square or can be treated as such
		/// </summary>
		public virtual bool Square {
			get { return true; }
		}
		
		/// <summary>
		/// Indicates that an item should be zoomed on hover. Reasons for not zooming include separators and xembed
		/// </summary>
		public virtual bool Zoom {
			get { return true; }
		}
		
		/// <summary>
		/// The text displayed when an item is hovered
		/// </summary>
		public string HoverText {
			get {
				return string.IsNullOrEmpty (remote_text) ? hover_text : remote_text;
			}
			protected set {
				if (hover_text == value)
					return;
				
				hover_text = value;
				text_buffer = ResetBuffer (text_buffer);
				OnHoverTextChanged ();
			}
		}
		
		/// <summary>
		/// Future use for displaying a header in menus
		/// </summary>
		public virtual string ShortName {
			get {
				return HoverText;
			}
		}
		
		/// <summary>
		/// The text displayed over a badge
		/// </summary>
		public string BadgeText {
			get {
				return string.IsNullOrEmpty (remote_badge_text) ? badge_text : remote_badge_text;
			}
			protected set {
				if (badge_text == value)
					return;
				badge_text = value;
				QueueRedraw ();
			}
		}
		
		/// <summary>
		/// The position of the icon, non-providers should not modify this value!
		/// </summary>
		int position;
		public int Position {
			get { 
				return position; 
			}
			set {
				LastPosition = position;
				position = value;
				SetStateTime (ItemState.Move, DateTime.UtcNow);
			}
		}
		
		int last_position;
		public int LastPosition {
			get {
				return last_position;
			}
			private set {
				last_position = value;
			}
		}
		
		/// <summary>
		/// The owning provider of the item
		/// </summary>
		public AbstractDockItemProvider Owner {
			get;
			internal set;
		}
		
		/// <summary>
		/// 
		/// </summary>
		public Docky.Menus.MenuList RemoteMenuItems {
			get;
			private set;
		}
		
		public const int HoverTextHeight = 26;
		
		protected int IconSize { get; private set; }
		
		protected bool IsSmall { get { return IconSize < 32; } }
		
		public AbstractDockItem ()
		{
			ScalableRendering = true;
			icon_buffers = new DockySurface[2];
			badgeColors = new Cairo.Color[2];
			redraw = new bool[2];
			state_times = new Dictionary<ItemState, DateTime> ();
			foreach (ItemState val in Enum.GetValues (typeof(ItemState)))
				state_times[val] = new DateTime (0);
			Gtk.IconTheme.Default.Changed += HandleIconThemeChanged;
			RemoteMenuItems = new Docky.Menus.MenuList ();
			
			AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
		}
		
		public void SetRemoteBadgeText (string text)
		{
			remote_badge_text = text;
			QueueRedraw ();
		}

		public void SetRemoteText (string text)
		{
			remote_text = text;
			text_buffer = ResetBuffer (text_buffer);
			OnHoverTextChanged ();
		}

		void HandleProcessExit (object sender, EventArgs e)
		{
			Dispose ();
		}
		
		/// <summary>
		/// Fetches the time at which a particular state was last set or unset
		/// </summary>
		/// <param name="state">
		/// A <see cref="ItemState"/>
		/// </param>
		/// <returns>
		/// The <see cref="DateTime"/> at which the state was changed
		/// </returns>
		public DateTime StateSetTime (ItemState state)
		{
			return state_times [state];
		}
		
		void SetStateTime (ItemState state, DateTime time)
		{
			foreach (ItemState i in Enum.GetValues (typeof(ItemState)).Cast<ItemState> ()) {
				if ((state & i) == i)
					state_times [i] = time;
			}
		}
		
		/// <summary>
		/// A string which uniquely identifies the instance of the current item and is stable across process runs.
		/// This string will be used to sort the item on the dock, failure to properly create a unique string
		/// will result in improper sorting.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public abstract string UniqueID ();
		
		/// <summary>
		/// The average color of the pixels in the current represenation of the item, 
		/// weighted for saturation and opacity.
		/// </summary>
		/// <returns>
		/// A <see cref="Cairo.Color"/>
		/// </returns>
		public Cairo.Color AverageColor ()
		{
			if (icon_buffers [0] == null)
				return new Cairo.Color (1, 1, 1, 1);
			
			if (average_color.HasValue)
				return average_color.Value;
				
			ImageSurface sr = new ImageSurface (Format.ARGB32, icon_buffers [0].Width, icon_buffers [0].Height);
			using (Context cr = new Context (sr)) {
				cr.Operator = Operator.Source;
				icon_buffers [0].Internal.Show (cr, 0, 0);
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
			
			double pixelCount = icon_buffers [0].Width * icon_buffers [0].Height * byte.MaxValue;
			
			sr.Destroy ();
			
			average_color = new Cairo.Color (rTotal / pixelCount, 
			                                 gTotal / pixelCount, 
			                                 bTotal / pixelCount)
				.SetValue (.8)
				.MultiplySaturation (1.15);
			
			return average_color.Value;
		}
		
		#region Drop Handling
		/// <summary>
		/// Called by the owning dock to determine if an item is capable of handling a set of URIs on a drop.
		/// </summary>
		/// <param name="uris">
		/// A <see cref="IEnumerable<System.String>"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
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
		
		/// <summary>
		/// Called by the owning dock when a set of URIs has been dropped on an item and CanAcceptDrop has returned true
		/// </summary>
		/// <param name="uris">
		/// A <see cref="IEnumerable<System.String>"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
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
		
		/// <summary>
		/// Called by the owning dock to determine if an item is capable of handling an AbstractDockItem on a drop event.
		/// </summary>
		/// <param name="item">
		/// A <see cref="AbstractDockItem"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
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
		
		/// <summary>
		/// Called by the owning dock when an AbstractDockitem has been dropped on an item and CanAcceptDrop has returned true
		/// </summary>
		/// <param name="item">
		/// A <see cref="AbstractDockItem"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
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
		/// <summary>
		/// Called by owning Dock when the item is clicked
		/// </summary>
		/// <param name="button">
		/// A <see cref="System.UInt32"/>
		/// </param>
		/// <param name="mod">
		/// A <see cref="Gdk.ModifierType"/>
		/// </param>
		/// <param name="xPercent">
		/// A <see cref="System.Double"/> representing the percentage across the icon on which the click took place
		/// </param>
		/// <param name="yPercent">
		/// A <see cref="System.Double"/> representing the percentage down the icon on which the click took place
		/// </param>
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
		
		/// <summary>
		/// Called by owning dock when item is scrolled
		/// </summary>
		/// <param name="direction">
		/// A <see cref="Gdk.ScrollDirection"/>
		/// </param>
		/// <param name="mod">
		/// A <see cref="Gdk.ModifierType"/>
		/// </param>
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
		/// <summary>
		/// Returns the <see cref="DockySurface"/> which holds the visual representation of the item.
		/// </summary>
		/// <param name="model">
		/// A <see cref="DockySurface"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="DockySurface"/>
		/// </returns>
		public DockySurface IconSurface (DockySurface model, int size, int iconSize, bool threeDimensional)
		{
			IconSize = iconSize;
			
			// look for something nice to return
			for (int j = 0; j < icon_buffers.Length; j++) {
				if (icon_buffers[j] == null || redraw[j])
					continue;
				
				if (icon_buffers[j].Height == size) {
					return icon_buffers[j];
				}
			}
			
			// Find the buffer most similar to the requested size so as to reduce cache thrashing
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
			
			if (icon_buffers[i] == null || icon_buffers[i].Width != size || icon_buffers[i].Height != size) {
				if (icon_buffers[i] != null)
					icon_buffers[i] = ResetBuffer (icon_buffers[i]);
				
				try {
					icon_buffers [i] = CreateIconBuffer (model, size);
				} catch (Exception e) {
					Log<AbstractDockItem>.Error (e.Message);
					Log<AbstractDockItem>.Debug (e.StackTrace);
					icon_buffers [i] = new DockySurface (size, size, model);
				}
			}
			
			average_color = null;
			
			icon_buffers [i].Clear ();
			icon_buffers [i].ResetContext ();
			
			try {
				if (threeDimensional)
					PaintIconSurface3d (icon_buffers [i]);
				else
					PaintIconSurface (icon_buffers [i]);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			
			PaintBadgeSurface (icon_buffers [i]);
			
			redraw [i] = false;
			
			return icon_buffers [i];
		}
		
		protected virtual DockySurface CreateIconBuffer (DockySurface model, int size)
		{
			return new DockySurface (size, size, model);
		}
		
		protected abstract void PaintIconSurface (DockySurface surface);
		
		protected virtual void PaintIconSurface3d (DockySurface surface)
		{
			PaintIconSurface (surface);
		}
		
		void PaintBadgeSurface (DockySurface surface)
		{
			if (string.IsNullOrEmpty (BadgeText))
				return;
			
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			layout.Width = Pango.Units.FromPixels (surface.Height / 2);
			layout.Ellipsize = Pango.EllipsizeMode.None;
			layout.FontDescription = new Gtk.Style().FontDescription;
			layout.FontDescription.Weight = Pango.Weight.Bold;
			
			Pango.Rectangle inkRect, logicalRect;
			int tsize = 3;
			do {
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (tsize);
				layout.SetText (BadgeText);
				layout.GetPixelExtents (out inkRect, out logicalRect);
				tsize++;
			} while (Math.Max (logicalRect.Width, logicalRect.Height) < surface.Height / (IsSmall ? 1 : 2) - 8);
			
			int size = Math.Max (logicalRect.Width, logicalRect.Height);
			int padding = 4;
			int lineWidth = 2;
			int x = surface.Width - size / 2 - padding - lineWidth;
			int y = size / 2 + padding + lineWidth;
			
			if (!IsSmall) {
				// draw outline shadow
				surface.Context.LineWidth = lineWidth;
				surface.Context.Color = new Cairo.Color (0, 0, 0, 0.5);
				surface.Context.Arc (x, y + 1, size / 2 + padding, 0, Math.PI * 2);
				surface.Context.Stroke ();
				
				// draw filled gradient
				RadialGradient rg = new RadialGradient (x, lineWidth, 0, x, lineWidth, size + 2 * padding);
				rg.AddColorStop (0, badgeColors [0]);
				rg.AddColorStop (1.0, badgeColors [1]);
				
				surface.Context.Pattern = rg;
				surface.Context.Arc (x, y, size / 2 + padding, 0, Math.PI * 2);
				surface.Context.Fill ();
				rg.Destroy ();
				
				// draw outline
				surface.Context.Color = new Cairo.Color (1, 1, 1, 1);
				surface.Context.Arc (x, y, size / 2 + padding, 0, Math.PI * 2);
				surface.Context.Stroke ();
				
				surface.Context.LineWidth = lineWidth / 2;
				surface.Context.Color = badgeColors [1];
				surface.Context.Arc (x, y, size / 2 + padding - lineWidth, 0, Math.PI * 2);
				surface.Context.Stroke ();
				
				surface.Context.Color = new Cairo.Color (0, 0, 0, 0.2);
			} else {
				x = surface.Width - logicalRect.Width / 2;
				y = logicalRect.Height / 2;
				surface.Context.Color = new Cairo.Color (0, 0, 0, 0.6);
			}
			
			// draw text
			surface.Context.MoveTo (x - logicalRect.Width / 2, y - logicalRect.Height / 2);
			
			Pango.CairoHelper.LayoutPath (surface.Context, layout);
			surface.Context.LineWidth = 2;
			surface.Context.StrokePreserve ();
			surface.Context.Color = new Cairo.Color (1, 1, 1, 1);
			surface.Context.Fill ();
		}
		
		/// <summary>
		/// Returns a <see cref="DockySurface"/> containing a visual representation of the HoverText
		/// </summary>
		/// <param name="model">
		/// A <see cref="DockySurface"/>
		/// </param>
		/// <param name="style">
		/// A <see cref="Style"/>
		/// </param>
		/// <returns>
		/// A <see cref="DockySurface"/>
		/// </returns>
		public DockySurface HoverTextSurface (DockySurface model, Style style, bool isLight)
		{
			if (string.IsNullOrEmpty (HoverText))
				return null;
			
			if (text_buffer == null) {
			
				Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
				
				layout.FontDescription = style.FontDescription;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (11);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.Width = Pango.Units.FromPixels (500);
				
				layout.SetText (HoverText);
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				int textWidth = inkRect.Width;
				int textHeight = logicalRect.Height;
				int buffer = HoverTextHeight - textHeight;
				text_buffer = new DockySurface (Math.Max (HoverTextHeight, textWidth + buffer), HoverTextHeight, model);
				
				Cairo.Context cr = text_buffer.Context;
				
				cr.MoveTo ((text_buffer.Width - textWidth) / 2, buffer / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Color = isLight ? new Cairo.Color (0.1, 0.1, 0.1) : new Cairo.Color (1, 1, 1);
				cr.Fill ();
				
				layout.Dispose ();
			}
			
			return text_buffer;
		}
		
		/// <summary>
		/// Resets the buffers of the item, forcing them to be redrawn the next time the item is shown
		/// </summary>
		public void ResetBuffers ()
		{
			for (int i = 0; i < icon_buffers.Length; i++)
				icon_buffers [i] = ResetBuffer (icon_buffers [i]);
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
				for (int i = 0; i < redraw.Length; i++)
					redraw [i] = true;
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
		
		/// <summary>
		/// Fetches the items which are placed into a menu
		/// </summary>
		/// <returns>
		/// A <see cref="Docky.Menus.MenuList"/>
		/// </returns>
		public Docky.Menus.MenuList GetMenuItems ()
		{
			try {
				return OnGetMenuItems ().Combine (RemoteMenuItems);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
				return new Docky.Menus.MenuList ();
			}
		}
		
		protected virtual Docky.Menus.MenuList OnGetMenuItems ()
		{
			return new Docky.Menus.MenuList ();
		}
		
		protected Gtk.Style Style { get; private set; }
		
		public void SetStyle (Gtk.Style style)
		{
			Style = style;
			try {
				OnStyleSet (style);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItem>.Debug (e.StackTrace);
			}
			Gdk.Color gdkColor = Style.Backgrounds [(int) StateType.Selected];
			badgeColors [0] = new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0).SetValue (1).SetSaturation (0.47);
			badgeColors [1] = badgeColors [0].SetValue (0.5).SetSaturation (0.51);
			QueueRedraw ();
		}
		
		protected virtual void OnStyleSet (Gtk.Style style)
		{
			// do nothing
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
