namespace Fmi.FmiModel.Internal
{
  public class CoSimulation
  {
    public string ModelIdentifier { get; set; }
    public bool NeedsExecutionTool { get; set; }
    // NB: By concept of this importer, the following can never be a problem
    public bool CanBeInstantiatedOnlyOncePerProcess { get; set; }
    public bool CanHandleVariableCommunicationStepSize { get; set; }
    public double? FixedInternalStepSize { get; set; }
    public uint MaxOutputDerivativeOrder { get; set; }

    public CoSimulation(Fmi3.fmi3CoSimulation input)
    {
      ModelIdentifier = input.modelIdentifier;
      NeedsExecutionTool = input.needsExecutionTool;
      CanBeInstantiatedOnlyOncePerProcess = input.canBeInstantiatedOnlyOncePerProcess;

      CanHandleVariableCommunicationStepSize = input.canHandleVariableCommunicationStepSize;
      FixedInternalStepSize = (input.fixedInternalStepSizeSpecified) ? input.fixedInternalStepSize : null;
      MaxOutputDerivativeOrder = input.maxOutputDerivativeOrder;
    }

    public CoSimulation(Fmi2.fmiModelDescriptionCoSimulation input)
    {
      ModelIdentifier = input.modelIdentifier;
      NeedsExecutionTool = input.needsExecutionTool;
      CanBeInstantiatedOnlyOncePerProcess = input.canBeInstantiatedOnlyOncePerProcess;

      CanHandleVariableCommunicationStepSize = input.canHandleVariableCommunicationStepSize;
      MaxOutputDerivativeOrder = input.maxOutputDerivativeOrder;
    }
  }
}