# Changelog

All notable changes to the Vector SIL Kit FMU Importer project shall be documented in this file.

The format is based on `Keep a Changelog (http://keepachangelog.com/en/1.0.0/) <http://keepachangelog.com/en/1.0.0/>`_.

---

## [1.0.3] - TBD

### Fixed

* Fixed crash on Ubuntu 22.04 (and up)
  * Ubuntu 22.04 and up does not ship with a libdl.so anymore. In this case, FMU Importer will now try to load libc.so instead.

---

## [1.0.2] - 2023-07-20

### Changed

* Updated SIL Kit libraries to 4.0.32 (QA release)

### Fixed

* Fixed crash on Windows during startup of the application
  * The SIL Kit DLL file was x86, but should have been x86_64 - this lead to a crash of the application (BadFormatException)

---

## [1.0.1] - 2023-06-28

### Added

* Building the FMU Importer project now automatically downloads the SIL Kit libraries needed to run the application on Windows and Linux/Ubuntu and copies them to the build directory
* Extended the build project to create a cross platform release package file (`SilKitFmuImporter-{Version}-xPlatform-x64`) that runs on Windows and Linux/Ubuntu.
Please note that there is currently no support for OSX.

---

## [1.0.0] - 2023-06-26

Initial release of the FMU Importer. Future versions will provide information about changes compared to the previously released version.
