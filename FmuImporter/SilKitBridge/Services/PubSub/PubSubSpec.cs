using System.Runtime.InteropServices;

namespace SilKit.Services.PubSub;

public class PubSubSpec
{
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
    Labels.Add(new MatchingLabel()
    {
      Key = key,
      Value = value,
      Kind = kind
    });
  }

  internal IntPtr toSilKitDataSpec()
  {
    var dataSpec = new SilKit_DataSpec
    {
      mediaType = this.MediaType,
      topic = this.Topic
    };

    var labelList = new SilKit_LabelList
    {
      numLabels = (IntPtr)this.Labels.Count
    };

    var labels = new SilKit_Label[(int)labelList.numLabels];
    var labelsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SilKit_Label>() * (int)labelList.numLabels);
    for (int i = 0; i < (int)labelList.numLabels; i++)
    {
      var l = Labels[i];
      labels[i] = new SilKit_Label()
      {
        key = l.Key,
        value = l.Value,
        kind = (int)l.Kind
      };

      Marshal.StructureToPtr(labels[i], labelsPtr + Marshal.SizeOf<SilKit_Label>() * i, false);
    }

    labelList.labels = labelsPtr;
    dataSpec.labelList = labelList;

    var dataSpecPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SilKit_DataSpec>());
    Marshal.StructureToPtr(dataSpec, dataSpecPtr, false);

    return dataSpecPtr;
  }
}

// Internal
[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
internal struct SilKit_DataSpec
{
  public SilKit_DataSpec()
  {
    structHeader =
      SilKitVersion.GetStructHeader(SilKitVersion.ServiceId.Data, SilKitVersion.DatatypeId.DataSpec);
    topic = string.Empty;
    mediaType = string.Empty;
    labelList = new SilKit_LabelList();
  }

  internal SilKitVersion.SilKit_StructHeader structHeader;
  [MarshalAs(UnmanagedType.LPStr)] internal string topic;
  [MarshalAs(UnmanagedType.LPStr)] internal string mediaType;
  internal SilKit_LabelList labelList;
}

[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
internal struct SilKit_LabelList
{
  internal IntPtr /* size_t */ numLabels;
  internal IntPtr labels;
};

[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
internal struct SilKit_Label
{
  [MarshalAs(UnmanagedType.LPStr)] internal string key;
  [MarshalAs(UnmanagedType.LPStr)] internal string value;
  internal int kind;
}
