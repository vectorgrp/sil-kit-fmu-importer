﻿// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
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

    TerminalsAndIcons? TerminalsAndIcons = null;

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

          var terminalsAndIconsPath = $"{extractedFmuPath}/terminalsAndIcons/terminalsAndIcons.xml";
          if (File.Exists(terminalsAndIconsPath))
          {
            using var fileStreamTerminals = File.Open(terminalsAndIconsPath, FileMode.Open);
            XmlSerializer serTerminals;
            serTerminals = new XmlSerializer(typeof(fmiTerminalsAndIcons));
            var terminalsAndIcons = serTerminals.Deserialize(fileStreamTerminals) as fmiTerminalsAndIcons;
            if (terminalsAndIcons != null)
            {
              TerminalsAndIcons = new TerminalsAndIcons(terminalsAndIcons, commonDescription, logCallback);
            }
          }
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
}
