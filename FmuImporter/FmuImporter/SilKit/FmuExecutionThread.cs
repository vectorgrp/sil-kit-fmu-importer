// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace FmuImporter.SilKit;

/// <summary>
///   A dedicated thread on which all FMU calls run. Some FMUs bind runtime state to the thread
///   that instantiated them and crash (access violation) when called from another thread.
/// </summary>
public sealed class FmuExecutionThread : IDisposable
{
  private readonly BlockingCollection<Action> _workItems = new();
  private readonly Thread _thread;

  public FmuExecutionThread()
  {
    _thread = new Thread(Run)
    {
      IsBackground = true,
      Name = "FmuImporterFmuThread"
    };
    _thread.Start();
  }

  private void Run()
  {
    foreach (var workItem in _workItems.GetConsumingEnumerable())
    {
      workItem();
    }
  }

  /// <summary>Executes <paramref name="action" /> on the FMU thread and blocks until it completes.</summary>
  public void Invoke(Action action)
  {
    if (Thread.CurrentThread == _thread)
    {
      // Already on the FMU thread - run inline to avoid a self-deadlock.
      action();
      return;
    }

    using var completed = new ManualResetEventSlim(false);
    ExceptionDispatchInfo? capturedException = null;
    _workItems.Add(
      () =>
      {
        try
        {
          action();
        }
        catch (Exception e)
        {
          capturedException = ExceptionDispatchInfo.Capture(e);
        }
        finally
        {
          completed.Set();
        }
      });

    completed.Wait();
    capturedException?.Throw();
  }

  /// <summary>Executes <paramref name="func" /> on the FMU thread and returns its result.</summary>
  public T Invoke<T>(Func<T> func)
  {
    var result = default(T)!;
    Invoke(() => { result = func(); });
    return result;
  }

  /// <summary>
  ///   Queues a long-running action (e.g. the simulation step loop) on the FMU thread without
  ///   blocking the caller. The returned task completes when the action returns or throws.
  /// </summary>
  public Task Post(Action action)
  {
    var tcs = new TaskCompletionSource();
    _workItems.Add(
      () =>
      {
        try
        {
          action();
          tcs.TrySetResult();
        }
        catch (Exception e)
        {
          tcs.TrySetException(e);
        }
      });
    return tcs.Task;
  }

  public void Dispose()
  {
    // Stops the consuming loop once all already-queued work items have been processed,
    // and waits for it so that no FMU work is still running once this returns.
    _workItems.CompleteAdding();
    if (Thread.CurrentThread != _thread)
    {
      _thread.Join();
      // only safe once the consuming loop has exited
      _workItems.Dispose();
    }
  }
}
