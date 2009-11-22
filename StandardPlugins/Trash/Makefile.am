# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Trash

FILES = \
	src/TrashItemProvider.cs \
	src/TrashDockItem.cs

RESOURCES = \
	Resources/Trash.addin.xml

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GCONF_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)
