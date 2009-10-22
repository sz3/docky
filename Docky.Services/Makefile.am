# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Services
TARGET = library

PKG_CONFIG_FILES = docky.services.pc

FILES = \
	AssemblyInfo.cs \
	Docky.Services/Logging/ConsoleCrayon.cs \
	Docky.Services/Logging/LogBase.cs \
	Docky.Services/Logging/ConsoleLog.cs \
	Docky.Services/Log.cs \
	Docky.Services/LogTSender.cs \
	Docky.Services/ConnectionStatusChangeEventArgs.cs \
	Docky.Services/IPreferences.cs \
 	Docky.Services/PreferencesService.cs \
	Docky.Services/DockServices.cs \
	Docky.Services/SystemService.cs \
	Docky.Services/DrawingService.cs \
	Docky.Services/NotificationService.cs \
	Docky.Services/Preferences.cs \
	Docky.Services/NativeInterop.cs

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GNOME_KEYRING_SHARP_LIBS) \
	$(MONO_CAIRO_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(LIBRSVG_SHARP_LIBS)
