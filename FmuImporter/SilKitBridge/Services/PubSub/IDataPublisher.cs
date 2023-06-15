// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.PubSub;

public interface IDataPublisher
{
  public void Publish(byte[] data);
}
