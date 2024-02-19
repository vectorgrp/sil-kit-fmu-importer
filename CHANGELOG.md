# Changelog

All notable changes to the Vector SIL Kit FMU Importer project shall be documented in this file.

The format is based on `Keep a Changelog (http://keepachangelog.com/en/1.0.0/) <http://keepachangelog.com/en/1.0.0/>`.

## [1.2.0] - TBD

### Added

* Added a configuration file schema ``FmuImporterConfiguration.schema.json`` for FmuImporter and included it in release package and extended documentation how to use it.

---

## [1.1.0] - 2023-12-20

### Added

* The vCDL Exporter now also exports enumerators
* Added a CLI option ``--time-sync-mode`` that allows to choose if virtual time synchronization should be used (``synchronized``) or not (``unsynchronized``), which tries to align the simulation time with the system's wall clock (e.g., a one second simulation step would also take approximately one second in real time). Defaults to ``synchronized``.

### Changed

* Moved the vCDL Exporter to a separate solution
* The FMU Importer now sets exit codes != 0 if any component (e.g., the FMU) reported an issue
* The FMU Importer now uses the stopTime of an FMU's model description by default
  * The ``--use-stop-time`` / ``-t`` CLI options are kept for backwards compatibility, but they will not have any effect
  * The options are not shown by the CLI help command and they are not mentioned in the documentation anymore
* Updated SIL Kit libraries to 4.0.43

### Fixed

* Fixed crash when reconfiguring structural parameters
  * Reconfigured structural parameters are now set in configuration mode
* Fixed missing log messages during FMU initialization
* Fixed incorrect array length for variables with dimensions that reference reconfigured structural parameters
* Fixed crash if array-typed parameters were overridden using YAML block style (only flow style worked)
* Fixed "FileNotFound" exception when including configuration files with absolute path
* Fixed incorrect topic name if a variable is reconfigured without specifying its topic name

---

## [1.0.5] - 2023-10-09

### Added

* Added the license of the FMI standard source code to the third-party licenses

### Changed

* Renamed SIL Kit third party licenses from ThirdParty/sil-kit to ThirdParty/sil-kit-ThirdParty

---

## [1.0.4] - 2023-09-26

### Changed

* If SIL Kit logger fails to log the log messages are now written to the console
* The importer source now includes the SIL Kit libraries that are used (removes the necessity to download and cache them)
* Updated SIL Kit libraries to 4.0.36

### Fixed

* The vCDL Exporter now exports correct types for FMI 2.0.x and 3.0.x
  * FMI 2.0.x: float32 -> float; float64 -> double
  * FMI 3.0.x: binary -> bytes
* Improved exception handling - FMU Importer will now try to end SIL Kit and FMU properly if it is possible
* Improved logger reliability
* Fixed erroneous translation of FMI and SIL Kit log levels
* Received events that are at least two simulation steps in the future are now handled correctly
* Changed the serialization and deserialization mechanism for SIL Kit and FMI
  * The new SIL Kit serializer & deserializer are based on the implementation used in SIL Kit
  * This improves the reliability and understandability of the serialization
* The initial unknowns of FMI 2.0.x FMUs are now initialized correctly
* Fixed a possible infinite loop if the importer crashed before the FMU was initialized
* The importer distributed output variable data even if the simulation step failed - this is not the case anymore
* Fixed a crash if an FMI 2.0.x FMU's variable was reconfigured via the importer's configuration file

---

## [1.0.3] - 2023-08-16

### Fixed

* Fixed crash on Ubuntu 22.04 (and up)
  * Ubuntu 22.04 and up does not ship with a libdl.so anymore. In this case, FMU Importer will now try to load libc.so instead.
* Fixed a crash when trying to publish non-scalar data (e.g., strings) in Linux
* Fixed the path that the Importer uses to search for extracted FMI 2.0.x FMUs (used FMI 3.0.x path before), which led to a crash
* Fixed a crash that occurred if a model description did not contain any initial unknowns
* Fixed a crash when the dependencies field of an FMU's output variable is empty
* Fixed a crash in case FMI 2.0.x FMUs tried to log
* Fixed the resource location provided to the FMU (FMI 2.0.x and FMI 3.0.x)

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
