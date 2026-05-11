using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class ThumbsUpDetector : MonoBehaviour
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

    public static event System.Action OnUndoGesture;

    readonly object _lock = new object();
    bool _pendingThumb = false;

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
        lock (_lock) raw = _pendingThumb;

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
                FireUndo();
                ResetAll();
            }
        }
        else
        {
            ResetAll();
        }
    }

    void ResetAll() { _dwellActive = false; _dwellStart = -1f; }

    void FireUndo()
    {
        Debug.Log("[ThumbsUpDetector] Thumbs Up → Undo");
        gestureLogger?.LogEvent("ThumbsUp", "Undo");
        OnUndoGesture?.Invoke();
    }

    void HandleResult(HandLandmarkerResult result)
    {
        bool found = false;
        if (result.handLandmarks != null)
            foreach (var hand in result.handLandmarks)
                if (hand.landmarks != null && hand.landmarks.Count > 20 && IsThumbsUp(hand))
                { found = true; break; }

        lock (_lock) _pendingThumb = found;
    }

    bool IsThumbsUp(NormalizedLandmarks hand)
    {
        var lm = hand.landmarks;

        // Thumb extended: tip (4) clearly above IP joint (3)
        bool thumbUp   = lm[4].y < lm[3].y - 0.02f;

        // All fingers folded
        bool indexFolded  = lm[8].y  > lm[6].y;
        bool middleFolded = lm[12].y > lm[10].y;
        bool ringFolded   = lm[16].y > lm[14].y;
        bool pinkyFolded  = lm[20].y > lm[18].y;

        // Extra: thumb tip must be above the wrist (landmark 0) to ensure the thumb is pointing up and not sideways
        bool thumbAboveWrist = lm[4].y < lm[0].y;

        return thumbUp && indexFolded && middleFolded && ringFolded && pinkyFolded && thumbAboveWrist;
    }
}
