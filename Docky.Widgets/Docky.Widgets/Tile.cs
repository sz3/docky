//
// Tile.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;

using Docky.Services;

using Gtk;

namespace Docky.Widgets
{    
	internal class Tile : Table
	{
		private Button add_remove_button;
		private Box button_box;
		
		#region tile items
		private Label title;
		private Image tileImage;
		private WrapLabel description;
        private WrapLabel subDesc;
		#endregion
		
		private int IconSize { get; set; }
		
		public TileView Owner { get; set; }
		
		public AbstractTileObject OwnedObject { get; private set; }
		public bool Last { get; private set; }

		public event EventHandler ActiveChanged;
		
		public Tile (AbstractTileObject obj, int iconSize) : base (3, 3, false)
		{
			OwnedObject = obj;	
			
			OwnedObject.IconUpdated += delegate {
				SetImage ();
			};
			
			OwnedObject.TextUpdated += delegate {
				SetText ();
			};
			
			OwnedObject.ButtonsUpdated += delegate {
				UpdateButtons ();
			};
			
			IconSize = iconSize;
			
			BuildTile ();
		}

		void UpdateButtons ()
		{
			foreach (Widget w in button_box.Children)
				button_box.Remove (w);
			
			foreach (Button b in OwnedObject.ExtraButtons) {
				button_box.PackStart (b, false, false, 0);
				b.Show ();
			}
			
			if (OwnedObject.ShowActionButton && add_remove_button != null)
				button_box.PackStart (add_remove_button, false, false, 0);
		}

		
		private void SetImage ()
		{
			Gdk.Pixbuf pbuf = DockServices.Drawing.EmptyPixbuf;
			try {
				if (OwnedObject.ForcePixbuf != null) {
					pbuf = OwnedObject.ForcePixbuf.Copy ();
					if (pbuf.Width != IconSize || pbuf.Height != IconSize)
						pbuf = DockServices.Drawing.ARScale (IconSize, IconSize, pbuf);
				} else {
					pbuf = DockServices.Drawing.LoadIcon (OwnedObject.Icon, IconSize);
				}

				pbuf = DockServices.Drawing.AddHueShift (pbuf, OwnedObject.HueShift);
				if (!OwnedObject.Enabled)
					pbuf = DockServices.Drawing.MonochromePixbuf (pbuf);
			} catch (Exception e) {
				Log<Tile>.Error ("Error loading pixbuf for {0} tile: {1}", OwnedObject.Name, e.Message);
				Log<Tile>.Debug (e.StackTrace);
				pbuf = DockServices.Drawing.EmptyPixbuf;
			} finally {
				tileImage.Pixbuf = pbuf;
				pbuf.Dispose ();
				tileImage.Show ();
			}
		}
		
		private void SetText ()
		{
			title.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (OwnedObject.Name));
			
			description.Text = OwnedObject.Description;
			
			if (!string.IsNullOrEmpty (OwnedObject.SubDescriptionText) &&
			    !string.IsNullOrEmpty (OwnedObject.SubDescriptionTitle))			
				subDesc.Markup = String.Format ("<small><b>{0}</b> <i>{1}</i></small>", 
					GLib.Markup.EscapeText (OwnedObject.SubDescriptionTitle), GLib.Markup.EscapeText (OwnedObject.SubDescriptionText));
		}
		
		private void BuildTile ()
		{
			BorderWidth = 5;
			RowSpacing = 1;
			ColumnSpacing = 5;
			
			tileImage = new Image ();
			
			tileImage.Yalign = 0.0f;
			Attach (tileImage, 0, 1, 0, 3, AttachOptions.Shrink, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
			
            title = new Label ();
            title.Show ();
            title.Xalign = 0.0f;
						
			Attach (title, 1, 3, 0, 1, 
			        AttachOptions.Expand | AttachOptions.Fill, 
			        AttachOptions.Expand | AttachOptions.Fill, 0, 0);
			
			description = new WrapLabel ();
			description.Show ();
			description.Wrap = false;
			
			Attach (description, 1, 3, 1, 2,
			        AttachOptions.Expand | AttachOptions.Fill, 
			        AttachOptions.Expand | AttachOptions.Fill, 0, 0);
			
			
			subDesc = new WrapLabel ();
			subDesc.Show ();

			Attach (subDesc, 1, 2, 2, 3,
			        AttachOptions.Expand | AttachOptions.Fill, 
			        AttachOptions.Expand | AttachOptions.Fill,  0, 4);
			
			SetText ();

			button_box = new HBox ();
			button_box.Spacing = 3;
			
			Pango.FontDescription font = PangoContext.FontDescription.Copy ();
			font.Size = (int)(font.Size * Pango.Scale.Small);
			
			Label label = new Label ();
			label.ModifyFont (font);
			add_remove_button = new Button ();
			add_remove_button.UseUnderline = true;
			add_remove_button.Add (label);
			add_remove_button.Clicked += OnAddRemoveClicked;

			UpdateButtons ();
			
			Attach (button_box, 2, 3, 2, 3, 
			        AttachOptions.Shrink, 
				        AttachOptions.Expand | AttachOptions.Fill, 0, 0);

			Show ();
			
			UpdateState ();
		}
		
		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.NoWindow;
			GdkWindow = Parent.GdkWindow;
			base.OnRealized ();
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (State == StateType.Selected) {
				Gtk.Style.PaintFlatBox (Style, evnt.Window, State, ShadowType.None, evnt.Area, 
				                        this, "cell_odd", Allocation.X, Allocation.Y, 
				                        Allocation.Width, Allocation.Height - (Last ? 0 : 1));
			}
			
			
			if (!Last) {            
				Gtk.Style.PaintHline (Style, evnt.Window, StateType.Normal, evnt.Area, this, null, 
				                      Allocation.X, Allocation.Right, Allocation.Bottom - 1);
			}
			
			return base.OnExposeEvent (evnt);
		}
		
		private void OnAddRemoveClicked (object o, EventArgs args)
		{
			if (ActiveChanged != null)
				ActiveChanged (this, EventArgs.Empty);
		}
		
		public void UpdateState ()
		{
			bool enabled = OwnedObject.Enabled;
			bool sensitive = enabled || (!enabled && State == StateType.Selected);
			
			SetImage ();
			title.Sensitive = sensitive;
			description.Sensitive = sensitive;
			description.Wrap = State == StateType.Selected;
			subDesc.Visible = State == StateType.Selected;
			add_remove_button.Label = enabled ? OwnedObject.ButtonStateEnabledText : OwnedObject.ButtonStateDisabledText;
		}
		
		public void Select (bool select)
		{
			State = select ? StateType.Selected : StateType.Normal;
			if (select) {
				button_box.ShowAll ();
			} else {
				button_box.Hide ();
			}
			button_box.State = StateType.Normal;
			UpdateState ();
			QueueResize ();
		}
	}
}