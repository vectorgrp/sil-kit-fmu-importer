// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Rpc;

public interface IRpcClient
{
  public void Call(ByteVector data, IntPtr userContext);
}
