using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class RunLogger
{
    private const string LogPrefix = "[RunLog]";
    private static readonly object FileLock = new object();

    private static StreamWriter writer;
    private static string currentLogPath;
    private static bool initialized;

    public static string CurrentLogPath => currentLogPath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        Initialize();
    }

    public static void Event(string message)
    {
        Initialize();
        Debug.Log($"{LogPrefix} {message}");
    }

    public static void Warning(string message)
    {
        Initialize();
        Debug.LogWarning($"{LogPrefix} {message}");
    }

    public static void Error(string message)
    {
        Initialize();
        Debug.LogError($"{LogPrefix} {message}");
    }

    private static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        TryOpenWriter();
        Application.logMessageReceived += OnUnityLog;
        Application.quitting += OnApplicationQuitting;

        WriteRaw($"=== Run Start {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===");
        WriteRaw($"unityVersion={Application.unityVersion}, platform={Application.platform}");
        WriteRaw($"persistentDataPath={Application.persistentDataPath}");
        if (!string.IsNullOrEmpty(currentLogPath))
            WriteRaw($"logFilePath={currentLogPath}");

        Debug.Log($"{LogPrefix} File: {currentLogPath}");
    }

    private static void TryOpenWriter()
    {
        try
        {
            string folder = Path.Combine(Application.persistentDataPath, "RunLogs");
            Directory.CreateDirectory(folder);

            string fileName = $"run_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";
            currentLogPath = Path.Combine(folder, fileName);

            var stream = new FileStream(currentLogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            currentLogPath = "<failed-to-open-log-file>";
            writer = null;
            Debug.LogWarning($"{LogPrefix} Failed to create log file: {ex.Message}");
        }
    }

    private static void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        WriteRaw($"{timestamp} [{type}] {condition}");

        if ((type == LogType.Error || type == LogType.Assert || type == LogType.Exception) &&
            !string.IsNullOrWhiteSpace(stackTrace))
        {
            WriteRaw(stackTrace);
        }
    }

    private static void OnApplicationQuitting()
    {
        WriteRaw($"=== Run End {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===");

        Application.logMessageReceived -= OnUnityLog;
        Application.quitting -= OnApplicationQuitting;

        lock (FileLock)
        {
            writer?.Dispose();
            writer = null;
        }
    }

    private static void WriteRaw(string line)
    {
        lock (FileLock)
        {
            writer?.WriteLine(line);
        }
    }
}
