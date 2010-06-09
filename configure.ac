dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])

AC_INIT([docky], [2.1.0])
AM_INIT_AUTOMAKE([foreign tar-pax])

AC_CONFIG_MACRO_DIR([m4])

dnl -- Intl
IT_PROG_INTLTOOL([0.35.0])
SHAMROCK_CONFIGURE_I18N(docky)

dnl -- Populate top_srcdir variable
top_srcdir=$(readlink -f $0 | sed -e s/configure$//)

dnl Plugin ABI version; bump whenever plugin interface changes
ABI_VERSION=0.1
AC_SUBST(ABI_VERSION)

dnl Bzr Revno, if release set BZR_REVNO to the release version number
if test -d $top_srcdir/.bzr ; then
	BZR_REVNO=`bzr version-info --custom \
	--template="{revno}"`
else
	BZR_REVNO=0.1
fi
AC_SUBST(BZR_REVNO)

if test -d $top_srcdir/.bzr ; then
   VERSION_INFO=`bzr version-info --custom \
   --template="bzr {branch_nick} r{revno}"`
elif test -e $top_srcdir/.version_info ; then
   VERSION_INFO=`cat .version_info`
else
   VERSION_INFO="Release"
fi
AC_SUBST(VERSION_INFO)

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL
AC_PROG_CC
AC_HEADER_STDC

SHAMROCK_FIND_C_SHARP_3_0_COMPILER

dnl Find gconftool-2
AC_PATH_PROG(GCONFTOOL, gconftool-2)
AM_GCONF_SOURCE_2

dnl find gapi2-fixup
AC_PATH_PROG(GAPI2FIXUP, gapi2-fixup, no)
if test ! -x "$GAPI2FIXUP"; then
   AC_MSG_ERROR([No gapi post-processor found])
fi

AC_PATH_PROG(GAPI2PARSER, gapi2-parser, no)
if test ! -x "$GAPI2PARSER"; then
   AC_MSG_ERROR([No gapi parser found])
fi

AC_PATH_PROG(GAPI2CODEGEN, gapi2-codegen, no)
if test ! -x "$GAPI2CODEGEN"; then
   AC_MSG_ERROR([No gapi code generator found])
fi

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR


PKG_CHECK_MODULES([GCONF_SHARP_20], [gconf-sharp-2.0])
PKG_CHECK_MODULES([GLADE_SHARP_20], [glade-sharp-2.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GIO_SHARP], [gio-sharp-2.0])
PKG_CHECK_MODULES([GNOME_DESKTOP_SHARP_20], [gnome-desktop-sharp-2.0])
PKG_CHECK_MODULES([GNOME_KEYRING_SHARP], [gnome-keyring-sharp-1.0])
PKG_CHECK_MODULES([GNOME_SHARP_20], [gnome-sharp-2.0])
PKG_CHECK_MODULES([GNOME_VFS_SHARP_20], [gnome-vfs-sharp-2.0])
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([LIBRSVG_SHARP], [rsvg2-sharp-2.0])
PKG_CHECK_MODULES([MONO_ADDINS], [mono-addins])
PKG_CHECK_MODULES([MONO_ADDINS_GUI], [mono-addins-gui])
PKG_CHECK_MODULES([MONO_ADDINS_SETUP], [mono-addins-setup])
PKG_CHECK_MODULES([MONO_CAIRO], [mono-cairo])
PKG_CHECK_MODULES([MONO_OPTIONS], [mono-options])
PKG_CHECK_MODULES([NDESK_DBUS_10], [ndesk-dbus-1.0])
PKG_CHECK_MODULES([NDESK_DBUS_GLIB_10], [ndesk-dbus-glib-1.0])
PKG_CHECK_MODULES([NOTIFY_SHARP], [notify-sharp])
PKG_CHECK_MODULES([WNCK_SHARP_10], [wnck-sharp-1.0])

dnl check for native libraries (ones that are p/invoked)
GTK_REQUIRED_VERSION=2.14.3
GLIB_REQUIRED_VERSION=2.22
PKG_CHECK_MODULES(GIO, gio-2.0 >= $GLIB_REQUIRED_VERSION)
PKG_CHECK_MODULES(GTK, gtk+-2.0 >= $GTK_REQUIRED_VERSION)
PKG_CHECK_MODULES(GLIB, gobject-2.0 >= $GLIB_REQUIRED_VERSION)


AC_SUBST([GCONF_SHARP_20_LIBS])
AC_SUBST([GLADE_SHARP_20_LIBS])
AC_SUBST([GLIB_SHARP_20_LIBS])
AC_SUBST([GIO_SHARP_LIBS])
AC_SUBST([GNOME_DESKTOP_SHARP_20_LIBS])
AC_SUBST([GNOME_KEYRING_SHARP_LIBS])
AC_SUBST([GNOME_SHARP_20_LIBS])
AC_SUBST([GNOME_VFS_SHARP_20_LIBS])
AC_SUBST([GTK_SHARP_20_LIBS])
AC_SUBST([LIBRSVG_SHARP_LIBS])
AC_SUBST([MONO_ADDINS_LIBS])
AC_SUBST([MONO_ADDINS_GUI_LIBS])
AC_SUBST([MONO_ADDINS_SETUP_LIBS])

MONO_CAIRO_LIBS="-r:Mono.Cairo"
AC_SUBST([MONO_CAIRO_LIBS])

AC_SUBST([MONO_OPTIONS_LIBS])
AC_SUBST([NDESK_DBUS_10_LIBS])
AC_SUBST([NDESK_DBUS_GLIB_10_LIBS])
AC_SUBST([NOTIFY_SHARP_LIBS])
AC_SUBST([WNCK_SHARP_10_LIBS])

dnl Docky references
DOCKY_CAIRO_LIBS="-r:$(pwd)/build/Docky.CairoHelper.dll"
AC_SUBST([DOCKY_CAIRO_LIBS])
DOCKY_ITEMS_LIBS="-r:$(pwd)/build/Docky.Items.dll"
AC_SUBST([DOCKY_ITEMS_LIBS])
DOCKY_SERVICES_LIBS="-r:$(pwd)/build/Docky.Services.dll"
AC_SUBST([DOCKY_SERVICES_LIBS])
DOCKY_WIDGETS_LIBS="-r:$(pwd)/build/Docky.Widgets.dll"
AC_SUBST([DOCKY_WIDGETS_LIBS])
DOCKY_WINDOWING_LIBS="-r:$(pwd)/build/Docky.Windowing.dll"
AC_SUBST([DOCKY_WINDOWING_LIBS])


PKG_CHECK_MODULES([NUNIT], [nunit], have_nunit=yes, have_nunit=no)
AM_CONDITIONAL(HAVE_NUNIT, test "$have_nunit" = "yes")
if test "$have_nunit" != "yes" ; then
   NUNIT_LIBS=
fi
AC_SUBST([NUNIT_LIBS])

dnl {{{ enable debug release configuration
AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test "x$enable_debug" = "xyes")
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
dnl }}}

dnl {{{ enable release build configuration
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_DEBUG, true)
fi
dnl }}}

AC_CONFIG_FILES([
Docky/Makefile
Docky.CairoHelper/Makefile
Docky.DBus/Makefile
Docky.Items/Makefile
Docky.Widgets/Makefile
Docky.Services/Makefile
Docky.Services/AssemblyInfo.cs
Docky.Windowing/Makefile
Docky/docky
Docky/AssemblyInfo.cs
StandardPlugins/Makefile
StandardPlugins/BatteryMonitor/Makefile
StandardPlugins/BatteryMonitor/Resources/BatteryMonitor.addin.xml
StandardPlugins/Bookmarks/Makefile
StandardPlugins/Bookmarks/Resources/Bookmarks.addin.xml
StandardPlugins/Clock/Makefile
StandardPlugins/Clock/Resources/Clock.addin.xml
StandardPlugins/Clock/Themes/Makefile
StandardPlugins/Clock/Themes/Default/Makefile
StandardPlugins/Clock/Themes/Default24/Makefile
StandardPlugins/CPUMonitor/Makefile
StandardPlugins/CPUMonitor/Resources/CPUMonitor.addin.xml
StandardPlugins/Desktop/Makefile
StandardPlugins/Desktop/Resources/Desktop.addin.xml
StandardPlugins/GMail/Makefile
StandardPlugins/GMail/Resources/GMail.addin.xml
StandardPlugins/Mounter/Makefile
StandardPlugins/Mounter/Resources/Mounter.addin.xml
StandardPlugins/NetworkManager/Makefile
StandardPlugins/NetworkManager/Resources/NetworkManager.addin.xml
StandardPlugins/NPR/Makefile
StandardPlugins/NPR/Resources/NPR.addin.xml
StandardPlugins/RecentDocuments/Makefile
StandardPlugins/RecentDocuments/Resources/RecentDocuments.addin.xml
StandardPlugins/SessionManager/Makefile
StandardPlugins/SessionManager/Resources/SessionManager.addin.xml
StandardPlugins/Timer/Makefile
StandardPlugins/Timer/Resources/Timer.addin.xml
StandardPlugins/Trash/Makefile
StandardPlugins/Trash/Resources/Trash.addin.xml
StandardPlugins/Weather/Makefile
StandardPlugins/Weather/Resources/Weather.addin.xml
StandardPlugins/WorkspaceSwitcher/Makefile
StandardPlugins/WorkspaceSwitcher/Resources/WorkspaceSwitcher.addin.xml
Makefile
data/Makefile
data/icons/Makefile
data/icons/hicolor/16x16/Makefile
data/icons/hicolor/16x16/apps/Makefile
data/icons/hicolor/22x22/Makefile
data/icons/hicolor/22x22/apps/Makefile
data/icons/hicolor/24x24/Makefile
data/icons/hicolor/24x24/apps/Makefile
data/icons/hicolor/32x32/Makefile
data/icons/hicolor/32x32/apps/Makefile
data/icons/hicolor/48x48/Makefile
data/icons/hicolor/48x48/apps/Makefile
data/icons/hicolor/48x48/mimetypes/Makefile
data/icons/hicolor/64x64/Makefile
data/icons/hicolor/64x64/apps/Makefile
data/icons/hicolor/128x128/Makefile
data/icons/hicolor/128x128/apps/Makefile
data/icons/hicolor/Makefile
data/themes/Makefile
data/themes/Air/Makefile
data/themes/Glass/Makefile
data/themes/Grayscale/Makefile
data/themes/HUD/Makefile
data/themes/Matte/Makefile
data/themes/Smoke/Makefile
data/themes/Transparent/Makefile
m4/Makefile
po/Makefile.in
])

AC_OUTPUT

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:     ${prefix}
    Mono C# Compiler:   ${MCS} ${GMCS_FLAGS}
    Mono Runtime:       ${MONO}

  Build/Development
    Release Build:      ${enable_release}
    NUnit:              ${have_nunit}

EOF
