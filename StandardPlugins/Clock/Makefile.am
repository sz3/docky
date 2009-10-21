# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Clock

FILES = \
	src/ClockItemProvider.cs \
	src/ClockDockItem.cs \
	src/ClockThemeSelector.cs

RESOURCES = \
	Resources/Clock.addin.xml

REFERENCES =  \
	System \
	System.Core \
	Mono.Cairo \
	Mono.Posix \
	$(DOCKY_CAIRO_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(LIBRSVG_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
