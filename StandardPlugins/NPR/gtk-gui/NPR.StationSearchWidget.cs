// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace NPR {
    
    
    public partial class StationSearchWidget {
        
        private Gtk.VBox vbox1;
        
        private Gtk.HBox hbox1;
        
        private Gtk.Button my_stations;
        
        private Gtk.Entry ZipEntry;
        
        private Gtk.Button Search;
        
        private Gtk.ScrolledWindow stationsScroll;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget NPR.StationSearchWidget
            Stetic.BinContainer.Attach(this);
            this.Name = "NPR.StationSearchWidget";
            // Container child NPR.StationSearchWidget.Gtk.Container+ContainerChild
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Name = "vbox1";
            this.vbox1.Spacing = 6;
            // Container child vbox1.Gtk.Box+BoxChild
            this.hbox1 = new Gtk.HBox();
            this.hbox1.Name = "hbox1";
            this.hbox1.Spacing = 6;
            // Container child hbox1.Gtk.Box+BoxChild
            this.my_stations = new Gtk.Button();
            this.my_stations.CanFocus = true;
            this.my_stations.Name = "my_stations";
            this.my_stations.UseUnderline = true;
            this.my_stations.Label = Mono.Unix.Catalog.GetString("My _Stations");
            this.hbox1.Add(this.my_stations);
            Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.hbox1[this.my_stations]));
            w1.Position = 0;
            w1.Expand = false;
            w1.Fill = false;
            // Container child hbox1.Gtk.Box+BoxChild
            this.ZipEntry = new Gtk.Entry();
            this.ZipEntry.CanFocus = true;
            this.ZipEntry.Name = "ZipEntry";
            this.ZipEntry.IsEditable = true;
            this.ZipEntry.InvisibleChar = '●';
            this.hbox1.Add(this.ZipEntry);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.hbox1[this.ZipEntry]));
            w2.Position = 1;
            // Container child hbox1.Gtk.Box+BoxChild
            this.Search = new Gtk.Button();
            this.Search.CanFocus = true;
            this.Search.Name = "Search";
            this.Search.UseUnderline = true;
            // Container child Search.Gtk.Container+ContainerChild
            Gtk.Alignment w3 = new Gtk.Alignment(0.5F, 0.5F, 0F, 0F);
            // Container child GtkAlignment.Gtk.Container+ContainerChild
            Gtk.HBox w4 = new Gtk.HBox();
            w4.Spacing = 2;
            // Container child GtkHBox.Gtk.Container+ContainerChild
            Gtk.Image w5 = new Gtk.Image();
            w5.Pixbuf = Stetic.IconLoader.LoadIcon(this, "gtk-find", Gtk.IconSize.Menu, 16);
            w4.Add(w5);
            // Container child GtkHBox.Gtk.Container+ContainerChild
            Gtk.Label w7 = new Gtk.Label();
            w7.LabelProp = Mono.Unix.Catalog.GetString("S_earch");
            w7.UseUnderline = true;
            w4.Add(w7);
            w3.Add(w4);
            this.Search.Add(w3);
            this.hbox1.Add(this.Search);
            Gtk.Box.BoxChild w11 = ((Gtk.Box.BoxChild)(this.hbox1[this.Search]));
            w11.Position = 2;
            w11.Expand = false;
            w11.Fill = false;
            this.vbox1.Add(this.hbox1);
            Gtk.Box.BoxChild w12 = ((Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
            w12.Position = 1;
            w12.Expand = false;
            w12.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.stationsScroll = new Gtk.ScrolledWindow();
            this.stationsScroll.CanFocus = true;
            this.stationsScroll.Name = "stationsScroll";
            this.stationsScroll.ShadowType = ((Gtk.ShadowType)(1));
            this.vbox1.Add(this.stationsScroll);
            Gtk.Box.BoxChild w13 = ((Gtk.Box.BoxChild)(this.vbox1[this.stationsScroll]));
            w13.Position = 2;
            this.Add(this.vbox1);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Hide();
            this.my_stations.Clicked += new System.EventHandler(this.MyStationsClicked);
            this.Search.Clicked += new System.EventHandler(this.SearchClicked);
        }
    }
}
