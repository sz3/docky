# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Mounter

FILES = \
	src/MountItem.cs \
	src/MountProvider.cs

RESOURCES = \
	Resources/Mounter.addin.xml

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(DOCKY_WIDGETS_LIBS) \
	$(GTK_SHARP_20_LIBS)
