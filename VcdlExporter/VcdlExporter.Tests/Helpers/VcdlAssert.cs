// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace VcdlExporter.Tests.Helpers;

public static class VcdlAssert
{
  public static string ExpectedFixture(string relativePath)
  {
    return Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Expected", relativePath);
  }

  /// <summary>
  /// Compares the written vCDL with the expected file, ignoring line ending differences.
  /// On mismatch, the full actual output is attached to the failure message so that the expected file can be
  /// reviewed and updated.
  /// </summary>
  public static void MatchesExpected(string actualVcdlPath, string expectedRelativePath)
  {
    MatchesExpectedFile(actualVcdlPath, ExpectedFixture(expectedRelativePath));
  }

  public static void MatchesExpectedFile(string actualVcdlPath, string expectedVcdlPath)
  {
    Assert.That(File.Exists(actualVcdlPath), Is.True, $"The vCDL file '{actualVcdlPath}' was not written.");

    var expected = Normalize(File.ReadAllText(expectedVcdlPath));
    var actual = Normalize(File.ReadAllText(actualVcdlPath));

    Assert.That(
      actual,
      Is.EqualTo(expected),
      $"The vCDL differs from '{expectedVcdlPath}'. Actual vCDL:\n{actual}");
  }

  public static void WasNotWritten(string vcdlPath)
  {
    Assert.That(
      File.Exists(vcdlPath),
      Is.False,
      "No vCDL file must be written when the export fails, otherwise an invalid file may be used by mistake.");
  }

  public static string Normalize(string vcdl)
  {
    return vcdl.Replace("\r\n", "\n");
  }
}
