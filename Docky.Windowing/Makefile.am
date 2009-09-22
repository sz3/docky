# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.Windowing
TARGET = library

PKG_CONFIG_FILES = docky.windowing.pc

FILES = \
    AssemblyInfo.cs \
    Wnck/Window_Extensions.cs \
    Windowing/ClickAction.cs \
    Windowing/WindowControl.cs \
    Xlib/X11Atoms.cs \
    Xlib/Xlib.cs \
    Windowing/Position.cs \
    Windowing/WindowMatcher.cs

REFERENCES =  \
	System \
	System.Core \
	$(GNOME_DESKTOP_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(MONO_CAIRO_LIBS)

CLEANFILES += Docky.Windowing.dll.config
EXTRA_DIST += Docky.Windowing.dll.config.in

module_DATA += Docky.Windowing.dll.config
