using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Collections.Generic;


[InitializeOnLoad]
public static class UnakinBridgeServer
{
    public static bool IsConnected = false;
    public static List<string> LogMessages = new List<string>();
    public static int ActivePort = -1; // Default value for uninitialized port

    public const string LoggingEnabledKey = "UnityCommandServer_LoggingEnabled";
    public static bool LoggingEnabled = false;

    private static TcpListener tcpListener;
    private static TcpClient connectedTcpClient;
    private static CancellationTokenSource cancellationTokenSource; // cancels the server

    private static int logMessageLimit = 100;
    private static CommandFactory commandFactory;

    private static string appDataTempFolder;

    static UnakinBridgeServer()
    {
        
        LoggingEnabled = EditorPrefs.GetBool(UnakinBridgeServer.LoggingEnabledKey);
        EditorApplication.quitting += OnEditorQuitting;

        // Subscribe to the beforeAssemblyReload event to stop the server before scripts reload
        AssemblyReloadEvents.beforeAssemblyReload += StopListenerTask;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        appDataTempFolder = Unakin.HashHelper.GetUnakinProjectAppDataTempFolder();

        DebugLogMessage("UnakinBridgeServer Static Constructor");

        StartListenerThread();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;

        DebugLogMessage($"OnPlayModeStateChanged(PlayModeStateChange {state})");

        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                OnEditorEnteredPlayMode();
                break;
            case PlayModeStateChange.ExitingPlayMode: // not quite exited yet
                break;
            default:
            case PlayModeStateChange.EnteredPlayMode:
            case PlayModeStateChange.EnteredEditMode:
                break;
        }

        //InitializeLogger();
    }
    
    public static void OnEditorEnteredPlayMode()
    {
        //StopListenerTask();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()  // called on play in editor but not on stopping play, also called on things like recompiling code
    {
        //DebugLogMessage("UnakinBridgeServer OnScriptsReloaded");
    }

    private static void StartListenerThread()
    {
        //StopListenerTask(); // ensure we cleanup any existing resources

        DebugLogMessage("UnakinBridgeServer StartListenerThread");
        // Run the asynchronous version of the method
        cancellationTokenSource = new CancellationTokenSource();
        commandFactory = new CommandFactory();
        //Task.Run(() => ListenForIncomingRequestsAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);

        // Start the asynchronous listener with error handling
        Task.Run(async () =>
        {
            try
            {
                await ListenForIncomingRequestsAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                DebugLogMessage("Listener task was cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unhandled exception in ListenForIncomingRequestsAsync: {ex}");
            }
        }, cancellationTokenSource.Token);
    }

    private static void OnEditorQuitting()
    {
        DebugLogMessage("UnakinBridgeServer OnEditorQuitting");
        StopListenerTask();
    }

    private static void StopListenerTask()
    {
        DebugLogMessage("UnakinBridgeServer StopListenerTask");
        string portFilePath = Path.Combine(appDataTempFolder, "UnakinBridge.txt");

        // Ensure the file exists before attempting to delete
        if (File.Exists(portFilePath))
        {
            File.Delete(portFilePath);
        }
        ActivePort = -1;

        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        // Ensure we free up the TcpListener and TcpClient resources
        if (tcpListener != null)
        {
            tcpListener.Stop();
           // tcpListener.Dispose();
            tcpListener = null; 
        }

        if (connectedTcpClient != null)
        {
            connectedTcpClient.Close();
            connectedTcpClient.Dispose();
            connectedTcpClient = null;
        }
    }

    
    public static void OnCommandFinished(string commandName, string commandID, string errors)
    {
        LogMessage("UnakinBridgeServer OnCommandFinished");
        if (string.IsNullOrEmpty(errors))
            SendMessage($"<CMD_FINISHED>: {commandName}:{commandID} finished successfully\n");
        else
            SendMessage($"<CMD_FINISHED><ERROR>: {commandName} finished with errors: \n\t {errors} \n");
    }

    public static void DebugLogMessage(string message) 
    {
        string timePrefix = DateTime.Now.ToString("[HH:mm:ss]");

        string unnamed = "Unnamed";
        string threadName = Thread.CurrentThread.Name ?? unnamed;
        int threadId = Thread.CurrentThread.ManagedThreadId;
        if(threadId==1 && threadName == unnamed) 
        {
            threadName = "MainThread";
        }
        
        string textToAppend = $"{threadId}:{threadName}, Port:{ActivePort}, Message: {message}";

        if(LoggingEnabled) Debug.Log(textToAppend);
        File.AppendAllText(DebugLogFilepath, $"{timePrefix} {textToAppend}\n");
    }

    // Add a message to the log
    public static void LogMessage(string message)
    {
        if (LoggingEnabled)
        {
            DebugLogMessage(message);
        }

        string timePrefix = DateTime.Now.ToString("[HH:mm:ss]");
        LogMessages.Add(timePrefix + message);

        while (LogMessages.Count > logMessageLimit) // Limit the log to 100 messages
        {
            LogMessages.RemoveAt(0);
        }
    }

    private static string DebugLogFilepath => Path.Combine(appDataTempFolder, "UnakinBridgeLog.txt");
    private static string PortFilePath => Path.Combine(appDataTempFolder, "UnakinBridge.txt");


    private static async Task ListenForIncomingRequestsAsync(CancellationToken token)
    {
        DebugLogMessage("UnakinBridgeServer ListenForIncomingRequestsAsync");
        bool listenerStarted = false;
         
        if(tcpListener == null)
        {
            try
            { 
                tcpListener = new TcpListener(IPAddress.Loopback, 0);
                tcpListener.Start();
                listenerStarted = true;
                ActivePort = tcpListener.LocalEndpoint is IPEndPoint endPoint ? endPoint.Port : -1;

                File.WriteAllText(PortFilePath, ActivePort.ToString());


                DebugLogMessage($"UnakinBridgeServer ListenForIncomingRequestsAsync on port {ActivePort}");

                LogMessage($"[Server] Listening on port {ActivePort}..");
            }
            catch (SocketException e)
            {
                LogMessage($"[Server] Exception creating TcpListener: {e.Message}");
                ActivePort = -1;
            }
            catch (Exception e)
            {
                LogMessage($"[Server] Exception creating TcpListener: {e.Message}");
                ActivePort = -1;
            }
        }

        if (!listenerStarted)
        {
            LogMessage("[Server] Failed to start listener.");
            return;
        }

        try
        {
            Byte[] bytes = new Byte[1024];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (connectedTcpClient = await tcpListener.AcceptTcpClientAsync()) // call IDispose which will in turn call close on the client
                    {
                        LogMessage("[Client]: Connected");
                        IsConnected = true;

                        using (NetworkStream stream = connectedTcpClient.GetStream())
                        {
                            int length;
                            while ((length = await stream.ReadAsync(bytes, 0, bytes.Length, token)) != 0)
                            {
                                var incomingData = new byte[length];
                                Array.Copy(bytes, 0, incomingData, 0, length);
                                string clientMessage = System.Text.Encoding.UTF8.GetString(incomingData); // encoding must match the client

                                bool isHeartbeatMessage = clientMessage.Contains("heartbeat");
                                if (!isHeartbeatMessage)
                                {
                                    LogMessage($"[Received]: {clientMessage}");
                                }
                                SendMessage("<ACK> " + clientMessage, !isHeartbeatMessage);

                                if (clientMessage.StartsWith("{"))
                                {
                                    try
                                    {
                                        CommandBase command = commandFactory.CreateCommandFromJson(clientMessage);
                                        if (command != null)
                                        {
                                            string commandName = command.GetType().Name;
                                            LogMessage($"[Command]: Executing {commandName}");
                                            await command.ExecuteOnMainThread();
                                        }
                                        else
                                        {
                                            LogMessage($"[Command]: Failed to create command from\n{clientMessage}\nCommand factory returned null.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"[Command]: Error executing command: {ex.Message}");
                                        SendMessage($"Error executing command: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    LogMessage("[Connection]: IOException: " + ioEx.Message);
                }
                catch (SocketException socketException)
                {
                    LogMessage("[Connection]: SocketException: " + socketException.Message);
                }
                finally
                {
                    IsConnected = false;
                    LogMessage("[Connection]: Disconnected client");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage("[Server]: Exception: " + ex.Message);
        }
        finally
        {
            LogMessage("[Server]: Stopping...");
            tcpListener?.Stop();
        }
    }

    // Method to send a message to the client
    private static void SendMessage(string message, bool add_log = true)
    {
        if (add_log)
        {
            DebugLogMessage($"UnakinBridgeServer SendMessage {message}");
        }
        if (connectedTcpClient == null || !connectedTcpClient.Connected)
        {
            Debug.LogError("No client connected.");
            return;
        }

        try
        {
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                byte[] serverMessageAsByteArray = System.Text.Encoding.UTF8.GetBytes($"{message}<END>");
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                stream.Flush();
                if (add_log && !message.Contains("<ACK>"))
                {
                    LogMessage("[Sent]: " + message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
    }

    public static void LogMessage(string message, LogLevel level = LogLevel.Info)
    {
        if (!LoggingEnabled) return;

        string timePrefix = DateTime.Now.ToString("[HH:mm:ss]");
        string logLevelPrefix = $"[{level}]";

        string logEntry = $"{timePrefix} {logLevelPrefix} {message}";
        Debug.Log("Unakin Bridge: " + logEntry);

        LogMessages.Add(logEntry);

        while (LogMessages.Count > logMessageLimit)
        {
            LogMessages.RemoveAt(0);
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}
