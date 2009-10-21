# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = CPUMonitor

FILES = \
	src/CPUMonitorDockItem.cs \
	src/CPUMonitorItemProvider.cs

RESOURCES = \
	Resources/CPUMonitor.addin.xml

REFERENCES =  \
	System \
	System.Core \
	Mono.Cairo \
	Mono.Posix \
	$(DOCKY_CAIRO_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)
