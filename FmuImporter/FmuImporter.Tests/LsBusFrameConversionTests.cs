// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Fmi.FmiModel.Internal;
using SilKit.Services;
using SilKit.Services.Can;
using SilKit.Services.Ethernet;
using SilKit.Services.Logger;

namespace FmuImporter.Tests;

// Conversion between FMI-LS-BUS transmit operations (FMU side) and SIL Kit CAN / Ethernet frames.
// The LS-BUS operations are built byte by byte here, so the tests do not depend on the FmiBridge structures.
[TestFixture]
public class LsBusFrameConversionTests
{
  private class TestLogger : ILogger
  {
    public List<string> Errors { get; } = new List<string>();

    public void Log(LogLevel level, string message)
    {
      if (level == LogLevel.Error)
      {
        Errors.Add(message);
      }
    }

    public LogLevel GetLogLevel()
    {
      return LogLevel.Trace;
    }
  }

  private const int CanHeaderSize = 16; // OP code 4 + length 4 + ID 4 + Ide 1 + Rtr 1 + data length 2
  private const int CanFdHeaderSize = 17; // OP code 4 + length 4 + ID 4 + Ide 1 + Brs 1 + Esi 1 + data length 2
  private const int EthernetHeaderSize = 29; // OP code 4 + length 4 + 3 fragment bytes + MACs 12 + EtherType 2 + data length 4

  // arbitrary test addresses
  private static readonly byte[] DestinationMac = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
  private static readonly byte[] SourceMac = { 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
  // 0x88B5 (local experimental EtherType): little endian in FMI-LS-BUS, big endian on the SIL Kit network
  private static readonly byte[] LsEtherType = { 0xB5, 0x88 };
  private static readonly byte[] SilKitEtherType = { 0x88, 0xB5 };

  private static byte[] CreatePayload(int size)
  {
    return Enumerable.Range(0, size).Select(i => (byte)(i + 1)).ToArray();
  }

  private static byte[] UInt32LittleEndian(uint value)
  {
    var bytes = new byte[4];
    BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
    return bytes;
  }

  private static byte[] UInt16LittleEndian(ushort value)
  {
    var bytes = new byte[2];
    BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
    return bytes;
  }

  private static byte[] CreateLsCanTransmitOperation(uint id, byte ide, byte rtr, byte[] payload)
  {
    return UInt32LittleEndian((uint)CanOperations.CAN_Transmit)
      .Concat(UInt32LittleEndian((uint)(CanHeaderSize + payload.Length)))
      .Concat(UInt32LittleEndian(id))
      .Concat(new[] { ide, rtr })
      .Concat(UInt16LittleEndian((ushort)payload.Length))
      .Concat(payload)
      .ToArray();
  }

  private static byte[] CreateLsCanFdTransmitOperation(uint id, byte ide, byte brs, byte esi, byte[] payload)
  {
    return UInt32LittleEndian((uint)CanOperations.CAN_FD_Transmit)
      .Concat(UInt32LittleEndian((uint)(CanFdHeaderSize + payload.Length)))
      .Concat(UInt32LittleEndian(id))
      .Concat(new[] { ide, brs, esi })
      .Concat(UInt16LittleEndian((ushort)payload.Length))
      .Concat(payload)
      .ToArray();
  }

  private static byte[] CreateLsEthernetTransmitOperation(byte[] payload)
  {
    return UInt32LittleEndian((uint)EthernetOperations.Transmit)
      .Concat(UInt32LittleEndian((uint)(EthernetHeaderSize + payload.Length)))
      .Concat(new byte[] { 0xD5, 0x00, 0x01 }) // start delimiter, fragment counter, last fragment
      .Concat(DestinationMac)
      .Concat(SourceMac)
      .Concat(LsEtherType)
      .Concat(UInt32LittleEndian((uint)payload.Length))
      .Concat(payload)
      .ToArray();
  }

  private static byte[] CreateSilKitEthernetFrame(byte[] payload)
  {
    return DestinationMac.Concat(SourceMac).Concat(SilKitEtherType).Concat(payload).ToArray();
  }

  // SIL Kit frames received from the network point to native memory; a pinned array stands in for it
  private static byte[] WithPinned(byte[] bytes, Func<ByteVector, byte[]> convert)
  {
    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
    try
    {
      return convert(new ByteVector { data = handle.AddrOfPinnedObject(), size = (IntPtr)bytes.Length });
    }
    finally
    {
      handle.Free();
    }
  }

#region CAN

  [TestCase((byte)0, (byte)0, 0u)]
  [TestCase((byte)1, (byte)0, (uint)SilKitCanFrameFlag.Ide)]
  [TestCase((byte)0, (byte)1, (uint)SilKitCanFrameFlag.Rtr)]
  [TestCase((byte)1, (byte)1, (uint)(SilKitCanFrameFlag.Ide | SilKitCanFrameFlag.Rtr))]
  public void Can_FmuToSilKit_ConvertsFrame(byte ide, byte rtr, uint expectedFlags)
  {
    var logger = new TestLogger();
    var payload = CreatePayload(8);

    var (frame, data) = new DataConverter().LsCanTransmitOperationToSilKitCanFrame(
      CreateLsCanTransmitOperation(0x123, ide, rtr, payload),
      logger).Value;

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(frame.id, Is.EqualTo(0x123));
        Assert.That(frame.flags, Is.EqualTo(expectedFlags));
        Assert.That(frame.dlc, Is.EqualTo(8));
        Assert.That(data, Is.EqualTo(payload));
      });
  }

  [Test]
  public void Can_FmuToSilKit_EmptyPayload()
  {
    var logger = new TestLogger();

    var (frame, data) = new DataConverter().LsCanTransmitOperationToSilKitCanFrame(
      CreateLsCanTransmitOperation(0x7FF, 0, 1, Array.Empty<byte>()),
      logger).Value;

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(frame.id, Is.EqualTo(0x7FF));
        Assert.That(frame.dlc, Is.EqualTo(0));
        Assert.That(data, Is.Empty);
      });
  }

  [Test]
  public void Can_FmuToSilKit_IgnoresBytesAfterPayload()
  {
    var logger = new TestLogger();
    var payload = CreatePayload(4);
    var lsBytes = CreateLsCanTransmitOperation(0x10, 0, 0, payload).Concat(new byte[] { 0xEE, 0xEE }).ToArray();

    var (frame, data) = new DataConverter().LsCanTransmitOperationToSilKitCanFrame(lsBytes, logger).Value;

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(frame.dlc, Is.EqualTo(4));
        Assert.That(data, Is.EqualTo(payload));
      });
  }

  [Test]
  public void Can_FmuToSilKit_OperationShorterThanHeader_IsRejected()
  {
    var logger = new TestLogger();

    var result = new DataConverter().LsCanTransmitOperationToSilKitCanFrame(
      CreateLsCanTransmitOperation(0x10, 0, 0, Array.Empty<byte>())[..(CanHeaderSize - 1)],
      logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(result, Is.Null);
      });
  }

  [Test]
  public void Can_FmuToSilKit_PayloadShorterThanDataLength_IsRejected()
  {
    var logger = new TestLogger();
    var lsBytes = CreateLsCanTransmitOperation(0x10, 0, 0, CreatePayload(8));

    var result = new DataConverter().LsCanTransmitOperationToSilKitCanFrame(lsBytes[..^1], logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(result, Is.Null);
      });
  }

  [Test]
  public void Can_SilKitToFmu_ConvertsFrame()
  {
    var payload = CreatePayload(8);
    var frame = new CanFrame
    {
      id = 0x1234,
      flags = (uint)(SilKitCanFrameFlag.Ide | SilKitCanFrameFlag.Rtr),
      dlc = 8
    };

    var lsBytes = WithPinned(
      payload,
      data =>
      {
        frame.data = data;
        return new DataConverter().SilKitCanFrameToLsCanTransmitOperation(frame);
      });

    Assert.That(lsBytes, Is.EqualTo(CreateLsCanTransmitOperation(0x1234, 1, 1, payload)));
  }

  [Test]
  public void Can_RoundTrip_PreservesTransmitOperation()
  {
    var logger = new TestLogger();
    var lsBytes = CreateLsCanTransmitOperation(0x1ABCDEF, 1, 0, CreatePayload(5));
    var dc = new DataConverter();

    var (frame, data) = dc.LsCanTransmitOperationToSilKitCanFrame(lsBytes, logger).Value;
    var roundTripped = WithPinned(
      data,
      pinned =>
      {
        frame.data = pinned;
        return dc.SilKitCanFrameToLsCanTransmitOperation(frame);
      });

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(roundTripped, Is.EqualTo(lsBytes));
      });
  }

#endregion CAN

#region CAN FD

  [Test]
  public void CanFd_FmuToSilKit_ConvertsFrame()
  {
    var logger = new TestLogger();
    var payload = CreatePayload(64);

    var (frame, data) = new DataConverter().LsCanFdTransmitOperationToSilKitCanFrame(
      CreateLsCanFdTransmitOperation(0x42, 1, 1, 0, payload),
      logger).Value;

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(frame.id, Is.EqualTo(0x42));
        Assert.That(
          frame.flags,
          Is.EqualTo((uint)(SilKitCanFrameFlag.Fdf | SilKitCanFrameFlag.Ide | SilKitCanFrameFlag.Brs)));
        Assert.That(frame.dlc, Is.EqualTo(15));
        Assert.That(data, Is.EqualTo(payload));
      });
  }

  [TestCase(0, 0)]
  [TestCase(8, 8)]
  [TestCase(12, 9)]
  [TestCase(16, 10)]
  [TestCase(20, 11)]
  [TestCase(24, 12)]
  [TestCase(32, 13)]
  [TestCase(48, 14)]
  [TestCase(64, 15)]
  public void CanFd_FmuToSilKit_SetsDlcFromDataLength(int dataLength, int expectedDlc)
  {
    var logger = new TestLogger();
    var payload = CreatePayload(dataLength);

    var (frame, data) = new DataConverter().LsCanFdTransmitOperationToSilKitCanFrame(
      CreateLsCanFdTransmitOperation(0x42, 0, 0, 0, payload),
      logger).Value;

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(frame.dlc, Is.EqualTo(expectedDlc));
        Assert.That(data, Is.EqualTo(payload));
      });
  }

  [Test]
  public void CanFd_FmuToSilKit_DataLongerThan64Bytes_IsRejected()
  {
    var logger = new TestLogger();

    var result = new DataConverter().LsCanFdTransmitOperationToSilKitCanFrame(
      CreateLsCanFdTransmitOperation(0x42, 0, 0, 0, CreatePayload(65)),
      logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(result, Is.Null);
      });
  }

  [Test]
  public void CanFd_FmuToSilKit_PayloadShorterThanDataLength_IsRejected()
  {
    var logger = new TestLogger();
    var lsBytes = CreateLsCanFdTransmitOperation(0x42, 0, 0, 0, CreatePayload(32));

    var result = new DataConverter().LsCanFdTransmitOperationToSilKitCanFrame(lsBytes[..^1], logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(result, Is.Null);
      });
  }

  [Test]
  public void CanFd_SilKitToFmu_ConvertsFrame()
  {
    var payload = CreatePayload(48);
    var frame = new CanFrame
    {
      id = 0x42,
      flags = (uint)(SilKitCanFrameFlag.Fdf | SilKitCanFrameFlag.Brs | SilKitCanFrameFlag.Esi),
      dlc = 14
    };

    var lsBytes = WithPinned(
      payload,
      data =>
      {
        frame.data = data;
        return new DataConverter().SilKitCanFrameToLsCanTransmitOperation(frame);
      });

    Assert.That(lsBytes, Is.EqualTo(CreateLsCanFdTransmitOperation(0x42, 0, 1, 1, payload)));
  }

  [Test]
  public void CanFd_RoundTrip_PreservesTransmitOperation()
  {
    var logger = new TestLogger();
    var lsBytes = CreateLsCanFdTransmitOperation(0x1ABCDEF, 1, 1, 1, CreatePayload(64));
    var dc = new DataConverter();

    var (frame, data) = dc.LsCanFdTransmitOperationToSilKitCanFrame(lsBytes, logger).Value;
    var roundTripped = WithPinned(
      data,
      pinned =>
      {
        frame.data = pinned;
        return dc.SilKitCanFrameToLsCanTransmitOperation(frame);
      });

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(roundTripped, Is.EqualTo(lsBytes));
      });
  }

#endregion CAN FD

#region Ethernet

  [Test]
  public void Ethernet_FmuToSilKit_ConvertsFrame()
  {
    var logger = new TestLogger();
    var payload = CreatePayload(100);

    var raw = new DataConverter().LsEthernetTransmitOperationToSilKitEthernetFrame(
      CreateLsEthernetTransmitOperation(payload),
      logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(raw, Is.EqualTo(CreateSilKitEthernetFrame(payload)));
      });
  }

  [TestCase(0)]
  [TestCase(10)]
  [TestCase(45)]
  public void Ethernet_FmuToSilKit_ShortFrameIsPadded(int payloadSize)
  {
    var logger = new TestLogger();
    var payload = CreatePayload(payloadSize);

    var raw = new DataConverter().LsEthernetTransmitOperationToSilKitEthernetFrame(
      CreateLsEthernetTransmitOperation(payload),
      logger);

    var expected = CreateSilKitEthernetFrame(payload)
      .Concat(new byte[EthernetFrame.MinFrameSize - EthernetFrame.EthernetHeaderSize - payloadSize])
      .ToArray();
    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(raw, Is.EqualTo(expected));
      });
  }

  [Test]
  public void Ethernet_FmuToSilKit_OperationShorterThanHeader_IsRejected()
  {
    var logger = new TestLogger();

    var raw = new DataConverter().LsEthernetTransmitOperationToSilKitEthernetFrame(
      CreateLsEthernetTransmitOperation(Array.Empty<byte>())[..(EthernetHeaderSize - 1)],
      logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(raw, Is.Null);
      });
  }

  [Test]
  public void Ethernet_FmuToSilKit_PayloadShorterThanDataLength_IsRejected()
  {
    var logger = new TestLogger();
    var lsBytes = CreateLsEthernetTransmitOperation(CreatePayload(20));

    var raw = new DataConverter().LsEthernetTransmitOperationToSilKitEthernetFrame(lsBytes[..^1], logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(raw, Is.Null);
      });
  }

  [Test]
  public void Ethernet_FmuToSilKit_NegativeDataLength_IsRejected()
  {
    var logger = new TestLogger();
    var lsBytes = CreateLsEthernetTransmitOperation(CreatePayload(20));
    BinaryPrimitives.WriteInt32LittleEndian(lsBytes.AsSpan(EthernetHeaderSize - 4), -1); // data length field = -1

    var raw = new DataConverter().LsEthernetTransmitOperationToSilKitEthernetFrame(lsBytes, logger);

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Has.Count.EqualTo(1));
        Assert.That(raw, Is.Null);
      });
  }

  [TestCase(0)]
  [TestCase(46)]
  [TestCase(1500)]
  public void Ethernet_SilKitToFmu_ConvertsFrame(int payloadSize)
  {
    var logger = new TestLogger();
    var payload = CreatePayload(payloadSize);

    var lsBytes = WithPinned(
      CreateSilKitEthernetFrame(payload),
      raw => new DataConverter().SilKitEthernetFrameToLsEthernetTransmitOperation(
        new EthernetFrame { raw = raw },
        logger));

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(lsBytes, Is.EqualTo(CreateLsEthernetTransmitOperation(payload)));
      });
  }

  [Test]
  public void Ethernet_RoundTrip_PreservesTransmitOperation()
  {
    var logger = new TestLogger();
    // at least the minimum frame size, so no padding is added
    var lsBytes = CreateLsEthernetTransmitOperation(CreatePayload(100));
    var dc = new DataConverter();

    var raw = dc.LsEthernetTransmitOperationToSilKitEthernetFrame(lsBytes, logger);
    var roundTripped = WithPinned(
      raw!,
      pinned => dc.SilKitEthernetFrameToLsEthernetTransmitOperation(new EthernetFrame { raw = pinned }, logger));

    Assert.Multiple(
      () =>
      {
        Assert.That(logger.Errors, Is.Empty);
        Assert.That(roundTripped, Is.EqualTo(lsBytes));
      });
  }

#endregion Ethernet
}
