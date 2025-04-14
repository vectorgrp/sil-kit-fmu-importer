// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using FmuImporter.Models.CommDescription.ParserExtensions;
using FmuImporter.Models.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace FmuImporter.Models.CommDescription;

public static class CommunicationInterfaceDescriptionParser
{
  private class ValidatingNodeDeserializer : INodeDeserializer
  {
    private readonly INodeDeserializer _nodeDeserializer;

    public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer)
    {
      _nodeDeserializer = nodeDeserializer;
    }

    public bool Deserialize(
      IParser reader,
      Type expectedType,
      Func<IParser, Type, object?> nestedObjectDeserializer,
      out object? value)
    {
      try
      {
        var success = _nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
        if (success)
        {
          try
          {
            var context = new ValidationContext(value!, null, null);
            Validator.ValidateObject(value!, context, true);
          }
          catch (ValidationException e)
          {
            throw new InvalidCommunicationInterfaceException($"The configuration is invalid: {e.Message}", e);
          }
          catch (Exception e)
          {
            throw new Exception($"{e.Message}", e);
          }
        }

        return success;
      }
      catch (InvalidCommunicationInterfaceException e)
      {
        throw new Exception($"{e.Message}", e);
      }
      catch (YamlException e)
      {
        if (e.InnerException != null)
        {
          throw new InvalidCommunicationInterfaceException($"{e.InnerException.Message}", e);
        }
        else
        {
          throw new InvalidCommunicationInterfaceException(
            $"The communication interface description is invalid. Parser reported: {e.Message}",
            e);
        }
      }
      catch (Exception e)
      {
        throw new InvalidCommunicationInterfaceException(
          $"The communication interface description is invalid: {e.Message}",
          e);
      }
    }
  }

  public static CommunicationInterfaceInternal LoadCommInterface(string path)
  {
    var publisherTypeConverter = new PublisherTypeConverter();
    var subscriberTypeConverter = new SubscriberTypeConverter();
    var structTypeConverter = new StructTypeConverter();
    var enumTypeConverter = new EnumTypeConverter();

    var deserializer =
      new DeserializerBuilder()
        .WithTypeConverter(publisherTypeConverter)
        .WithTypeConverter(subscriberTypeConverter)
        .WithTypeConverter(structTypeConverter)
        .WithTypeConverter(enumTypeConverter)
        .WithNodeDeserializer(
          inner => new ValidatingNodeDeserializer(inner),
          s => s.InsteadOf<ObjectNodeDeserializer>())
        .Build();

    CommunicationInterfaceInternal? commInterface;
    try
    {
      commInterface = deserializer.Deserialize<CommunicationInterfaceInternal?>(File.ReadAllText(path));
      if (commInterface == null)
      {
        throw new InvalidCommunicationInterfaceException(
          "Failed to deserialize the provided communication interface file");
      }

      commInterface.CheckAndAssignCustomTypeDependencies();
    }
    catch (Exception e)
    {
      throw ProcessException(e);
    }

    return commInterface;
  }

  private static Exception ProcessException(Exception e)
  {
    var currentException = e;
    var continueDescending = (currentException.InnerException != null) &&
                             currentException is not InvalidCommunicationInterfaceException;

    while (continueDescending)
    {
      if (currentException is YamlException yamlException)
      {
        if ((currentException.InnerException != null) &&
            currentException.InnerException is YamlException or InvalidCommunicationInterfaceException)
        {
          currentException = currentException.InnerException;
          continue;
        }

        currentException = new InvalidCommunicationInterfaceException(
          $"Invalid communication interface description. Issue detected between {yamlException.Start} and {yamlException.End}.",
          yamlException);
      }
      else
      {
        currentException = new InvalidCommunicationInterfaceException(
          $"Invalid communication interface description. {currentException.Message}",
          currentException);
      }

      continueDescending = false;
    }

    return currentException;
  }
}
