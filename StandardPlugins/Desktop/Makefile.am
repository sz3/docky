# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Desktop

FILES = \
	src/Xlib/X11Atoms.cs \
	src/Xlib/Xlib.cs \
	src/Wink/Position.cs \
	src/Wink/Viewport.cs \
	src/Wink/ScreenUtils.cs \
	src/Wink/WnckWindow_Extensions.cs \
	src/DesktopDockItem.cs \
	src/ShowDesktopItem.cs \
	src/TileDesktopItem.cs \
	src/CascadeDesktopItem.cs \
	src/DesktopActionsProvider.cs \
	src/DesktopItemProvider.cs
	
RESOURCES = \
	Resources/Desktop.addin.xml

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(LIBRSVG_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS)
