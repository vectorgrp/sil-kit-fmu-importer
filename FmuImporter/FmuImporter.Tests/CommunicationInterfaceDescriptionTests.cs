// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;
using FmuImporter.CommDescription;
using FmuImporter.Exceptions;

namespace FmuImporter.Tests;

[SetUpFixture]
public class SetupTrace
{
  [OneTimeSetUp]
  public void StartTest()
  {
    Trace.Listeners.Add(new ConsoleTraceListener());
  }

  [OneTimeTearDown]
  public void EndTest()
  {
    Trace.Flush();
  }
}

[TestFixture]
public class CommunicationInterfaceDescriptionTests
{
  [SetUp]
  public void Setup()
  {
  }

#region basic tests

  [Test, Order(0)]
  public void CID_Basic_FileValid()
  {
    var cidPath = "CIDs/Basic/MinValid.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(0)]
  public void CID_Basic_FileNotAvailable()
  {
    var cidPath = "CIDs/Basic/NoFile.yaml";

    Assert.Throws<FileNotFoundException>(() => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_FileEmpty()
  {
    var cidPath = "CIDs/Basic/InvalidEmpty.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_FileInvalid()
  {
    var cidPath = "CIDs/Basic/InvalidIncorrectFormat.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_NoVersionField()
  {
    var cidPath = "CIDs/Basic/InvalidNoVersionField.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_IncorrectVersion()
  {
    var cidPath = "CIDs/Basic/InvalidIncorrectVersionField.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(1)]
  public void CID_Basic_SampleCommunicationInterface()
  {
    var cidPath = "CIDs/Basic/SampleCommunicationInterface.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);

    Assert.That(commInterface.Version, Is.EqualTo(1));
    Assert.That(commInterface.EnumDefinitions, Is.Not.Null);
    Assert.That(commInterface.EnumDefinitions!.Count, Is.EqualTo(1));
    var firstEnum = commInterface.EnumDefinitions![0];
    Assert.That(firstEnum, Is.Not.Null);
    Assert.That(firstEnum.Name, Is.EqualTo("EnumSample"));
    Assert.That(firstEnum.IndexType, Is.EqualTo("int32"));
    Assert.That(firstEnum.Items, Is.Not.Null);
    Assert.That(firstEnum.Items.Count, Is.EqualTo(3));
    Assert.That(firstEnum.Items[0], Is.Not.Null);
    Assert.That(firstEnum.Items[0].Name, Is.EqualTo("EnumValue1"));

    Assert.That(firstEnum.Items[0].Value, Is.EqualTo(1));
    Assert.That(firstEnum.Items[1], Is.Not.Null);
    Assert.That(firstEnum.Items[1].Name, Is.EqualTo("EnumValue2"));
    Assert.That(firstEnum.Items[1].Value, Is.EqualTo(2));
    Assert.That(firstEnum.Items[2], Is.Not.Null);
    Assert.That(firstEnum.Items[2].Name, Is.EqualTo("3"));
    Assert.That(firstEnum.Items[2].Value, Is.EqualTo(3));

    Assert.That(commInterface.StructDefinitions, Is.Not.Null);
    Assert.That(commInterface.StructDefinitions!.Count, Is.EqualTo(1));
    var firstStruct = commInterface.StructDefinitions![0];
    Assert.That(firstStruct, Is.Not.Null);
    Assert.That(firstStruct.Name, Is.EqualTo("StructSample"));
    Assert.That(firstStruct.Members, Is.Not.Null);
    Assert.That(firstStruct.Members.Count, Is.EqualTo(3));
    Assert.That(firstStruct.Members[0], Is.Not.Null);
    Assert.That(firstStruct.Members[0].Name, Is.EqualTo("Member1"));
    Assert.That(firstStruct.Members[0].Type, Is.EqualTo("int"));
    Assert.That(firstStruct.Members[1], Is.Not.Null);
    Assert.That(firstStruct.Members[1].Name, Is.EqualTo("Member2"));
    Assert.That(firstStruct.Members[1].Type, Is.EqualTo("double"));
    Assert.That(firstStruct.Members[2], Is.Not.Null);
    Assert.That(firstStruct.Members[2].Name, Is.EqualTo("Member3"));
    Assert.That(firstStruct.Members[2].Type, Is.EqualTo("EnumSample"));

    Assert.That(commInterface.Publishers, Is.Not.Null);
    Assert.That(commInterface.Publishers!.Count, Is.EqualTo(4));
    Assert.That(commInterface.Publishers![0], Is.Not.Null);
    Assert.That(commInterface.Publishers![0].Name, Is.EqualTo("PubInt"));
    Assert.That(commInterface.Publishers![0].Type, Is.EqualTo("Int"));
    Assert.That(commInterface.Publishers![1], Is.Not.Null);
    Assert.That(commInterface.Publishers![1].Name, Is.EqualTo("PubDouble"));
    Assert.That(commInterface.Publishers![1].Type, Is.EqualTo("double"));
    Assert.That(commInterface.Publishers![2], Is.Not.Null);
    Assert.That(commInterface.Publishers![2].Name, Is.EqualTo("PubEnum"));
    Assert.That(commInterface.Publishers![2].Type, Is.EqualTo("EnumSample"));
    Assert.That(commInterface.Publishers![3], Is.Not.Null);
    Assert.That(commInterface.Publishers![3].Name, Is.EqualTo("PubStruct"));
    Assert.That(commInterface.Publishers![3].Type, Is.EqualTo("StructSample"));

    Assert.That(commInterface.Subscribers, Is.Not.Null);
    Assert.That(commInterface.Subscribers!.Count, Is.EqualTo(4));
    Assert.That(commInterface.Subscribers![0], Is.Not.Null);
    Assert.That(commInterface.Subscribers![0].Name, Is.EqualTo("SubInt"));
    Assert.That(commInterface.Subscribers![0].Type, Is.EqualTo("Int"));
    Assert.That(commInterface.Subscribers![1], Is.Not.Null);
    Assert.That(commInterface.Subscribers![1].Name, Is.EqualTo("SubDouble"));
    Assert.That(commInterface.Subscribers![1].Type, Is.EqualTo("double"));
    Assert.That(commInterface.Subscribers![2], Is.Not.Null);
    Assert.That(commInterface.Subscribers![2].Name, Is.EqualTo("SubEnum"));
    Assert.That(commInterface.Subscribers![2].Type, Is.EqualTo("EnumSample"));
    Assert.That(commInterface.Subscribers![3], Is.Not.Null);
    Assert.That(commInterface.Subscribers![3].Name, Is.EqualTo("SubStruct"));
    Assert.That(commInterface.Subscribers![3].Type, Is.EqualTo("StructSample"));
  }

#endregion basic tests

#region publisher tests

  [Test, Order(10)]
  public void CID_PubSub_PubNoSub()
  {
    var cidPath = "CIDs/PubSub/PubNoSub.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);
    Assert.That(commInterface.Publishers?.Count, Is.EqualTo(1));
    Assert.That(commInterface.Subscribers, Is.Null);
  }

  [Test, Order(10)]
  public void CID_PubSub_PubNoEntries()
  {
    var cidPath = "CIDs/PubSub/PubNoEntries.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(11)]
  public void CID_PubSub_PubEntryNoName()
  {
    var cidPath = "CIDs/PubSub/PubEntryNoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(11)]
  public void CID_PubSub_PubEntryTypeMissing()
  {
    var cidPath = "CIDs/PubSub/PubEntryTypeMissing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(11)]
  public void CID_PubSub_PubEntryTypeEmpty()
  {
    var cidPath = "CIDs/PubSub/PubEntryTypeEmpty.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

#endregion publisher tests

#region subscriber tests

  [Test, Order(20)]
  public void CID_PubSub_SubNoPub()
  {
    var cidPath = "CIDs/PubSub/SubNoPub.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);
    Assert.That(commInterface.Subscribers?.Count, Is.EqualTo(1));
    Assert.That(commInterface.Publishers, Is.Null);
  }

  [Test, Order(20)]
  public void CID_PubSub_SubNoEntries()
  {
    var cidPath = "CIDs/PubSub/SubNoEntries.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(21)]
  public void CID_PubSub_SubEntryNoName()
  {
    var cidPath = "CIDs/PubSub/SubEntryNoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(21)]
  public void CID_PubSub_SubEntryTypeMissing()
  {
    var cidPath = "CIDs/PubSub/SubEntryTypeMissing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(21)]
  public void CID_PubSub_SubEntryTypeEmpty()
  {
    var cidPath = "CIDs/PubSub/SubEntryTypeEmpty.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

#endregion subscriber tests

#region enum tests

  [Test, Order(30)]
  public void CID_Enum_NoEntries()
  {
    var cidPath = "CIDs/Enum/Section_NoEntries.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(31)]
  public void CID_Enum_Def_NoName()
  {
    var cidPath = "CIDs/Enum/Def_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(31)]
  public void CID_Enum_Def_IndexTypeMissing()
  {
    var cidPath = "CIDs/Enum/Def_IndexTypeMissing.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(31)]
  public void CID_Enum_Def_NoItemsList()
  {
    var cidPath = "CIDs/Enum/Def_NoItemsList.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(31)]
  public void CID_Enum_Def_NoItems()
  {
    var cidPath = "CIDs/Enum/Def_NoItems.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(32)]
  public void CID_Enum_Def_Item_NoName()
  {
    var cidPath = "CIDs/Enum/Def_Item_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(32)]
  public void CID_Enum_Def_Item_NoValue()
  {
    var cidPath = "CIDs/Enum/Def_Item_NoValue.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(32)]
  public void CID_Enum_Def_Item_EmptyValue()
  {
    var cidPath = "CIDs/Enum/Def_Item_EmptyValue.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

#endregion enum tests

#region struct tests

  [Test, Order(40)]
  public void CID_Struct_NoEntries()
  {
    var cidPath = "CIDs/Struct/Section_NoEntries.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(41)]
  public void CID_Struct_Def_NoName()
  {
    var cidPath = "CIDs/Struct/Def_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(41)]
  public void CID_Struct_Def_NoMembersList()
  {
    var cidPath = "CIDs/Struct/Def_NoMembersList.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(41)]
  public void CID_Struct_Def_NoMembers()
  {
    var cidPath = "CIDs/Struct/Def_NoMembers.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(42)]
  public void CID_Struct_Member_NoName()
  {
    var cidPath = "CIDs/Struct/Def_Member_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

  [Test, Order(42)]
  public void CID_Struct_Member_NoType()
  {
    var cidPath = "CIDs/Struct/Def_Member_NoType.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.LoadCommInterface(cidPath));
  }

#endregion struct tests
}
