// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.FmiModel.Internal;

public class ModelStructure
{
  public HashSet<uint> InitialUnknowns { get; set; }

  public ModelStructure(
    Fmi2.fmiModelDescriptionModelStructure fmiModelDescriptionModelStructure,
    Fmi2.fmiModelDescriptionModelVariables fmiModelDescriptionModelVariables)
  {
    InitialUnknowns = new HashSet<uint>();
    if (fmiModelDescriptionModelStructure.InitialUnknowns != null)
    {
      // convert to 0-based index
      var initialUnknownIndices =
        fmiModelDescriptionModelStructure.InitialUnknowns.Unknown.Select(u => u.index - 1).ToHashSet();
      foreach (var initialUnknownIndex in initialUnknownIndices)
      {
        InitialUnknowns.Add(fmiModelDescriptionModelVariables.ScalarVariable[initialUnknownIndex].valueReference);
      }
    }
  }

  public ModelStructure(Fmi3.fmiModelDescriptionModelStructure fmiModelDescriptionModelStructure)
  {
    if (fmiModelDescriptionModelStructure.InitialUnknown == null)
    {
      InitialUnknowns = new HashSet<uint>();
    }
    else
    {
      InitialUnknowns = fmiModelDescriptionModelStructure.InitialUnknown.Select(u => u.valueReference).ToHashSet();
    }
  }
}
