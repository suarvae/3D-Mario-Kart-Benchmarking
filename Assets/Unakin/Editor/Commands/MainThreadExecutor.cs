#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

[InitializeOnLoad]
public static class MainThreadExecutor
{
    private static readonly Queue<Action> mainThreadActions = new Queue<Action>();

    static MainThreadExecutor()
    {
        // Register Update method to EditorApplication.update
        EditorApplication.update += Update;
    }

    public static void Enqueue(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private static void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }
}
#endif
