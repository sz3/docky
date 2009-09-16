# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.CairoHelper
TARGET = library

PKG_CONFIG_FILES = docky.cairohelper.pc

FILES = \
	Docky.CairoHelper/DockySurface.cs \
	Cairo/Context_Extensions.cs \
	Cairo/Color_Extensions.cs

REFERENCES =  \
	System \
	System.Core \
	$(GLIB_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	Mono.Cairo \
	$(LIBRSVG_SHARP_LIBS)
