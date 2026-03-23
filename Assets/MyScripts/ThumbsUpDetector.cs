using UnityEngine;
using TMPro;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class ThumbsUpDetector : MonoBehaviour
{
    [Header("Refs")]
    public HandLandmarkerRunner runner; 
    public TMP_Text messageText;

    [Header("Display")]
    public float messageDuration = 1.5f;
    public float triggerCooldown = 1.5f;

    private readonly object _lock = new object(); 

    private bool _pendingThumbsUp = false;
    private string _pendingMessage = "";

    private float _messageTimer = 0f;
    private float _lastTriggerTime = -999f;

    void OnEnable()
    {
        if (runner != null) runner.OnResult += HandleResult; 
    }

    void OnDisable()
    {
        if (runner != null) runner.OnResult -= HandleResult;
    }

    void Update()
    {
        bool showMessage = false; 
        string msg = "";

        lock (_lock) // check if there's a pending thumbs up message to show
        {
            if (_pendingThumbsUp)
            {
                showMessage = true;
                msg = _pendingMessage;
                _pendingThumbsUp = false;
            }
        }

        if (showMessage && Time.time - _lastTriggerTime >= triggerCooldown) // only trigger if cooldown has passed
        {
            _lastTriggerTime = Time.time;
            _messageTimer = messageDuration;

            if (messageText != null)
                messageText.text = msg;

            Debug.Log(msg);
        }

        if (_messageTimer > 0f)
        {
            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0f && messageText != null)
            {
                messageText.text = "";
            }
        }
    }

    void HandleResult(HandLandmarkerResult result) // callback from MediaPipe thread with hand landmark results
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            return;

        for (int i = 0; i < result.handLandmarks.Count; i++)
        {
            var hand = result.handLandmarks[i];
            if (hand.landmarks == null || hand.landmarks.Count <= 20)
                continue;

            if (IsThumbsUp(hand))
            {
                string handLabel = "Unknown hand";

                
                if (result.handedness != null && result.handedness.Count > i && result.handedness[i].categories.Count > 0) // check if we have handedness info for this hand
                {
                    handLabel = result.handedness[i].categories[0].categoryName; // "Left" or "Right" as labeled by MediaPipe

                    // Flip it
                    handLabel = (handLabel == "Left") ? "Right" : "Left";
                }

                string msg = $"{handLabel} hand thumbs up!";

                lock (_lock)
                {
                    _pendingThumbsUp = true;
                    _pendingMessage = msg;
                }

                return;
            }
        }
    }

    bool IsThumbsUp(NormalizedLandmarks hand) 
    {
        var lm = hand.landmarks;

        Vector3 thumbTip = ToVec3(lm[4]); 
        Vector3 thumbIp  = ToVec3(lm[3]); 

        Vector3 indexTip = ToVec3(lm[8]); 
        Vector3 indexPip = ToVec3(lm[6]);

        Vector3 middleTip = ToVec3(lm[12]);
        Vector3 middlePip = ToVec3(lm[10]);

        Vector3 ringTip = ToVec3(lm[16]);
        Vector3 ringPip = ToVec3(lm[14]);

        Vector3 pinkyTip = ToVec3(lm[20]);
        Vector3 pinkyPip = ToVec3(lm[18]);

        bool thumbExtended = thumbTip.y < thumbIp.y; // thumb extended if tip is above IP joint in normalized coordinates
        bool indexFolded   = indexTip.y > indexPip.y; // index folded if tip is below PIP joint
        bool middleFolded  = middleTip.y > middlePip.y; // middle folded if tip is below PIP joint
        bool ringFolded    = ringTip.y > ringPip.y; // ring folded if tip is below PIP joint
        bool pinkyFolded   = pinkyTip.y > pinkyPip.y; // pinky folded if tip is below PIP joint

        Debug.Log($"thumbTip.y={thumbTip.y:F3}, thumbIp.y={thumbIp.y:F3}");
        Debug.Log($"indexTip.y={indexTip.y:F3}, indexPip.y={indexPip.y:F3}");

        return thumbExtended && indexFolded && middleFolded && ringFolded && pinkyFolded; // thumbs up if thumb is extended and all fingers are folded
    }

    Vector3 ToVec3(NormalizedLandmark lm) // helper to convert MediaPipe normalized landmark to Unity Vector3
    {
        return new Vector3(lm.x, lm.y, lm.z);
    }
}
