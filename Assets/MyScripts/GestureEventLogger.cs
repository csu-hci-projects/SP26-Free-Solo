using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes a timestamped CSV log of every gesture event that fires during a
/// session. Other detectors call LogEvent() directly, so no polling is needed.
///
/// Output file: Application.persistentDataPath/gesture_log_<timestamp>.csv
/// Columns: SessionTime, WallClock, Gesture, Command, ParticipantID, Condition
/// </summary>
public class GestureEventLogger : MonoBehaviour
{
    [Header("Study Metadata")]
    [Tooltip("Set before each participant session.")]
    public string participantID = "P00";

    [Tooltip("'MouseOnly' or 'GestureAugmented'")]
    public string condition = "GestureAugmented";

    // Runtime state
    StreamWriter _writer;
    float        _sessionStart;
    string       _filePath;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------
    void Awake()
    {
        string filename = $"gesture_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        _filePath = Path.Combine(Application.persistentDataPath, filename);

        _writer = new StreamWriter(_filePath, append: false, encoding: Encoding.UTF8);
        _writer.WriteLine("SessionTimeSec,WallClock,Gesture,Command,ParticipantID,Condition");
        _writer.Flush();

        _sessionStart = Time.time;

        Debug.Log($"[GestureEventLogger] Logging to: {_filePath}");
    }

    void OnDestroy()
    {
        _writer?.Flush();
        _writer?.Close();
    }

    // ---------------------------------------------------------------
    // Public API – call from any detector
    // ---------------------------------------------------------------

    /// <summary>
    /// Log a gesture-to-command mapping event. Thread-safe (uses lock).
    /// </summary>
    public void LogEvent(string gestureName, string command)
    {
        if (_writer == null) return;

        float  sessionSec = Time.time - _sessionStart;
        string wallClock  = DateTime.Now.ToString("HH:mm:ss.fff");

        string line = $"{sessionSec:F3},{wallClock},{gestureName},{command},{participantID},{condition}";

        lock (_writer) // StreamWriter is not thread-safe by default
        {
            _writer.WriteLine(line);
            _writer.Flush();
        }

        Debug.Log($"[GestureEventLogger] {line}");
    }

    /// <summary>
    /// Log any custom event (task start/end markers, mode switches, errors, etc.)
    /// </summary>
    public void LogRawEvent(string gestureName, string command, string participantOverride = null, string conditionOverride = null)
    {
        if (_writer == null) return;

        float  sessionSec = Time.time - _sessionStart;
        string wallClock  = DateTime.Now.ToString("HH:mm:ss.fff");
        string pid        = participantOverride ?? participantID;
        string cond       = conditionOverride   ?? condition;

        string line = $"{sessionSec:F3},{wallClock},{gestureName},{command},{pid},{cond}";

        lock (_writer)
        {
            _writer.WriteLine(line);
            _writer.Flush();
        }

        Debug.Log($"[GestureEventLogger] {line}");
    }

    // Convenience property so other scripts can read the path
    public string FilePath => _filePath;
}
