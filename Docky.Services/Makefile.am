# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Services
TARGET = library

PKG_CONFIG_FILES = docky.services.pc

FILES = \
	AssemblyInfo.cs \
	Docky.Services/Extensions/GLibFileExtension.cs \
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
	Docky.Services/NativeInterop.cs \
	gtk-gui/Docky.Services.GUI.PasswordSaveWidget.cs \
	gtk-gui/Docky.Services.GUI.MountOperationDialog.cs \
	gtk-gui/Docky.Services.GUI.UsernameWidget.cs \
	gtk-gui/Docky.Services.GUI.AnonymousWidget.cs \
	gtk-gui/Docky.Services.GUI.PasswordWidget.cs \
	gtk-gui/generated.cs \
	Docky.Services/GUI/PasswordSaveWidget.cs \
	Docky.Services/GUI/MountOperationDialog.cs \
	Docky.Services/GUI/PasswordWidget.cs \
	Docky.Services/GUI/UsernameWidget.cs \
	Docky.Services/GUI/AnonymousWidget.cs \
	Docky.Services/GUI/DockyMountOperation.cs
	
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
