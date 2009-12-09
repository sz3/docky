# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.DBus
TARGET = library

FILES = \
	Docky.DBus/DBusManager.cs \
	Docky.DBus/DockyDBus.cs \
	Docky.DBus/DockyDBusMenus.cs \
	Docky.DBus/IDockyDBus.cs \
	Docky.DBus/IDockyDBusMenus.cs

PROJECT_REFERENCES = \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	$(GLIB_SHARP_20_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS)
