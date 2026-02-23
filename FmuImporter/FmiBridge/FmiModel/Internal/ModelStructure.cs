// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;

namespace Fmi.FmiModel.Internal;

public class ModelStructure
{
  public HashSet<uint> InitialUnknowns { get; set; }

  public ModelStructure(
    Fmi2.fmiModelDescriptionModelStructure fmiModelDescriptionModelStructure,
    System.Collections.ObjectModel.Collection<Fmi2.fmi2ScalarVariable> fmiModelDescriptionModelVariables)
  {

    // Validate that dependencies / dependenciesKind have matching lengths for all unknowns
    ValidateDependencies(fmiModelDescriptionModelStructure.Outputs);
    ValidateDependencies(fmiModelDescriptionModelStructure.Derivatives);
    ValidateDependencies(fmiModelDescriptionModelStructure.InitialUnknowns);
    
    InitialUnknowns = new HashSet<uint>();
    if (fmiModelDescriptionModelStructure.InitialUnknowns.Count > 0)
    {
      // convert to 0-based index
      var initialUnknownIndices =
        fmiModelDescriptionModelStructure.InitialUnknowns.Select(u => u.index - 1).ToHashSet();
      foreach (var initialUnknownIndex in initialUnknownIndices)
      {
        InitialUnknowns.Add(fmiModelDescriptionModelVariables[(int)initialUnknownIndex].valueReference);
      }
    }
  }

  public ModelStructure(Fmi3.fmiModelDescriptionModelStructure input)
  {
    // Validate dependencies / dependenciesKind pairing before mapping
    ValidateDependencies(input.Output);
    ValidateDependencies(input.ContinuousStateDerivative);
    ValidateDependencies(input.ClockedState);
    ValidateDependencies(input.InitialUnknown);
    ValidateDependencies(input.EventIndicator);

    if (input.InitialUnknown.Count == 0)
    {
      InitialUnknowns = new HashSet<uint>();
    }
    else
    {
      InitialUnknowns = input.InitialUnknown.Select(u => u.valueReference).ToHashSet();
    }
  }

  private static void ValidateDependencies(
  System.Collections.ObjectModel.Collection<Fmi3.fmi3Unknown>? unknowns)
  {
    if (unknowns == null)
    {
      return;
    }

    foreach (var unk in unknowns)
    {
      ValidateDependencies(
        unk.dependencies,
        unk.dependenciesKind,
        $"valueReference '{unk.valueReference}'");
    }
  }

  private static void ValidateDependencies(
    System.Collections.ObjectModel.Collection<Fmi2.fmi2VariableDependencyUnknown>? unknowns)
  {
    if (unknowns == null)
    {
      return;
    }

    foreach (var unk in unknowns)
    {
      ValidateDependencies(
        unk.dependencies,
        unk.dependenciesKind,
        $"index '{unk.index}'");
    }
  }

  private static void ValidateDependencies(
    System.Collections.ObjectModel.Collection<Fmi2.fmiModelDescriptionModelStructureInitialUnknownsUnknown>? unknowns)
  {
    if (unknowns == null)
    {
      return;
    }

    foreach (var unk in unknowns)
    {
      ValidateDependencies(
        unk.dependencies,
        unk.dependenciesKind,
        $"index '{unk.index}'");
    }
  }

  private static void ValidateDependencies<TDep, TKind>(
    System.Collections.ObjectModel.Collection<TDep>? deps,
    System.Collections.ObjectModel.Collection<TKind>? kinds,
    string id)
  {
    var depsCount = deps?.Count ?? 0;
    var kindsCount = kinds?.Count ?? 0;

    if (kindsCount > 0 && depsCount == 0)
    {
      throw new ModelDescriptionException(
        $"Inconsistent ModelStructure entry for {id}: " +
        $"dependenciesKind has {kindsCount} entries but dependencies is empty.");
    }

    if (kindsCount > 0 && depsCount != kindsCount)
    {
      throw new ModelDescriptionException(
        $"Inconsistent ModelStructure entry for {id}: " +
        $"dependencies count ({depsCount}) does not match dependenciesKind count ({kindsCount}).");
    }
  }
}
