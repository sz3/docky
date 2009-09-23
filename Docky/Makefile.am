# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky
TARGET = exe

FILES = \
	Docky/Docky.cs \
	Docky/UserArgs.cs \
	Docky/DockController.cs \
	Docky/Interface/Dock.cs \
	Docky/Interface/DockPosition.cs \
	Docky/Interface/AutohideType.cs \
	gtk-gui/generated.cs \
	Docky/Interface/DockPreferences.cs \
	gtk-gui/Docky.Interface.DockPreferences.cs \
	Docky/Interface/DockWindow.cs \
	Docky/Interface/IDockPreferences.cs \
	Docky/Interface/CursorTracker.cs \
	Docky/Interface/CursorPostionChangedArgs.cs \
	Docky/Interface/AutohideManager.cs \
	Docky/Interface/ItemProvidersChangedEventArgs.cs \
	Docky/Interface/AnimationState.cs \
	Docky/Items/SeparatorItem.cs \
	Docky/CairoHelper/DockySurface_Extensions.cs \
	Docky/ConfigurationWindow.cs \
	gtk-gui/Docky.ConfigurationWindow.cs \
	Docky/DockPlacementWidget.cs \
	Docky/Items/SpacingItem.cs \
	Gtk/Widget_Extensions.cs \
	Docky/Interface/HoveredItemChangedArgs.cs \
    Docky/PluginManager.cs \
    Docky/Items/DockyItem.cs \
    Docky/Menus/DockMenu.cs \
    Docky/Menus/MenuItemWidget.cs \
	Docky/Menus/SeparatorWidget.cs \
    Docky/Menus/DockItemMenu.cs

RESOURCES = \
	gtk-gui/gui.stetic \
	Resources/classic.svg \
	Resources/grayscale.svg \
    Resources/Docky.addin.xml \
    Resources/logo.svg \
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
	Mono.Cairo \
	$(WNCK_SHARP_10_LIBS) \
	$(GNOME_VFS_SHARP_20_LIBS) \
	$(LIBRSVG_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONO_ADDINS_GUI_LIBS) \
	$(MONO_ADDINS_SETUP_LIBS)

#
# Extra rules for our binary, not worth factoring out
#

CLEANFILES += docky
EXTRA_DIST += docky.in

bin_SCRIPTS = docky
