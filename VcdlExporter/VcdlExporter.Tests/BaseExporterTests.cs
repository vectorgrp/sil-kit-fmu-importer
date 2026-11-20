// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using FmuImporter.Models.Helpers;

namespace VcdlExporter.Tests;

[TestFixture]
public class BaseExporterTests
{
  private class TestExporter : BaseExporter
  {
  }

  private TestExporter _exporter = default!;
  private StringBuilder _sb = default!;

  [SetUp]
  public void Setup()
  {
    _exporter = new TestExporter();
    _sb = new StringBuilder();
  }

  private string Text
  {
    get
    {
      return _sb.ToString().Replace("\r\n", "\n");
    }
  }

#region CanonizeTokenTypeName

  [TestCase("bool", "bool")]
  [TestCase("boolean", "bool")]
  [TestCase("sbyte", "int8")]
  [TestCase("int8", "int8")]
  [TestCase("short", "int16")]
  [TestCase("int16", "int16")]
  [TestCase("int", "int32")]
  [TestCase("integer", "int32")]
  [TestCase("int32", "int32")]
  [TestCase("long", "int64")]
  [TestCase("int64", "int64")]
  [TestCase("byte", "uint8")]
  [TestCase("uint8", "uint8")]
  [TestCase("ushort", "uint16")]
  [TestCase("uint16", "uint16")]
  [TestCase("uint", "uint32")]
  [TestCase("uint32", "uint32")]
  [TestCase("ulong", "uint64")]
  [TestCase("uint64", "uint64")]
  [TestCase("float", "float")]
  [TestCase("float32", "float")]
  [TestCase("single", "float")]
  [TestCase("float64", "double")]
  [TestCase("real", "double")]
  [TestCase("double", "double")]
  [TestCase("string", "string")]
  [TestCase("binary", "bytes")]
  [TestCase("byte[]", "bytes")]
  public void CanonizeTokenTypeName_KnownAlias_ReturnsVcdlType(string input, string expected)
  {
    Assert.That(BaseExporter.CanonizeTokenTypeName(input), Is.EqualTo(expected));
  }

  [TestCase("Int32", "int32")]
  [TestCase("DOUBLE", "double")]
  [TestCase("Boolean", "bool")]
  [TestCase("Byte[]", "bytes")]
  public void CanonizeTokenTypeName_IsCaseInsensitive(string input, string expected)
  {
    Assert.That(BaseExporter.CanonizeTokenTypeName(input), Is.EqualTo(expected));
  }

  [TestCase("MyStruct")]
  [TestCase("MyEnum")]
  [TestCase("")]
  public void CanonizeTokenTypeName_UnknownType_IsReturnedUnchanged(string input)
  {
    Assert.That(BaseExporter.CanonizeTokenTypeName(input), Is.EqualTo(input));
  }

#endregion

#region CreateVariableType

  [TestCase(typeof(bool), "bool")]
  [TestCase(typeof(sbyte), "int8")]
  [TestCase(typeof(short), "int16")]
  [TestCase(typeof(int), "int32")]
  [TestCase(typeof(long), "int64")]
  [TestCase(typeof(byte), "uint8")]
  [TestCase(typeof(ushort), "uint16")]
  [TestCase(typeof(uint), "uint32")]
  [TestCase(typeof(ulong), "uint64")]
  [TestCase(typeof(float), "float")]
  [TestCase(typeof(double), "double")]
  [TestCase(typeof(string), "string")]
  [TestCase(typeof(byte[]), "bytes")]
  public void CreateVariableType_BuiltInType_ReturnsVcdlType(Type type, string expected)
  {
    var optionalType = new OptionalType(isOptional: false, isList: false, type: type);

    Assert.That(_exporter.CreateVariableType(optionalType), Is.EqualTo(expected));
  }

  [Test]
  public void CreateVariableType_CustomType_ReturnsCustomTypeName()
  {
    var optionalType = new OptionalType(isOptional: false, isList: false, customTypeName: "MyStruct");

    Assert.That(_exporter.CreateVariableType(optionalType), Is.EqualTo("MyStruct"));
  }

  [Test]
  public void CreateVariableType_OptionalType_DoesNotChangeTypeName()
  {
    var optionalType = new OptionalType(isOptional: true, isList: false, type: typeof(int));

    Assert.That(_exporter.CreateVariableType(optionalType), Is.EqualTo("int32"));
  }

  [Test]
  public void CreateVariableType_List_ReturnsListOfInnerType()
  {
    var inner = new OptionalType(isOptional: false, isList: false, type: typeof(double));
    var list = new OptionalType(isOptional: false, isList: true, optionalType: inner);

    Assert.That(_exporter.CreateVariableType(list), Is.EqualTo("list<double>"));
  }

  [Test]
  public void CreateVariableType_NestedLists_ReturnsNestedListTypes()
  {
    var inner = new OptionalType(isOptional: false, isList: false, customTypeName: "MyEnum");
    var list = new OptionalType(isOptional: false, isList: true, optionalType: inner);
    var listOfList = new OptionalType(isOptional: true, isList: true, optionalType: list);
    var listOfListOfList = new OptionalType(isOptional: false, isList: true, optionalType: listOfList);

    Assert.That(_exporter.CreateVariableType(listOfListOfList), Is.EqualTo("list<list<list<MyEnum>>>"));
  }

#endregion

#region text blocks

  [Test]
  public void AddVcdlHeader_WithNamespace_WritesVersionImportAndNamespace()
  {
    _exporter.AddVcdlHeader(_sb, "MyNamespace");

    Assert.That(Text, Is.EqualTo("version 2.0;\nimport module \"SilKit\";\n\nnamespace MyNamespace\n{\n"));
  }

  [Test]
  public void AddVcdlHeader_WithoutNamespace_UsesExporterNamespace()
  {
    _exporter.AddVcdlHeader(_sb);

    Assert.That(Text, Does.Contain("namespace VcdlExporter\n{"));
  }

  [Test]
  public void AddVcdlFooter_ClosesNamespace()
  {
    _exporter.AddVcdlFooter(_sb);

    Assert.That(Text, Is.EqualTo("}\n"));
  }

  [Test]
  public void AddInterfaceHeader_WithoutCausality_PrefixesNameWithI()
  {
    _exporter.AddInterfaceHeader("Model", null, _sb);

    Assert.That(Text, Is.EqualTo("  [Binding=\"SilKit\"]\n  interface IModel\n  {\n"));
  }

  // VcdlCausality is internal, so it cannot be a parameter of a (public) test method
  [TestCase("Provider", "IModel_Provider")]
  [TestCase("Consumer", "IModel_Consumer")]
  public void AddInterfaceHeader_WithCausality_AppendsCausality(string causality, string expectedName)
  {
    _exporter.AddInterfaceHeader("Model", Enum.Parse<BaseExporter.VcdlCausality>(causality), _sb);

    Assert.That(Text, Does.Contain($"interface {expectedName}\n"));
  }

  [Test]
  public void AddInterfaceFooter_ClosesInterface()
  {
    _exporter.AddInterfaceFooter(_sb);

    Assert.That(Text, Is.EqualTo("  }\n"));
  }

  [Test]
  public void AddInterfaceBody_Provider_WritesProvidedData()
  {
    var variables = new HashSet<BaseExporter.VcdlVariable>
    {
      new() { Name = "a", Type = "int32" },
      new() { Name = "b", Type = "list<double>" }
    };

    _exporter.AddInterfaceBody(variables, BaseExporter.VcdlCausality.Provider, _sb);

    Assert.That(Text, Is.EqualTo("    provided data int32 a;\n    provided data list<double> b;\n"));
  }

  [Test]
  public void AddInterfaceBody_Consumer_WritesConsumedData()
  {
    var variables = new HashSet<BaseExporter.VcdlVariable> { new() { Name = "a", Type = "bool" } };

    _exporter.AddInterfaceBody(variables, BaseExporter.VcdlCausality.Consumer, _sb);

    Assert.That(Text, Is.EqualTo("    consumed data bool a;\n"));
  }

  [Test]
  public void AddInterfaceBody_NoVariables_WritesNothing()
  {
    _exporter.AddInterfaceBody(new HashSet<BaseExporter.VcdlVariable>(), BaseExporter.VcdlCausality.Provider, _sb);

    Assert.That(Text, Is.Empty);
  }

  [Test]
  public void AddObjectInstance_WritesInterfaceInstance()
  {
    _exporter.AddObjectInstance("Model", "Instance", _sb);

    Assert.That(Text, Is.EqualTo("  IModel Instance;\n"));
  }

#endregion
}
