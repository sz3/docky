// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace Docky.Interface {
    
    public partial class DockPreferences {
        
        private Gtk.VBox vbox1;
        
        private Gtk.Table table3;
        
        private Gtk.ComboBox autohide_box;
        
        private Gtk.CheckButton fade_on_hide_check;
        
        private Gtk.HScale icon_scale;
        
        private Gtk.Label label1;
        
        private Gtk.Label label3;
        
        private Gtk.CheckButton multiple_window_indicator_check;
        
        private Gtk.CheckButton window_manager_check;
        
        private Gtk.CheckButton zoom_checkbutton;
        
        private Gtk.HScale zoom_scale;
        
        private Gtk.HSeparator hseparator1;
        
        private Gtk.Table table2;
        
        private Gtk.ScrolledWindow active_scroll;
        
        private Gtk.ScrolledWindow inactive_scroll;
        
        private Gtk.Label label5;
        
        private Gtk.Label label6;
        
        private Gtk.VBox vbox2;
        
        private Gtk.Button enable_plugin_button;
        
        private Gtk.Image asdf;
        
        private Gtk.Button disable_plugin_button;
        
        private Gtk.Image image3;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget Docky.Interface.DockPreferences
            Stetic.BinContainer.Attach(this);
            this.Name = "Docky.Interface.DockPreferences";
            // Container child Docky.Interface.DockPreferences.Gtk.Container+ContainerChild
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Spacing = 6;
            // Container child vbox1.Gtk.Box+BoxChild
            this.table3 = new Gtk.Table(((uint)(5)), ((uint)(3)), false);
            this.table3.Name = "table3";
            this.table3.RowSpacing = ((uint)(6));
            this.table3.ColumnSpacing = ((uint)(6));
            // Container child table3.Gtk.Table+TableChild
            this.autohide_box = Gtk.ComboBox.NewText();
            this.autohide_box.AppendText(Mono.Unix.Catalog.GetString("None"));
            this.autohide_box.AppendText(Mono.Unix.Catalog.GetString("Autohide"));
            this.autohide_box.AppendText(Mono.Unix.Catalog.GetString("Intellihide"));
            this.autohide_box.Name = "autohide_box";
            this.autohide_box.Active = 0;
            this.table3.Add(this.autohide_box);
            Gtk.Table.TableChild w1 = ((Gtk.Table.TableChild)(this.table3[this.autohide_box]));
            w1.LeftAttach = ((uint)(1));
            w1.RightAttach = ((uint)(2));
            w1.XOptions = ((Gtk.AttachOptions)(4));
            w1.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.fade_on_hide_check = new Gtk.CheckButton();
            this.fade_on_hide_check.CanFocus = true;
            this.fade_on_hide_check.Name = "fade_on_hide_check";
            this.fade_on_hide_check.Label = Mono.Unix.Catalog.GetString("Fade On Hide");
            this.fade_on_hide_check.DrawIndicator = true;
            this.fade_on_hide_check.UseUnderline = true;
            this.table3.Add(this.fade_on_hide_check);
            Gtk.Table.TableChild w2 = ((Gtk.Table.TableChild)(this.table3[this.fade_on_hide_check]));
            w2.LeftAttach = ((uint)(2));
            w2.RightAttach = ((uint)(3));
            w2.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.icon_scale = new Gtk.HScale(null);
            this.icon_scale.CanFocus = true;
            this.icon_scale.Name = "icon_scale";
            this.icon_scale.Adjustment.Upper = 100;
            this.icon_scale.Adjustment.PageIncrement = 10;
            this.icon_scale.Adjustment.StepIncrement = 1;
            this.icon_scale.DrawValue = true;
            this.icon_scale.Digits = 0;
            this.icon_scale.ValuePos = ((Gtk.PositionType)(0));
            this.table3.Add(this.icon_scale);
            Gtk.Table.TableChild w3 = ((Gtk.Table.TableChild)(this.table3[this.icon_scale]));
            w3.TopAttach = ((uint)(1));
            w3.BottomAttach = ((uint)(2));
            w3.LeftAttach = ((uint)(1));
            w3.RightAttach = ((uint)(3));
            w3.XOptions = ((Gtk.AttachOptions)(4));
            w3.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.label1 = new Gtk.Label();
            this.label1.Name = "label1";
            this.label1.Xalign = 1F;
            this.label1.LabelProp = Mono.Unix.Catalog.GetString("Icon Size:");
            this.table3.Add(this.label1);
            Gtk.Table.TableChild w4 = ((Gtk.Table.TableChild)(this.table3[this.label1]));
            w4.TopAttach = ((uint)(1));
            w4.BottomAttach = ((uint)(2));
            w4.XOptions = ((Gtk.AttachOptions)(4));
            w4.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.label3 = new Gtk.Label();
            this.label3.Name = "label3";
            this.label3.Xalign = 1F;
            this.label3.LabelProp = Mono.Unix.Catalog.GetString("Autohide:");
            this.table3.Add(this.label3);
            Gtk.Table.TableChild w5 = ((Gtk.Table.TableChild)(this.table3[this.label3]));
            w5.XOptions = ((Gtk.AttachOptions)(4));
            w5.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.multiple_window_indicator_check = new Gtk.CheckButton();
            this.multiple_window_indicator_check.CanFocus = true;
            this.multiple_window_indicator_check.Name = "multiple_window_indicator_check";
            this.multiple_window_indicator_check.Label = Mono.Unix.Catalog.GetString("Indicate Multiple Windows");
            this.multiple_window_indicator_check.DrawIndicator = true;
            this.multiple_window_indicator_check.UseUnderline = true;
            this.table3.Add(this.multiple_window_indicator_check);
            Gtk.Table.TableChild w6 = ((Gtk.Table.TableChild)(this.table3[this.multiple_window_indicator_check]));
            w6.TopAttach = ((uint)(3));
            w6.BottomAttach = ((uint)(4));
            w6.RightAttach = ((uint)(3));
            w6.XOptions = ((Gtk.AttachOptions)(4));
            w6.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.window_manager_check = new Gtk.CheckButton();
            this.window_manager_check.TooltipMarkup = "When set, windows which do not already have launchers on a dock will be added to this dock.";
            this.window_manager_check.CanFocus = true;
            this.window_manager_check.Name = "window_manager_check";
            this.window_manager_check.Label = Mono.Unix.Catalog.GetString("Manage Windows Without Launcher");
            this.window_manager_check.DrawIndicator = true;
            this.window_manager_check.UseUnderline = true;
            this.window_manager_check.Xalign = 0F;
            this.table3.Add(this.window_manager_check);
            Gtk.Table.TableChild w7 = ((Gtk.Table.TableChild)(this.table3[this.window_manager_check]));
            w7.TopAttach = ((uint)(4));
            w7.BottomAttach = ((uint)(5));
            w7.RightAttach = ((uint)(3));
            w7.XOptions = ((Gtk.AttachOptions)(4));
            w7.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.zoom_checkbutton = new Gtk.CheckButton();
            this.zoom_checkbutton.CanFocus = true;
            this.zoom_checkbutton.Name = "zoom_checkbutton";
            this.zoom_checkbutton.Label = Mono.Unix.Catalog.GetString("Zoom:");
            this.zoom_checkbutton.DrawIndicator = true;
            this.zoom_checkbutton.UseUnderline = true;
            this.zoom_checkbutton.Xalign = 1F;
            this.table3.Add(this.zoom_checkbutton);
            Gtk.Table.TableChild w8 = ((Gtk.Table.TableChild)(this.table3[this.zoom_checkbutton]));
            w8.TopAttach = ((uint)(2));
            w8.BottomAttach = ((uint)(3));
            w8.XOptions = ((Gtk.AttachOptions)(4));
            w8.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table3.Gtk.Table+TableChild
            this.zoom_scale = new Gtk.HScale(null);
            this.zoom_scale.CanFocus = true;
            this.zoom_scale.Name = "zoom_scale";
            this.zoom_scale.UpdatePolicy = ((Gtk.UpdateType)(1));
            this.zoom_scale.Adjustment.Upper = 100;
            this.zoom_scale.Adjustment.PageIncrement = 10;
            this.zoom_scale.Adjustment.StepIncrement = 0.01;
            this.zoom_scale.DrawValue = true;
            this.zoom_scale.Digits = 2;
            this.zoom_scale.ValuePos = ((Gtk.PositionType)(0));
            this.table3.Add(this.zoom_scale);
            Gtk.Table.TableChild w9 = ((Gtk.Table.TableChild)(this.table3[this.zoom_scale]));
            w9.TopAttach = ((uint)(2));
            w9.BottomAttach = ((uint)(3));
            w9.LeftAttach = ((uint)(1));
            w9.RightAttach = ((uint)(3));
            w9.XOptions = ((Gtk.AttachOptions)(4));
            w9.YOptions = ((Gtk.AttachOptions)(4));
            this.vbox1.Add(this.table3);
            Gtk.Box.BoxChild w10 = ((Gtk.Box.BoxChild)(this.vbox1[this.table3]));
            w10.Position = 0;
            w10.Expand = false;
            w10.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.hseparator1 = new Gtk.HSeparator();
            this.hseparator1.Name = "hseparator1";
            this.vbox1.Add(this.hseparator1);
            Gtk.Box.BoxChild w11 = ((Gtk.Box.BoxChild)(this.vbox1[this.hseparator1]));
            w11.Position = 1;
            w11.Expand = false;
            w11.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.table2 = new Gtk.Table(((uint)(2)), ((uint)(3)), false);
            this.table2.Name = "table2";
            this.table2.RowSpacing = ((uint)(6));
            this.table2.ColumnSpacing = ((uint)(6));
            // Container child table2.Gtk.Table+TableChild
            this.active_scroll = new Gtk.ScrolledWindow();
            this.active_scroll.CanFocus = true;
            this.active_scroll.Name = "active_scroll";
            this.active_scroll.ShadowType = ((Gtk.ShadowType)(1));
            this.table2.Add(this.active_scroll);
            Gtk.Table.TableChild w12 = ((Gtk.Table.TableChild)(this.table2[this.active_scroll]));
            w12.TopAttach = ((uint)(1));
            w12.BottomAttach = ((uint)(2));
            w12.LeftAttach = ((uint)(2));
            w12.RightAttach = ((uint)(3));
            w12.XOptions = ((Gtk.AttachOptions)(4));
            // Container child table2.Gtk.Table+TableChild
            this.inactive_scroll = new Gtk.ScrolledWindow();
            this.inactive_scroll.CanFocus = true;
            this.inactive_scroll.Name = "inactive_scroll";
            this.inactive_scroll.ShadowType = ((Gtk.ShadowType)(1));
            this.table2.Add(this.inactive_scroll);
            Gtk.Table.TableChild w13 = ((Gtk.Table.TableChild)(this.table2[this.inactive_scroll]));
            w13.TopAttach = ((uint)(1));
            w13.BottomAttach = ((uint)(2));
            w13.XOptions = ((Gtk.AttachOptions)(4));
            // Container child table2.Gtk.Table+TableChild
            this.label5 = new Gtk.Label();
            this.label5.Name = "label5";
            this.label5.LabelProp = Mono.Unix.Catalog.GetString("<b>Inactive Plugins</b>");
            this.label5.UseMarkup = true;
            this.table2.Add(this.label5);
            Gtk.Table.TableChild w14 = ((Gtk.Table.TableChild)(this.table2[this.label5]));
            w14.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table2.Gtk.Table+TableChild
            this.label6 = new Gtk.Label();
            this.label6.Name = "label6";
            this.label6.LabelProp = Mono.Unix.Catalog.GetString("<b>Active Plugins</b>");
            this.label6.UseMarkup = true;
            this.table2.Add(this.label6);
            Gtk.Table.TableChild w15 = ((Gtk.Table.TableChild)(this.table2[this.label6]));
            w15.LeftAttach = ((uint)(2));
            w15.RightAttach = ((uint)(3));
            w15.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table2.Gtk.Table+TableChild
            this.vbox2 = new Gtk.VBox();
            this.vbox2.Name = "vbox2";
            this.vbox2.Spacing = 6;
            // Container child vbox2.Gtk.Box+BoxChild
            this.enable_plugin_button = new Gtk.Button();
            this.enable_plugin_button.CanFocus = true;
            this.enable_plugin_button.Name = "enable_plugin_button";
            this.enable_plugin_button.Relief = ((Gtk.ReliefStyle)(2));
            // Container child enable_plugin_button.Gtk.Container+ContainerChild
            this.asdf = new Gtk.Image();
            this.asdf.Name = "asdf";
            this.asdf.Pixbuf = Stetic.IconLoader.LoadIcon(this, "gtk-go-forward", Gtk.IconSize.Menu, 16);
            this.enable_plugin_button.Add(this.asdf);
            this.enable_plugin_button.Label = null;
            this.vbox2.Add(this.enable_plugin_button);
            Gtk.Box.BoxChild w17 = ((Gtk.Box.BoxChild)(this.vbox2[this.enable_plugin_button]));
            w17.Position = 0;
            // Container child vbox2.Gtk.Box+BoxChild
            this.disable_plugin_button = new Gtk.Button();
            this.disable_plugin_button.CanFocus = true;
            this.disable_plugin_button.Name = "disable_plugin_button";
            this.disable_plugin_button.Relief = ((Gtk.ReliefStyle)(2));
            // Container child disable_plugin_button.Gtk.Container+ContainerChild
            this.image3 = new Gtk.Image();
            this.image3.Name = "image3";
            this.image3.Pixbuf = Stetic.IconLoader.LoadIcon(this, "gtk-go-back", Gtk.IconSize.Menu, 16);
            this.disable_plugin_button.Add(this.image3);
            this.disable_plugin_button.Label = null;
            this.vbox2.Add(this.disable_plugin_button);
            Gtk.Box.BoxChild w19 = ((Gtk.Box.BoxChild)(this.vbox2[this.disable_plugin_button]));
            w19.Position = 2;
            this.table2.Add(this.vbox2);
            Gtk.Table.TableChild w20 = ((Gtk.Table.TableChild)(this.table2[this.vbox2]));
            w20.TopAttach = ((uint)(1));
            w20.BottomAttach = ((uint)(2));
            w20.LeftAttach = ((uint)(1));
            w20.RightAttach = ((uint)(2));
            w20.XOptions = ((Gtk.AttachOptions)(4));
            this.vbox1.Add(this.table2);
            Gtk.Box.BoxChild w21 = ((Gtk.Box.BoxChild)(this.vbox1[this.table2]));
            w21.Position = 2;
            this.Add(this.vbox1);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Hide();
            this.window_manager_check.Toggled += new System.EventHandler(this.OnWindowManagerCheckToggled);
            this.multiple_window_indicator_check.Toggled += new System.EventHandler(this.OnMultipleWindowIndicatorCheckToggled);
            this.enable_plugin_button.Clicked += new System.EventHandler(this.OnEnablePluginButtonClicked);
            this.disable_plugin_button.Clicked += new System.EventHandler(this.OnDisablePluginButtonClicked);
        }
    }
}
