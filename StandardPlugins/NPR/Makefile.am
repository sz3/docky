# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = NPR

FILES = \
	src/NPRItemProvider.cs \
	src/NPR.cs \
	src/Station.cs \
	src/StationsUpdatedEventArgs.cs \
	src/StationUrl.cs \
	src/StationSearchWidget.cs \
	src/ConfigDialog.cs \
	src/StationsView.cs \
	src/StationTile.cs \
	src/WrapLabel.cs \
	gtk-gui/NPR.StationSearchWidget.cs \
	gtk-gui/generated.cs

RESOURCES = \
	Resources/NPR.addin.xml \
	Resources/nprlogo.png

REFERENCES =  \
	System \
	System.Web \
	System.Xml.Linq \
	System.Core \
	Mono.Posix \
	$(DOCKY_CAIRO_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
