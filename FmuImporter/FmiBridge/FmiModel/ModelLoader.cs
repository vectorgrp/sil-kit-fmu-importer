// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;
using Fmi.FmiModel.Internal;
using Fmi3;

namespace Fmi.FmiModel;

public class ModelLoader
{
  internal static string GenerateHashString(string fmuPath)
  {
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(fmuPath);
    var hashBytes = sha256.ComputeHash(stream);
    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
  }

  internal static void SpawnFmuHashFile(string fmuPath, string outputDirectory)
  {
    var filePath = Path.Combine(outputDirectory, "fmu_SHA256.txt");
    File.WriteAllText(filePath, GenerateHashString(fmuPath));
  }

  private static string GetTargetFolderPath(string fmuPath)
  {
    return Path.Combine(Path.GetDirectoryName(fmuPath)!, Path.GetFileNameWithoutExtension(fmuPath));
  }

  private static void ExtractFmuToDirectory(string fmuPath, string destinationPath)
  {
    Directory.CreateDirectory(destinationPath);
    ZipFile.ExtractToDirectory(fmuPath, destinationPath);
  }

  internal static bool ShaComparisonIsOk(string fmuPath)
  {
    var fmuDirectory = Path.Combine(Path.GetDirectoryName(fmuPath)!, Path.GetFileNameWithoutExtension(fmuPath));
    var hashFilePath = Path.Combine(fmuDirectory, "fmu_SHA256.txt");

    if (!File.Exists(hashFilePath))
    {
      return false;
    }

    var existingHash = File.ReadAllText(hashFilePath).Trim();
    var currentHash = GenerateHashString(fmuPath);

    return string.Equals(existingHash, currentHash, StringComparison.OrdinalIgnoreCase);
  }

  public static void CreatePersistentFmuArtifacts(string fmuPath)
  {
    var targetFolderPath = GetTargetFolderPath(fmuPath);

    if (Directory.Exists(targetFolderPath))
    {
      Console.WriteLine($"An extracted FMU already exists at [{targetFolderPath}], overwriting the directory with the new FMU.");
      Directory.Delete(targetFolderPath, recursive: true);
    }

    ExtractFmuToDirectory(fmuPath, targetFolderPath);
    SpawnFmuHashFile(fmuPath, targetFolderPath);

    Console.WriteLine($"Extracted FMU to the following persistent directory [{targetFolderPath}]");
  }

  internal static void ExtractFmu(string fmuPath, bool usePersistedFmu, out string extractedPath, out bool isTemporary, Action<LogSeverity, string> logCallback)
  {
    var targetFolderPath = GetTargetFolderPath(fmuPath);
    bool folderExists = Directory.Exists(targetFolderPath) && Directory.EnumerateFileSystemEntries(targetFolderPath).Any();

    if(usePersistedFmu)
    {
      if(folderExists) 
      {
        isTemporary = false;
        extractedPath = targetFolderPath;
        if (ShaComparisonIsOk(fmuPath))
        {
          logCallback.Invoke(LogSeverity.Debug, $"Persistence SHA256 check passed.");
          return;
        }
        else
        {
          throw new InvalidOperationException(
          $"Persistence SHA256 check failed: hash of FMU at '{fmuPath}' does not match the hash in '{targetFolderPath}'.\n" +
          $"Persistent artifacts may be outdated or invalid. Please regenerate them by running:\n" +
          $"FmuImporter(.exe) -f {fmuPath} --persist");
        }
      }
      else 
      {
        throw new InvalidOperationException(
          $"Persistence use requested, but no persistent FMU found at '{targetFolderPath}'.\n" +
          $"Please generate it by running: FmuImporter(.exe) -f {fmuPath} --persist\")");
      }
    }

    var tempDirectory = CreateTempDirectory();
    var tempTargetPath = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(fmuPath));

    ExtractFmuToDirectory(fmuPath, tempTargetPath);

    extractedPath = tempTargetPath;
    isTemporary = true;
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

  private static string LoadAndNormalizeModelDescriptionXml(string modelDescriptionPath)
  {
    // Normalize empty dependencies / dependenciesKind attributes (e.g. dependencies="")
    // which some generated XML classes cannot handle for collection-backed attributes.
    var xml = File.ReadAllText(modelDescriptionPath);
    xml = xml.Replace(" dependencies=\"\"", string.Empty)
             .Replace(" dependenciesKind=\"\"", string.Empty);
    return xml;
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

    switch (fmiVersion)
    {
      case FmiVersions.Fmi2:
      {
        var xml = LoadAndNormalizeModelDescriptionXml(modelDescriptionPath);
        using var stringReader = new StringReader(xml);
        var ser = new XmlSerializer(typeof(Fmi2.fmiModelDescription));
        var fmiModelDescription = ser.Deserialize(stringReader) as Fmi2.fmiModelDescription;
        if (fmiModelDescription != null)
        {
          commonDescription = new ModelDescription(fmiModelDescription, logCallback);
        }

        break;
      }
      case FmiVersions.Fmi3:
      {
        var xml = LoadAndNormalizeModelDescriptionXml(modelDescriptionPath);
        using var stringReader = new StringReader(xml);
        var ser = new XmlSerializer(typeof(fmiModelDescription));
        var fmiModelDescription = ser.Deserialize(stringReader) as Fmi3.fmiModelDescription;
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

