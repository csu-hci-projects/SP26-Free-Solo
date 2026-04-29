using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

/// <summary>
/// Detects Index Point Up (only index finger extended, others curled, thumb tucked).
/// Uses a vote buffer + dwell timer for reliability.
/// </summary>
public class IndexPointDetector : MonoBehaviour
{
    [Header("Refs")]
    public HandLandmarkerRunner runner;
    public InteractionManager   interactionManager;
    public GestureEventLogger   gestureLogger;

    [Header("Reliability")]
    public int voteBufferSize = 6;
    public int voteThreshold  = 4;

    [Header("Timing")]
    public float dwellTime = 0.5f;
    public float cooldown  = 1.5f;

    public static event System.Action OnRotateGesture;

    readonly object _lock = new object();
    bool _pendingPoint = false;

    bool[] _voteBuffer;
    int    _voteIndex = 0;
    int    _voteSum   = 0;

    float _dwellStart  = -1f;
    bool  _dwellActive = false;
    float _lastTrigger = -999f;

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
            ResetAll(); return;
        }

        bool raw;
        lock (_lock) raw = _pendingPoint;

        _voteSum -= _voteBuffer[_voteIndex] ? 1 : 0;
        _voteBuffer[_voteIndex] = raw;
        _voteSum += raw ? 1 : 0;
        _voteIndex = (_voteIndex + 1) % voteBufferSize;

        bool confident = _voteSum >= voteThreshold;

        if (confident)
        {
            if (!_dwellActive) { _dwellActive = true; _dwellStart = Time.time; }

            if (Time.time - _dwellStart  >= dwellTime &&
                Time.time - _lastTrigger >= cooldown)
            {
                _lastTrigger = Time.time;
                FireRotate();
                ResetAll();
            }
        }
        else
        {
            ResetAll();
        }
    }

    void ResetAll() { _dwellActive = false; _dwellStart = -1f; }

    void FireRotate()
    {
        Debug.Log("[IndexPointDetector] Index Point → Rotate");
        gestureLogger?.LogEvent("IndexPoint", "Rotate");
        OnRotateGesture?.Invoke();
    }

    void HandleResult(HandLandmarkerResult result)
    {
        bool found = false;
        if (result.handLandmarks != null)
            foreach (var hand in result.handLandmarks)
                if (hand.landmarks != null && hand.landmarks.Count > 20 && IsIndexPoint(hand))
                { found = true; break; }

        lock (_lock) _pendingPoint = found;
    }

    // ---------------------------------------------------------------
    // Geometry
    // ---------------------------------------------------------------
    bool IsIndexPoint(NormalizedLandmarks hand)
    {
        var lm = hand.landmarks;

        // Index extended: tip (8) clearly above PIP (6)
        // Use a margin so a slightly bent index doesn't count
        bool indexExtended = lm[8].y < lm[6].y - 0.03f;

        // Other fingers folded
        bool middleFolded = lm[12].y > lm[10].y;
        bool ringFolded   = lm[16].y > lm[14].y;
        bool pinkyFolded  = lm[20].y > lm[18].y;

        // Thumb tucked — tip near index MCP
        float thumbToIndexMcp = Dist2D(lm[4], lm[5]);
        bool thumbTucked = thumbToIndexMcp < 0.15f;

        return indexExtended && middleFolded && ringFolded && pinkyFolded && thumbTucked;
    }

    float Dist2D(NormalizedLandmark a, NormalizedLandmark b)
    {
        float dx = a.x - b.x, dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
