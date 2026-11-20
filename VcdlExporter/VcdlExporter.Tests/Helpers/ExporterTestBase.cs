// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace VcdlExporter.Tests.Helpers;

/// <summary>
/// Gives each test its own empty working directory, deleted after the test.
/// </summary>
public abstract class ExporterTestBase
{
  protected string WorkDir { get; private set; } = default!;

  protected string OutputVcdl
  {
    get
    {
      return Path.Combine(WorkDir, "output.vcdl");
    }
  }

  [SetUp]
  public void CreateWorkDir()
  {
    WorkDir = Path.Combine(Path.GetTempPath(), "VcdlExporterTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(WorkDir);
  }

  [TearDown]
  public void DeleteWorkDir()
  {
    try
    {
      Directory.Delete(WorkDir, true);
    }
    catch (IOException)
    {
      // a file may still be locked by a crashed exporter process - not relevant for the test result
    }
  }

  protected string BuildFmu(string modelDescriptionFixture, IDictionary<string, string>? additionalEntries = null)
  {
    return FmuBuilder.Build(FmuBuilder.ModelDescriptionFixture(modelDescriptionFixture), WorkDir, additionalEntries);
  }

  protected static string CidFixture(string relativePath)
  {
    return Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "CIDs", relativePath);
  }
}
