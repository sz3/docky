# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.Services
TARGET = library

PKG_CONFIG_FILES = docky.services.pc

FILES = \
    Docky.Services/IPreferences.cs \
    Docky.Services/PreferencesService.cs \
    Docky.Services/DockServices.cs \
    Docky.Services/SystemService.cs \
    Docky.Services/DrawingService.cs \
    Docky.Services/NotificationService.cs \
    Docky.Services/Preferences.cs

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GCONF_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GNOME_KEYRING_SHARP_LIBS) \
	$(MONO_CAIRO_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(LIBRSVG_SHARP_LIBS)
