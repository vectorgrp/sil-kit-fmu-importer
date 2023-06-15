// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.PubSub;

public interface IDataSubscriber
{
  public void SetDataMessageHandler(DataMessageHandler dataMessageHandler);
}
