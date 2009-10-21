# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Bookmarks

FILES = \
	src/BookmarksItemProvider.cs \
	src/BookmarkDockItem.cs

RESOURCES = \
	Resources/Bookmarks.addin.xml

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)
