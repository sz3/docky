# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.Items
TARGET = library

PKG_CONFIG_FILES = docky.items.pc

FILES = \
	Docky.Items/Enumerable_Extensions.cs \
	Docky.Items/ItemState.cs \
	Docky.Menus/MenuItem.cs \
	Docky.Menus/RelatedFileMenuItem.cs \
	Docky.Menus/SeparatorMenuItem.cs \
	Docky.Menus/WindowMenuItem.cs \
	Docky.Items/WindowDockItem.cs \
	Docky.Items/AbstractDockItem.cs \
	Docky.Items/AbstractDockItemProvider.cs \
	Docky.Items/ApplicationDockItem.cs \
	Docky.Items/FileDockItem.cs \
	Docky.Items/ItemsChangedArgs.cs \
	Docky.Items/ActivityIndicator.cs \
	Docky.Items/ClickAnimation.cs \
	Docky.Items/PaintNeededEventArgs.cs \
	Docky.Items/FileApplicationProvider.cs \
	Docky.Items/IconDockItem.cs \
	Docky.Items/WnckDockItem.cs

RESOURCES = \
	Resources/close.svg \
	Resources/maximize.svg \
	Resources/minimize.svg \
	Resources/run.svg \
	Resources/Docky.Items.addin.xml

PROJECT_REFERENCES= \
	Docky.Services \
	Docky.CairoHelper \
	Docky.Zeitgeist \
	Docky.Windowing

REFERENCES =  \
	System \
	System.Core \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	Mono.Cairo \
	$(LIBRSVG_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GNOME_VFS_SHARP_20_LIBS)
