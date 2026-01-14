// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.PubSub;

public class PubSubSpec
{
  public const string InstanceKey = "Instance";
  public const string NamespaceKey = "Namespace";

  public struct MatchingLabel
  {
    public enum Kinds : uint
    {
      Optional = 1,
      Mandatory = 2
    }

    public string Key;
    public string Value;
    public Kinds Kind;
  }

  public PubSubSpec(string topic, string mediaType)
  {
    Topic = topic;
    MediaType = mediaType;
    Labels = new List<MatchingLabel>();
  }

  public PubSubSpec(string topic, string mediaType, List<MatchingLabel> labels)
  {
    Topic = topic;
    MediaType = mediaType;
    Labels = labels;
  }

  public string Topic { get; }
  public string MediaType { get; }

  public List<MatchingLabel> Labels { get; }


  public void AddLabel(MatchingLabel label)
  {
    if (label.Kind != MatchingLabel.Kinds.Mandatory && label.Kind != MatchingLabel.Kinds.Optional)
    {
      throw new InvalidOperationException(
        "SilKit::Services::MatchingLabel must specify a SilKit::Services::MatchingLabel::Kind.");
    }

    Labels.Add(label);
  }

  public void AddLabel(string key, string value, MatchingLabel.Kinds kind)
  {
    Labels.Add(
      new MatchingLabel
      {
        Key = key,
        Value = value,
        Kind = kind
      });
  }

  internal IntPtr ToSilKitDataSpec()
  {
    var dataSpec = new DataSpec
    {
      mediaType = MediaType,
      topic = Topic
    };

    var labelList = new LabelList
    {
      numLabels = (IntPtr)Labels.Count
    };

    var labels = new Label[(int)labelList.numLabels];
    var labelsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Label>() * (int)labelList.numLabels);
    for (var i = 0; i < (int)labelList.numLabels; i++)
    {
      var l = Labels[i];
      labels[i] = new Label()
      {
        key = l.Key,
        value = l.Value,
        kind = (int)l.Kind
      };

      Marshal.StructureToPtr(labels[i], labelsPtr + Marshal.SizeOf<Label>() * i, false);
    }

    labelList.labels = labelsPtr;
    dataSpec.labelList = labelList;

    var dataSpecPtr = Marshal.AllocHGlobal(Marshal.SizeOf<DataSpec>());
    Marshal.StructureToPtr(dataSpec, dataSpecPtr, false);

    return dataSpecPtr;
  }
}

// Internal
[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
internal struct DataSpec
{
  public DataSpec()
  {
    structHeader =
      SilKitVersion.GetStructHeader(SilKitVersion.ServiceId.Data, SilKitVersion.DatatypeId.DataSpec);
    topic = string.Empty;
    mediaType = string.Empty;
    labelList = new LabelList();
  }

  internal SilKitVersion.StructHeader structHeader;
  [MarshalAs(UnmanagedType.LPStr)] internal string topic;
  [MarshalAs(UnmanagedType.LPStr)] internal string mediaType;
  internal LabelList labelList;
}
