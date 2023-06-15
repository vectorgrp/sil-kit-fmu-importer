// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using SilKit.Config;

namespace SilKit;

/**
   * ##############################
   * # Mapping: [C] -> [C# Type] #
   * ##############################
   * [class]** -> [Out] out IntPtr
   * [class]* -> IntPtr
   * char* -> [MarshalAs(UnmanagedType.LPStr)] string
   */
public class SilKitWrapper
{
  private static readonly Lazy<SilKitWrapper> sSilKitWrapperInstance =
    new Lazy<SilKitWrapper>(() => new SilKitWrapper());

  private SilKitWrapper()
  {
  }

  public static SilKitWrapper Instance
  {
    get
    {
      return sSilKitWrapperInstance.Value;
    }
  }

  public ParticipantConfiguration GetConfigurationFromFile(string configPath)
  {
    return GetConfigurationFromString(File.ReadAllText(configPath));
  }

  public ParticipantConfiguration GetConfigurationFromString(string configurationString)
  {
    return new ParticipantConfiguration(configurationString);
  }

  public Participant CreateParticipant(
    ParticipantConfiguration configuration,
    string participantName,
    string registryUri)
  {
    return new Participant(configuration, participantName, registryUri);
  }

  public Participant CreateParticipant(ParticipantConfiguration configuration, string participantName)
  {
    return CreateParticipant(configuration, participantName, "silkit://127.0.0.1:8500");
  }
}

#region object version

// #define SK_ID_MAKE(SERVICE_NAME, DATATYPE_NAME) \
//     (uint64_t)(\
//           ((uint64_t)83 /*S*/ << 56)\
//         | ((uint64_t)75 /*K*/ << 48)\
//         | ((uint64_t)(SK_ID_SERVICE_ ## SERVICE_NAME & 0xFF) << 40)\
//         | ((uint64_t)(DATATYPE_NAME ## _DATATYPE_ID & 0xff) << 32)\
//         | ((uint64_t)(DATATYPE_NAME ## _VERSION  & 0xff) << 24)\
//         | 0x0)
internal static class SilKitVersion
{
  public enum ServiceId : byte
  {
    Can = 1,
    Ethernet = 2,
    Flexray = 3,
    Lin = 4,
    Data = 5,
    Rpc = 6,
    Participant = 7
  }

  public enum DatatypeId : byte
  {
    // CAN

    CanFrame = 1,
    CanFrameTransmitEvent = 2,
    CanFrameEvent = 3,
    CanStateChangeEvent = 4,
    CanErrorStateChangeEvent = 5,

    // // CAN data type versions
    // #define SilKit_CanFrame_VERSION 1
    // #define SilKit_CanFrameTransmitEvent_VERSION 2
    // #define SilKit_CanFrameEvent_VERSION 1
    // #define SilKit_CanStateChangeEvent_VERSION 1
    // #define SilKit_CanErrorStateChangeEvent_VERSION 1
    // 
    // // CAN make versioned IDs
    // #define SilKit_CanFrame_STRUCT_VERSION                     SK_ID_MAKE(Can, SilKit_CanFrame)
    // #define SilKit_CanFrameTransmitEvent_STRUCT_VERSION        SK_ID_MAKE(Can, SilKit_CanFrameTransmitEvent)
    // #define SilKit_CanFrameEvent_STRUCT_VERSION                SK_ID_MAKE(Can, SilKit_CanFrameEvent)
    // #define SilKit_CanStateChangeEvent_STRUCT_VERSION          SK_ID_MAKE(Can, SilKit_CanStateChangeEvent)
    // #define SilKit_CanErrorStateChangeEvent_STRUCT_VERSION     SK_ID_MAKE(Can, SilKit_CanErrorStateChangeEvent)

    // Ethernet

    EthernetFrameEvent = 1,
    EthernetFrameTransmitEvent = 2,
    EthernetStateChangeEvent = 3,
    EthernetBitrateChangeEvent = 4,
    EthernetFrame = 5,

    // // Ethernet data type versions
    // #define SilKit_EthernetFrameEvent_VERSION 1
    // #define SilKit_EthernetFrameTransmitEvent_VERSION 1
    // #define SilKit_EthernetStateChangeEvent_VERSION 1
    // #define SilKit_EthernetBitrateChangeEvent_VERSION 1
    // #define SilKit_EthernetFrame_VERSION 1
    // 
    // #define SilKit_EthernetFrameEvent_STRUCT_VERSION           SK_ID_MAKE(Ethernet, SilKit_EthernetFrameEvent)
    // #define SilKit_EthernetFrameTransmitEvent_STRUCT_VERSION   SK_ID_MAKE(Ethernet, SilKit_EthernetFrameTransmitEvent)
    // #define SilKit_EthernetStateChangeEvent_STRUCT_VERSION     SK_ID_MAKE(Ethernet, SilKit_EthernetStateChangeEvent)
    // #define SilKit_EthernetBitrateChangeEvent_STRUCT_VERSION   SK_ID_MAKE(Ethernet, SilKit_EthernetBitrateChangeEvent)
    // #define SilKit_EthernetFrame_STRUCT_VERSION                SK_ID_MAKE(Ethernet, SilKit_EthernetFrame)

    // FlexRay

    FlexrayFrameEvent = 1,
    FlexrayFrameTransmitEvent = 2,
    FlexraySymbolEvent = 3,
    FlexraySymbolTransmitEvent = 5,
    FlexrayCycleStartEvent = 6,
    FlexrayPocStatusEvent = 7,
    FlexrayWakeupEvent = 8,
    FlexrayControllerConfig = 9,
    FlexrayClusterParameters = 10,
    FlexrayNodeParameters = 11,
    FlexrayHostCommand = 12,
    FlexrayHeader = 13,
    FlexrayFrame = 14,
    FlexrayTxBufferConfig = 15,
    FlexrayTxBufferUpdate = 16,

    // // Flexray data type versions
    // #define SilKit_FlexrayFrameEvent_VERSION 1
    // #define SilKit_FlexrayFrameTransmitEvent_VERSION 1
    // #define SilKit_FlexraySymbolEvent_VERSION 1
    // #define SilKit_FlexraySymbolTransmitEvent_VERSION 1
    // #define SilKit_FlexrayCycleStartEvent_VERSION 1
    // #define SilKit_FlexrayPocStatusEvent_VERSION 1
    // #define SilKit_FlexrayWakeupEvent_VERSION 1
    // #define SilKit_FlexrayControllerConfig_VERSION 1
    // #define SilKit_FlexrayClusterParameters_VERSION 1
    // #define SilKit_FlexrayNodeParameters_VERSION 1
    // #define SilKit_FlexrayHostCommand_VERSION 1
    // #define SilKit_FlexrayHeader_VERSION 1
    // #define SilKit_FlexrayFrame_VERSION 1
    // #define SilKit_FlexrayTxBufferConfig_VERSION 1
    // #define SilKit_FlexrayTxBufferUpdate_VERSION 1
    // 
    // // Flexray make versioned IDs
    // #define SilKit_FlexrayFrameEvent_STRUCT_VERSION            SK_ID_MAKE(Flexray, SilKit_FlexrayFrameEvent)
    // #define SilKit_FlexrayFrameTransmitEvent_STRUCT_VERSION    SK_ID_MAKE(Flexray, SilKit_FlexrayFrameTransmitEvent)
    // #define SilKit_FlexraySymbolEvent_STRUCT_VERSION           SK_ID_MAKE(Flexray, SilKit_FlexraySymbolEvent)
    // #define SilKit_FlexraySymbolTransmitEvent_STRUCT_VERSION   SK_ID_MAKE(Flexray, SilKit_FlexraySymbolTransmitEvent)
    // #define SilKit_FlexrayCycleStartEvent_STRUCT_VERSION       SK_ID_MAKE(Flexray, SilKit_FlexrayCycleStartEvent)
    // #define SilKit_FlexrayPocStatusEvent_STRUCT_VERSION        SK_ID_MAKE(Flexray, SilKit_FlexrayPocStatusEvent)
    // #define SilKit_FlexrayWakeupEvent_STRUCT_VERSION           SK_ID_MAKE(Flexray, SilKit_FlexrayWakeupEvent)
    // #define SilKit_FlexrayControllerConfig_STRUCT_VERSION      SK_ID_MAKE(Flexray, SilKit_FlexrayControllerConfig)
    // #define SilKit_FlexrayClusterParameters_STRUCT_VERSION     SK_ID_MAKE(Flexray, SilKit_FlexrayClusterParameters)
    // #define SilKit_FlexrayNodeParameters_STRUCT_VERSION        SK_ID_MAKE(Flexray, SilKit_FlexrayNodeParameters)
    // #define SilKit_FlexrayHostCommand_STRUCT_VERSION           SK_ID_MAKE(Flexray, SilKit_FlexrayHostCommand)
    // #define SilKit_FlexrayHeader_STRUCT_VERSION                SK_ID_MAKE(Flexray, SilKit_FlexrayHeader)
    // #define SilKit_FlexrayFrame_STRUCT_VERSION                 SK_ID_MAKE(Flexray, SilKit_FlexrayFrame)
    // #define SilKit_FlexrayTxBufferConfig_STRUCT_VERSION        SK_ID_MAKE(Flexray, SilKit_FlexrayTxBufferConfig)
    // #define SilKit_FlexrayTxBufferUpdate_STRUCT_VERSION        SK_ID_MAKE(Flexray, SilKit_FlexrayTxBufferUpdate)

    // LIN

    // // LIN data type IDs
    // #define SilKit_LinFrame_DATATYPE_ID 1
    // #define SilKit_LinFrameResponse_DATATYPE_ID 2
    // #define SilKit_LinControllerConfig_DATATYPE_ID 3
    // #define SilKit_LinFrameStatusEvent_DATATYPE_ID 4
    // #define SilKit_LinGoToSleepEvent_DATATYPE_ID 5
    // #define SilKit_LinWakeupEvent_DATATYPE_ID 6
    // #define SilKit_Experimental_LinSlaveConfigurationEvent_DATATYPE_ID 7
    // #define SilKit_Experimental_LinSlaveConfiguration_DATATYPE_ID 8
    // 
    // // LIN data type versions
    // #define SilKit_LinFrame_VERSION 1
    // #define SilKit_LinFrameResponse_VERSION 1
    // #define SilKit_LinControllerConfig_VERSION 1
    // #define SilKit_LinFrameStatusEvent_VERSION 1
    // #define SilKit_LinGoToSleepEvent_VERSION 1
    // #define SilKit_LinWakeupEvent_VERSION 1
    // #define SilKit_Experimental_LinSlaveConfigurationEvent_VERSION 1
    // #define SilKit_Experimental_LinSlaveConfiguration_VERSION 1
    // 
    // // LIN make versioned IDs
    // #define SilKit_LinFrame_STRUCT_VERSION                     SK_ID_MAKE(Lin, SilKit_LinFrame)
    // #define SilKit_LinFrameResponse_STRUCT_VERSION             SK_ID_MAKE(Lin, SilKit_LinFrameResponse)
    // #define SilKit_LinControllerConfig_STRUCT_VERSION          SK_ID_MAKE(Lin, SilKit_LinControllerConfig)
    // #define SilKit_LinFrameStatusEvent_STRUCT_VERSION          SK_ID_MAKE(Lin, SilKit_LinFrameStatusEvent)
    // #define SilKit_LinGoToSleepEvent_STRUCT_VERSION            SK_ID_MAKE(Lin, SilKit_LinGoToSleepEvent)
    // #define SilKit_LinWakeupEvent_STRUCT_VERSION               SK_ID_MAKE(Lin, SilKit_LinWakeupEvent)
    // #define SilKit_Experimental_LinSlaveConfigurationEvent_STRUCT_VERSION   SK_ID_MAKE(Lin, SilKit_Experimental_LinSlaveConfigurationEvent)
    // #define SilKit_Experimental_LinSlaveConfiguration_STRUCT_VERSION        SK_ID_MAKE(Lin, SilKit_Experimental_LinSlaveConfiguration)

    // Data
    // Data data type IDs
    DataMessageEvent = 1,
    DataSpec = 2,

    // // Data data type versions
    // #define SilKit_DataMessageEvent_VERSION 1
    // #define SilKit_DataSpec_VERSION 1
    // 
    // // Data public API IDs
    // #define SilKit_DataMessageEvent_STRUCT_VERSION             SK_ID_MAKE(Data, SilKit_DataMessageEvent)
    // #define SilKit_DataSpec_STRUCT_VERSION                     SK_ID_MAKE(Data, SilKit_DataSpec)

    // // Rpc
    // // Rpc data type IDs
    // #define SilKit_RpcCallEvent_DATATYPE_ID 1
    // #define SilKit_RpcCallResultEvent_DATATYPE_ID 2
    // #define SilKit_RpcSpec_DATATYPE_ID 3
    // 
    // // Rpc data type Versions
    // #define SilKit_RpcCallEvent_VERSION 1
    // #define SilKit_RpcCallResultEvent_VERSION 1
    // #define SilKit_RpcSpec_VERSION 1
    // 
    // // Rpc public API IDs
    // #define SilKit_RpcCallEvent_STRUCT_VERSION                 SK_ID_MAKE(Rpc, SilKit_RpcCallEvent)
    // #define SilKit_RpcCallResultEvent_STRUCT_VERSION           SK_ID_MAKE(Rpc, SilKit_RpcCallResultEvent)
    // #define SilKit_RpcSpec_STRUCT_VERSION                      SK_ID_MAKE(Rpc, SilKit_RpcSpec)

    // Participant

    ParticipantStatus = 1,
    LifecycleConfiguration = 2,
    WorkflowConfiguration = 3

    // // Participant data type Versions
    // #define SilKit_ParticipantStatus_VERSION 1
    // #define SilKit_LifecycleConfiguration_VERSION 1
    // #define SilKit_WorkflowConfiguration_VERSION 3
    // 
    // // Participant public API IDs
    // #define SilKit_ParticipantStatus_STRUCT_VERSION            SK_ID_MAKE(Participant, SilKit_ParticipantStatus)
    // #define SilKit_LifecycleConfiguration_STRUCT_VERSION       SK_ID_MAKE(Participant, SilKit_LifecycleConfiguration)
    // #define SilKit_WorkflowConfiguration_STRUCT_VERSION        SK_ID_MAKE(Participant, SilKit_WorkflowConfiguration)
  }

  private static ulong GetVersion(ServiceId serviceName, DatatypeId datatypeName)
  {
    var id = SK_ID_MAKE(serviceName, datatypeName, 1);
    return id;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal unsafe struct StructHeader
  {
    public ulong version; //!< Version encoded using SK_ID_MAKE
    public fixed ulong _reserved[3]; //!< For future expansions
  }

  internal static StructHeader GetStructHeader(ServiceId serviceName, DatatypeId datatypeName)
  {
    return new StructHeader()
    {
      version = GetVersion(serviceName, datatypeName)
    };
  }

  internal static ulong SK_ID_MAKE(ServiceId serviceId, DatatypeId datatypeId, byte datatypeVersion)
  {
    return ((ulong)83 /*S*/ << 56)
           | ((ulong)75 /*K*/ << 48)
           | ((ulong)serviceId << 40)
           | ((ulong)datatypeId << 32)
           | ((ulong)datatypeVersion << 24);
  }
}

#endregion
