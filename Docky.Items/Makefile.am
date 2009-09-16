# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.Items
TARGET = library

PKG_CONFIG_FILES = docky.items.pc

FILES = \
	Docky.Items/AbstractDockItem.cs \
	Docky.Items/ApplicationDockItem.cs \
	Docky.Items/FileDockItem.cs \
	Docky.Items/ItemsChangedArgs.cs \
	Docky.Items/ActivityIndicator.cs \
	Docky.Items/ClickAnimation.cs \
	Docky.Items/IDockItemProvider.cs \
	Docky.Items/PaintNeededEventArgs.cs \
	Docky.Items/AddRemoveChangeType.cs \
	Docky.Items/FileApplicationProvider.cs \
	Docky.Items/IconDockItem.cs \
	Docky.Items/WnckDockItem.cs

PROJECT_REFERENCES= \
	Docky.Services \
	Docky.CairoHelper \
	Docky.Windowing

REFERENCES =  \
	System \
	System.Core \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	Mono.Cairo \
	$(LIBRSVG_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(GNOME_DESKTOP_SHARP_20_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GNOME_VFS_SHARP_20_LIBS)
