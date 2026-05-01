using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class ClosedFistDetector : MonoBehaviour
{
    [Header("Refs")]
    public HandLandmarkerRunner runner;
    public InteractionManager   interactionManager;
    public GestureEventLogger   gestureLogger;

    [Header("Reliability")]
    [Tooltip("Frame buffer size for vote smoothing.")]
    public int voteBufferSize = 6;
    [Tooltip("How many of the last N frames must show the gesture before dwell starts.")]
    public int voteThreshold  = 4;

    [Header("Timing")]
    public float dwellTime = 0.5f;
    public float cooldown  = 1.5f;

    public static event System.Action OnDeleteGesture;

    // Thread-safe
    readonly object _lock    = new object();
    bool _pendingFist        = false;

    // Main-thread vote buffer
    bool[] _voteBuffer;
    int    _voteIndex  = 0;
    int    _voteSum    = 0;

    float _fistDwellStart = -1f;
    bool  _dwellActive    = false;
    float _lastTrigger    = -999f;

    // ---------------------------------------------------------------
    void Awake()
    {
        _voteBuffer = new bool[voteBufferSize];
    }

    void OnEnable()  { if (runner != null) runner.OnResult += HandleResult; }
    void OnDisable() { if (runner != null) runner.OnResult -= HandleResult; }

    void Update()
    {
        if (interactionManager != null && !interactionManager.handMode)
        {
            ResetAll();
            return;
        }

        // Pull latest raw reading from background thread
        bool raw;
        lock (_lock) raw = _pendingFist;

        // Update circular vote buffer
        _voteSum -= _voteBuffer[_voteIndex] ? 1 : 0;
        _voteBuffer[_voteIndex] = raw;
        _voteSum += raw ? 1 : 0;
        _voteIndex = (_voteIndex + 1) % voteBufferSize;

        bool gestureConfident = _voteSum >= voteThreshold;

        if (gestureConfident)
        {
            if (!_dwellActive)
            {
                _dwellActive    = true;
                _fistDwellStart = Time.time;
            }

            if (Time.time - _fistDwellStart >= dwellTime &&
                Time.time - _lastTrigger    >= cooldown)
            {
                _lastTrigger = Time.time;
                FireDelete();
                ResetAll();
            }
        }
        else
        {
            _dwellActive    = false;
            _fistDwellStart = -1f;
        }
    }

    void ResetAll()
    {
        _dwellActive    = false;
        _fistDwellStart = -1f;
    }

    void FireDelete()
    {
        Debug.Log("[ClosedFistDetector] Fist → Delete");
        gestureLogger?.LogEvent("ClosedFist", "Delete");
        OnDeleteGesture?.Invoke();
    }

    // Background thread
    void HandleResult(HandLandmarkerResult result)
    {
        bool found = false;
        if (result.handLandmarks != null)
            foreach (var hand in result.handLandmarks)
                if (hand.landmarks != null && hand.landmarks.Count > 20 && IsClosedFist(hand))
                { found = true; break; }

        lock (_lock) _pendingFist = found;
    }

    // ---------------------------------------------------------------
    // Geometry
    // ---------------------------------------------------------------
    bool IsClosedFist(NormalizedLandmarks hand)
    {
        var lm = hand.landmarks;

        // Four fingers: tip must be below (higher y value) its PIP joint
        bool indexFolded  = lm[8].y  > lm[6].y;
        bool middleFolded = lm[12].y > lm[10].y;
        bool ringFolded   = lm[16].y > lm[14].y;
        bool pinkyFolded  = lm[20].y > lm[18].y;

        // Thumb: tip should be close to the index MCP (landmark 5) — indicates tucked
        // Use 2D distance in normalized space
        float thumbToIndexMcp = Dist2D(lm[4], lm[5]);
        bool thumbTucked = thumbToIndexMcp < 0.12f; // tune if needed

        return indexFolded && middleFolded && ringFolded && pinkyFolded && thumbTucked;
    }

    float Dist2D(Mediapipe.Tasks.Components.Containers.NormalizedLandmark a,
                 Mediapipe.Tasks.Components.Containers.NormalizedLandmark b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
