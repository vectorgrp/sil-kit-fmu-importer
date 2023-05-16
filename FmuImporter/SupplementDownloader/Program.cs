using System.CommandLine;
using System.Globalization;
using System.IO.Compression;

namespace Tests_Demos;

public class Demo
{
  // Entry point
  public static async Task Main(string[] args)
  {
    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand("Supplement Downloader for FMU Importer for SIL Kit");

    var silKitCommand = new Command("sil-kit", "Download SIL Kit library");
    silKitCommand.SetHandler(
      async () =>
      {
        await DownloadSilKit();
      });
    rootCommand.Add(silKitCommand);

    var referenceFmuCommand = new Command(
      "reference-fmus",
      "Download Modelica Reference FMUs and create sample config for SIL Kit FMU Importer");
    referenceFmuCommand.SetHandler(
      async () =>
      {
        await DownloadModelicaReferenceFmus();
      });
    rootCommand.Add(referenceFmuCommand);


    await rootCommand.InvokeAsync(args);
  }

  private static async Task DownloadSilKit()
  {
    const string silKitVersion = "4.0.23";
    const string osArchSuffixWin = "Win-x86_64-VS2017";
    const string osArchSuffixLinux = "ubuntu-18.04-x86_64-gcc";

#if OS_WINDOWS
    const string usedOsArch = osArchSuffixWin;
    const string libraryName = "SilKit.dll";
#elif OS_LINUX
    const string usedOsArch = osArchSuffixLinux;
    const string libraryName = "libSilKit.so";
#else
    Console.WriteLine("Unsupported OS");
    return;
#endif

    var silKitVersionName = $"SilKit-{silKitVersion}-{usedOsArch}";
    var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
    if (assemblyLocation == null)
    {
      Console.WriteLine("Failed to retrieve path of current application.");
      return;
    }

    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
    if (assemblyDirectory == null)
    {
      Console.WriteLine("Failed to retrieve directory of current application.");
      return;
    }

    var destinationPath = Path.GetFullPath(Path.Combine(assemblyDirectory, libraryName));

    if (File.Exists(destinationPath))
    {
      // SIL Kit library detected -> skip download
      return;
    }

    // Automatically download sil-kit
    // SampleURL: https://github.com/vectorgrp/sil-kit/releases/download/sil-kit%2Fv4.0.23/SilKit-4.0.23-Win-x86_64-VS2017.zip
    var uriWindows = new Uri(
      $"https://github.com/vectorgrp/sil-kit/releases/download/sil-kit%2Fv{silKitVersion}/{silKitVersionName}.zip");

    var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    Console.Write("Downloading SIL Kit package from github.com...");
    var response = await client.GetAsync(uriWindows);
    var contentStream = await response.Content.ReadAsStreamAsync();
    Console.Write($" Done.\nExtracting SIL Kit library ({libraryName})...");

    var zip = new ZipArchive(contentStream, ZipArchiveMode.Read, false);
#if OS_WINDOWS
    var zipEntry = zip.GetEntry($"{silKitVersionName}/SilKit/bin/{libraryName}");
#elif OS_LINUX
    var zipEntry = zip.GetEntry($"{silKitVersionName}/SilKit/lib/{libraryName}");
#else
    Console.WriteLine("Unsupported OS");
    return;
#endif

    if (zipEntry == null)
    {
      throw new NullReferenceException($"failed to retrieve {libraryName}.");
    }

    zipEntry.ExtractToFile(destinationPath);
    Console.WriteLine(" Done.");
  }

  private static async Task DownloadModelicaReferenceFmus()
  {
    const string refFmuVersion = "0.0.23";

    var refFmuArchiveNameNoExt = $"Reference-FMUs-{refFmuVersion}";
    var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
    if (assemblyLocation == null)
    {
      Console.WriteLine("Failed to retrieve path of current application.");
      return;
    }

    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
    if (assemblyDirectory == null)
    {
      Console.WriteLine("Failed to retrieve directory of current application.");
      return;
    }

    var destinationPath = Path.GetFullPath(Path.Combine(assemblyDirectory, refFmuArchiveNameNoExt));

    if (Directory.Exists(destinationPath))
    {
      // Existing reference FMU directory detected -> skip download
      return;
    }

    // Automatically download reference FMU
    // Sample URL: https://github.com/modelica/Reference-FMUs/releases/download/v0.0.23/Reference-FMUs-0.0.23.zip
    var uriWindows = new Uri(
      $"https://github.com/modelica/Reference-FMUs/releases/download/v{refFmuVersion}/{refFmuArchiveNameNoExt}.zip");

    var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    Console.Write("Downloading Modelica reference FMU archive from github.com...");
    var response = await client.GetAsync(uriWindows);
    var contentStream = await response.Content.ReadAsStreamAsync();
    Console.Write(" Done.\nExtracting content...");

    var zip = new ZipArchive(contentStream, ZipArchiveMode.Read, false);
    zip.ExtractToDirectory(destinationPath);
    Console.WriteLine(" Done.");
  }
}
