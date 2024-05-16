// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services.PubSub;

namespace FmuImporter.SilKit;

public class SilKitDataManager : IDisposable
{
  private readonly SilKitEntity _silKitEntity;
  private SortedList<ulong, Dictionary<uint, byte[]>> DataBuffer { get; }

  public SilKitDataManager(SilKitEntity silKitEntity)
  {
    _silKitEntity = silKitEntity;

    DataBuffer = new SortedList<ulong, Dictionary<uint, byte[]>>();
    if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      DataBuffer.Add(0, new Dictionary<uint, byte[]>());
    }

    ValueRefToPublisher = new Dictionary<uint, IDataPublisher>();
    ValueRefToSubscriber = new Dictionary<uint, IDataSubscriber>();
  }

#region service creation

  public Dictionary<uint, IDataPublisher> ValueRefToPublisher { get; }
  public Dictionary<uint, IDataSubscriber> ValueRefToSubscriber { get; }

  public bool CreatePublisher(string serviceName, string topicName, IntPtr context, byte historySize)
  {
    return ValueRefToPublisher.TryAdd(
      (uint)context,
      _silKitEntity.CreateDataPublisher(serviceName, topicName, historySize));
  }

  public bool CreateSubscriber(
    string serviceName,
    string topicName,
    IntPtr context)
  {
    var sub = _silKitEntity.CreateDataSubscriber(
      serviceName,
      topicName,
      context,
      DataMessageHandler);

    return ValueRefToSubscriber.TryAdd((uint)context, sub);
  }

#endregion service creation

  public void PublishAll(List<Tuple<uint, byte[]>> dataList)
  {
    foreach (var tuple in dataList)
    {
      Publish(tuple.Item1, tuple.Item2);
    }
  }

  public void Publish(uint valueReference, byte[] data)
  {
    var success = ValueRefToPublisher.TryGetValue(valueReference, out var publisher);
    if (!success)
    {
      throw new ArgumentOutOfRangeException(
        $"The value reference '{valueReference}' does not have a publisher assigned to it.");
    }

    publisher!.Publish(data);
  }

#region data collection & processing

  private void DataMessageHandler(IntPtr context, IDataSubscriber subscriber, DataMessageEvent dataMessageEvent)
  {
    // buffer data
    // Use a last-is-best approach for storage

    var valueRef = (uint)context;
    var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0L : dataMessageEvent.TimestampInNS;

    // data is processed in sim. step callback (OnSimulationStep)
    if (DataBuffer.TryGetValue(dataMessageEvent.TimestampInNS, out var futureDict))
    {
      futureDict[valueRef] = dataMessageEvent.Data;
    }
    else
    {
      var dict = new Dictionary<uint, byte[]>
      {
        { valueRef, dataMessageEvent.Data }
      };
      DataBuffer.Add(dataMessageEvent.TimestampInNS, dict);
    }
  }

  /// <summary>
  ///   Retrieve all received data up to a specific point in time.
  ///   The data will be removed from the buffer and the result is aggregated.
  /// </summary>
  /// <param name="currentTime">The time (included) up to which the data will be retrieved.</param>
  /// <returns>The aggregated data, divided by the value reference of the variable.</returns>
  public Dictionary<uint, byte[]> RetrieveReceivedData(ulong currentTime)
  {
    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var removeList = new List<ulong>();
    var valueUpdates = new Dictionary<uint, byte[]>();
    foreach (var timeDataPair in DataBuffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeDataPair.Key <= currentTime)
      {
        foreach (var dataBufferKvp in timeDataPair.Value)
        {
          valueUpdates[dataBufferKvp.Key] = dataBufferKvp.Value;
        }

        removeList.Add(timeDataPair.Key);
      }
      else
      {
        // no need to iterate future events
        break;
      }
    }

    // remove all processed entries from the buffer
    foreach (var _ in removeList)
    {
      DataBuffer.RemoveAt(0);
    }

    return valueUpdates;
  }

#endregion data collection & processing

#region IDisposable

  ~SilKitDataManager()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool _disposedValue;

  protected void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
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

#endregion
}
