using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

[InitializeOnLoad]
public static class UnakinLogger
{
    private static string LogFilePath;
    private static string LogFilePathPIE;
    private static string LogFileFolder;
    private const string LogFileName = "UnakinEditor.log";
    private const string LogPIEFileName = "UnakinEditorPlay.log";

    private const string Separator = "------------------------------------------------------------";
    
    //[MenuItem("Unakin/Test Log")]
    //public static void LogMe()
    //{ 
    //    Debug.Log("UnityCommandServerWindow");
    //}


    static UnakinLogger()
    {        
        string LogFileFolder = Unakin.HashHelper.GetUnakinProjectAppDataTempFolder();
        LogFilePath = Path.Combine(LogFileFolder, LogFileName);
        LogFilePathPIE = Path.Combine(LogFileFolder, LogPIEFileName);

        Logger.IsPlaying = EditorApplication.isPlayingOrWillChangePlaymode;

        InitializeLogger();

        // Write session start info
        Logger.Append($"Unakin log session started at {DateTime.Now}");

        // Console.Write("Unakin Logger initialized.");
    }

    private static void InitializeLogger()
    {
        // Reassign the log handler
        Debug.unityLogger.logHandler = new Logger();

        // Prevent duplicate handlers
        Application.logMessageReceived -= HandleLogMessage; 
        Application.logMessageReceived += HandleLogMessage;
        EditorApplication.quitting -= OnEditorQuitting;
        EditorApplication.quitting += OnEditorQuitting;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        //Debug.Log("UnakinLogger initialized or reloaded.");
    }

    private static void OnEditorEnteredPlayMode()
    {
        // Rotate log files for PIE log
        RotateLogs(LogFilePathPIE, 5);
    }

    //[UnityEditor.Callbacks.DidReloadScripts]
    //private static void OnScriptsReloaded()
    //{
    //    InitializeLogger();
    //}

    private static void RotateLogs(string filePath, int maxFiles)
    {
        string extension = Path.GetExtension(filePath);
        string fileWithoutExtension = filePath.Substring(0, filePath.Length - extension.Length);

        // Move older files up the chain, ensuring we don't overwrite existing files
        for (int i = maxFiles - 1; i > 0; i--)
        {
            string olderFilePath = $"{fileWithoutExtension}.{i}{extension}";
            string newerFilePath = $"{fileWithoutExtension}.{i + 1}{extension}";

            if (File.Exists(olderFilePath))
            {
                // Ensure we delete the newer file if it already exists to avoid duplicate entries
                if (File.Exists(newerFilePath))
                {
                    File.Delete(newerFilePath);
                }
                File.Move(olderFilePath, newerFilePath);
            }
        }

        // Rename the current file to file.1
        string firstBackupFilePath = $"{fileWithoutExtension}.1{extension}";
        if (File.Exists(firstBackupFilePath))
        {
            File.Delete(firstBackupFilePath);
        }
        if (File.Exists(filePath))
        {
            File.Move(filePath, firstBackupFilePath);
        }
    }


    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Logger.IsPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                OnEditorEnteredPlayMode();
                break;
            case PlayModeStateChange.ExitingPlayMode: // not quite exited yet
                Logger.IsPlaying = true;
                break;
            default:
            case PlayModeStateChange.EnteredPlayMode:
            case PlayModeStateChange.EnteredEditMode:
                break;
        }

        InitializeLogger();
    }

    private static void OnEditorQuitting()
    {
        Debug.Log("Unakin Log: Unity Editor is shutting down.");
        RotateLogs(LogFilePath, 5);
    }

    private static void HandleLogMessage(string message, string stackTrace, LogType type)
    {
        Logger.AddSeperator();
    
        // Write compile errors or other log messages to the log file
        Logger.Append($"[{Logger.TimeStamp}] [{type}] {message}");
        if (!string.IsNullOrEmpty(stackTrace))
        {
            string filteredStackTrace = Logger.FilterStackTrace(stackTrace);
            Logger.Append($"Stack Trace:\n{filteredStackTrace}");
        }
    }

    private class Logger : ILogHandler
    {
        private readonly ILogHandler defaultLogHandler = Debug.unityLogger.logHandler;

        public static string TimeStamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            //string logLevel = logType.ToString();
            //string message = string.Format(format, args);
            //string stackTrace = (logType == LogType.Error || logType == LogType.Exception || logType == LogType.Warning)
            //    ? Environment.StackTrace
            //    : string.Empty;

            //// Context information
            //string contextInfo = context != null ? $"Context: {context.name} (Type: {context.GetType().Name})" : "Context: None";

            //// Write detailed information to the log file
            //Append($"[{TimeStamp}] [{logLevel}] {message}");
            //Append(contextInfo);
            //if (!string.IsNullOrEmpty(stackTrace)) 
            //{
            //    stackTrace = FilterStackTrace(stackTrace);
            //    Append($"Stack Trace:\n{stackTrace}");
            //}
            //AddSeperator();

            // Pass log messages to Unity's default logger as well
            defaultLogHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            AddSeperator();

            string stackTrace = FilterStackTrace(exception.StackTrace);
            string contextInfo = context != null ? $"Context: {context.name} (Type: {context.GetType().Name})" : "Context: None";

            // Write detailed exception info to the log file
            Append($"[{TimeStamp}] [Exception] {exception.Message}");
            Append($"{contextInfo}");
            Append($"Stack Trace:\n{stackTrace}");

            // Re-throw the exception with the original stack trace
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
        public static void AddSeperator()
        {
            Append(Separator);
        }

        public static bool IsPlaying = false;
        public static void Append(string line)
        {
            if (IsPlaying) // can only be called from the main thread
            {
                File.AppendAllText(LogFilePathPIE, line + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }

        public static string FilterStackTrace(string stackTrace)
        {
            // Split the stack trace into individual lines
            var lines = stackTrace.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                // Add only lines that do not contain irrelevant information
                if (!line.Contains("UnityEngine.DebugLogHandler") &&
                    !line.Contains("UnakinLogger/Logger:LogFormat") &&
                    !line.Contains("UnityEngine.Debug:Log"))
                {
                    filteredLines.Add(line.Trim());
                }
            }

            // Join the filtered lines back into a single string
            return string.Join("\n", filteredLines);
        }

    }
}