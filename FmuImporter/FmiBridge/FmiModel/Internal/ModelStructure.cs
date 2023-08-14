// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.FmiModel.Internal;

public class ModelStructure
{
  public HashSet<uint> InitialUnknowns { get; set; }

  public ModelStructure(Fmi2.fmiModelDescriptionModelStructure fmiModelDescriptionModelStructure)
  {
    if (fmiModelDescriptionModelStructure.InitialUnknowns == null)
    {
      InitialUnknowns = new HashSet<uint>();
    }
    else
    {
      InitialUnknowns = fmiModelDescriptionModelStructure.InitialUnknowns.Unknown.Select(u => u.index).ToHashSet();
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
