# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Windowing
TARGET = library

PKG_CONFIG_FILES = docky.windowing.pc

FILES = \
    AssemblyInfo.cs \
    Wnck/Window_Extensions.cs \
    Windowing/ClickAction.cs \
    Windowing/DesktopItem.cs \
    Windowing/WindowControl.cs \
    Xlib/X11Atoms.cs \
    Xlib/Xlib.cs \
    Windowing/Position.cs \
    Windowing/DesktopFileChangedEventArgs.cs \
    Windowing/WindowMatcher.cs

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(DOCKY_SERVICES_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(MONO_CAIRO_LIBS)

EXTRA_DIST += Docky.Windowing.dll.config

module_DATA += Docky.Windowing.dll.config
