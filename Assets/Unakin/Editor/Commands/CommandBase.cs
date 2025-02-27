using System.Threading.Tasks;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Threading;

public abstract class CommandBase
{
    //private UnakinBridgeServer server;

    //public void SetServer(UnakinBridgeServer _Server) => server = _Server;

    public string Id;

    private CancellationTokenSource cancellationTokenSource;
    public void Cancel()
    {
        cancellationTokenSource?.Cancel();
    }

    public bool IsValid { get; protected set; }

    // method to handle main-thread execution
    public async Task ExecuteOnMainThread()
    {
        // Schedule the virtual Execute method to run on the main thread
        await RunOnMainThreadAsync(async () =>
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                await ExecuteAsync(cancellationTokenSource.Token);
            }
            // Catch the cancellation
            catch (OperationCanceledException)
            {
                // Do nothing, cancellation is not an error
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                OnFinish(e.Message);
                return;
            }

            // Call OnFinish with no errors if execution succeeds
            OnFinish(null);
        });
    }

    protected Task RunOnMainThreadAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<bool>();
        MainThreadExecutor.Enqueue(async () =>
        {
            try
            {
                await action();
                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        return tcs.Task;
    }

    public abstract Task ExecuteAsync(CancellationToken cancellationToken);


    public void OnFinish(string errors)
    {
        UnakinBridgeServer.OnCommandFinished(GetType().Name, Id, errors);
    }

    public abstract void InitializeFromJson(string jsonData);
}