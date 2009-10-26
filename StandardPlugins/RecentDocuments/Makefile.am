# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = RecentDocuments

FILES = \
	src/RecentDocumentsItemProvider.cs \
	src/RecentDocumentsItem.cs

RESOURCES = \
	Resources/RecentDocuments.addin.xml

REFERENCES =  \
	System \
	System.Xml \
	System.Xml.Linq \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)
