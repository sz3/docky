# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = GMail

FILES = \
	src/GMailItemProvider.cs \
	src/GMailAtom.cs \
	src/GMailErrorArgs.cs \
	src/GMailPreferences.cs \
	src/GMailMenuItem.cs \
	src/GMailDockItem.cs

RESOURCES = \
	Resources/GMail.addin.xml

PROJECT_REFERENCES = \
	Docky.Services \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Web \
	System.Xml \
	System.Core \
	Mono.Cairo \
	Mono.Posix \
	$(GTK_SHARP_20_LIBS)

MCS_FLAGS += -unsafe
