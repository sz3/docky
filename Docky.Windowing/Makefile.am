# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Windowing
TARGET = library

PKG_CONFIG_FILES = docky.windowing.pc

FILES = \
    AssemblyInfo.cs

PROJECT_REFERENCES= \
	Docky.Services
	
REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GLIB_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(MONO_CAIRO_LIBS)

EXTRA_DIST += Docky.Windowing.dll.config

module_DATA += Docky.Windowing.dll.config
