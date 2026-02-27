// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using FmuImporter.Fmu;
using FmuImporter.Models.CommDescription;
using FmuImporter.Models.Config;
using FmuImporter.Models.Exceptions;
using FmuImporter.SilKit;
using SilKit.Services.Can;
using SilKit.Services.Logger;
using SilKit.Services.Orchestration;
using SilKit.Services.Rpc;
using System.Data;
using System.Diagnostics;

namespace FmuImporter;

public class FmuImporter
{
  private enum SilKitStatus
  {
    Uninitialized,
    Initialized, // Connected & Logger available
    LifecycleStarted,
    Stopped,
    ShutDown
  }

  private SilKitStatus CurrentSilKitStatus { get; set; } = SilKitStatus.Uninitialized;

  private SilKitEntity SilKitEntity { get; }
  private SilKitDataManager SilKitDataManager { get; }
  private SilKitCanManager SilKitCanManager { get; }
  private FmuEntity FmuEntity { get; }
  private FmuDataManager FmuDataManager { get; set; }
  private FmuCanManager FmuCanManager { get; set; }
  private FmuRpcManager FmuRpcClientManager { get; set; }
  private SilKitRpcClientManager SilKitRpcClientManager { get; set; }
  private FmuRpcManager FmuRpcServerManager { get; set; }
  private SilKitRpcServerManager SilKitRpcServerManager { get; set; }

  private readonly Configuration _fmuImporterConfig = null!;
  private readonly CommunicationInterfaceInternal? _fmuImporterCommInterface;

  private readonly Dictionary<string, Parameter>? _configuredParameters;
  private readonly Dictionary<string, Parameter>? _configuredStructuralParameters;


  public FmuImporter(
    string fmuPath,
    string silKitConfigurationPath,
    string? fmuImporterConfigFilePath,
    string? fmuImporterCommInterfaceFilePath,
    string participantName,
    LifecycleService.LifecycleConfiguration.Modes lifecycleMode,
    TimeSyncModes timeSyncMode,
    bool usePersistedFmu,
    bool useClockPubSubElements)
  {
    AppDomain.CurrentDomain.UnhandledException +=
      (sender, e) =>
      {
        if (Environment.ExitCode == ExitCodes.Success)
        {
          Environment.ExitCode = ExitCodes.UnhandledException;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        // Log unhandled exceptions
        if (e.ExceptionObject is Exception ex)
        {
          Console.WriteLine($"Unhandled exception: {ex.Message}.\nMore information was written to the debug console.");
          Debug.WriteLine($"Unhandled exception: {ex}.");
        }
        else
        {
          Console.WriteLine($"Unhandled non-exception object: {e.ExceptionObject}");
        }

        Console.ResetColor();
      };

    try
    {
      SilKitEntity = new SilKitEntity(
        silKitConfigurationPath,
        participantName,
        lifecycleMode,
        timeSyncMode);
      SilKitDataManager = new SilKitDataManager(SilKitEntity);
      CurrentSilKitStatus = SilKitStatus.Initialized;
    }
    catch (Exception ex)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"An error occurred: {ex.Message}.\nMore information was written to the debug console.");
      Debug.WriteLine($"An error occurred: {ex}.");
      Console.ResetColor();
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.ErrorDuringInitialization;
      }
      throw;
    }

    try
    {
      if (string.IsNullOrEmpty(fmuImporterConfigFilePath))
      {
        _fmuImporterConfig = new Configuration();
        _fmuImporterConfig.MergeIncludes();
      }
      else
      {
        _fmuImporterConfig = ConfigParser.LoadConfiguration(fmuImporterConfigFilePath);
        _fmuImporterConfig.SetSilKitLogger(SilKitEntity.Logger);
        _fmuImporterConfig.MergeIncludes();
      }
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.Message);
      SilKitEntity.Logger.Log(LogLevel.Debug, e.ToString());
      throw;
    }

    try
    {
      if (!string.IsNullOrEmpty(fmuImporterCommInterfaceFilePath))
      {
        _fmuImporterCommInterface =
          CommunicationInterfaceDescriptionParser.Load(fmuImporterCommInterfaceFilePath);
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }

    try
    {
      FmuEntity = new FmuEntity(fmuPath, usePersistedFmu, FmuEntity_OnFmuLog);

      _configuredParameters = _fmuImporterConfig.GetParameters();
      _configuredStructuralParameters = new Dictionary<string, Parameter>();

      if (_fmuImporterCommInterface is null)
      {
        if (FmuEntity.ModelDescription.VariableNamingConvention is ModelDescription.VariableNamingConventions
              .Structured ||
            _fmuImporterConfig?.AlwaysUseStructuredNamingConvention is true)
        {
          _fmuImporterCommInterface = CommunicationInterfaceDescriptionParser.Parse(
            new Fmi.Supplements.CommInterfaceGenerator(FmuEntity.Binding, useClockPubSubElements).CommInterfaceText);
        }
      }

      var myStructParams = FmuEntity.ModelDescription.Variables
                                    .Where(kvp => kvp.Value.Causality == Variable.Causalities.StructuralParameter)
                                    .Select(v => v.Value.Name);
      foreach (var myStructParam in myStructParams)
      {
        var success = _configuredParameters.Remove(myStructParam, out var param);
        if (!success)
        {
          continue;
        }

        _configuredStructuralParameters.Add(myStructParam, param!);
      }

      // Initialize FMU
      FmuEntity.PrepareFmu(ApplyParameterConfiguration, ApplyParameterConfiguration);

      // Initialize FmuDataManager
      FmuDataManager = new FmuDataManager(FmuEntity.Binding, FmuEntity.ModelDescription, FmuEntity_OnFmuLog, useClockPubSubElements);

      // create a temporary copy to avoid modifying the FmuEntity's ModelDescription
      var modelDescriptionVariables = new Dictionary<uint, Variable>(FmuEntity.ModelDescription.Variables);

      if (FmuEntity.TerminalsAndIcons != null)
      {
        foreach (var pairNameTerminal in FmuEntity.TerminalsAndIcons.Terminals)
        {
          // handle CAN terminal if any
          if (pairNameTerminal.Value.InternalTerminalKind == InternalTerminalKind.CAN)
          {
            FmuCanManager ??= new FmuCanManager(FmuEntity.Binding, FmuEntity_OnFmuLog);
            SilKitCanManager ??= new SilKitCanManager(SilKitEntity);
          }
          // handle RPC terminal if any
          else if (pairNameTerminal.Value.InternalTerminalKind == InternalTerminalKind.RPC_CLIENT)
          {
            FmuRpcClientManager ??= new FmuRpcManager(FmuEntity.Binding, FmuEntity_OnFmuLog);
            SilKitRpcClientManager ??= new SilKitRpcClientManager(SilKitEntity);

            var (vRef_RxId, vRef_RxArgs) = pairNameTerminal.Value.GetRpcValueRefsByIdArgs("Rx_ReturnId", "Rx_ReturnArgs");
            var (vRef_TxId, vRef_TxArgs) = pairNameTerminal.Value.GetRpcValueRefsByIdArgs("Tx_CallId", "Tx_CallArgs");

            FmuRpcClientManager.AddRxValueRefs((vRef_RxId, vRef_RxArgs));
            FmuRpcClientManager.AddTxValueRefs((vRef_TxId, vRef_TxArgs));

            SilKitRpcClientManager.AddTxRxMapping(vRef_TxId, vRef_RxId);
          }
          else if (pairNameTerminal.Value.InternalTerminalKind == InternalTerminalKind.RPC_SERVER)
          {
            FmuRpcServerManager ??= new FmuRpcManager(FmuEntity.Binding, FmuEntity_OnFmuLog);
            SilKitRpcServerManager ??= new SilKitRpcServerManager(SilKitEntity);

            var (vRef_RxId, vRef_RxArgs) = pairNameTerminal.Value.GetRpcValueRefsByIdArgs("Rx_CallId", "Rx_CallArgs");
            var (vRef_TxId, vRef_TxArgs) = pairNameTerminal.Value.GetRpcValueRefsByIdArgs("Tx_ReturnId", "Tx_ReturnArgs");

            FmuRpcServerManager.AddRxValueRefs((vRef_RxId, vRef_RxArgs));
            FmuRpcServerManager.AddTxValueRefs((vRef_TxId, vRef_TxArgs));

            SilKitRpcServerManager.AddTxRxMapping(vRef_TxId, vRef_RxId);
          }
        }

        // process can variables and remove them from the modelDescriptionVariables
        if (FmuCanManager != null) ProcessCanVariables(ref modelDescriptionVariables);

        if (FmuRpcClientManager != null) ProcessRpcClientVariables(ref modelDescriptionVariables);
        if (FmuRpcServerManager != null) ProcessRpcServerVariables(ref modelDescriptionVariables);
      }

      // if no CanManager, FmuRpcClientManager or FmuRpcServerManager: use the default ctor to avoid making them nullable
      FmuCanManager ??= new FmuCanManager();
      SilKitCanManager ??= new SilKitCanManager();
      FmuRpcClientManager ??= new FmuRpcManager();
      SilKitRpcClientManager ??= new SilKitRpcClientManager();
      FmuRpcServerManager ??= new FmuRpcManager();
      SilKitRpcServerManager ??= new SilKitRpcServerManager();

      PrepareConfiguredVariables(modelDescriptionVariables.Values.ToList());

      SilKitCanManager.StartCanControllers();
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.Message);
      SilKitEntity.Logger.Log(LogLevel.Debug, e.ToString());
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.ErrorDuringInitialization;
      }

      ExitFmuImporter();
      throw;
    }
  }

  private void FmuEntity_OnFmuLog(LogSeverity severity, string message)
  {
    SilKitEntity.Logger.Log(Helpers.Helpers.FmiLogLevelToSilKitLogLevel(severity), message);
  }

  private void ApplyParameterConfiguration()
  {
    Dictionary<string, Parameter>.ValueCollection usedConfiguredParameters;
    var isHandlingStructuredParameters = false;

    // initialize all configured parameters
    if ((FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Instantiated) &&
        (_configuredStructuralParameters != null))
    {
      usedConfiguredParameters = _configuredStructuralParameters.Values;
      isHandlingStructuredParameters = true;
    }
    else if ((FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Initializing) &&
             (_configuredParameters != null))
    {
      usedConfiguredParameters = _configuredParameters.Values;
    }
    else
    {
      throw new Exception();
    }

    if (usedConfiguredParameters.Count == 0)
    {
      return;
    }

    foreach (var configuredParameter in usedConfiguredParameters)
    {
      var result = FmuEntity.ModelDescription.NameToValueReference.TryGetValue(
        configuredParameter.VariableName,
        out var refValue);
      if (!result)
      {
        throw new InvalidConfigurationException(
          $"Configured parameter '{configuredParameter.VariableName}' not found in model description.");
      }

      result = FmuEntity.ModelDescription.Variables.TryGetValue(refValue, out var v);
      if (!result || (v == null))
      {
        throw new InvalidConfigurationException(
          $"Configured parameter '{configuredParameter.VariableName}' not found in model description.");
      }

      byte[] data;
      var binSizes = new List<int>();
      if (configuredParameter.Value.Value is List<object> objectList)
      {
        // the parameter is an array
        if (FmuEntity.FmiVersion == FmiVersions.Fmi2)
        {
          throw new NotSupportedException(
            $"FMI 2.0.x does not support arrays. Exception thrown by the following " +
            $"value reference: {refValue}");
        }

        if (isHandlingStructuredParameters)
        {
          // modify model description to make sure that the array lengths can be calculated correctly
          v.Start = objectList.ToArray();
        }

        data = Fmi.Supplements.Serializer.Serialize(objectList, v, binSizes);
      }
      else
      {
        if (isHandlingStructuredParameters)
        {
          // modify model description to make sure that the array lengths can be calculated correctly
          v.Start = new[] { configuredParameter.Value.Value };
        }

        data = Fmi.Supplements.Serializer.Serialize(configuredParameter.Value.Value, v, binSizes);
      }

      if (v.VariableType != VariableTypes.Binary)
      {
        FmuEntity.Binding.SetValue(refValue, data);
      }
      else
      {
        // binary type
        if (FmuEntity.FmiVersion == FmiVersions.Fmi2)
        {
          throw new NotSupportedException(
            $"FMI 2.0.x does not support the binary data type. Exception thrown by the " +
            $"following value reference: {refValue}");
        }

        FmuEntity.Binding.SetValue(refValue, data, binSizes.ToArray());
      }
    }
  }

  private void PrepareConfiguredVariables(List<Variable> modelDescriptionVariables)
  {
    if (FmuDataManager == null)
    {
      throw new NullReferenceException($"{nameof(FmuDataManager)} was null.");
    }

    FmuDataManager.Initialize(_fmuImporterConfig, _fmuImporterCommInterface, modelDescriptionVariables);

    // create subscribers for input variables
    foreach (var configuredVariable in FmuDataManager.InputConfiguredVariables.Values)
    {
      // This is a redundant sanity check. It is also done when adding the variable to InputConfiguredVariable.
      if (configuredVariable.FmuVariableDefinition.Causality != Variable.Causalities.Input)
      {
        throw new InvalidConfigurationException(
          $"An internal error occurred. " +
          $"'{configuredVariable.FmuVariableDefinition.Name}' was added to the list of input variables, but its " +
          $"causality was '{configuredVariable.FmuVariableDefinition.Causality}'");
      }

      SilKitDataManager.CreateSubscriber(
        configuredVariable.FmuVariableDefinition.Name,
        configuredVariable.TopicName,
        _fmuImporterConfig.Instance,
        _fmuImporterConfig.Namespace,
        new IntPtr(configuredVariable.FmuVariableDefinition.ValueReference));
    }

    // create subscribers for input structures
    foreach (var configuredStructure in FmuDataManager.InputConfiguredStructures.Values)
    {
      if (Environment.Is64BitProcess)
      {
        SilKitDataManager.CreateSubscriber(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr(configuredStructure.StructureId));
      }
      else
      {
        SilKitDataManager.CreateSubscriber(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr((int)configuredStructure.StructureId));
      }
    }

    // create subscribers for input clocked structures
    foreach (var configuredStructure in FmuDataManager.InputConfiguredClockedStructures.Values)
    {
      if (Environment.Is64BitProcess)
      {
        SilKitDataManager.CreateSubscriber(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr(configuredStructure.StructureId));
      }
      else
      {
        SilKitDataManager.CreateSubscriber(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr((int)configuredStructure.StructureId));
      }
    }

    // create publishers for output variables
    var flattenedOutputVariables = FmuDataManager.OutputConfiguredVariables.Values.SelectMany(list => list).ToList();

    foreach (var configuredVariable in flattenedOutputVariables)
    {
      if (configuredVariable.FmuVariableDefinition.Causality is not Variable.Causalities.Output)
      {
        throw new InvalidConfigurationException(
          $"An internal error occurred. " +
          $"'{configuredVariable.FmuVariableDefinition.Name}' with value reference " +
          $"{configuredVariable.FmuVariableDefinition.ValueReference} was added to the list of output variables, but its " +
          $"causality was '{configuredVariable.FmuVariableDefinition.Causality}'");
      }

      SilKitDataManager.CreatePublisher(
        configuredVariable.FmuVariableDefinition.Name,
        configuredVariable.TopicName,
        _fmuImporterConfig.Instance,
        _fmuImporterConfig.Namespace,
        new IntPtr(configuredVariable.FmuVariableDefinition.ValueReference),
        0);
    }

    // create publishers for output structures
    foreach (var configuredStructure in FmuDataManager.OutputConfiguredStructures.Values)
    {
      if (Environment.Is64BitProcess)
      {
        SilKitDataManager.CreatePublisher(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr(configuredStructure.StructureId),
          0);
      }
      else
      {
        SilKitDataManager.CreatePublisher(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr((int)configuredStructure.StructureId),
          0);
      }
    }

    // create publishers for output clocked structures
    foreach (var configuredStructure in FmuDataManager.OutputConfiguredClockedStructures.Values)
    {
      if (Environment.Is64BitProcess)
      {
        SilKitDataManager.CreatePublisher(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr(configuredStructure.StructureId),
          0);
      }
      else
      {
        SilKitDataManager.CreatePublisher(
          configuredStructure.Name,
          configuredStructure.Name,
          _fmuImporterConfig.Instance,
          _fmuImporterConfig.Namespace,
          new IntPtr((int)configuredStructure.StructureId),
          0);
      }
    }
  }

  private static int canControllerId = 1;

  private void ProcessCanVariables(ref Dictionary<uint /* ValueReference */, Variable> modelDescriptionVariables)
  {
    FmuCanManager.Initialize(ref modelDescriptionVariables);

    // if any, get the terminal names with their respective vRef in/out  
    var networkNamesValueRefs = FmuEntity.GetTerminalsValueRefs();
    if (networkNamesValueRefs.Count == 0)
    {
      return;
    }

    for (var i = 0; i < FmuCanManager.OutputCanVariables.Count; i++)
    {
      var networkName = "";
      foreach (var pairTerminalNameVrefs in networkNamesValueRefs)
      {
        if (pairTerminalNameVrefs.Value.Item2 == FmuCanManager.OutputCanVariables[i].ValueReference)
        {
          var vRefOut = pairTerminalNameVrefs.Value.Item2;
          var vRefIn = pairTerminalNameVrefs.Value.Item1;
          networkName = pairTerminalNameVrefs.Key;
          SilKitCanManager.CreateCanController("SilKit_CAN_CTRL_" + canControllerId, networkName, vRefOut.Value);
          SilKitCanManager.AddCanFrameHandler(
            vRefOut.Value,
            vRefIn!.Value,
            SilKitCanManager.FuncCanFrameHandler,
            (int)TransmitDirection.RX);
          if (SilKitEntity.Logger.GetLogLevel() is LogLevel.Trace or LogLevel.Debug)
          {
            SilKitCanManager.AddFrameTransmitHandler(
              vRefOut.Value,
              SilKitCanManager.FuncCanFrameTransmitHandler,
              (int)(CanTransmitStatus.Transmitted | CanTransmitStatus.Canceled | CanTransmitStatus.TransmitQueueFull));
          }

          canControllerId++;
          break;
        }
      }

      if (networkName == "")
      {
        throw new InvalidConfigurationException(
          $"An internal error occurred. The value reference " +
          $"{FmuCanManager.OutputCanVariables[i].ValueReference} with the mimeType application/org.fmi-standard.fmi-ls-bus.can does not have " +
          $"a corresponding variable in the TerminalsAndIcons.xml file.");
      }
    }
  }

  private static int rpcClientId = 1;
  private void ProcessRpcClientVariables(ref Dictionary<uint /* ValueReference */, Variable> modelDescriptionVariables)
  {
    FmuRpcClientManager.Initialize(ref modelDescriptionVariables);

    foreach (var idVar in FmuRpcClientManager.InputIdVariables)
    {
      var rpcSpec = new RpcSpec()
      {
        functionName = idVar.Value.Name.Split('.')[0], // extract the operation name
        mediaType = RpcTypes.MediaTypeRpc()
      };
      SilKitRpcClientManager.CreateRpcClient("SilKit_RPC_Client_CTRL_" + rpcClientId, rpcSpec, (IntPtr)idVar.Key, SilKitRpcClientManager.FuncRpcCallResultHandler);
      ++rpcClientId;
    }
  }

  private static int rpcServerId = 1;
  private void ProcessRpcServerVariables(ref Dictionary<uint /* ValueReference */, Variable> modelDescriptionVariables)
  {
    FmuRpcServerManager.Initialize(ref modelDescriptionVariables);

    foreach (var idVar in FmuRpcServerManager.InputIdVariables)
    {
      var rpcSpec = new RpcSpec()
      {
        functionName = idVar.Value.Name.Split('.')[0], // extract the operation name
        mediaType = RpcTypes.MediaTypeRpc()
      };
      SilKitRpcServerManager.CreateRpcServer("SilKit_RPC_Server_CTRL_" + rpcServerId, rpcSpec, (IntPtr)idVar.Key, SilKitRpcServerManager.FuncRpcCallHandler);
      ++rpcServerId;
    }
  }

  private void RecordClocksAndClockedVariables()
  {
    // retrieve and send CAN frames
    var outputCan = FmuCanManager.GetCanData();
    SilKitCanManager.SendAllFrames(outputCan);
  }

  private void ApplyClocksAndClockedInputs()
  {
    // handle received CAN frames
    var receivedSilKitCanData = SilKitCanManager.RetrieveReceivedCanData(_lastSimStep!.Value);
    FmuCanManager.SetCanData(receivedSilKitCanData);
  }

  public void StartSimulation()
  {
    SilKitEntity.Logger.Log(LogLevel.Info, "Starting Simulation.");
    ulong stepDuration;
    if (_fmuImporterConfig.StepSize != null)
    {
      stepDuration = _fmuImporterConfig.StepSize.Value;
    }
    else
    {
      var fmuStepSize = FmuEntity.GetStepSize();
      stepDuration = fmuStepSize.HasValue
                       ? Helpers.Helpers.FmiTimeToSilKitTime(fmuStepSize.Value)
                       : Helpers.Helpers.DefaultSimStepDuration;
    }

    try
    {
      SilKitEntity.StartSimulation(OnSimulationStepWrapper, stepDuration);
      CurrentSilKitStatus = SilKitStatus.LifecycleStarted;

      SilKitEntity.WaitForLifecycleToComplete();
      CurrentSilKitStatus = SilKitStatus.ShutDown;
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.Message);
      SilKitEntity.Logger.Log(LogLevel.Debug, e.ToString());
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.ErrorDuringSimulation;
      }
    }
    finally
    {
      ExitFmuImporter();
      if (CurrentSilKitStatus != SilKitStatus.ShutDown)
      {
        SilKitEntity.WaitForLifecycleToComplete();
      }
    }
  }

  private ulong? _initialSimTime;
  private ulong? _lastSimStep;
  private Stopwatch? _stopWatch;
  private long _nextSimTime;

  private void OnSimulationStepWrapper(ulong nowInNs, ulong durationInNs)
  {
    try
    {
      OnSimulationStep(nowInNs, durationInNs);
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.Message);
      SilKitEntity.Logger.Log(LogLevel.Debug, e.ToString());
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.ErrorDuringUserCallbackExecution;
      }

      ExitFmuImporter();
    }
  }

  private void OnSimulationStep(ulong nowInNs, ulong durationInNs)
  {
    var eventEncountered = false;
    var discreteStatesNeedUpdate = true;
    double lastSuccesfullTime = 0;
    var terminateRequested = false;

    if (FmuDataManager == null)
    {
      throw new NullReferenceException($"{nameof(FmuDataManager)} was null.");
    }

    if (FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Terminated)
    {
      ExitFmuImporter();
      return;
    }

    // wall-clock alignment init
    if (SilKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      long elapsedTime;
      if (_stopWatch == null)
      {
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
        elapsedTime = 0;
      }
      else
      {
        elapsedTime = _stopWatch.ElapsedMilliseconds;
      }

      // in ms
      var deltaToNextSimStep = _nextSimTime - (elapsedTime * 1_000_000);
      // TODO improve timer performance to decrease threshold (ms is a lot)
      if (deltaToNextSimStep > 2 * 1_000_000 /* 2 ms in ns */)
      {
        new ManualResetEvent(false).WaitOne((int)(deltaToNextSimStep / 1_000_000));
      }

      if (IsSimulationStopped())
      {
        return;
      }

      _nextSimTime += (long)durationInNs;
    }

    if (_initialSimTime == null)
    {
      _lastSimStep = nowInNs;
      _initialSimTime = nowInNs;

      if (FmuEntity.ModelDescription.CoSimulation.hasEventMode)
      {
        RecordClocksAndClockedVariables();

        do
        {
          ApplyClocksAndClockedInputs();

          FmuEntity.UpdateDiscreteStates(out discreteStatesNeedUpdate, out terminateRequested);

          if (terminateRequested)
          {
            SilKitCanManager.StopCanControllers();
            FmuEntity.Terminate();
            ExitFmuImporter();
            return;
          }

          RecordClocksAndClockedVariables();

        } while (discreteStatesNeedUpdate);

        FmuEntity.EnterStepMode();
      }

      // However, publish all initial output variable values
      var initialData = FmuDataManager.GetVariableOutputData();
      SilKitDataManager.PublishAll(initialData);
      return;
    }

    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var receivedSilKitData = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputVariableRefs);
    var receivedSilKitClocks = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputClockRefs);
    var receivedSilKitClockedData = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputClockedVariableRefs);
    var receivedSilKitDataStruct = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputStructureIds);
    var receivedSilKitDataClockedStruct = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputClockedStructureIds);

    FmuDataManager.SetData(receivedSilKitData);
    FmuDataManager.SetData(receivedSilKitClocks);
    FmuDataManager.SetData(receivedSilKitClockedData);
    FmuDataManager.SetData(receivedSilKitDataStruct);
    FmuDataManager.SetData(receivedSilKitDataClockedStruct);

    // handle client RPC results
    var receivedRpcResults = SilKitRpcClientManager.RetrieveReceivedRpcEvents(_lastSimStep!.Value);
    FmuRpcClientManager.SetData(receivedRpcResults);

    // handle server RPC calls
    var receivedRpcCalls = SilKitRpcServerManager.RetrieveReceivedRpcCalls(_lastSimStep!.Value);
    FmuRpcServerManager.SetData(receivedRpcCalls);

    _lastSimStep = nowInNs;

    if (_initialSimTime > 0)
    {
      // apply offset to initial time in hop-on scenario
      nowInNs -= _initialSimTime.Value;
    }

    // Calculate simulation step
    var fmiNow = Helpers.Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    try
    {
      FmuEntity.DoStep(
        fmiNow,
        Helpers.Helpers.SilKitTimeToFmiTime(durationInNs),
        out lastSuccesfullTime,
        out eventEncountered,
        out terminateRequested);

      // Retrieve and publish non-structured variables
      var currentOutputData = FmuDataManager.GetVariableOutputData();

      var activeInputClocks = receivedSilKitClocks.Where(clock => clock.Value[0] != 0).Select(clock => clock.Key).ToHashSet();
      var currentOutputClocks = FmuDataManager.GetClocks(activeInputClocks); // can be clocked by in clocks

      var outputActiveClocks = currentOutputClocks.Where(clock => clock.Item2[0] != 0).Select(clock => clock.Item1).ToHashSet();
      var currentOutputClockedData = FmuDataManager.GetClockedVariableData(activeInputClocks, outputActiveClocks); // clocked by in/out clocks

      SilKitDataManager.PublishAll(currentOutputData);
      SilKitDataManager.PublishAll(currentOutputClocks);
      SilKitDataManager.PublishAll(currentOutputClockedData);

      // Retrieve and publish structures
      var currentStructureOutputData = FmuDataManager.GetStructureOutputData();
      var currentClockedStructureOutputData = FmuDataManager.GetClockedStructureData(activeInputClocks);

      SilKitDataManager.PublishAll(currentStructureOutputData);
      SilKitDataManager.PublishAll(currentClockedStructureOutputData);

      if (terminateRequested)
      {
        SilKitCanManager.StopCanControllers();
        FmuEntity.Terminate();
        ExitFmuImporter();
        return;
      }

      if (FmuEntity.ModelDescription.CoSimulation.hasEventMode &&
          eventEncountered &&
          (FmuEntity.Binding.CurrentState != InternalFmuStates.Terminated))
      {
        FmuEntity.EnterEventMode();

        RecordClocksAndClockedVariables();

        // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
        var receivedSilKitDataEventMode = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputVariableRefs);
        var receivedSilKitClocksEventMode = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputClockRefs);
        var receivedSilKitClockedDataEventMode = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputClockedVariableRefs);
        var receivedSilKitDataStructEventMode = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputStructureIds);
        var receivedSilKitDataClockedStructEventMode = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value, FmuDataManager.InputClockedStructureIds);

        FmuDataManager.SetData(receivedSilKitDataEventMode);
        FmuDataManager.SetData(receivedSilKitClocksEventMode);
        FmuDataManager.SetData(receivedSilKitClockedDataEventMode);
        FmuDataManager.SetData(receivedSilKitDataStructEventMode);
        FmuDataManager.SetData(receivedSilKitDataClockedStructEventMode);

        // handle client RPC results
        var recvRpcResults = SilKitRpcClientManager.RetrieveReceivedRpcEvents(_lastSimStep!.Value);
        FmuRpcClientManager.SetData(recvRpcResults);

        // handle server RPC calls
        var recvRpcCalls = SilKitRpcServerManager.RetrieveReceivedRpcCalls(_lastSimStep!.Value);
        FmuRpcServerManager.SetData(recvRpcCalls);

        do
        {
          ApplyClocksAndClockedInputs();

          FmuEntity.UpdateDiscreteStates(out discreteStatesNeedUpdate, out terminateRequested);

          RecordClocksAndClockedVariables();

          if (terminateRequested)
          {
            SilKitCanManager.StopCanControllers();
            FmuEntity.Terminate();
            ExitFmuImporter();
            return;
          }

          var rpcCalls = FmuRpcServerManager.GetOperations();
          SilKitRpcServerManager.SubmitResult(rpcCalls);

          var futureRpcCalls = FmuRpcClientManager.GetOperations();
          SilKitRpcClientManager.Call(futureRpcCalls);

        } while (discreteStatesNeedUpdate);

        // Retrieve and publish non-structured variables
        var currentOutputDataEventMode = FmuDataManager.GetVariableOutputData();

        var activeInputClocksEventMode = receivedSilKitClocksEventMode.Where(clock => clock.Value[0] != 0).Select(clock => clock.Key).ToHashSet();
        var currentOutputClocksEventMode = FmuDataManager.GetClocks(activeInputClocksEventMode); // can be clocked by input clocks

        var outputActiveClocksEventMode = currentOutputClocksEventMode.Where(clock => clock.Item2[0] != 0).Select(clock => clock.Item1).ToHashSet();
        var currentOutputClockedDataEventMode = FmuDataManager.GetClockedVariableData(activeInputClocksEventMode, outputActiveClocksEventMode); // clocked by in/out clocks

        SilKitDataManager.PublishAll(currentOutputDataEventMode);
        SilKitDataManager.PublishAll(currentOutputClocksEventMode);
        SilKitDataManager.PublishAll(currentOutputClockedDataEventMode);

        // Retrieve and publish structures
        var currentStructureOutputDataEventMode = FmuDataManager.GetStructureOutputData();
        var currentClockedStructureOutputDataEventMode = FmuDataManager.GetClockedStructureData(activeInputClocksEventMode);

        SilKitDataManager.PublishAll(currentStructureOutputDataEventMode);
        SilKitDataManager.PublishAll(currentClockedStructureOutputDataEventMode);

        FmuEntity.EnterStepMode();
      }
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.Message);
      SilKitEntity.Logger.Log(LogLevel.Debug, e.ToString());
      ExitFmuImporter();
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.ErrorDuringFmuSimulationStepExecution;
      }

      return;
    }

    var stopTime = FmuEntity.GetStopTime();
    if (stopTime.HasValue)
    {
      if (Helpers.Helpers.SilKitTimeToFmiTime(nowInNs) >= stopTime)
      {
        // stop the SIL Kit simulation
        SilKitEntity.StopSimulation("FMU stopTime reached.");
        _stopWatch?.Stop();
        _stopWatch = null;
      }
    }
  }

  private bool IsSimulationStopped()
  {
    return (FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Terminated) ||
           (CurrentSilKitStatus == SilKitStatus.Stopped) ||
           (CurrentSilKitStatus == SilKitStatus.ShutDown);
  }

  public void ExitFmuImporter()
  {
    if (FmuEntity == null)
    {
      return;
    }

    if ((FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Initialized) &&
        !(FmuEntity.Binding.CurrentState is
            InternalFmuStates.TerminatedWithError or InternalFmuStates.Terminated or InternalFmuStates.Freed))
    {
      SilKitCanManager.StopCanControllers();
      FmuEntity.Terminate();
      // FreeInstance will be called by the dispose pattern
    }

    switch (CurrentSilKitStatus)
    {
      case SilKitStatus.Uninitialized:
      case SilKitStatus.Initialized:
        // NOP - SIL Kit did not start a lifecycle yet
        break;
      case SilKitStatus.LifecycleStarted:
        try
        {
          SilKitEntity.StopSimulation("FMU Importer is exiting.");
        }
        finally
        {
          CurrentSilKitStatus = SilKitStatus.Stopped;
        }

        break;
      case SilKitStatus.Stopped:
      case SilKitStatus.ShutDown:
        // NOP - SIL Kit already stopped
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

#region IDisposable

  ~FmuImporter()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool _disposedValue;

  protected virtual void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects

        // cleanup SIL Kit
        SilKitDataManager.Dispose();
        SilKitEntity.Dispose();

        // cleanup FMU
        FmuEntity.Dispose();
      }

      ReleaseUnmanagedResources();
      _disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

#endregion IDisposable
}
