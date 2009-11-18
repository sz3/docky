# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = NetworkManager

FILES = \
	src/ConnectionManager.cs \
	src/DBusObject.cs \
	src/DeviceManager.cs \
	src/DeviceStateChangedArgs.cs \
	src/Enums.cs \
	src/NetworkConnection.cs \
	src/NetworkDevice.cs \
	src/NetworkManager.cs \
	src/NetworkManagerDocklet.cs \
	src/NetworkManagerItemProvider.cs \
	src/WiredConnection.cs \
	src/WiredDevice.cs \
	src/WirelessAccessPoint.cs \
	src/WirelessConnection.cs \
	src/WirelessDevice.cs \
	src/Interfaces/IAccessPoint.cs \
	src/Interfaces/IActiveConnection.cs \
	src/Interfaces/IDHCP4Config.cs \
	src/Interfaces/IIP4Config.cs \
	src/Interfaces/INetManager.cs \
	src/Interfaces/INetworkConnection.cs \
	src/Interfaces/INetworkDevice.cs \
	src/Interfaces/IWiredDevice.cs \
	src/Interfaces/IWirelessDevice.cs

RESOURCES = \
	Resources/NetworkManager.addin.xml

REFERENCES =  \
	System \
	System.Core \
	Mono.Cairo \
	$(DOCKY_ITEMS_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(GTK_SHARP_20_LIBS)
