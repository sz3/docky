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
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)
