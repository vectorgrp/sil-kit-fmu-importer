// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services.PubSub;

namespace FmuImporter.SilKit;

public enum DataCategory
{
  Clock,
  Variable,
  ClockedVariable,
  Structure,
  ClockedStructure
}

public class SilKitDataManager : IDisposable
{
  private readonly SilKitEntity _silKitEntity;
  private Dictionary<DataCategory, SortedList<ulong, Dictionary<long, byte[]>>> DataBuffers { get; }

  public SilKitDataManager(SilKitEntity silKitEntity)
  {
    _silKitEntity = silKitEntity;

    DataBuffers = new Dictionary<DataCategory, SortedList<ulong, Dictionary<long, byte[]>>>();
    foreach (var category in Enum.GetValues<DataCategory>())
    {
      var buffer = new SortedList<ulong, Dictionary<long, byte[]>>();
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
      {
        buffer.Add(0, new Dictionary<long, byte[]>());
      }
      DataBuffers[category] = buffer;
    }

    ValueRefToPublisher = new Dictionary<long, IDataPublisher>();
    ValueRefToSubscriber = new Dictionary<long, IDataSubscriber>();
  }

#region service creation

  public Dictionary<long, IDataPublisher> ValueRefToPublisher { get; }
  public Dictionary<long, IDataSubscriber> ValueRefToSubscriber { get; }

  // Create labeled publisher for regular FMU variable
  public bool CreatePublisher(
    string serviceName,
    string topicName,
    string? instanceName,
    string? namespaceName,
    IntPtr context,
    byte historySize)
  {
    var pub = _silKitEntity.CreateDataPublisher(
      serviceName,
      topicName,
      instanceName,
      namespaceName,
      historySize);
    return ValueRefToPublisher.TryAdd(
      (long)context,
      pub);
  }

  // Create labeled subscriber for regular FMU variable
  public bool CreateSubscriber(
    string serviceName,
    string topicName,
    string? labelInstanceName,
    string? labelNamespace,
    IntPtr context,
    DataCategory category)
  {
    var buffer = DataBuffers[category];
    var sub = _silKitEntity.CreateDataSubscriber(
      serviceName,
      topicName,
      labelInstanceName,
      labelNamespace,
      context,
      (ctx, subscriber, dataMessageEvent) => DataMessageHandler(ctx, subscriber, dataMessageEvent, buffer));

    return ValueRefToSubscriber.TryAdd((long)context, sub);
  }

#endregion service creation

  public void PublishAll(List<Tuple<long, byte[]>> dataList)
  {
    foreach (var tuple in dataList)
    {
      Publish(tuple.Item1, tuple.Item2);
    }
  }

  public void Publish(long valueReference, byte[] data)
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

  private void DataMessageHandler(
    IntPtr context,
    IDataSubscriber subscriber,
    DataMessageEvent dataMessageEvent,
    SortedList<ulong, Dictionary<long, byte[]>> buffer)
  {
    // buffer data
    // Use a last-is-best approach for storage

    var valueRef = (long)context;
    var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0UL : dataMessageEvent.TimestampInNS;

    // data is processed in sim. step callback (OnSimulationStep)
    if (buffer.TryGetValue(timeStamp, out var futureDict))
    {
      futureDict[valueRef] = dataMessageEvent.Data;
    }
    else
    {
      var dict = new Dictionary<long, byte[]>
      {
        { valueRef, dataMessageEvent.Data }
      };
      buffer.Add(timeStamp, dict);
    }
  }

  /// <summary>
  ///   Retrieve all received data of a specific category up to a specific point in time.
  ///   The data will be removed from the buffer and the result is aggregated.
  /// </summary>
  /// <param name="currentTime">The time (included) up to which the data will be retrieved.</param>
  /// <param name="category">The data category to retrieve.</param>
  /// <returns>The aggregated data, divided by the value reference of the variable.</returns>
  public Dictionary<long, byte[]> RetrieveReceivedData(ulong currentTime, DataCategory category)
  {
    var buffer = DataBuffers[category];
    var valueUpdates = new Dictionary<long, byte[]>();
    var removeCount = 0;

    foreach (var timeDataPair in buffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeDataPair.Key <= currentTime)
      {
        foreach (var dataBufferKvp in timeDataPair.Value)
        {
          valueUpdates[dataBufferKvp.Key] = dataBufferKvp.Value;
        }

        removeCount++;
      }
      else
      {
        break;
      }
    }

    // clean up processed timestamps
    while (removeCount-- > 0)
    {
      buffer.RemoveAt(0);
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
