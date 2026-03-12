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
  /// </summary>
  /// <param name="currentTime">The time (included) up to which the data will be retrieved.</param>
  /// <param name="category">The data category to retrieve.</param>
  /// <returns>The aggregated data, divided by the value reference of the variable.</returns>
  public Dictionary<long, byte[]> RetrieveReceivedData(ulong currentTime, DataCategory category)
  {
    var buffer = DataBuffers[category];
    var valueUpdates = new Dictionary<long, byte[]>();

    foreach (var timeDataPair in buffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeDataPair.Key <= currentTime)
      {
        foreach (var dataBufferKvp in timeDataPair.Value)
        {
          valueUpdates[dataBufferKvp.Key] = dataBufferKvp.Value;
        }
      }
      else
      {
        break;
      }
    }

    return valueUpdates;
  }

public void ClearDataUpTo(double pointInTime, params DataCategory[] categories)
  {
    var buffers = categories.Length > 0
      ? categories.Select(c => DataBuffers[c])
      : DataBuffers.Values;

    foreach (var buffer in buffers)
    {
      var keysToRemove = buffer.Keys.Where(time => time <= pointInTime).ToList();
      foreach (var key in keysToRemove)
      {
        buffer.Remove(key);
      }
    }
  }

  /// <summary>
  ///   Look up active clocks up to a specific point in time without modifying the buffer.
  ///   A clock is considered active if its first byte is non-zero.
  /// </summary>
  /// <param name="currentTime">The time (included) up to which the data will be looked up.</param>
  /// <returns>The set of value references of active clocks.</returns>
  public HashSet<long> LookupActiveClocks(ulong currentTime)
  {
    var activeClocks = new HashSet<long>();
    var clocksBuffer = DataBuffers[DataCategory.Clock];

    foreach (var timeDataPair in clocksBuffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeDataPair.Key <= currentTime)
      {
        foreach (var clockRefVal in timeDataPair.Value)
        {
          if (clockRefVal.Value[0] != 0)
          {
            activeClocks.Add(clockRefVal.Key);
          }
        }
      }
      else
      {
        break;
      }
    }

    return activeClocks;
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
