// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;
using FmuImporter.Models.CommDescription;
using FmuImporter.Models.Exceptions;
using FmuImporter.Models.Helpers;

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

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(0)]
  public void CID_Basic_FileNotAvailable()
  {
    var cidPath = "CIDs/Basic/NoFile.yaml";

    Assert.Throws<FileNotFoundException>(() => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_FileEmpty()
  {
    var cidPath = "CIDs/Basic/InvalidEmpty.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_FileInvalid()
  {
    var cidPath = "CIDs/Basic/InvalidIncorrectFormat.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_NoVersionField()
  {
    var cidPath = "CIDs/Basic/InvalidNoVersionField.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(0)]
  public void CID_Basic_IncorrectVersion()
  {
    var cidPath = "CIDs/Basic/InvalidIncorrectVersionField.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(1)]
  public void CID_Basic_SampleCommunicationInterface()
  {
    var cidPath = "CIDs/Basic/SampleCommunicationInterface.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

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
    Assert.That(commInterface.StructDefinitions!.Count, Is.EqualTo(2));
    var firstStruct = commInterface.StructDefinitions![0];
    Assert.That(firstStruct, Is.Not.Null);
    Assert.That(firstStruct.Name, Is.EqualTo("StructSample"));
    Assert.That(firstStruct.Members, Is.Not.Null);
    Assert.That(firstStruct.Members.Count, Is.EqualTo(4));
    Assert.That(firstStruct.Members[0], Is.Not.Null);
    Assert.That(firstStruct.Members[0].Name, Is.EqualTo("Member1"));
    Assert.That(firstStruct.Members[0].Type, Is.EqualTo("int"));
    Assert.That(firstStruct.Members[1], Is.Not.Null);
    Assert.That(firstStruct.Members[1].Name, Is.EqualTo("Member2"));
    Assert.That(firstStruct.Members[1].Type, Is.EqualTo("double"));
    Assert.That(firstStruct.Members[2], Is.Not.Null);
    Assert.That(firstStruct.Members[2].Name, Is.EqualTo("Member3"));
    Assert.That(firstStruct.Members[2].Type, Is.EqualTo("EnumSample"));
    Assert.That(firstStruct.Members[3], Is.Not.Null);
    Assert.That(firstStruct.Members[3].Name, Is.EqualTo("Member4"));
    Assert.That(firstStruct.Members[3].Type, Is.EqualTo("StructSample2"));

    Assert.That(commInterface.Publishers, Is.Not.Null);
    Assert.That(commInterface.Publishers!.Count, Is.EqualTo(4));
    Assert.That(commInterface.Publishers![0], Is.Not.Null);
    Assert.That(commInterface.Publishers![0].Name, Is.EqualTo("PubInt"));
    Assert.That(commInterface.Publishers![0].Type, Is.EqualTo("Int"));
    Assert.That(commInterface.Publishers![0].ResolvedType.Type, Is.EqualTo(typeof(int)));
    // sanity check if actual data type is not nullable
    Assert.That(commInterface.Publishers![0].ResolvedType.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![1], Is.Not.Null);
    Assert.That(commInterface.Publishers![1].Name, Is.EqualTo("PubDouble"));
    Assert.That(commInterface.Publishers![1].Type, Is.EqualTo("double"));
    Assert.That(commInterface.Publishers![1].ResolvedType.Type, Is.EqualTo(typeof(double)));
    Assert.That(commInterface.Publishers![2], Is.Not.Null);
    Assert.That(commInterface.Publishers![2].Name, Is.EqualTo("PubEnum"));
    Assert.That(commInterface.Publishers![2].Type, Is.EqualTo("EnumSample"));
    Assert.That(commInterface.Publishers![2].ResolvedType.Type, Is.EqualTo(null));
    Assert.That(commInterface.Publishers![3], Is.Not.Null);
    Assert.That(commInterface.Publishers![3].Name, Is.EqualTo("PubStruct"));
    Assert.That(commInterface.Publishers![3].Type, Is.EqualTo("StructSample"));
    Assert.That(commInterface.Publishers![3].ResolvedType.Type, Is.EqualTo(null));
    Assert.That(commInterface.Publishers![3].ResolvedType.CustomType, Is.Not.Null);
    Assert.That(
      commInterface.Publishers![3].ResolvedType.CustomType!.GetType(),
      Is.EqualTo(typeof(StructDefinitionInternal)));
    // check content of structure
    var structMembers = ((StructDefinitionInternal)commInterface.Publishers![3].ResolvedType.CustomType!)
      .FlattenedMembers;
    Assert.That(structMembers.Count, Is.EqualTo(5));
    Assert.That(structMembers[0], Is.Not.Null);
    Assert.That(structMembers[0].Name, Is.EqualTo("Member1"));
    Assert.That(structMembers[0].Type, Is.EqualTo("int"));
    Assert.That(structMembers[0].ResolvedType.Type, Is.EqualTo(typeof(int)));
    // instance name is added by FMU Importer to avoid unnecessary definition copies
    Assert.That(structMembers[0].QualifiedName, Is.EqualTo("Member1"));
    // The outer member is removed by FlattenMembers -> Check for inner member with correct qualified name
    Assert.That(structMembers[3], Is.Not.Null);
    Assert.That(structMembers[3].QualifiedName, Is.EqualTo("Member4.InnerMember1"));
    Assert.That(structMembers[3].Type, Is.EqualTo("int"));
    Assert.That(structMembers[3].ResolvedType.Type, Is.EqualTo(typeof(int)));
    // The outer member is removed by FlattenMembers -> Check for inner member with correct qualified name
    Assert.That(structMembers[4], Is.Not.Null);
    Assert.That(structMembers[4].QualifiedName, Is.EqualTo("Member4.InnerMember2"));
    Assert.That(structMembers[4].Type, Is.EqualTo("int"));
    Assert.That(structMembers[4].ResolvedType.Type, Is.EqualTo(typeof(int)));

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

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
    Assert.That(commInterface.Publishers?.Count, Is.EqualTo(1));
    Assert.That(commInterface.Subscribers, Is.Null);
  }

  [Test, Order(10)]
  public void CID_PubSub_PubNoEntries()
  {
    var cidPath = "CIDs/PubSub/PubNoEntries.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(11)]
  public void CID_PubSub_PubEntryNoName()
  {
    var cidPath = "CIDs/PubSub/PubEntryNoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(11)]
  public void CID_PubSub_PubEntryTypeMissing()
  {
    var cidPath = "CIDs/PubSub/PubEntryTypeMissing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(11)]
  public void CID_PubSub_PubEntryTypeEmpty()
  {
    var cidPath = "CIDs/PubSub/PubEntryTypeEmpty.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

#endregion publisher tests

#region subscriber tests

  [Test, Order(20)]
  public void CID_PubSub_SubNoPub()
  {
    var cidPath = "CIDs/PubSub/SubNoPub.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
    Assert.That(commInterface.Subscribers?.Count, Is.EqualTo(1));
    Assert.That(commInterface.Publishers, Is.Null);
  }

  [Test, Order(20)]
  public void CID_PubSub_SubNoEntries()
  {
    var cidPath = "CIDs/PubSub/SubNoEntries.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(21)]
  public void CID_PubSub_SubEntryNoName()
  {
    var cidPath = "CIDs/PubSub/SubEntryNoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(21)]
  public void CID_PubSub_SubEntryTypeMissing()
  {
    var cidPath = "CIDs/PubSub/SubEntryTypeMissing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(21)]
  public void CID_PubSub_SubEntryTypeEmpty()
  {
    var cidPath = "CIDs/PubSub/SubEntryTypeEmpty.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

#endregion subscriber tests

#region enum tests

  [Test, Order(30)]
  public void CID_Enum_NoEntries()
  {
    var cidPath = "CIDs/Enum/Section_NoEntries.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(31)]
  public void CID_Enum_Def_Missing()
  {
    var cidPath = "CIDs/Enum/Def_Missing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(31)]
  public void CID_Enum_Def_MissingInStructDef()
  {
    var cidPath = "CIDs/Enum/Def_MissingInStructDef.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(31)]
  public void CID_Enum_Def_NoName()
  {
    var cidPath = "CIDs/Enum/Def_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(31)]
  public void CID_Enum_Def_IndexTypeMissing()
  {
    var cidPath = "CIDs/Enum/Def_IndexTypeMissing.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(31)]
  public void CID_Enum_Def_NoItemsList()
  {
    var cidPath = "CIDs/Enum/Def_NoItemsList.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(31)]
  public void CID_Enum_Def_NoItems()
  {
    var cidPath = "CIDs/Enum/Def_NoItems.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(32)]
  public void CID_Enum_Def_Item_NoName()
  {
    var cidPath = "CIDs/Enum/Def_Item_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(32)]
  public void CID_Enum_Def_Item_NoValue()
  {
    var cidPath = "CIDs/Enum/Def_Item_NoValue.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(32)]
  public void CID_Enum_Def_Item_EmptyValue()
  {
    var cidPath = "CIDs/Enum/Def_Item_EmptyValue.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

#endregion enum tests

#region struct tests

  [Test, Order(40)]
  public void CID_Struct_NoEntries()
  {
    var cidPath = "CIDs/Struct/Section_NoEntries.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
  }

  [Test, Order(41)]
  public void CID_Struct_Def_Missing()
  {
    var cidPath = "CIDs/Struct/Def_Missing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(41)]
  public void CID_Struct_Def_NoName()
  {
    var cidPath = "CIDs/Struct/Def_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(41)]
  public void CID_Struct_Def_NoMembersList()
  {
    var cidPath = "CIDs/Struct/Def_NoMembersList.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(41)]
  public void CID_Struct_Def_NoMembers()
  {
    var cidPath = "CIDs/Struct/Def_NoMembers.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(42)]
  public void CID_Struct_Member_StructDefMissing()
  {
    var cidPath = "CIDs/Struct/Def_Member_StructDefMissing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(42)]
  public void CID_Struct_Member_EnumDefMissing()
  {
    var cidPath = "CIDs/Struct/Def_Member_EnumDefMissing.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(42)]
  public void CID_Struct_Member_NoName()
  {
    var cidPath = "CIDs/Struct/Def_Member_NoName.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

  [Test, Order(42)]
  public void CID_Struct_Member_NoType()
  {
    var cidPath = "CIDs/Struct/Def_Member_NoType.yaml";

    Assert.Throws<InvalidCommunicationInterfaceException>(
      () => CommunicationInterfaceDescriptionParser.Load(cidPath));
  }

#endregion struct tests

#region optional tests

  [Test, Order(50)]
  public void CID_Optionals_Scalars()
  {
    var cidPath = "CIDs/Optionals/Scalars.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
    // Boolean/Integers
    Assert.That(commInterface.Publishers, Is.Not.Null);
    Assert.That(commInterface.Publishers!.Count, Is.EqualTo(24));
    Assert.That(commInterface.Publishers![0], Is.Not.Null);
    Assert.That(commInterface.Publishers![0].Name, Is.EqualTo("PubBool"));
    Assert.That(commInterface.Publishers![0].Type, Is.EqualTo("Bool?"));
    Assert.That(commInterface.Publishers![0].ResolvedType.Type, Is.EqualTo(typeof(bool)));
    Assert.That(commInterface.Publishers![0].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![1], Is.Not.Null);
    Assert.That(commInterface.Publishers![1].Name, Is.EqualTo("PubBoolean"));
    Assert.That(commInterface.Publishers![1].Type, Is.EqualTo("Boolean?"));
    Assert.That(commInterface.Publishers![1].ResolvedType.Type, Is.EqualTo(typeof(bool)));
    Assert.That(commInterface.Publishers![1].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![2], Is.Not.Null);
    Assert.That(commInterface.Publishers![2].Name, Is.EqualTo("PubSByte"));
    Assert.That(commInterface.Publishers![2].Type, Is.EqualTo("SByte?"));
    Assert.That(commInterface.Publishers![2].ResolvedType.Type, Is.EqualTo(typeof(sbyte)));
    Assert.That(commInterface.Publishers![2].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![3], Is.Not.Null);
    Assert.That(commInterface.Publishers![3].Name, Is.EqualTo("PubInt8"));
    Assert.That(commInterface.Publishers![3].Type, Is.EqualTo("Int8?"));
    Assert.That(commInterface.Publishers![3].ResolvedType.Type, Is.EqualTo(typeof(sbyte)));
    Assert.That(commInterface.Publishers![3].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![4], Is.Not.Null);
    Assert.That(commInterface.Publishers![4].Name, Is.EqualTo("PubShort"));
    Assert.That(commInterface.Publishers![4].Type, Is.EqualTo("Short?"));
    Assert.That(commInterface.Publishers![4].ResolvedType.Type, Is.EqualTo(typeof(short)));
    Assert.That(commInterface.Publishers![4].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![5], Is.Not.Null);
    Assert.That(commInterface.Publishers![5].Name, Is.EqualTo("PubInt16"));
    Assert.That(commInterface.Publishers![5].Type, Is.EqualTo("Int16?"));
    Assert.That(commInterface.Publishers![5].ResolvedType.Type, Is.EqualTo(typeof(short)));
    Assert.That(commInterface.Publishers![5].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![6], Is.Not.Null);
    Assert.That(commInterface.Publishers![6].Name, Is.EqualTo("PubInt"));
    Assert.That(commInterface.Publishers![6].Type, Is.EqualTo("Int?"));
    Assert.That(commInterface.Publishers![6].ResolvedType.Type, Is.EqualTo(typeof(int)));
    Assert.That(commInterface.Publishers![6].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![7], Is.Not.Null);
    Assert.That(commInterface.Publishers![7].Name, Is.EqualTo("PubInt32"));
    Assert.That(commInterface.Publishers![7].Type, Is.EqualTo("Int32?"));
    Assert.That(commInterface.Publishers![7].ResolvedType.Type, Is.EqualTo(typeof(int)));
    Assert.That(commInterface.Publishers![7].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![8], Is.Not.Null);
    Assert.That(commInterface.Publishers![8].Name, Is.EqualTo("PubLong"));
    Assert.That(commInterface.Publishers![8].Type, Is.EqualTo("Long?"));
    Assert.That(commInterface.Publishers![8].ResolvedType.Type, Is.EqualTo(typeof(long)));
    Assert.That(commInterface.Publishers![8].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![9], Is.Not.Null);
    Assert.That(commInterface.Publishers![9].Name, Is.EqualTo("PubInt64"));
    Assert.That(commInterface.Publishers![9].Type, Is.EqualTo("Int64?"));
    Assert.That(commInterface.Publishers![9].ResolvedType.Type, Is.EqualTo(typeof(long)));
    Assert.That(commInterface.Publishers![9].ResolvedType.IsOptional, Is.True);
    // FLoating point
    Assert.That(commInterface.Publishers![18], Is.Not.Null);
    Assert.That(commInterface.Publishers![18].Name, Is.EqualTo("PubFloat"));
    Assert.That(commInterface.Publishers![18].Type, Is.EqualTo("Float?"));
    Assert.That(commInterface.Publishers![18].ResolvedType.Type, Is.EqualTo(typeof(float)));
    Assert.That(commInterface.Publishers![18].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![19], Is.Not.Null);
    Assert.That(commInterface.Publishers![19].Name, Is.EqualTo("PubFloat32"));
    Assert.That(commInterface.Publishers![19].Type, Is.EqualTo("Float32?"));
    Assert.That(commInterface.Publishers![19].ResolvedType.Type, Is.EqualTo(typeof(float)));
    Assert.That(commInterface.Publishers![19].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![20], Is.Not.Null);
    Assert.That(commInterface.Publishers![20].Name, Is.EqualTo("PubDouble"));
    Assert.That(commInterface.Publishers![20].Type, Is.EqualTo("Double?"));
    Assert.That(commInterface.Publishers![20].ResolvedType.Type, Is.EqualTo(typeof(double)));
    Assert.That(commInterface.Publishers![20].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![21], Is.Not.Null);
    Assert.That(commInterface.Publishers![21].Name, Is.EqualTo("PubFloat64"));
    Assert.That(commInterface.Publishers![21].Type, Is.EqualTo("Float64?"));
    Assert.That(commInterface.Publishers![21].ResolvedType.Type, Is.EqualTo(typeof(double)));
    Assert.That(commInterface.Publishers![21].ResolvedType.IsOptional, Is.True);
    // Enum
    Assert.That(commInterface.Publishers![22], Is.Not.Null);
    Assert.That(commInterface.Publishers![22].Name, Is.EqualTo("PubEnum"));
    Assert.That(commInterface.Publishers![22].Type, Is.EqualTo("EnumSample?"));
    Assert.That(commInterface.Publishers![22].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![22].ResolvedType.CustomTypeName, Is.EqualTo("EnumSample"));
    Assert.That(commInterface.Publishers![22].ResolvedType.IsOptional, Is.True);
    // Struct
    Assert.That(commInterface.Publishers![23], Is.Not.Null);
    Assert.That(commInterface.Publishers![23].Name, Is.EqualTo("PubStruct"));
    Assert.That(commInterface.Publishers![23].Type, Is.EqualTo("StructSample?"));
    Assert.That(commInterface.Publishers![23].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![23].ResolvedType.CustomTypeName, Is.EqualTo("StructSample"));
    Assert.That(commInterface.Publishers![23].ResolvedType.IsOptional, Is.True);
  }

  [Test, Order(51)]
  public void CID_Optionals_Lists()
  {
    var cidPath = "CIDs/Optionals/Lists.yaml";

    var commInterface = CommunicationInterfaceDescriptionParser.Load(cidPath);

    Assert.IsNotNull(commInterface);
    Assert.That(commInterface.Publishers, Is.Not.Null);
    Assert.That(commInterface.Publishers!.Count, Is.EqualTo(10));

    Assert.That(commInterface.Publishers![0], Is.Not.Null);
    Assert.That(commInterface.Publishers![0].Name, Is.EqualTo("PubDouble_L_D"));
    Assert.That(commInterface.Publishers![0].Type, Is.EqualTo("List<Double>"));
    // list
    Assert.That(commInterface.Publishers![0].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![0].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![0].ResolvedType.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![0].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![0].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![0].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // list type
    Assert.That(commInterface.Publishers![0].ResolvedType.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![0].ResolvedType.InnerType!.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![0].ResolvedType.InnerType!.InnerType, Is.Null);
    Assert.That(commInterface.Publishers![0].ResolvedType.InnerType!.Type, Is.EqualTo(typeof(double)));

    Assert.That(commInterface.Publishers![1], Is.Not.Null);
    Assert.That(commInterface.Publishers![1].Name, Is.EqualTo("PubDouble_L_Dopt"));
    Assert.That(commInterface.Publishers![1].Type, Is.EqualTo("List<Double?>"));
    // list
    Assert.That(commInterface.Publishers![1].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![1].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![1].ResolvedType.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![1].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![1].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![1].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // list type
    Assert.That(commInterface.Publishers![1].ResolvedType.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![1].ResolvedType.InnerType!.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![1].ResolvedType.InnerType!.InnerType, Is.Null);
    Assert.That(commInterface.Publishers![1].ResolvedType.InnerType!.Type, Is.EqualTo(typeof(double)));

    Assert.That(commInterface.Publishers![2], Is.Not.Null);
    Assert.That(commInterface.Publishers![2].Name, Is.EqualTo("PubDouble_Lopt_D"));
    Assert.That(commInterface.Publishers![2].Type, Is.EqualTo("List<Double>?"));
    // list
    Assert.That(commInterface.Publishers![2].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![2].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![2].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![2].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![2].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![2].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // list type
    Assert.That(commInterface.Publishers![2].ResolvedType.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![2].ResolvedType.InnerType!.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![2].ResolvedType.InnerType!.InnerType, Is.Null);
    Assert.That(commInterface.Publishers![2].ResolvedType.InnerType!.Type, Is.EqualTo(typeof(double)));

    Assert.That(commInterface.Publishers![3], Is.Not.Null);
    Assert.That(commInterface.Publishers![3].Name, Is.EqualTo("PubDouble_Lopt_Dopt"));
    Assert.That(commInterface.Publishers![3].Type, Is.EqualTo("List<Double?>?"));
    // list
    Assert.That(commInterface.Publishers![3].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![3].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![3].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![3].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![3].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![3].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // list type
    Assert.That(commInterface.Publishers![3].ResolvedType.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![3].ResolvedType.InnerType!.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![3].ResolvedType.InnerType!.InnerType, Is.Null);
    Assert.That(commInterface.Publishers![3].ResolvedType.InnerType!.Type, Is.EqualTo(typeof(double)));

    // nested lists
    Assert.That(commInterface.Publishers![4], Is.Not.Null);
    Assert.That(commInterface.Publishers![4].Name, Is.EqualTo("PubDouble_L_L_D"));
    Assert.That(commInterface.Publishers![4].Type, Is.EqualTo("List<List<Double>>"));
    // outer list
    Assert.That(commInterface.Publishers![4].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![4].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![4].ResolvedType.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![4].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.IsList, Is.True);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.Type, Is.Null);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list type
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.InnerType!.Type, Is.Not.Null);
    Assert.That(commInterface.Publishers![4].ResolvedType.InnerType!.InnerType!.InnerType, Is.Null);

    Assert.That(commInterface.Publishers![5], Is.Not.Null);
    Assert.That(commInterface.Publishers![5].Name, Is.EqualTo("PubDouble_L_Lopt_D"));
    Assert.That(commInterface.Publishers![5].Type, Is.EqualTo("List<List<Double>?>"));
    // outer list
    Assert.That(commInterface.Publishers![5].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![5].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![5].ResolvedType.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![5].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.IsList, Is.True);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.Type, Is.Null);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list type
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.InnerType!.Type, Is.Not.Null);
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.InnerType!.Type, Is.EqualTo(typeof(double)));
    Assert.That(commInterface.Publishers![5].ResolvedType.InnerType!.InnerType!.InnerType, Is.Null);

    Assert.That(commInterface.Publishers![6], Is.Not.Null);
    Assert.That(commInterface.Publishers![6].Name, Is.EqualTo("PubDouble_Lopt_L_D"));
    Assert.That(commInterface.Publishers![6].Type, Is.EqualTo("List<List<Double>>?"));
    // outer list
    Assert.That(commInterface.Publishers![6].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![6].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![6].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![6].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.IsList, Is.True);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.IsOptional, Is.False);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.Type, Is.Null);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list type
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.InnerType!.Type, Is.Not.Null);
    Assert.That(commInterface.Publishers![6].ResolvedType.InnerType!.InnerType!.InnerType, Is.Null);

    Assert.That(commInterface.Publishers![7], Is.Not.Null);
    Assert.That(commInterface.Publishers![7].Name, Is.EqualTo("PubDouble_Lopt_Lopt_D"));
    Assert.That(commInterface.Publishers![7].Type, Is.EqualTo("List<List<Double>?>?"));
    // outer list
    Assert.That(commInterface.Publishers![7].ResolvedType, Is.Not.Null);
    Assert.That(commInterface.Publishers![7].ResolvedType.IsList, Is.True);
    Assert.That(commInterface.Publishers![7].ResolvedType.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![7].ResolvedType.Type, Is.Null);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.IsList, Is.True);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.IsOptional, Is.True);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.Type, Is.Null);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.InnerType, Is.Not.Null);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.GetType(), Is.EqualTo(typeof(OptionalType)));
    // inner list type
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.InnerType!.IsList, Is.False);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.InnerType!.Type, Is.Not.Null);
    Assert.That(commInterface.Publishers![7].ResolvedType.InnerType!.InnerType!.InnerType, Is.Null);
  }

#endregion optional tests
}
