// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Config;
using FmuImporter.Exceptions;

namespace FmuImporter.Tests;

[TestFixture]
public class ConfigFileTests
{
  private Configuration CheckNoThrowNotNull(string configPath)
  {
    Assert.Multiple(
      () =>
      {
        Assert.That(() => ConfigParser.LoadConfiguration(configPath), Throws.Nothing);
        Assert.That(() => ConfigParser.LoadConfiguration(configPath), Is.Not.Null);
      });
    return ConfigParser.LoadConfiguration(configPath);
  }

  [SetUp]
  public void Setup()
  {
  }

#region general config file checks

  [Test, Order(0)]
  public void Configuration_Basic_FileNotAvailable()
  {
    var configPath = "HelloWorld";

    Assert.Throws<FileNotFoundException>(() => ConfigParser.LoadConfiguration(configPath));
  }

  [Test, Order(0)]
  public void Configuration_Basic_RelativePath()
  {
    var configPath = "Configs/Basic/MinValid.yaml";

    CheckNoThrowNotNull(configPath);
  }

  [Test, Order(0)]
  public void Configuration_Basic_AbsolutePath()
  {
    var configPath = Path.GetFullPath("Configs/Basic/MinValid.yaml");

    CheckNoThrowNotNull(configPath);
  }

  [Test, Order(0)]
  public void Configuration_Basic_EmptyFile()
  {
    var configPath = "Configs/Basic/InvalidEmpty.yaml";

    Assert.Throws<InvalidConfigurationException>(() => ConfigParser.LoadConfiguration(configPath));
  }

  [Test, Order(0)]
  public void Configuration_Basic_NoConfigFormat()
  {
    var configPath = "Configs/Basic/InvalidNoConfigFormat.yaml";

    Assert.Throws<InvalidConfigurationException>(() => ConfigParser.LoadConfiguration(configPath));
  }

  [Test, Order(0)]
  public void Configuration_Basic_NoVersionField()
  {
    var configPath = "Configs/Basic/InvalidNoVersionField.yaml";

    Assert.Throws<InvalidConfigurationException>(() => ConfigParser.LoadConfiguration(configPath));
  }

#endregion general config file checks

#region include block checks

  [Test, Order(1)]
  public void Configuration_Include_IncludeRelative()
  {
    var configPath = "Configs/Include/IncludeRelativePath.yaml";

    var cfg = CheckNoThrowNotNull(configPath);

    Assert.Multiple(
      () =>
      {
        Assert.That(() => cfg.Include != null, Is.True);
        Assert.That(() => cfg.Include!.Count, Is.EqualTo(1));
      });
  }

  [Test, Order(1)]
  public void Configuration_Include_IncludeAbsolute()
  {
    var configPath = "Configs/Include/IncludeAbsolutePath.yaml";

    // prepare file
    File.Copy("Configs/Include/IncludeEmpty.yaml", configPath);
    try
    {
      File.AppendAllText(
        configPath,
        Environment.NewLine + "  - " + Path.GetFullPath("Configs/Basic/ValidMin.yaml"));

      var cfg = CheckNoThrowNotNull(configPath);

      Assert.Multiple(
        () =>
        {
          Assert.That(() => cfg.Include != null, Is.True);
          Assert.That(() => cfg.Include!.Count, Is.EqualTo(1));
        });
    }
    finally
    {
      // delete file
      File.Delete(configPath);
    }
  }

  [Test, Order(1)]
  public void Configuration_Include_IncludeEmpty()
  {
    var configPath = "Configs/Include/IncludeEmpty.yaml";

    var cfg = CheckNoThrowNotNull(configPath);

    Assert.Multiple(
      () =>
      {
        Assert.That(() => (cfg.Include == null || cfg.Include.Count == 0), Is.True);
        Assert.That(
          () => cfg.MergeIncludes(),
          Throws.Nothing);
      });
  }

  [Test, Order(1)]
  public void Configuration_Include_InvalidPath()
  {
    var configPath = "Configs/Include/IncludeInvalidPath.yaml";

    Assert.Throws<InvalidConfigurationException>(
      () => ConfigParser.LoadConfiguration(configPath).MergeIncludes());
  }

  [Test, Order(1)]
  public void Configuration_Include_IncludeInvalidConfig()
  {
    var configPath = "Configs/Include/IncludeInvalidConfig.yaml";

    Assert.Throws<InvalidConfigurationException>(
      () => ConfigParser.LoadConfiguration(configPath).MergeIncludes());
  }

  [Test, Order(1)]
  public void Configuration_Include_IncludeOkCircular()
  {
    var configPath = "Configs/Include/IncludeCircularRoot.yaml";

    // Include itself is checked in another test

    Assert.That(() => ConfigParser.LoadConfiguration(configPath).MergeIncludes(), Throws.Nothing);
  }

  [Test, Order(1)]
  public void Configuration_Include_IncludeOkSelf()
  {
    var configPath = "Configs/Include/IncludeCircularSelf.yaml";

    // Include itself is checked in another test

    Assert.That(() => ConfigParser.LoadConfiguration(configPath).MergeIncludes(), Throws.Nothing);
  }

#endregion include block checks

#region parameters block checks

  [Test, Order(1)]
  public void Configuration_Parameters_EmptyParameters()
  {
    var configPath = "Configs/Parameters/EmptyParams.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.Multiple(
      () =>
      {
        Assert.That(() => (cfg.Parameters == null || cfg.Parameters.Count == 0), Is.True);
        Assert.That(
          () => cfg.GetParameters(),
          Throws.Nothing);
        Assert.That(
          () => cfg.GetParameters().Count == 0,
          Is.True);
      });
  }

  [Test, Order(1)]
  public void Configuration_Parameters_OneParam()
  {
    var configPath = "Configs/Parameters/OneParam.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.Multiple(
      () =>
      {
        Assert.That(() => cfg.Parameters != null, Is.True);
        Assert.That(() => cfg.Parameters!.Count, Is.EqualTo(1));
        Assert.That(
          () => cfg.GetParameters(),
          Throws.Nothing);
        Assert.That(
          () => cfg.GetParameters().Count,
          Is.EqualTo(1));
      });
  }

  [Test, Order(1)]
  public void Configuration_Parameters_MultiParams()
  {
    var configPath = "Configs/Parameters/MultiParams.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.Multiple(
      () =>
      {
        Assert.That(() => cfg.Parameters != null, Is.True);
        Assert.That(() => cfg.Parameters!.Count, Is.EqualTo(2));
        Assert.That(
          () => cfg.GetParameters(),
          Throws.Nothing);
        Assert.That(
          () => cfg.GetParameters().Count,
          Is.EqualTo(2));
      });
  }

  [Test, Order(1)]
  public void Configuration_Parameters_InvalidNoVarName()
  {
    var configPath = "Configs/Parameters/InvalidNoVarName.yaml";

    Assert.Throws<InvalidConfigurationException>(
      () => ConfigParser.LoadConfiguration(configPath));
  }

  [Test, Order(1)]
  public void Configuration_Parameters_InvalidNoValue()
  {
    var configPath = "Configs/Parameters/InvalidNoValue.yaml";

    Assert.Throws<InvalidConfigurationException>(
      () => ConfigParser.LoadConfiguration(configPath));
  }

  [Test, Order(1)]
  public void Configuration_Parameters_InvalidUnknownField()
  {
    var configPath = "Configs/Parameters/InvalidUnknownField.yaml";

    Assert.Throws<InvalidConfigurationException>(
      () => ConfigParser.LoadConfiguration(configPath));
  }

#endregion parameters block checks


#region variableMapping block checks

  [Test, Order(1)]
  public void Configuration_VariableMapping_EmptyMappings()
  {
    var configPath = "Configs/VariableMappings/EmptyMapping.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.Multiple(
      () =>
      {
        Assert.That(() => (cfg.VariableMappings == null || cfg.VariableMappings.Count == 0), Is.True);
        Assert.That(
          () => cfg.GetVariables(),
          Throws.Nothing);
        Assert.That(
          () => cfg.GetVariables().Count == 0,
          Is.True);
      });
  }

  [Test, Order(1)]
  public void Configuration_VariableMapping_OneMapping()
  {
    var configPath = "Configs/VariableMappings/OneMapping.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.Multiple(
      () =>
      {
        Assert.That(() => cfg.VariableMappings != null, Is.True);
        Assert.That(() => cfg.VariableMappings!.Count, Is.EqualTo(1));
        Assert.That(
          () => cfg.GetVariables(),
          Throws.Nothing);
        Assert.That(
          () => cfg.GetVariables().Count,
          Is.EqualTo(1));
      });
  }

  [Test, Order(1)]
  public void Configuration_VariableMapping_MultiMappings()
  {
    var configPath = "Configs/VariableMappings/MultiMappings.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.Multiple(
      () =>
      {
        Assert.That(() => cfg.VariableMappings != null, Is.True);
        Assert.That(() => cfg.VariableMappings!.Count, Is.EqualTo(2));
        Assert.That(
          () => cfg.GetVariables(),
          Throws.Nothing);
        Assert.That(
          () => cfg.GetVariables().Count,
          Is.EqualTo(2));
      });
  }

  [Test, Order(1)]
  public void Configuration_VariableMapping_SimpleMapping()
  {
    var configPath = "Configs/VariableMappings/OneMapping.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.That(
      () =>
      {
        var testVar = cfg.GetVariables().First().Value;
        return
          testVar.VariableName == "ValidInt32" &&
          testVar.TopicName == null &&
          testVar.Transformation == null;
      },
      Is.True);
  }

  [Test, Order(1)]
  public void Configuration_VariableMapping_AllFields()
  {
    var configPath = "Configs/VariableMappings/AllFields.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    Assert.That(
      () =>
      {
        var testVar = cfg.GetVariables().First().Value;
        return
          testVar.VariableName == "ValidInt32" &&
          testVar.TopicName == "RenamedValidInt32" &&
          testVar.Transformation != null &&
          testVar.Transformation.Offset == 1D &&
          testVar.Transformation.Factor == 2D &&
          testVar.Transformation.TransmissionType == "Int32" &&
          testVar.Transformation.ReverseTransform == true;
      },
      Is.True);
  }

#endregion variableMapping block checks

#region override mechanic checks

  [Test, Order(2)]
  public void Configuration_Override_Parameter()
  {
    var configPath = "Configs/Override/ParameterRoot.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    double res;

    var parameters = cfg.GetParameters();
    var localParam = parameters["OnlyLocal"];
    Assert.That(
      () =>
      {
        return
          localParam.VariableName == "OnlyLocal" &&
          localParam.Value != null &&
          double.TryParse((string)localParam.Value, out res) &&
          res == 7.0;
      },
      Is.True);

    var globalParam = parameters["Global"];
    Assert.That(
      () =>
      {
        return
          globalParam.VariableName == "Global" &&
          globalParam.Value != null &&
          double.TryParse((string)globalParam.Value, out res) &&
          res == 7.0;
      },
      Is.True);

    var includeParam = parameters["OnlyInclude"];
    Assert.That(
      () =>
      {
        return
          includeParam.VariableName == "OnlyInclude" &&
          includeParam.Value != null &&
          double.TryParse((string)includeParam.Value, out res) &&
          res == 7.0;
      },
      Is.True);
  }

  [Test, Order(2)]
  public void Configuration_Override_Mapping()
  {
    var configPath = "Configs/Override/MappingRoot.yaml";

    var cfg = CheckNoThrowNotNull(configPath);
    cfg.MergeIncludes();

    var variables = cfg.GetVariables();
    var topicOverride = variables["TopicOverride"];
    Assert.That(
      () =>
      {
        return
          topicOverride.VariableName == "TopicOverride" &&
          topicOverride.TopicName != null &&
          topicOverride.TopicName == "LocalTopic";
      },
      Is.True);

    var offsetOverride = variables["OffsetOverride"];
    Assert.That(
      () =>
      {
        return
          offsetOverride.VariableName == "OffsetOverride" &&
          offsetOverride.Transformation?.Offset != null &&
          offsetOverride.Transformation.Offset == 7.0;
      },
      Is.True);

    var factorOverride = variables["FactorOverride"];
    Assert.That(
      () =>
      {
        return
          factorOverride.VariableName == "FactorOverride" &&
          factorOverride.Transformation?.Factor != null &&
          factorOverride.Transformation.Factor == 7.0;
      },
      Is.True);
    var typeOverride = variables["TypeOverride"];
    Assert.That(
      () =>
      {
        return
          typeOverride.VariableName == "TypeOverride" &&
          typeOverride.Transformation?.TransmissionType != null &&
          typeOverride.Transformation.TransmissionType == "Float32";
      },
      Is.True);

    var reverseTransformToTrue = variables["ReverseTransformToTrueOverride"];
    Assert.That(
      () =>
      {
        return
          reverseTransformToTrue.VariableName == "ReverseTransformToTrueOverride" &&
          reverseTransformToTrue.Transformation?.ReverseTransform != null &&
          reverseTransformToTrue.Transformation?.ReverseTransform == true;
      },
      Is.True);

    var reverseTransformToFalse = variables["ReverseTransformToFalseOverride"];
    Assert.That(
      () =>
      {
        return
          reverseTransformToFalse.VariableName == "ReverseTransformToFalseOverride" &&
          reverseTransformToFalse.Transformation?.ReverseTransform != null &&
          reverseTransformToFalse.Transformation?.ReverseTransform == false;
      },
      Is.True);
  }

#endregion override mechanic checks
}
