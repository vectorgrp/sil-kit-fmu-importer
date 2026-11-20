// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;

namespace VcdlExporter.Tests.Helpers;

public record ExporterResult(int ExitCode, string StdOut, string StdErr)
{
  public string Output => StdOut + StdErr;

  public override string ToString()
  {
    return $"Exit code: {ExitCode}\n--- stdout ---\n{StdOut}\n--- stderr ---\n{StdErr}";
  }
}

/// <summary>
/// Runs the vCDL Exporter as a separate process, exactly like a user would.
/// This tests the command line, the exit code and the written file together.
/// </summary>
public static class ExporterRunner
{
  private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

  private static string ExporterDllPath
  {
    get
    {
      return Path.Combine(TestContext.CurrentContext.TestDirectory, "VcdlExporter.dll");
    }
  }

  public static ExporterResult Run(params string[] args)
  {
    Assert.That(File.Exists(ExporterDllPath), Is.True, $"The exporter was not found at '{ExporterDllPath}'.");

    var startInfo = new ProcessStartInfo
    {
      // DOTNET_HOST_PATH is set by the .NET CLI ('dotnet test'); fall back to the dotnet on the PATH
      FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      WorkingDirectory = TestContext.CurrentContext.TestDirectory
    };
    startInfo.ArgumentList.Add(ExporterDllPath);
    foreach (var arg in args)
    {
      startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo)!;
    var stdOutTask = process.StandardOutput.ReadToEndAsync();
    var stdErrTask = process.StandardError.ReadToEndAsync();

    if (!process.WaitForExit(Timeout))
    {
      process.Kill(entireProcessTree: true);
      Assert.Fail($"The exporter did not finish within {Timeout}. Arguments: {string.Join(' ', args)}");
    }

    var result = new ExporterResult(process.ExitCode, stdOutTask.Result, stdErrTask.Result);
    TestContext.WriteLine($"> VcdlExporter {string.Join(' ', args)}\n{result}");
    return result;
  }

  public static ExporterResult ExportFmu(string fmuPath, string vcdlPath, bool useClockPubSubElements = false)
  {
    return useClockPubSubElements
             ? Run("fmu", "-i", fmuPath, "-o", vcdlPath, "--use-clock-pub-sub-elements")
             : Run("fmu", "-i", fmuPath, "-o", vcdlPath);
  }

  public static ExporterResult ExportCommInterface(string cidPath, string vcdlPath, string interfaceName)
  {
    return Run("communicationInterface", "-i", cidPath, "-o", vcdlPath, "--interface-name", interfaceName);
  }
}
