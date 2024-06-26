// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;
using Fmi.Supplements;

namespace FmiBridge.Tests;

[TestFixture]
public class NamingConventionParserTests
{
  [SetUp]
  public void Setup()
  {
  }

  [Test, Order(0)]
  public void Parser_Basic_NoSeparator()
  {
    var variableName = "test";

    var structuredNameList = new List<string>
    {
      "test"
    };
    var compareContainer = new StructuredNameContainer(structuredNameList);

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(0)]
  public void Parser_Basic_TwoUnquoted()
  {
    var variableName = "test1.test2";

    var structuredNameList = new List<string>
    {
      "test1",
      "test2"
    };

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(0)]
  public void Parser_Basic_TwoQuoted()
  {
    var variableName = "'qtest1'.'qtest2'";

    var structuredNameList = new List<string>
    {
      "'qtest1'",
      "'qtest2'"
    };

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(0)]
  public void Parser_Basic_TwoMixedA()
  {
    var variableName = "test1.'qtest1'";

    var structuredNameList = new List<string>
    {
      "test1",
      "'qtest1'"
    };

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(0)]
  public void Parser_Basic_TwoMixedB()
  {
    var variableName = "'qtest1'.test1";

    var structuredNameList = new List<string>
    {
      "'qtest1'",
      "test1"
    };

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(0)]
  public void Parser_Basic_QuotedSeparator()
  {
    var variableName = "'qtest1.qtestX'";

    var structuredNameList = new List<string>
    {
      "'qtest1.qtestX'"
    };

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(0)]
  public void Parser_Basic_EscapedQuote()
  {
    var variableName = "'qtest1\\'qtestX'";

    var structuredNameList = new List<string>
    {
      "'qtest1\\'qtestX'"
    };

    Assert.That(() => StructuredVariableParser.Parse(variableName).Path, Is.EqualTo(structuredNameList));
  }

  [Test, Order(1)]
  public void Parser_Invalid_EmptyName()
  {
    var variableName = "";

    // Empty names are not allowed
    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(1)]
  public void Parser_Invalid_SeparatorOnly()
  {
    var variableName = ".";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(2)]
  public void Parser_Separator_LeadingSeparator()
  {
    var variableName = ".test";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(2)]
  public void Parser_Separator_TrailingSeparatorA()
  {
    var variableName = "test.";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(2)]
  public void Parser_Separator_TrailingSeparatorB()
  {
    var variableName = "'test'.";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(2)]
  public void Parser_Separator_ConsequtiveSeparator()
  {
    var variableName = "test1..test2";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(2)]
  public void Parser_Separator_MissingSeparatorAfterQuoteA()
  {
    var variableName = "'test'test";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(2)]
  public void Parser_Separator_MissingSeparatorAfterQuoteB()
  {
    var variableName = "'test''test'";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(3)]
  public void Parser_Quote_OpenQuote()
  {
    var variableName = "'test";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(3)]
  public void Parser_Quote_TrailingQuote()
  {
    var variableName = "test'";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }

  [Test, Order(3)]
  public void Parser_Quote_UnquotedEscape()
  {
    var variableName = "test\\'";

    Assert.Throws<ParserException>(() => StructuredVariableParser.Parse(variableName));
  }
}
