// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Config;

public class VariableConfiguration
{
  public VariableConfiguration()
  {
    VariableName = string.Empty;
    TopicName = string.Empty;
  }

  public VariableConfiguration(string variableName, string topicName)
  {
    VariableName = variableName;
    TopicName = topicName;
  }

  [Required]
  public string VariableName { get; set; }

  public string? TopicName { get; set; }
  public Transformation? Transformation { get; set; }
}
