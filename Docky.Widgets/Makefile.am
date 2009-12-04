# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Widgets
TARGET = library

PKG_CONFIG_FILES = docky.widgets.pc

FILES = \
	AssemblyInfo.cs \
	Docky.Widgets/ConfigDialog.cs \
	Docky.Widgets/ExtensionMethods.cs \
	Docky.Widgets/Actor.cs \
	Docky.Widgets/AnimatedBox.cs \
	Docky.Widgets/AnimatedHBox.cs \
	Docky.Widgets/AnimatedVBox.cs \
	Docky.Widgets/AnimatedWidget.cs \
	Docky.Widgets/Choreographer.cs \
	Docky.Widgets/SingleActorStage.cs \
	Docky.Widgets/Stage.cs \
	Docky.Widgets/WrapLabel.cs
	
REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GTK_SHARP_20_LIBS)

EXTRA_DIST += Docky.Widgets.dll.config

module_DATA += Docky.Widgets.dll.config
