# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.StandardPlugins
TARGET = library

PKG_CONFIG_FILES = docky.standardplugins.pc

FILES = \
	Clock/ClockItemProvider.cs \
	Clock/ClockDockItem.cs \
	Clock/ClockThemeSelector.cs \
	CPUMonitor/CPUMonitorDockItem.cs \
	CPUMonitor/CPUMonitorItemProvider.cs \
	GMail/GMailItemProvider.cs \
	GMail/GMailAtom.cs \
	GMail/GMailErrorArgs.cs \
	GMail/GMailLoginConfig.cs \
	GMail/GMailPreferences.cs \
	GMail/GMailConfiguration.cs \
	GMail/GMailConfigurationDialog.cs \
	GMail/GMailMenuItem.cs \
	GMail/GMailDockItem.cs \
	GMail/AbstractLoginWidget.cs \
	GMail/gtk-gui/GMail.GMailConfiguration.cs \
	GMail/gtk-gui/GMail.AbstractLoginWidget.cs \
	GMail/gtk-gui/generated.cs \
	Switcher/SwitcherItemProvider.cs \
	Switcher/SwitcherDockItem.cs \
	Trash/TrashItemProvider.cs \
	Trash/TrashDockItem.cs

RESOURCES = \
	Resources/Docky.StandardPlugins.addin.xml \
	Resources/icons/badge-yellow.svg \
	Resources/icons/gmail-logo-dark.png \
	Resources/icons/gmail-logo.png

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Services \
	Docky.Items \
	Docky.Windowing

REFERENCES =  \
	System \
	System.Core \
	System.Xml \
	System.Web \
	Mono.Cairo \
	Mono.Posix \
	$(GTK_SHARP_20_LIBS) \
	$(LIBRSVG_SHARP_LIBS)
