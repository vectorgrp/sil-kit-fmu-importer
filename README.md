# SIL Kit FMU Importer

>Note: This is a preliminary documentation for the FMU Importer.
>The target audience are internal developers that are interested in the current state of the importer.
>This documentation will be extended before the official release.

The SIL Kit FMU Importer is an extension for SIL Kit (downloadable on [GitHub](https://github.com/vectorgrp/sil-kit); documentation on [github.io](https://vectorgrp.github.io/sil-kit-docs/)) that allows to import Functional Mockup Units (FMUs, see [https://fmi-standard.org](https://fmi-standard.org/)) as SIL Kit participants and run them together with other participants (which may be other FMUs or other SIL Kit participants).

The FMU Importer was designed as a standalone tool that does not need any user interaction after its initial configuration upon startup.

## Table of Contents

1. [Overview of FMI](#overview-of-fmi)
2. [Setup](#setup)
    1. [Requirements](#requirements)
    2. [FMU Importer](#fmu-importer)
    3. [vCDL Exporter](#vcdl-exporter)
3. [Running the FMU Importer](#running-the-fmu-importer)

## **Overview of FMI**

The FMI (Functional Mockup Interface) standard defines "a ZIP archive and an application programming interface (API) to exchange dynamic models using a combination of XML files, binaries and C code: the Functional Mock-up Unit (FMU)" (see https://fmi-standard.org/docs/3.0/#_overview).

In other words, an FMU represents a simulation component with defined parameter, output, and input variables in addition to an interface that allows to set and get those variables, control the simulation state of the FMU, and let the internal simulation time progress (and thus process the input variables).

The interface is stimulated through an _Importer_ - an external tool with the task to synchronize the input and output variables of FMUs and coordinate their simulation progress.

![Relation between an FMU and the Importer](./Docs/Images/FmiStructure.png)

The SIL Kit FMU Importer has two roles

- make an FMU accessible to a SIL Kit simulation
- act as a distributed importer that uses SIL Kit's virtual time synchronization for time synchronization between FMUs and synchronize the input and output variables via publishers and subscribers.

## **Setup**

Currently, the SIL Kit FMU Importer does not provide prebuilt packages.
Therefore, you must build them yourselves before you can run the tool.

### **Requirements**

- .NET 6
  - [.NET's download site](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) has guides on how to get .NET 6 for your OS
- SIL Kit
  - Can be downloaded from [GitHub](https://github.com/vectorgrp/sil-kit/releases)
- FMU Importer Source Code
  - Check out FMU Importer project from GitHub

### **Build instructions**

The FMU Importer comes with preconfigured projects.
The following sections provide instructions which projects you may build and what they do.
The binaries of the projects will be built into the folder `_build/{os}-x64-Debug`.
>Note: Depending on how you installed the Vector SIL Kit, you may need to manually copy the SilKit{.dll|.so} file into the build directory.

---

### FMU Importer

To build the FMU Importer itself, open a terminal in the project's root directory (the one that contains FmuImporter.sln) and run the .NET build command

- Windows: `dotnet build ./FmuImporter/FmuImporter/FmuImporter.csproj -c Debug --no-self-contained -r win-x64`

- Linux: `dotnet build ./FmuImporter/FmuImporter.csproj -c Debug --no-self-contained -r linux-x64`

You can find a separate section below on how to use the importer.

---

### vCDL Exporter

This tool allows you to export the model descriptions of FMUs as a vCDL file that can be imported into CANoe. To build it, open a terminal in the project's root directory (the one that contains FmuImporter.sln) and run the .NET build command

- Windows: `dotnet build ./VcdlExporter/VcdlExporter.csproj -c Debug --no-self-contained -r win-x64`

- Linux: `dotnet build ./VcdlExporter/VcdlExporter.csproj -c Debug --no-self-contained -r linux-x64`

>Note: The vCDL exporter is currently a tool for internal debugging purposes and does not have any quality assurance. You may encounter crashes while using it.

To export a vCDL you can run the following command from the folder where your vCDL Exporter binary resides:
`VcdlExporter <Path/To/Exported/vCDL> [<Path/To/FMU>]*`

You must provide the path to the vCDL output file (including its file extension) as the first argument and then the paths to your FMUs (including their file extension). Note that currently only FMU containers are supported (not already extracted FMUs).

---

## **Running the FMU Importer**

Please make sure that you have a running and reachable SIL Kit registry before starting the FMU Importer executable.

To run the FMU Importer, you need to run the following command from the directory your built execuable resides in:

`FmuImporter [options]`

Available options are:

| Option | Description |
| ------ | ----------- |
|-f, --fmu-path \<fmu-path> (REQUIRED) | Set the path to the FMU file (.fmu). This is mandatory. |
| -c, --sil-kit-config-file \<sil-kit-config-file> | Set the path to the SIL Kit configuration file. Defaults to an empty configuration. |
| -p, --participant-name \<participant-name> | Set the name of the SIL Kit participant. Defaults to the FMU's model name. |
| -?, -h, --help | Show help and usage information |

After running the command, the FMU Importer will internally create a SIL Kit participant and connect to the SIL Kit registry configured in the SIL Kit configuration file. If none was provided or if the configuration file did not specify a registry URI, the default URI `silkit://localhost:8500` will be assumed.

Note that the started participant uses a coordinated lifecycle and virtual time synchronization. This means that you need to start a sil-kit-system-controller that comprises the FMU Importers's participant as well as all other required participants.
