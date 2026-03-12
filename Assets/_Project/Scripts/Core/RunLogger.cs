using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class RunLogger
{
    private const string LogPrefix = "[RunLog]";
    private const string LogsFolderName = "RunLogs";
    private const string LatestLogFileName = "run_latest.log";
    private const string LatestErrorFileName = "run_last_error.log";

    private static readonly object FileLock = new object();

    private static StreamWriter writer;
    private static string currentLogPath;
    private static string latestLogPath;
    private static string latestErrorLogPath;
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
        if (initialized)
            return;

        initialized = true;

        TryOpenWriter();
        Application.logMessageReceivedThreaded += OnUnityLogThreaded;
        Application.quitting += OnApplicationQuitting;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        WriteRaw($"=== Run Start {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===");
        WriteRaw($"unityVersion={Application.unityVersion}, platform={Application.platform}");
        WriteRaw($"productName={Application.productName}, version={Application.version}");
        WriteRaw($"persistentDataPath={Application.persistentDataPath}");
        WriteRaw($"commandLine={Environment.CommandLine}");
        if (!string.IsNullOrEmpty(currentLogPath))
            WriteRaw($"logFilePath={currentLogPath}");
        if (!string.IsNullOrEmpty(latestErrorLogPath))
            WriteRaw($"latestErrorFilePath={latestErrorLogPath}");

        Debug.Log($"{LogPrefix} File: {currentLogPath}");
    }

    private static void TryOpenWriter()
    {
        try
        {
            string folder = Path.Combine(Application.persistentDataPath, LogsFolderName);
            Directory.CreateDirectory(folder);

            string fileName = $"run_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";
            currentLogPath = Path.Combine(folder, fileName);
            latestLogPath = Path.Combine(folder, LatestLogFileName);
            latestErrorLogPath = Path.Combine(folder, LatestErrorFileName);

            var stream = new FileStream(currentLogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            TryOverwriteFile(latestLogPath, $"Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}");
            TryOverwriteFile(latestErrorLogPath, $"No errors recorded for current session yet.{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            currentLogPath = "<failed-to-open-log-file>";
            latestLogPath = string.Empty;
            latestErrorLogPath = string.Empty;
            writer = null;
            Debug.LogWarning($"{LogPrefix} Failed to create log file: {ex.Message}");
        }
    }

    private static void OnUnityLogThreaded(string condition, string stackTrace, LogType type)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"{timestamp} [{type}] {condition}";
        WriteRaw(line);

        bool isError = type == LogType.Error || type == LogType.Assert || type == LogType.Exception;
        if (isError && !string.IsNullOrWhiteSpace(stackTrace))
            WriteRaw(stackTrace);

        if (isError)
            WriteLatestError(condition, stackTrace, type, timestamp);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        string message = ex != null ? ex.ToString() : "Unknown unhandled exception";
        WriteRaw($"[UnhandledException] {message}");
        WriteLatestError(message, ex != null ? ex.StackTrace : string.Empty, LogType.Exception, DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        string message = e.Exception.ToString();
        WriteRaw($"[UnobservedTaskException] {message}");
        WriteLatestError(message, e.Exception.StackTrace, LogType.Exception, DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    private static void OnApplicationQuitting()
    {
        WriteRaw($"=== Run End {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===");

        Application.logMessageReceivedThreaded -= OnUnityLogThreaded;
        Application.quitting -= OnApplicationQuitting;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        lock (FileLock)
        {
            writer?.Dispose();
            writer = null;
        }
    }

    private static void WriteLatestError(string condition, string stackTrace, LogType type, string timestamp)
    {
        if (string.IsNullOrEmpty(latestErrorLogPath))
            return;

        var builder = new StringBuilder(512);
        builder.AppendLine($"Last error captured at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"Session log: {currentLogPath}");
        builder.AppendLine($"{timestamp} [{type}] {condition}");
        if (!string.IsNullOrWhiteSpace(stackTrace))
            builder.AppendLine(stackTrace);

        TryOverwriteFile(latestErrorLogPath, builder.ToString());
    }

    private static void WriteRaw(string line)
    {
        lock (FileLock)
        {
            writer?.WriteLine(line);
            writer?.Flush();
        }

        if (!string.IsNullOrEmpty(latestLogPath))
            TryAppendLine(latestLogPath, line);
    }

    private static void TryOverwriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static void TryAppendLine(string path, string line)
    {
        try
        {
            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
        }
    }
}
