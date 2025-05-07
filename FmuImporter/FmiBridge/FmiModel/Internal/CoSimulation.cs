// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.FmiModel.Internal;

public class CoSimulation
{
  public string ModelIdentifier { get; set; }
  public bool NeedsExecutionTool { get; set; }
  public bool CanHandleVariableCommunicationStepSize { get; set; }
  public double? FixedInternalStepSize { get; set; }
  public uint MaxOutputDerivativeOrder { get; set; }
  public int RecommendedIntermediateInputSmoothness { get; set; }
  public bool ProvidesIntermediateUpdate { get; set; }
  public bool MightReturnEarlyFromDoStep { get; set; }
  public bool CanReturnEarlyAfterIntermediateUpdate { get; set; }
  public bool hasEventMode { get; set; }
  public bool CanBeInstantiatedOnlyOncePerProcess { get; set; }
  
  public CoSimulation(Fmi3.fmi3CoSimulation input)
  {
    ModelIdentifier = input.modelIdentifier;
    NeedsExecutionTool = input.needsExecutionTool;
    CanBeInstantiatedOnlyOncePerProcess = input.canBeInstantiatedOnlyOncePerProcess;
    
    CanHandleVariableCommunicationStepSize = input.canHandleVariableCommunicationStepSize;
    FixedInternalStepSize = (input.fixedInternalStepSizeSpecified) ? input.fixedInternalStepSize : null;
    MaxOutputDerivativeOrder = input.maxOutputDerivativeOrder;
    RecommendedIntermediateInputSmoothness = input.recommendedIntermediateInputSmoothness;
    ProvidesIntermediateUpdate = input.providesIntermediateUpdate;
    MightReturnEarlyFromDoStep = input.mightReturnEarlyFromDoStep;
    CanReturnEarlyAfterIntermediateUpdate = input.canReturnEarlyAfterIntermediateUpdate;
    hasEventMode = input.hasEventMode;
  }

  public CoSimulation(Fmi2.fmiModelDescriptionCoSimulation input)
  {
    ModelIdentifier = input.modelIdentifier;
    NeedsExecutionTool = input.needsExecutionTool;
    CanBeInstantiatedOnlyOncePerProcess = input.canBeInstantiatedOnlyOncePerProcess;

    CanHandleVariableCommunicationStepSize = input.canHandleVariableCommunicationStepSize;
    MaxOutputDerivativeOrder = input.maxOutputDerivativeOrder;
    hasEventMode = false;
  }
}
