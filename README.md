# SIL Kit FMU Importer
[![Vector Informatik](https://img.shields.io/badge/Vector%20Informatik-rgb(180,0,50))](https://www.vector.com/int/en/)
[![SocialNetwork](https://img.shields.io/badge/vectorgrp%20LinkedIn®-rgb(0,113,176))](https://www.linkedin.com/company/vectorgrp/)\
[![ReleaseBadge](https://img.shields.io/github/v/release/vectorgrp/sil-kit-fmu-importer.svg)](https://github.com/vectorgrp/sil-kit-fmu-importer/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vectorgrp/sil-kit-fmu-importer/blob/main/LICENSE)
[![SIL Kit](https://img.shields.io/badge/SIL%20Kit-353b42?logo=github&logoColor=969da4)](https://github.com/vectorgrp/sil-kit)
[![FMIStandard](https://img.shields.io/badge/FMI%20Standard-353b42)](https://fmi-standard.org/)\
[![Build&Test](https://github.com/vectorgrp/sil-kit-fmu-importer/actions/workflows/build-public.yaml/badge.svg)](https://github.com/vectorgrp/sil-kit-fmu-importer/actions/workflows/build-public.yaml)
[![Build&Release](https://github.com/vectorgrp/sil-kit-fmu-importer/actions/workflows/release-public.yaml/badge.svg)](https://github.com/vectorgrp/sil-kit-fmu-importer/actions/workflows/release-public.yaml)


The SIL Kit FMU Importer is an extension for SIL Kit (downloadable on [GitHub](https://github.com/vectorgrp/sil-kit); documentation on [github.io](https://vectorgrp.github.io/sil-kit-docs/)) that allows to import Functional Mockup Units (FMUs, see [https://fmi-standard.org](https://fmi-standard.org/)) as SIL Kit participants and run them together with other participants (which may be other FMUs or other SIL Kit participants).

The FMU Importer is designed as a headless tool that does not need any user interaction.
Its behavior is configured by configuration files that are passed during launch.

## Table of Contents
  1. [Overview of FMI](#overview-of-fmi)
  2. [Setup](#setup)
     1. [Requirements](#requirements)
     2. [Build Instructions](#build-instructions)
          1. [FMU Importer](#fmu-importer)
          2. [vCDL Exporter](#vcdl-exporter)
          3. [Communication Interface Exporter](#communication-interface-exporter)
  3. [Running the FMU Importer](#running-the-fmu-importer)
     1. [Example setup with a FMU exported by Vector vVIRTUALtarget](#example-setup-with-a-fmu-exported-by-vector-vvirtualtarget)
  4. [Data, Time, and Lifecycle Handling](#data-time-and-lifecycle-handling)
     1. [Variable Representation](#variable-representation)
     2. [Time and Lifecycle Management](#time-and-lifecycle-management)
     3. [Data Synchronization](#data-synchronization)
  5. [Integration in SIL Kit Setups With Complex Data Structures](#integration-in-sil-kit-setups-with-complex-data-structures)
     1. [Structures in SIL Kit](#structures-in-sil-kit)
     2. [The Communication Interface Description](#the-communication-interface-description)
     3. [Available Options in Communication Interface Description](#available-options-in-communication-interface-description)
     4. [Enumeration Definitions](#enumeration-definitions)
     5. [Structure Definitions](#structure-definitions)
     6. [Publishers and Subscribers](#publishers-and-subscribers)
     7. [Optional Data Types](#optional-data-types)
     8. [Variable Naming Convention in FMI and the FMU Importer](#variable-naming-convention-in-fmi-and-the-fmu-importer)
  6. [Configuring the FMU and the FMU Importer](#configuring-the-fmu-and-the-fmu-importer)
     1. [Configuration Outline](#configuration-outline)
     2. [Available Options in Configuration](#available-options-in-configuration)
        1. [Include](#include)
        2. [Parameters](#parameters)
        3. [VariableMappings](#variablemappings)
        4. [VariableMappings.Transformation](#variablemappingstransformation)
        5. [Supported Data Types](#supported-data-types)
  7. [Supported FMUs](#supported-fmus)
     1. [General Co-Simulation settings](#general-co-simulation-settings)
     2. [FMI-LS-BUS CAN support](#fmi-ls-bus-can-support)
  8. [Error Handling](#error-handling)

## **Overview of FMI**

The FMI (Functional Mockup Interface) standard defines "a ZIP archive and an application programming interface (API) to exchange dynamic models using a combination of XML files, binaries and C code: the Functional Mock-up Unit (FMU)" (see [https://fmi-standard.org/docs/3.0/#_overview](https://fmi-standard.org/docs/3.0/#_overview)).

In other words, an FMU represents a simulation component with defined parameter, output, and input variables in addition to an interface that allows to set and get those variables, control the simulation state of the FMU, and let the internal simulation time progress (and thus process the input variables).

The interface is stimulated through an _Importer_ - an external tool with the task to synchronize the input and output variables of FMUs and coordinate their simulation progress.

![Relation between an FMU and the Importer](./Docs/Images/FmiStructure.png)

The SIL Kit FMU Importer has two roles

- make an FMU accessible as part of a SIL Kit simulation
- act as a distributed importer that uses SIL Kit's virtual time synchronization for time synchronization between FMUs and synchronize the input and output variables via publishers and subscribers.

## **Setup**

The SIL Kit FMU Importer ships as prebuilt portable packages.
This means that the FMU Importer requires an installed .NET Runtime on the target machine.
The package itself provides executables for Windows and Linux.
The Linux artifacts are tested on Ubuntu 22.04, but they should also run on other similar distributions.

Please refer to the section [Build Instructions](#build-instructions) if you want to build the FMU Importer yourself.

### **Requirements**

- .NET 8
  - [.NET's download site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) has guides on how to get .NET 8 for your OS
  - May also be installed as part of a Visual Studio (v17.8+ / 2022) installation
- SIL Kit* (only needed to run the FMU Importer; not required for build step)
  - Package can be downloaded from [GitHub (SIL Kit)](https://github.com/vectorgrp/sil-kit/releases)
- FMU Importer Source Code
  - Can be downloaded from [GitHub (FMU Importer)](https://github.com/vectorgrp/sil-kit-fmu-importer)

### **Build Instructions**

The FMU Importer comes with preconfigured projects.
The following sections provide instructions which projects you may build and what they do.
Manually built binaries of the projects usually build into the folder `_build/crossplatform-x64-{Configuration}`.

> Note that the build instructions are given for x64 platforms but you can choose `win-86` runtime identifier if you use 32-bit FMUs.
> The only limitation of the current 32-bit version of the FMU Importer is the maximum number of FMU variables it can handle, which is restricted to the ushort limit of 65,535.

---

#### **FMU Importer**

To build the FMU Importer itself, open a terminal in the project's root directory (the one that contains FmuImporter.sln) and run the .NET build command

- Windows: `dotnet build ./FmuImporter/FmuImporter.csproj -c Debug --no-self-contained -r win-x64`

- Linux: `dotnet build ./FmuImporter/FmuImporter.csproj -c Debug --no-self-contained -r linux-x64`

This builds the FMU Importer with a `Debug` configuration (`-c Debug`) and the Importer will need an installed DotNet 8.0 (`--no-self-contained`).

You can find a separate section below on how to use the importer.

---

#### **vCDL Exporter**

This tool allows you to export the model descriptions of FMUs as a vCDL file that can be imported into CANoe. To build it, open a terminal in the VcdlExporter directory (the one that contains VcdlExporter.sln) and run the .NET build command

- Windows: `dotnet build ./VcdlExporter/VcdlExporter.csproj -c Debug --no-self-contained -r win-x64`

- Linux: `dotnet build ./VcdlExporter/VcdlExporter.csproj -c Debug --no-self-contained -r linux-x64`

Alternatively, you can open the solution file in Visual Studio and build the vCDL Exporter from there.

>Note: The vCDL exporter is currently a tool for internal debugging purposes and does not have any quality assurance.
You may encounter crashes while using it.

The vCDL exporter can either export a vCDL file based on an FMU or based on a [communication interface description](#the-communication-interface-description).

To export a vCDL based on an FMU, you can run the following command from the folder where your vCDL Exporter binary resides:
`VcdlExporter fmu --input-path <Path/To/FMU> --output-path <Path/To/Exported/vCDL>`
> Only FMU containers are supported (not already extracted FMUs).

> By default the VcdlExporter does not export the clocks associated with the clocked variables. To export them you can pass the following argument: --use-clock-pub-sub-elements
> This argument only applies to the VcdlExporter based on an FMU. VcdlExporter based on a communication interface description always exports all variables, including clocks.

> Note: An FMU with a model description that includes variables named according to the structured naming convention (i.e. containing a `.`) will generate an incorrect vCDL file. To generate a proper one, use a matching communication interface description (e.g. generated with the [Communication Interface Exporter](#communication-interface-exporter) below) and use the following feature to generate the vCDL from it:

To export a vCDL based on a communication interface description, you can run the following command from the folder where your vCDL Exporter binary resides:
`VcdlExporter communicationInterface --input-path <Path/To/Comm-Interface> --output-path <Path/To/Exported/vCDL> --interface-name Default`

All provided paths must include the file's extensions (e.g., .vcdl, .fmu, .yaml).

---

#### **Communication Interface Exporter**

This tool allows you to export the model descriptions of FMUs as a communication interface description file that can be provided to the FMU Importer.
The order it will give to the members of structures is arbitrary.
The structure types are based on the variables' names following FMI's structured naming convention, and the members are assigned to the correct structures.
See [Integration in SIL Kit Setups With Complex Data Structures](#integration-in-sil-kit-setups-with-complex-data-structures) for more details why you may want to use this tool.

To build it, open a terminal in the CommInterfaceExporter directory (the one that contains CommInterfaceExporter.sln) and run the .NET build command

- Windows: `dotnet build ./CommInterfaceExporter/CommInterfaceExporter.csproj -c Debug --no-self-contained -r win-x64`

- Linux: `dotnet build ./CommInterfaceExporter/CommInterfaceExporter.csproj -c Debug --no-self-contained -r linux-x64`

Alternatively, you can open the solution file in Visual Studio and build the Communication Interface Exporter from there.

>Note: The Communication Interface Exporter is currently a tool for internal debugging purposes and does not have any quality assurance.
You may encounter crashes while using it.

To export a communication interface description you can run the following command from the folder where your Communication Interface Exporter binary resides:
`CommInterfaceExporter --input-path <Path/To/FMU> --output-path <Path/To/Exported/CommItf>`

> Only FMU containers are supported (not already extracted FMUs).

> By default the CommInterfaceExporter does not export the clocks associated with the clocked variables. To export them you can pass the following argument: --use-clock-pub-sub-elements

---

## **Running the FMU Importer**

>Please make sure that you have a running and reachable SIL Kit registry before starting the FMU Importer executable.

>If you built the FMU Importer yourself, you may need to copy the SilKit.dll/libSilKit.so file from a SIL Kit release package (we recommend the version given in FmuImporter/common.props) to the root folder of your build directory (the same folder where FmuImporter[.exe] is located).

To run the FMU Importer, you need to run the following command from the directory your built executable resides in:

`FmuImporter [options]`

Available options are:

| Option                                                                           | Description |
|----------------------------------------------------------------------------------|-------------|
| -f, --fmu-path \<fmu-path> (REQUIRED)                                            | Set the path to the FMU file (.fmu). **This is mandatory.** |
| -s, --sil-kit-config-file \<sil-kit-config-file>                                 | Set the path to the SIL Kit configuration file. [default: <path_to_build_directory>/Config.silkit.yaml] |
| -c, --fmu-importer-config-file \<config-file>                                    | Set the path to the [FMU Importer configuration](#configuring-the-fmu-and-the-fmu-importer) file. |
| -i, --fmu-importer-communication-interface-file \<communication-interface-file>  | Set the path to the [Communication Interface Description](#the-communication-interface-description) file. |
| -p, --participant-name \<participant-name>                                       | Set the name of the SIL Kit participant. [default: sil-kit-fmu-importer] |
| --time-sync-mode \<synchronized \| unsynchronized>                               | Choose the [time synchronization mode](#time-and-lifecycle-management). [default: synchronized] |
| --persist                                                                        | Unpack the FMU into a persisted FMU directory. [default: False] |
| --use-persisted                                                                  | Use a persisted FMU directory (created with "--persist" option). [default: False] |
| --use-clock-pub-sub-elements                                                     | Handle clocks and clocked variables separately. |
| --version                                                                        | Show version information |
| -?, -h, --help                                                                   | Show help and usage information |

After running the command, the FMU Importer will internally create a SIL Kit participant and connect to the SIL Kit registry configured in the SIL Kit configuration file.
If no configuration was provided or if it did not specify a registry URI, the default URI `silkit://localhost:8500` will be used.
The FMU Importer uses the SIL Kit logger for its output.
Therefore, you need to provide a SIL Kit configuration file that contains a `Logging` section to see any output provided by the FMU Importer. The FmuImporter defaults to loading this configuration file from ./Config.silkit.yaml, but a different one can be provided by the corresponding command line option (-s).
The FMU Importer prints most of its logs on the `Info` level, but in case an error occurs, there is one log message on the `Error` level that contains the error message and a log message on the `Debug` level that contains the error message including further information (such as a stack trace) to track the error's origin more easily.
> Persistence allows the FMU to be extracted once and reused across runs by storing it in a known folder (that has the same name as the FMU itself) and validating it with a hash.

### **Example setup with a FMU exported by Vector vVIRTUALtarget**
* Export the FMU with vVIRTUALtarget 9 or newer (refer to the tools help section `Functional Mock-up Unit` for further details).
* Optionally create a CANoe configuration if you want to interact with the FMU.
  * Load the vVIRTUALtarget generated FMU specific .vCDL file (*_FMU.vCDL) into the CANoe configurations Communication Setup (use CANoe 19 or newer for the best experience).
  * If not in possession of the .vCDL file, you can also use the VcdlExporter to extract it from the FMU.
  * Enable SIL Kit and keep the default settings.
  * In the CANoe Options set the `Working mode` to `Custom` and the `Time Source` to `External: SIL Kit`.
  * If FMI LS-BUS CAN is used, make sure that the terminal names and the CAN network names in CANoe are the same and mapped accordingly to SIL Kit.
  
* Make sure a SIL Kit Registry is already running, if not start one manually with the default URI:

      ./sil-kit-registry --listen-uri 'silkit://0.0.0.0:8500'

* Start the SIL Kit FMU Importer with the FMU e.g.:

      ./FmuImporter -f ./anySutFolder/anySut.fmu

* If you want a synchronized simulation start a SIL Kit System Controller listing all the participants e.g. with the following parameters which reflect the default participant names:
      
      ./sil-kit-system-controller "sil-kit-fmu-importer" "CANoe"

* Start any other participants and/or the simulation in CANoe.

## **Data, Time, and Lifecycle Handling**

From an FMU's point of view, the FMU Importer acts as a master (FMI 2.0)/importer (FMI 3.0).
As such, the Importer will handle the data exchange via SIL Kit, the time management, and the lifecycle of the FMU.
The FMU Importer does not provide any numeric solvers or interpolation mechanisms.
Therefore, it only supports FMUs that can run in `co-simulation` mode.

### **Variable Representation**

The FMU Importer exposes an FMU's variables as DataPublisher and DataSubscriber services to other SIL Kit simulation participants.
By default, the variable's name is used as the topic name by the SIL Kit service.
The FMU Importer will always map the different kinds of variable types as follows:
| Variable Type         | Created Service |
|-----------------------|-----------------|
| Input                 | DataSubscriber  |
| Independent           | DataPublisher   |
| Output                | DataPublisher   |
| Parameter             | DataPublisher   |
| Structural parameter  | DataPublisher   |

It is also possible to change the default name and type mapping.
See [Configuring the FMU and the FMU Importer](#configuring-the-fmu-and-the-fmu-importer) for details.

### **Time and Lifecycle Management**
The FMU Importer uses the simulation step size provided by the FMU's model description as simulation step size for the FMU, if it is available.
In case the simulation step size is not available, the FMU Importer requires users to provide the simulation step size via the FMU Importer configuration file.
If neither the FMU nor the configuration file provide a step size, the FMU Importer will exit and show an error message.

The FMU Importer provides time synchronization modes that allows users to choose, how virtual time shall be handled by the FMU Importer:
* `Synchronized` (default): The FMU Importer's simulation time will be synchronized with other SIL Kit participants through SIL Kit's _virtual time synchronization_.
  If active, it ensures that all data from previous points in virtual time were received before processing the next simulation step.
  An FMU Importer that uses virtual time synchronization will execute the FMU's simulation steps as soon as the SIL Kit component signals that a simulation step may be performed.
  In addition, the FMU Importer coordinates its SIL Kit lifecycle with other SIL Kit participants.
  This means that the FMU Importer will first wait until all required participants have joined the SIL Kit simulation and then coordinates the lifecycle state with them.
  For details about SIL Kit's virtual time synchronization, refer to the [SIL Kit Documentation](https://vectorgrp.github.io/sil-kit-docs/).
  Further, if any of the required participants stops the simulation, all other participants, including the FMU Importer, will stop as well.
  >Please make sure to start a `sil-kit-system-controller` (part of SIL Kit) that comprises the FMU Importer's participant name as well as all other required participants.

* `Unsynchronized`: If the FMU Importer does not use virtual time synchronization, the simulation step execution will be artificially slowed down to to match the system's wall clock.
  For example, a one second simulation step would also take approximately one second in real time.
  It is important to note that the FMU Importer is not designed as a real-time application and cannot guarantee that the simulation steps will always have a perfect alignment with the real time, but it will stay as close as possible in the long run.
  In case it is not possible to execute the simulation step in time, the simulation will be executed as fast as possible.
  As an unsynchronized SIL Kit participant, FMU Importer receives its SIL Kit data without a timestamp and therefore schedules all messages to be provided to the FMU in the next simulation step (see [below](#data-synchronization) for details).

> The time synchronization mode only affects how the FMU Importer interacts with SIL Kit, but it does not affect how the FMU Importer interacts with the FMU.


### **Data Synchronization**

As mentioned in [Variable Representation](#variable-representation), input variables with the same name as other FMUs' output variables are connected and therefore receive their data.

The general procedure to synchronize the data between SIL Kit and the FMU is similar in `synchronized` and `unsynchronized` time synchronization mode (see [Time and Lifecycle Management](#time-and-lifecycle-management)):

1. Once SIL Kit grants the execution of the next simulation step, the FMU Importer provides all updated variable values to the FMU.
When receiving data for a specific variable more than once before a simulation time step is executed, the last received value is used ("last-is-best").
2. The simulation step of the FMU is executed.
3. All output variables are read from the FMU and published via SIL Kit.

> (Structural) parameters are only published at the beginning of a simulation (t=0), because they are not meant to change after the simulation started.

<details>
  <summary>Sequence of a synchronized simulation step</summary>

![Synchronization between an FMU and the Importer (synchronized)](./Docs/Images/SimStepSynchronized.png)
</details>

<details>
  <summary>Sequence of an unsynchronized simulation step</summary>

![Synchronization between an FMU and the Importer (unsynchronized)](./Docs/Images/SimStepUnsynchronized.png)
</details>

## Integration in SIL Kit Setups With Complex Data Structures

In FMI, a variable's data type is limited to a scalar data type or an array thereof.
However, SIL Kit's (de-)serialization classes do not have this restriction, and they may aggregate data in complex arrangements (structures).
The following subsections explain how variables can be aggregated to structures in SIL Kit.

### Structures in SIL Kit

The SIL Kit serialization classes allow to (de)serialize structured data.
Structures may contain simple data types, enumerations, and other structures.
Further, structure members can be optional (see [Optional Data Types](#optional-data-types)).
Such members may not have a payload and are therefore "skipped" when processing a structure.
The order and the data type of structure members are important to serialize and deserialize structures correctly and must therefore match between all SIL Kit participants.

### The Communication Interface Description

The communication interface description defines the FMU Importer's communication interface toward other SIL Kit participants.
It is described via a YAML file that can be provided via the command line interface of the FMU Importer (see [Running the FMU Importer](#running-the-fmu-importer)).
Providing a communication interface description file is mandatory if structures are used, because it defines the order in which structure members are serialized.

```yaml
    Version: 1

    EnumDefinitions:
    - ...

    StructDefinitions:
    - ...

    Publishers:
    - ...

    Subscribers:
    - ...
```
### **Available Options in Communication Interface Description**

| Name                                        | Type           | Description |
|---------------------------------------------|----------------|-------------|
| Version                                     | Integer        | The version of the config format (mandatory). |
| [EnumDefinitions](#enumeration-definitions) | Array\<Object> | Used to define enumerations and their items. |
| [StructDefinitions](#structure-definitions) | Array\<Object> | Used to define structures. |
| [Publishers](#publishers-and-subscribers)   | Array\<Object> | Used to declare publisher services. This could be a scalar (including enums), list, or structure. |
| [Subscribers](#publishers-and-subscribers)  | Array\<Object> | Used to declare subscriber services. This could be a scalar (including enums), list, or structure. |

#### Enumeration Definitions

| Name         | Type           | Description |
|--------------|----------------|-------------|
| Name         | String         | Name of the enumeration definition. |
| IndexType    | String         | Type of the items' values. Defaults to Int64. |
| Items        | Array\<Object> | List of items that belong to the enumeration. |

Each element of the `Item` list is of the form `Name : Value`.

Example:  
```yaml
EnumDefinitions:
  - Name: EnumSample
    Items:
      - "FirstEnumItem" : 1
      - "SecondEnumItem" : 7
```

#### Structure Definitions

| Name         | Type           | Description |
|--------------|----------------|-------------|
| Name         | String         | Name of the structure definition. |
| Members      | Array\<Object> | List of members that belong to the structure. |

Each element of the `Members` list must be in the form `Name : Type` where `Type` is a [Supported Data Type](#supported-data-types), or a structure / enumeration defined in this file.
Example:  
```yaml
StructDefinitions:
  - Name: StructSample
    Members:
      - Member1: int
      - Member2: double
      - Member3: EnumSample # reference to the enum defined in the example above
```

#### Publishers and Subscribers

Publishers and Subscribers are lists of `Name : Type` elements, identical to struct members.
See [Supported Data Types](#supported-data-types) for a list of valid types.
Example:  
```yaml
Publishers:
  - PubInt : Int
  - PubDouble : double
  - PubEnum : EnumSample
  - PubStruct : StructSample
  
Subscribers:
  - SubInt : Int
  - SubDouble : double
  - SubEnum : EnumSample
  - SubStruct : StructSample
```

### Optional Data Types

In SIL Kit, variables (including structures) and structure members may be optional.
Optional members provide information if their payload is available.
Optional and non-optional data types are not compatible.
In the configuration and the communication interface description, an optional data type is indicated by adding a question mark at the end of the type description.
For example, a communication interface description may specify the following publishers:

```yaml
Publishers:
   NonOptionalIntArray : List<Int32> #non-optional list of 32-bit integers
   OptionalIntArray : List<Int32>? #Optional list of non-optional 32-bit integers
```

The FMU Importer does not support optional data types within a list, since in FMI 3.0, arrays must be provided without missing entries.
However, lists themselves may be optional (as shown in the example).
Optional data types can be declared in the communication interface description and in the FMU Importer configuration file.
> Note: Variables must have the same type in the FMU and comm. interface description.  
If the types deviate, an explicit variables transformation must be defined via the FMU Importer configuration file.

### Variable Naming Convention in FMI and the FMU Importer

With a few exceptions (e.g., string, binary), the FMI standard does not define any non-scalar data types.
To indicate, that a group of variables (or other groups) belongs together, the FMI standard defines a variable naming convention.
Essentially, a variable's name is separated through periods (`.`) to indicate its structure.
For example, the structure of the three variables
```
gps.timecode
gps.position.latitude
gps.position.longitude
```
corresponds to the following structure:
```
gps
  timecode
  position
    latitude
    longitude
```

More details about FMI's naming convention can be found in the FMI standard ([link to corresponding section in FMI 3.0](https://fmi-standard.org/docs/3.0.1/#namingSection)).

The behavior of the SIL Kit FMU Importer regarding the structured naming convention is as follows:

If the FMU defines its naming convention to be 'structured' or the usage is enforced by [FMU Importer Configuration](#configuring-the-fmu-and-the-fmu-importer) file AlwaysUseStructuredNamingConvention [option](#available-options-in-configuration) set to true the SIL Kit FMU Importer automatically maps FMU variables to SIL Kit structures.

It is also possible to deviate from the default automatic mapping. This can make sense if you want to modify the mapping yourself. To do so you can generate, modify and pass a [Communication Interface Description](#the-communication-interface-description) file to the SIL Kit FMU Importer (see [Running the FMU Importer](#running-the-fmu-importer)).

> Note: It is possible to manually 'map' a variable, which is not named like a structure member, by assigning it a topic name that corresponds to the name it should have based on the structured naming convention. This is done using [`VariableMappings`](#variablemappings) in the FMU Importer Configuration file.

---

## **Configuring the FMU and the FMU Importer**

The FMU Importer can be optionally provided with a configuration file that affects, how the FMU Importer synchronizes data and time between the FMU and a SIL Kit simulation.
Further, it allows to override the default values of parameter variables.

### **Configuration Outline**

The configuration file is expected to be a valid YAML file with the following outline:

```yaml
    Version: 2

    Include:
    - ...

    Parameters:
    - ...

    VariableMappings:
    - ...

    AlwaysUseStructuredNamingConvention: False

    IgnoreUnmappedVariables: False

    StepSize: 1000000

    Namespace: MyNamespace

    Instance: MyInstanceName
```

To help write this file, you may use the schema named `FmuImporterConfiguration.schema.json` in the root directory of the release package.
It depends on your code editor if it supports YAML schemas and how they can be used.
For instance, if you use Visual Studio Code with the Red Hat YAML extension, you can use the schema by adding the following first line to your configuration file:
```yaml
# yaml-language-server: $schema=</path/to/FmuImporter>/FmuImporterConfiguration.schema.json
```

### **Available Options in Configuration**

| Name                                  | Type           | Description |
|---------------------------------------|----------------|-------------|
| Version                               | Integer        | The version of the config format (mandatory). |
| [Include](#include)                   | Array\<String> | Used to include contents of other valid FMU Importer configuration files. |
| [Parameters](#parameters)             | Array\<Object> | Used to override default values of parameters. |
| [VariableMappings](#variablemappings) | Array\<Object> | Used to modify how a variable is represented in a SIL Kit simulation. |
| AlwaysUseStructuredNamingConvention   | Boolean        | Force usage of variable naming convention for automatic structure mapping (see [Variable Naming Convention in FMI and the FMU Importer](#variable-naming-convention-in-fmi-and-the-fmu-importer)). |
| IgnoreUnmappedVariables               | Boolean        | Set to true to prevent synchronization of variables that are not listed in VariableMappings (including parameters). |
| StepSize                              | Integer        | Simulation step size in ns. Overrides step size provided by FMU (if available). |
| Namespace                             | String         | Namespace for all SIL Kit publishers and subscribers (only used for disambiguation - do not use if not necessary). |
| Instance                              | String         | Instance name for all SIL Kit publishers and subscribers (only used for disambiguation - do not use if not necessary). |

The options `Namespace` and `Instance` help to disambiguate if multiple SIL Kit participants provide data with the same topic name.
Only Publishers / Subscribers with the same instance name and namespace will exchange data.
If the options are not set, all data will be received, and sent data will not be discernible.
> These options may negatively impact the performance of the SIL Kit simulation. If possible, disambiguation should be done by renaming the topic names via [transformations](#variablemappingstransformation)
#### *Include_**

Used to include contents of other valid FMU Importer configuration files.
Imported configuration files are evaluated before local definitions (e.g., parameters or variable mappings) are applied.
Local definitions take precedence over imported definitions.
In case of circular imports, a file will only be imported the first time it is encountered.

Syntax:
```yaml
Include:
  - <path/to/fmu-importer-config>
  - ...
```

The paths to the included files can either absolute or relative to the including configuration file.

#### **_Parameters_**

Used to override default values of parameters.
Each entry of the list must have the following attributes:

| Name           | Type   | Description |
|----------------|--------|-------------|
| VariableName   | String | Name of the variable in the model description. |
| Value          | Object | Value of the parameter. Type must match the definition in the model description. |

The `Value` attribute may be one of the following:
* A string (with single (`'`) or double (`"`) quotes) even if the content of the string is a numeric value;
* A numeric value (example: `42`, `3.14`). If it's not representable by an integer type of size 64 bits or a double-precision floating point due to overflow, it'll be considered a string;
* A list of the above (lists nesting is not supported).

If the `VariableName` designates a variable whose type is an enumeration, integer values are interpreted as the the enumerator's underlying value. If it is a string, it is interpreted as an enumerator's name and the value is taken from the Model Description.

In the case where the `VariableName` designates a variable whose type is a multi-dimensional array, the `Value` attribute is the flattened array which will be used to initialize the variable.

Syntax:
```yaml
Parameters:
  - VariableName: <name-in-model-description>
    Value: <new-start-value>
  - VariableName: <enum-typed-variable-name-in-model-description>
    Value: "<name-of-enumerator-used-as-start-value>"
  - VariableName: <array-name-in-model-description>
    Value:
      - <first-item-of-the-start-value>
      - <second-item-of-the-start-value>
      - ...
  - ...
```

#### **_VariableMappings_**

Used to modify how a variable is represented in a SIL Kit simulation.
The following properties of a variable can be modified:

| Name                                              | Type   | Description |
|---------------------------------------------------|--------|-------------|
| VariableName                                      | String | Name of the variable in the model description (mandatory). |
| TopicName                                         | String | The topic under which the publisher / subscriber that corresponds to the variable sends / receives the data. This means that input and output variables with the same topic name are connected. |
| [Transformation](#variablemappingstransformation) | Object | Allows to add a linear transformation (factor and offset) and a typecast to the data before it is serialized by SIL Kit. |

In the example below, there are two FMUs (FMU1 and FMU2) with variables that should be connected.
However, they need to be reconfigured, because they do not have the same name.
After applying configurations with the shown excerpts to their FMU Importers, the variables are connected.

![Relation between an FMU and the Importer](./Docs/Images/VariableConfiguration.png)

#### **VariableMappings.Transformation**

In addition to the optional transformation that is part of a variable's unit, the FMU Importer allows to additional linear transformation as well as a type cast for floating point numbers and integers.
If a linear transformation is applied to an integer, the result will be type casted from the resulting floating point number to the original integer type.

The point in time when the transformations are applied depends on the variable type:
- `Output variables:`
The linear transformation is applied after the UnitDefinition's transformation.
The result is then cast to the target transmission type, serialized, and sent via the variable's corresponding SIL Kit DataPublisher.
- `Input variables:`
Once received by SIL Kit's DataSubscriber, the data is cast from the provided transmission type to the variable's data type.
Then, the linear transformation is applied.
At last, the FMU's unit transformation is applied.

The following properties of a variable can be modified:

| Name             | Type    | Description |
|------------------|---------|-------------|
| Factor           | Double  | First order part of the linear transformation. Applied before the offset. |
| Offset           | Double  | Constant offset of the linear transformation. Applied after the factor. |
| TransmissionType | String  | Data encoding in SIL Kit. If necessary, the data is converted between the SIL Kit and the FMU components. See [Supported Data Types](#supported-data-types) for details |
| ReverseTransform | Boolean | Allows to reverse transformation if original factor and offset are known. |

Syntax:
```yaml
VariableMapping:
  - VariableName: <name-in-model-description>
    TopicName: <topic-name-in-sil-kit>
    Transformation: 
      Factor: <factor-of-linear-transform>
      Offset: <offset-of-linear-transform>
      TransmissionType: <see Supported Data Types below>
      ReverseTransform: {true|false}
  - ...
```
#### Supported Data Types

The FMU Importer supports the data type notation of FMI 2.0, 3.0, and their corresponding C# built-in data types.

| FMI 3.0 | FMI 2.0 | C# type | IsConvertible | Description |
|---------|---------|---------|---------------|--------------|
| Float32 | -       | float   | &check;       | Floating point number 32 with bits. |
| Float64 | Real    | double  | &check;       | Floating point number 64 with bits. |
| Int8    | -       | sbyte   | &check;       | Signed integer with 8 bits. |
| Int16   | -       | short   | &check;       | Signed integer with 16 bits. |
| Int32   | Integer | int     | &check;       | Signed integer with 32 bits. |
| Int64   | -       | long    | &check;       | Signed integer with 64 bits. |
| UInt8   | -       | byte    | &check;       | Unsigned integer with 8 bits.  |
| UInt16  | -       | ushort  | &check;       | Unsigned integer with 16 bits. |
| UInt32  | -       | uint    | &check;       | Unsigned integer with 32 bits. |
| UInt64  | -       | ulong   | &check;       | Unsigned integer with 64 bits. |
| Boolean | Boolean | bool    | &cross;       | Boolean with the values `true` or `false` (serialized as 8 bit) |
| String  | String  | string  | &cross;       | Concatenation of characters. Internally serialized as a list of bytes. |
| Binary  | -       | byte[]  | &cross;       | byte array or arbitrary length. Internally serialized as a list of bytes. |

Notes:
* All built-in data types are case-independent.
* Enumerators are also supported.
* Convertible data types can be converted into each other (as good as possible).
Enumerators are not convertible.
* In FMI 3.0, variables may also be (multi-dimensional) arrays.
These are declared as lists in the FMU Importer (e.g., `List<Double>`).
* Non-convertible data types cannot be used in type transformations.

>Note: Currently, the FMU Importer does not support nested lists.
Therefore, multi-dimensional FMI arrays are handled as their flattened counterparts as defined in [the FMI 3.0 standard](https://fmi-standard.org/docs/3.0.1/#serialization-of-variables).
This information is primarily important for other SIL Kit applications that want to exchange array data with the FMU Importer.

Members of structures that are not always available when the structure is distributed must be declared to be `optional`.
This is done by appending a question mark (`?`) at the end of the data type.

> The FMU Importer will never skip optional data when distributing structures.
However, it is still important to declare the structure's data types correctly, as this impacts the serialization of the structure.

---

## **Supported FMU settings and features**
The FMU Importer expects FMUs in Co-Simulation mode with some specific settings defined in `modelDescription.xml`, in a way to guarantee compatibility and synchronization with other SIL Kit participants. These settings are shown below. 

### **General Co-Simulation settings**
These settings guarantee synchronization on the SIL Kit network and avoid disruptions.

|    **Model Description Attribute**     | **Required value**                |
|----------------------------------------|-----------------------------------|
| providesIntermediateUpdate             | false                             |
| mightReturnEarlyFromDoStep             | false                             |
| canReturnEarlyAfterIntermediateUpdate  | false                             |

### **FMI-LS-BUS CAN support**
The SIL Kit FMU Importer supports FMI-LS-BUS CAN according to [the layered standard](https://modelica.github.io/fmi-ls-bus/main/#low-cut). Clocks are essential for synchronizing events between the FMU Importer and the FMUs and needed for LS-BUS CAN support. Because Clocks are closely tied to Event Mode support from the importer, in LS-BUS enabled FMUs the following setting must be respected by the FMU.

|    **Model Description Attribute**     | **Required value**                |
|----------------------------------------|-----------------------------------|
| hasEventMode                           | true                              |

Please be aware that currently the SIL Kit FMU Importer does only support Triggered Clocks.

Also be aware that currently the SIL Kit FMU Importer only supports CAN Communication via the LS-BUS Operation with OP Code 0x10 and a basic Format Error handling. For more details on the operations please refer to [the layered standard](https://modelica.github.io/fmi-ls-bus/main/#low-cut-can-operations).

---

## **Error Handling**
If the FMU Importer detects an issue, it will generally exit after terminating the FMU and stopping its SIL Kit lifecycle.
* The error message is logged on an `Error` log level.
* A more extensive error description is logged on the `Debug` log level.
This includes a stack trace if the error originated from an exception.

If the FMU Importer exited without any issues, the exit code will be 0.
All other exit codes set by the FMU Importer have four digits, of which the first digit indicates the component that reported the error:
* 1-49: The error originated in the FMU Importer itself
* 50-99: The error originated in the FMI binding
* 100+: The error originated in the SIL Kit binding

The following table lists the meaning of the current exit codes:


| Code | Origin          | Description |
|------|-----------------|-------------|
|  0   | FMU Importer    | The application terminated successfully |
|  1   | FMU Importer    | Error during initialization |
|  2   | FMU Importer    | Error during simulation |
|  3   | FMU Importer    | Error during FMU's simulation step execution |
|  4   | FMU Importer    | Error during user callback execution |
|  5   | FMU Importer    | One of the required or provided files was not found |
| 49   | FMU Importer    | Encountered an unknown or unspecified error |
| 51   | FMI Binding     | Failed to load a library, most likely the FMU's .dll or .so file |
| 52   | FMI Binding     | Failed to read the FMU's model description file |
| 53   | FMI Binding     | FMU failed to terminate |
| 54   | FMI Binding     | FMU failed due to call that return with 'discard' status code  |
| 55   | FMI Binding     | FMU failed due to call that return with 'error' status code |
| 56   | FMI Binding     | FMU failed due to call that return with 'fatal' status code |
| 57   | FMI Binding     | Failed to read the FMU's terminals and icons file |
| 101  | SIL Kit Binding | Failed to log using SIL Kit's logger |