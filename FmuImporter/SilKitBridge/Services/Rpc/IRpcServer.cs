// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Rpc;

public interface IRpcServer
{
  public void SubmitResult(IntPtr callHandle, ByteVector resultData);
}
