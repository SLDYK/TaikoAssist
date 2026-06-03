using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CopilotConsoleBridge
{
    private const string RelativeLogPath = "Logs/Copilot/unity-console.ndjson";

    private static readonly ConcurrentQueue<LogRecord> PendingRecords = new ConcurrentQueue<LogRecord>();
    private static readonly object WriteLock = new object();

    static CopilotConsoleBridge()
    {
        Application.logMessageReceivedThreaded += OnLogMessageReceived;
        EditorApplication.update += FlushPendingRecords;
        EnqueueBridgeMessage("Bridge started");
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        PendingRecords.Enqueue(new LogRecord
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            type = type.ToString(),
            message = condition,
            stackTrace = stackTrace,
            frame = Time.frameCount,
            threadId = Environment.CurrentManagedThreadId
        });
    }

    private static void FlushPendingRecords()
    {
        if (PendingRecords.IsEmpty)
        {
            return;
        }

        string path = GetAbsoluteLogPath();
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        lock (WriteLock)
        {
            using (var writer = new StreamWriter(path, append: true))
            {
                while (PendingRecords.TryDequeue(out var record))
                {
                    writer.WriteLine(JsonUtility.ToJson(record));
                }
            }
        }
    }

    [MenuItem("Tools/Copilot/Console Bridge/Open Log File")]
    private static void OpenLogFile()
    {
        FlushPendingRecords();

        string path = GetAbsoluteLogPath();
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty);
        }

        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Copilot/Console Bridge/Add Session Marker")]
    private static void AddSessionMarker()
    {
        EnqueueBridgeMessage("Session marker");
        FlushPendingRecords();
        Debug.Log("[CopilotConsoleBridge] Session marker written.");
    }

    private static void EnqueueBridgeMessage(string message)
    {
        PendingRecords.Enqueue(new LogRecord
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            type = "Bridge",
            message = message,
            stackTrace = string.Empty,
            frame = Time.frameCount,
            threadId = Environment.CurrentManagedThreadId
        });
    }

    private static string GetAbsoluteLogPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, RelativeLogPath).Replace("\\", "/");
    }

    [Serializable]
    private class LogRecord
    {
        public string timestampUtc;
        public string type;
        public string message;
        public string stackTrace;
        public int frame;
        public int threadId;
    }
}
