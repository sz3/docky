# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = GMail

FILES = \
	src/GMailItemProvider.cs \
	src/GMailAtom.cs \
	src/GMailErrorArgs.cs \
	src/GMailLoginConfig.cs \
	src/GMailPreferences.cs \
	src/GMailConfiguration.cs \
	src/GMailConfigurationDialog.cs \
	src/GMailMenuItem.cs \
	src/GMailDockItem.cs \
	src/AbstractLoginWidget.cs \
	src/gtk-gui/GMail.GMailConfiguration.cs \
	src/gtk-gui/GMail.AbstractLoginWidget.cs \
	src/gtk-gui/generated.cs

RESOURCES = \
	Resources/GMail.addin.xml \
	Resources/icons/gmail-logo.png

REFERENCES =  \
	System \
	System.Web \
	System.Xml \
	System.Core \
	Mono.Cairo \
	Mono.Posix \
	$(DOCKY_CAIRO_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)

MCS_FLAGS += -unsafe
