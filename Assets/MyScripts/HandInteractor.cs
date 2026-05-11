using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class HandInteractor : MonoBehaviour
{
    [Header("Refs")]
    public HandLandmarkerRunner runner; // reference to the MediaPipe hand landmark runner
    public Camera mainCamera;
    public LayerMask raycastMask = ~0; // layers to raycast against, default to everything

    [Header("Hand Mapping")]
    public float pinchThreshold = 0.05f;
    public bool useFirstDetectedHand = true;

    public Vector3 PointerWorld { get; private set; }
    public bool HasHit { get; private set; }
    public bool GrabDown { get; private set; }
    public bool GrabHeld { get; private set; }
    public bool GrabUp { get; private set; }

    bool _prevPinch; // to track pinch state across frames

    // Data copied from MediaPipe callback thread
    readonly object _dataLock = new object();
    bool _hasPendingHandData = false;
    bool _handVisible = false;
    Vector2 _indexTipNorm;
    bool _pendingPinch;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (runner != null)
            runner.OnResult += HandleResult; // subscribe to hand landmark results
    }

    void OnDisable()
    {
        if (runner != null)
            runner.OnResult -= HandleResult; // unsubscribe to avoid memory leaks
    }

    void Update()
    {
    
        GrabDown = false;
        GrabUp = false;

        bool handVisible;
        Vector2 indexTipNorm;
        bool pinching;
        bool hasFreshData;

        lock (_dataLock) 
        {
            hasFreshData = _hasPendingHandData; // indicates if we got new hand data since last frame
            handVisible = _handVisible;
            indexTipNorm = _indexTipNorm;
            pinching = _pendingPinch;
            _hasPendingHandData = false;
        }

        if (!hasFreshData)
            return;

        if (!handVisible) // if hand not visible rest
        {
            HasHit = false;
            UpdatePinchState(false);
            return;
        }

        if (mainCamera == null) 
        {
            HasHit = false;
            UpdatePinchState(false);
            return;
        }

        float screenX = indexTipNorm.x * Screen.width; // convert normalized coordinates to screen space
        float screenY = (1f - indexTipNorm.y) * Screen.height; 

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenX, screenY, 0f)); // raycast into the world from the index fingertip position

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, raycastMask, QueryTriggerInteraction.Ignore)) 
        {
            HasHit = true;
            PointerWorld = hit.point;
        }
        else
        {
            HasHit = false;
        }

        UpdatePinchState(pinching);
        Debug.Log($"Hand visible: {handVisible}, HasHit: {HasHit}, GrabHeld: {GrabHeld}");
    }

    void HandleResult(HandLandmarkerResult result) // callback from MediaPipe thread with hand landmark results
    {
        bool handVisible = false;
        Vector2 indexTipNorm = Vector2.zero;
        bool pinching = false;

        if (result.handLandmarks != null && result.handLandmarks.Count > 0)
        {
            var landmarks = result.handLandmarks[0]; //use first detected hand

            if (landmarks.landmarks != null && landmarks.landmarks.Count > 8) 
            {
                var indexTip = landmarks.landmarks[8]; // index fingertip is landmark 8 in MediaPipe hand model
                var thumbTip = landmarks.landmarks[4]; // thumb tip is landmark 4

                handVisible = true;
                indexTipNorm = new Vector2(indexTip.x, indexTip.y); 

                float pinchDist = Vector2.Distance( // distance between index fingertip and thumb tip in normalized screen space
                    new Vector2(indexTip.x, indexTip.y),
                    new Vector2(thumbTip.x, thumbTip.y)
                );

                pinching = pinchDist < pinchThreshold; // consider it a pinch if distance is below threshold
            }
        }

        lock (_dataLock)
        {
            _handVisible = handVisible;
            _indexTipNorm = indexTipNorm;
            _pendingPinch = pinching;
            _hasPendingHandData = true;
        }
    }

    void UpdatePinchState(bool pinching) // update grab state based on pinch input, with one-frame GrabDown and GrabUp events
    {
        GrabDown = pinching && !_prevPinch;
        GrabUp = !pinching && _prevPinch;
        GrabHeld = pinching;
        _prevPinch = pinching;
    }
}
