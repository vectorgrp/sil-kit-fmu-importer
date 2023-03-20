namespace Fmi.FmiModel.Internal;

public class DefaultExperiment
{
  public double StartTime { get; set; }
  public double? StopTime { get; set; }
  public double? Tolerance { get; set; }
  public double? StepSize { get; set; }

  public DefaultExperiment(Fmi3.fmiModelDescriptionDefaultExperiment input)
  {
    StartTime = input.startTime;
    StopTime = (input.stopTimeSpecified) ? input.stopTime : null;
    Tolerance = (input.toleranceSpecified) ? input.tolerance : null;
    StepSize = (input.stepSizeSpecified) ? input.stepSize : null;
  }

  public DefaultExperiment(Fmi2.fmiModelDescriptionDefaultExperiment input)
  {
    StartTime = input.startTime;
    StopTime = (input.stopTimeSpecified) ? input.stopTime : null;
    Tolerance = (input.toleranceSpecified) ? input.tolerance : null;
    StepSize = (input.stepSizeSpecified) ? input.stepSize : null;
  }
}
