# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Zeitgeist
TARGET = library

FILES = \
	Zeitgeist/ZeitgeistResult.cs \
	Zeitgeist/ZeitgeistFilter.cs \
	Zeitgeist/ZeitgeistProxy.cs

REFERENCES =  \
	System \
	System.Core \
	$(DOCKY_SERVICES_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS)
