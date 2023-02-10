using System.IO.Compression;
using System.Xml;
using System.Xml.Serialization;
using Fmi.FmiModel.Internal;

#pragma warning disable CS8618 // non-nullable field must be initialized in constructor

namespace Fmi.FmiModel
{
  public class ModelLoader
  {
    public enum FmiVersions
    {
      Invalid,
      Fmi2,
      Fmi3
    }

    internal static string ExtractFmu(string fmuPath)
    {
      // check if the directory exists
      var targetFolderPath = 
        $"{Path.GetDirectoryName(fmuPath)}/{Path.GetFileNameWithoutExtension(fmuPath)}";

      // TODO switch to temporary directory as soon as everything works as intended
      //var dir = Directory.CreateTempSubdirectory(
      //  $"SilKitImporter_{Path.GetFileNameWithoutExtension(fmuPath)}");
      if (Directory.Exists(targetFolderPath) && Directory.EnumerateFileSystemEntries(targetFolderPath).Any())
      {
        // directory exists and has entries -> skip extraction
        return targetFolderPath;
      }

      var dir = Directory.CreateDirectory(
          $"{Path.GetDirectoryName(fmuPath)}/{Path.GetFileNameWithoutExtension(fmuPath)}");

      ZipFile.ExtractToDirectory(fmuPath, dir.FullName);
      return dir.FullName;
    }

    internal static void RemoveExtractedFmu(string fmuPath)
    {
      var dir = $"{Path.GetDirectoryName(fmuPath)}/{Path.GetFileNameWithoutExtension(fmuPath)}";
      if (Directory.Exists(dir))
      {
        Directory.Delete(dir, true);
      }
    }

    internal static ModelDescription LoadModelFromExtractedPath(string extractedFmuPath)
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
          Fmi2.fmiModelDescription? fmiModelDescription = ser.Deserialize(fileStream) as Fmi2.fmiModelDescription;
          if (fmiModelDescription != null)
          {
            commonDescription = new ModelDescription(fmiModelDescription);
          }

          break;
        }
        case FmiVersions.Fmi3:
        {
          ser = new XmlSerializer(typeof(Fmi3.fmiModelDescription));
          Fmi3.fmiModelDescription? fmiModelDescription = ser.Deserialize(fileStream) as Fmi3.fmiModelDescription;
          if (fmiModelDescription != null)
          {
            commonDescription = new ModelDescription(fmiModelDescription);
          }

          break;
        }
        default:
          throw new NotSupportedException();
      }

      if (commonDescription == null)
      {
        throw new NullReferenceException("Failed to initialize model description object");
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
        ZipArchive zip = ZipFile.Open(fmuPath, ZipArchiveMode.Read);
        var zipEntry = zip.GetEntry("modelDescription.xml");
        if (zipEntry == null)
        {
          throw new NullReferenceException("fmu does not have a model description.");
        }
        stream = zipEntry.Open();
      }

      XmlDocument doc = new XmlDocument();
      doc.Load(stream);

      XmlElement? root = doc.DocumentElement;
      XmlNode? node = root?.SelectSingleNode("//fmiModelDescription");
      var attr = node?.Attributes?.GetNamedItem("fmiVersion");
      var returnValue = attr?.Value;

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
}
