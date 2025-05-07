// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Fmi.FmiModel.Internal;

namespace Fmi.FmiModel;

public class ModelLoader
{
  internal static void ExtractFmu(string fmuPath, out string extractedPath, out bool isTemporary)
  {
    // check if the directory exists
    var targetFolderPath =
      $"{Path.GetDirectoryName(fmuPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(fmuPath)}";

    // TODO switch to temporary directory as soon as everything works as intended
    //var dir = Directory.CreateTempSubdirectory(
    //  $"SilKitImporter_{Path.GetFileNameWithoutExtension(fmuPath)}");
    if (Directory.Exists(targetFolderPath) && Directory.EnumerateFileSystemEntries(targetFolderPath).Any())
    {
      isTemporary = false;
      extractedPath = targetFolderPath;
      // directory exists and has entries -> skip extraction
      return;
    }

    var tempDirectory = CreateTempDirectory();

    var dir = Directory.CreateDirectory(
      $"{tempDirectory}/{Path.GetFileNameWithoutExtension(fmuPath)}");

    ZipFile.ExtractToDirectory(fmuPath, dir.FullName);
    isTemporary = true;
    extractedPath = dir.FullName;
  }

  private static string CreateTempDirectory()
  {
    var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    if (File.Exists(tempDirectory))
    {
      // try again
      return CreateTempDirectory();
    }
    else
    {
      Directory.CreateDirectory(tempDirectory);
      return tempDirectory;
    }
  }

  internal static ModelDescription LoadModelFromExtractedPath(
    string extractedFmuPath, Action<LogSeverity, string> logCallback)
  {
    var modelDescriptionPath = $"{extractedFmuPath}/modelDescription.xml";
    if (!File.Exists(modelDescriptionPath))
    {
      throw new FileNotFoundException("ModelDescription.xml is missing.");
    }

    var fmiVersion = FindFmiVersion(modelDescriptionPath);

    ModelDescription? commonDescription = null;

    using var fileStream = File.Open(modelDescriptionPath, FileMode.Open);
    XmlSerializer ser;
    switch (fmiVersion)
    {
      case FmiVersions.Fmi2:
      {
        ser = new XmlSerializer(typeof(Fmi2.fmiModelDescription));
        var fmiModelDescription = ser.Deserialize(fileStream) as Fmi2.fmiModelDescription;
        if (fmiModelDescription != null)
        {
          commonDescription = new ModelDescription(fmiModelDescription, logCallback);
        }

        break;
      }
      case FmiVersions.Fmi3:
      {
        ser = new XmlSerializer(typeof(Fmi3.fmiModelDescription));
        var fmiModelDescription = ser.Deserialize(fileStream) as Fmi3.fmiModelDescription;
        if (fmiModelDescription != null)
        {
          commonDescription = new ModelDescription(fmiModelDescription, logCallback);
        }
        break;
      }
      default:
        throw new NotSupportedException();
    }

    if (commonDescription == null)
    {
      throw new NullReferenceException("Failed to initialize model description object.");
    }

    return commonDescription;
  }


  internal static TerminalsAndIcons? LoadTerminalsAndIconsFromExtractedPath(
    string extractedFmuPath, ModelDescription modelDescription, Action<LogSeverity, string> logCallback)
  {
    var terminalsAndIconsPath = $"{extractedFmuPath}/terminalsAndIcons/terminalsAndIcons.xml";

    TerminalsAndIcons? TerminalsAndIcons = null;

    if (File.Exists(terminalsAndIconsPath))
    {
      using var fileStreamTerminals = File.Open(terminalsAndIconsPath, FileMode.Open);
      XmlSerializer serTerminals;
      serTerminals = new XmlSerializer(typeof(fmiTerminalsAndIcons));
      var terminalsAndIcons = serTerminals.Deserialize(fileStreamTerminals) as fmiTerminalsAndIcons;
      if (terminalsAndIcons != null)
      {
        TerminalsAndIcons = new TerminalsAndIcons(terminalsAndIcons, modelDescription, logCallback);
      }

      if (TerminalsAndIcons == null)
      {
        throw new NullReferenceException("Failed to initialize terminal and icons object.");
      }
    }

    return TerminalsAndIcons;
  }

  public static FmiVersions FindFmiVersion(string fmuPath)
  {
    Stream stream;
    if (Path.GetExtension(fmuPath) == ".xml")
    {
      stream = File.Open(fmuPath, FileMode.Open);
    }
    else
    {
      var zip = ZipFile.Open(fmuPath, ZipArchiveMode.Read);
      var zipEntry = zip.GetEntry("modelDescription.xml");
      if (zipEntry == null)
      {
        throw new NullReferenceException("FMU does not have a model description.");
      }

      stream = zipEntry.Open();
    }

    var doc = new XmlDocument();
    doc.Load(stream);

    var root = doc.DocumentElement;
    var node = root?.SelectSingleNode("//fmiModelDescription");
    var attr = node?.Attributes?.GetNamedItem("fmiVersion");
    var returnValue = attr?.Value;

    stream.Close();
    stream.Dispose();

    switch (returnValue)
    {
      case "2.0":
        return FmiVersions.Fmi2;
      case "3.0":
        return FmiVersions.Fmi3;
      default:
        return FmiVersions.Invalid;
    }
  }

  private static bool IsLayeredStandardBus(in TerminalsAndIcons? terminalsAndIcons)
  {
    return (terminalsAndIcons != null) && terminalsAndIcons.Terminals.Values.Any(terminal => terminal.TerminalKind == "org.fmi-ls-bus.network-terminal");
  }

  public static void CheckCoSimAttributes(in ModelDescription modelDescription, in TerminalsAndIcons? TerminalsAndIcons, Action<LogSeverity, string> logCallback)
  {
    var coSimulation = modelDescription.CoSimulation;
    // Check general CoSimulation attributes' requirements
    if(coSimulation.ProvidesIntermediateUpdate)
    {
      logCallback.Invoke(LogSeverity.Warning, "[providesIntermediateUpdate=true] has been loaded as a Co-Simulation attribute but the FMU Importer expects it to be [providesIntermediateUpdate=false].");
    }

    if (coSimulation.MightReturnEarlyFromDoStep)
    {
      logCallback.Invoke(LogSeverity.Warning, "[mightReturnEarlyFromDoStep=true] has been loaded as a Co-Simulation attribute but the FMU Importer expects it to be [mightReturnEarlyFromDoStep=false].");
    }

    if (coSimulation.CanReturnEarlyAfterIntermediateUpdate)
    {
      logCallback.Invoke(LogSeverity.Warning, "[canReturnEarlyAfterIntermediateUpdate=true] has been loaded as a Co-Simulation attribute but the FMU Importer expects it to be [canReturnEarlyAfterIntermediateUpdate=false].");
    }

    // Check FLI-LS-BUS specific attributes' requirements
    if (IsLayeredStandardBus(in TerminalsAndIcons) && !coSimulation.hasEventMode)
    {
      logCallback.Invoke(LogSeverity.Warning, "[hasEventMode=false] has been loaded as a Co-Simulation attribute but the FMU Importer expects it to be [hasEventMode=true] for FMI-LS-BUS compatibility.");
    }

    // Special case of the FixedInternalStepSize that defaults to 1ms if not set in modelDescription.xml
    if (modelDescription.FmiVersion == FmiVersions.Fmi3.ToString())
    {
      if (coSimulation.FixedInternalStepSize == null)
      {
        logCallback.Invoke(LogSeverity.Warning, "The Model Description lacks the Co-Simulation attribute [fixedInternalStepSize]. The FMU Importer will internally default it to 1[ms].");
      }
    }
  }
}

