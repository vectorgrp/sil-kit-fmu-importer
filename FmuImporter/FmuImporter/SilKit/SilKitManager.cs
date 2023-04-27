using SilKit.Config;
using SilKit.Services.Orchestration;
using SilKit;
using SilKit.Services.Logger;
using SilKit.Services.PubSub;
using SilKit.Supplements.VendorData;
using System.Runtime.InteropServices;
using System.Text;

namespace FmuImporter.SilKit;

public class SilKitManager : IDisposable
{
  private readonly Participant _participant;
  private readonly ILifecycleService _lifecycleService;
  private readonly LifecycleService.ITimeSyncService _timeSyncService;
  public ILogger Logger { get; set; }

  public SilKitManager(string? configurationPath, string participantName)
  {
    var wrapper = SilKitWrapper.Instance;
    ParticipantConfiguration config;
    if (string.IsNullOrEmpty(configurationPath))
    {
      config = wrapper.GetConfigurationFromString("");
    }
    else
    {
      config = wrapper.GetConfigurationFromFile(configurationPath);
    }

    var lc = new LifecycleService.LifecycleConfiguration(LifecycleService.LifecycleConfiguration.Modes.Coordinated);

    _participant = wrapper.CreateParticipant(config, participantName);
    _lifecycleService = _participant.CreateLifecycleService(lc);
    _timeSyncService = _lifecycleService.CreateTimeSyncService();

    // get logger
    Logger = _participant.GetLogger();
    config.Dispose();
  }

  #region service creation
  public IDataPublisher CreatePublisher(string serviceName, string topicName, byte historySize)
  {
    var dataSpec = new PubSubSpec(topicName, Vector.MediaTypeData);

    return _participant.CreateDataPublisher(serviceName, dataSpec, historySize);
  }

  public IDataSubscriber CreateSubscriber(string serviceName, string topicName, IntPtr context, DataMessageHandler handler)
  {
    var dataSpec = new PubSubSpec(topicName, Vector.MediaTypeData);

    return _participant.CreateDataSubscriber(
      serviceName,
      dataSpec,
      context,
      handler);
  }
  #endregion service creation

  #region data publishing
  public static byte[] EncodeData(float[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(double[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(sbyte[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(byte[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(short[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(ushort[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    if (!isScalar)
    {
      var arrayPrefix = BitConverter.GetBytes(variableValues.Length);
      Helpers.ToLittleEndian(ref arrayPrefix);
      byteList.AddRange(arrayPrefix);
    }

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(int[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    if (!isScalar)
    {
      var arrayPrefix = BitConverter.GetBytes(variableValues.Length);
      Helpers.ToLittleEndian(ref arrayPrefix);
      byteList.AddRange(arrayPrefix);
    }

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(uint[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    if (!isScalar)
    {
      var arrayPrefix = BitConverter.GetBytes(variableValues.Length);
      Helpers.ToLittleEndian(ref arrayPrefix);
      byteList.AddRange(arrayPrefix);
    }

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(long[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    if (!isScalar)
    {
      var arrayPrefix = BitConverter.GetBytes(variableValues.Length);
      Helpers.ToLittleEndian(ref arrayPrefix);
      byteList.AddRange(arrayPrefix);
    }

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(ulong[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    if (!isScalar)
    {
      var arrayPrefix = BitConverter.GetBytes(variableValues.Length);
      Helpers.ToLittleEndian(ref arrayPrefix);
      byteList.AddRange(arrayPrefix);
    }

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(bool[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    if (!isScalar)
    {
      var arrayPrefix = BitConverter.GetBytes(variableValues.Length);
      Helpers.ToLittleEndian(ref arrayPrefix);
      byteList.AddRange(arrayPrefix);
    }

    for (int i = 0; i < variableValues.Length; i++)
    {
      var byteArr = BitConverter.GetBytes(variableValues[i]);
      Helpers.ToLittleEndian(ref byteArr);
      byteList.AddRange(byteArr);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(string[] variableValues, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);
    for (int i = 0; i < variableValues.Length; i++)
    {
      var encodedString = Encoding.UTF8.GetBytes(variableValues[i]);
      var localByteList = InitByteList(false, encodedString.Length);
      localByteList.AddRange(encodedString);
      byteList.AddRange(localByteList);
    }

    return byteList.ToArray();
  }

  public static byte[] EncodeData(IntPtr[] variableValues, IntPtr[] valueSizes, bool isScalar)
  {
    var byteList = InitByteList(isScalar, variableValues.Length);

    for (int i = 0; i < variableValues.Length; i++)
    {
      var binDataPtr = variableValues[i];
      var rawDataLength = (int)valueSizes[i];
      var binData = new byte[rawDataLength];
      Marshal.Copy(binDataPtr, binData, 0, rawDataLength);

      // append length of following binary blob
      byteList.AddRange(InitByteList(false, binData.Length));
      // add the data blob
      byteList.AddRange(binData);
    }

    return byteList.ToArray();
  }

  private static List<byte> InitByteList(bool isScalar, int arrayLength)
  {
    if (isScalar)
    {
      return new List<byte>();
    }
    else
    {
      var byteArr = BitConverter.GetBytes(arrayLength);
      Helpers.ToLittleEndian(ref byteArr);

      return byteArr.ToList();
    }
  }
  #endregion data publishing

  #region simulation control
  public void StartSimulation(SimulationStepHandler handler, ulong initialStepSizeInNs)
  {
    _timeSyncService.SetSimulationStepHandler(handler, initialStepSizeInNs);
    _lifecycleService.StartLifecycle();
    _lifecycleService.WaitForLifecycleToComplete();
  }

  public void StopSimulation(string reason)
  {
    _lifecycleService.Stop(reason);
  }
  #endregion simulation control

  #region IDisposable
  ~SilKitManager()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool mDisposedValue;

  protected void Dispose(bool disposing)
  {
    if (!mDisposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
        _participant.Dispose();
      }

      ReleaseUnmanagedResources();
      mDisposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
  #endregion
}