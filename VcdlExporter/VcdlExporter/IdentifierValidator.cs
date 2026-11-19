// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Microsoft.Extensions.Logging;

namespace VcdlExporter;

public class InvalidIdentifierException : Exception
{
  public InvalidIdentifierException(string message) : base(message)
  {
  }
}

// Collects the names that cannot be written as vCDL identifiers, so that all of them are reported at once
internal class IdentifierValidator
{
  private readonly List<string> _issues = new();
  private readonly string _source;
  private readonly string? _structuredNameHint;
  private bool _hasStructuredName;

  // source: where the names come from (used in the error message)
  // structuredNameHint: optional advice added to the error message if a name contains '.'
  public IdentifierValidator(string source, string? structuredNameHint = null)
  {
    _source = source;
    _structuredNameHint = structuredNameHint;
  }

  // name may be null, e.g. if a required attribute is missing in the model description
  public void Check(string? name, string kind)
  {
    var problem = GetProblem(name);
    if (problem == null)
    {
      return;
    }

    _issues.Add($"{kind} '{name}': {problem}.");
    _hasStructuredName |= name?.Contains('.') == true;
  }

  // Reports names that are distinct in the source but become the same vCDL identifier
  public void ReportDuplicate(string name, string firstOriginalName, string secondOriginalName, string kind)
  {
    _issues.Add(
      $"{kind} '{name}': both '{firstOriginalName}' and '{secondOriginalName}' become '{name}', " +
      "which would create a duplicate.");
  }

  public void ThrowIfInvalid(ILogger logger)
  {
    if (_issues.Count == 0)
    {
      return;
    }

    foreach (var issue in _issues)
    {
      logger.LogError("{Issue}", issue);
    }

    var message = $"At least one name in the {_source} is not a valid vCDL identifier.";
    if (_hasStructuredName && _structuredNameHint != null)
    {
      message += " " + _structuredNameHint;
    }

    throw new InvalidIdentifierException(message);
  }

  private static string? GetProblem(string? name)
  {
    if (string.IsNullOrEmpty(name))
    {
      return "the name is empty";
    }

    var invalidCharacters = name.Where(c => !char.IsAsciiLetterOrDigit(c) && c != '_').Distinct().ToList();
    if (invalidCharacters.Count > 0)
    {
      return $"the character(s) {string.Join(", ", invalidCharacters.Select(c => $"'{c}'"))} are not allowed";
    }

    if (char.IsAsciiDigit(name[0]))
    {
      return "the name starts with a digit";
    }

    return null;
  }
}
