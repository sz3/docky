# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky
TARGET = exe

FILES = \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/Docky.Interface.DockPreferences.cs \
	gtk-gui/Docky.ConfigurationWindow.cs \
	Gtk/Widget_Extensions.cs \
	Docky/ConfigurationWindow.cs \
	Docky/DockController.cs \
	Docky/DockPlacementWidget.cs \
	Docky/Docky.cs \
	Docky/UserArgs.cs \
	Docky/PluginManager.cs \
	Docky/CairoHelper/DockySurface_Extensions.cs \
	Docky/Interface/AddinTreeView.cs \
	Docky/Interface/AnimationState.cs \
	Docky/Interface/AutohideManager.cs \
	Docky/Interface/AutohideType.cs \
	Docky/Interface/CursorPostionChangedArgs.cs \
	Docky/Interface/CursorTracker.cs \
	Docky/Interface/Dock.cs \
	Docky/Interface/DockDragTracker.cs \
	Docky/Interface/DockPosition.cs \
	Docky/Interface/DockWindow.cs \
	Docky/Interface/DockPreferences.cs \
	Docky/Interface/HoveredItemChangedArgs.cs \
	Docky/Interface/HoverTextManager.cs \
	Docky/Interface/IDockPreferences.cs \
	Docky/Interface/ItemProvidersChangedEventArgs.cs \
	Docky/Interface/PoofWindow.cs \
	Docky/Items/DockyItem.cs \
	Docky/Items/INonPersistedItem.cs \
	Docky/Items/SeparatorItem.cs \
	Docky/Items/SpacingItem.cs \
	Docky/Menus/DockMenu.cs \
	Docky/Menus/MenuItemWidget.cs \
	Docky/Menus/SeparatorWidget.cs \
	Docky/Menus/DockItemMenu.cs

RESOURCES = \
	gtk-gui/gui.stetic \
	Resources/classic.svg \
	Resources/menu.svg

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services \
	Docky.Windowing

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GLIB_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GNOME_DESKTOP_SHARP_20_LIBS) \
	Mono.Cairo \
	$(WNCK_SHARP_10_LIBS) \
	$(GNOME_VFS_SHARP_20_LIBS) \
	$(LIBRSVG_SHARP_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONO_ADDINS_GUI_LIBS) \
	$(MONO_ADDINS_SETUP_LIBS)

#
# Extra rules for our binary, not worth factoring out
#

CLEANFILES += docky
EXTRA_DIST += docky.in Docky.addins

module_DATA += Docky.addins
bin_SCRIPTS = docky

MCS_FLAGS += -unsafe
