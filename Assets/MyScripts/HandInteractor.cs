using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class HandInteractor : MonoBehaviour
{
    [Header("Refs")]
    public HandLandmarkerRunner runner;
    public Camera mainCamera;
    public LayerMask raycastMask = ~0;

    [Header("Hand Mapping")]
    public float pinchThreshold = 0.05f;
    public bool useFirstDetectedHand = true;

    public Vector3 PointerWorld { get; private set; }
    public bool HasHit { get; private set; }
    public bool GrabDown { get; private set; }
    public bool GrabHeld { get; private set; }
    public bool GrabUp { get; private set; }

    bool _prevPinch;

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
            runner.OnResult += HandleResult;
    }

    void OnDisable()
    {
        if (runner != null)
            runner.OnResult -= HandleResult;
    }

    void Update()
    {
        // one-frame flags reset at start of frame
        GrabDown = false;
        GrabUp = false;

        bool handVisible;
        Vector2 indexTipNorm;
        bool pinching;
        bool hasFreshData;

        lock (_dataLock)
        {
            hasFreshData = _hasPendingHandData;
            handVisible = _handVisible;
            indexTipNorm = _indexTipNorm;
            pinching = _pendingPinch;
            _hasPendingHandData = false;
        }

        if (!hasFreshData)
            return;

        if (!handVisible)
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

        float screenX = indexTipNorm.x * Screen.width;
        float screenY = (1f - indexTipNorm.y) * Screen.height;

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenX, screenY, 0f));

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

    void HandleResult(HandLandmarkerResult result)
    {
        bool handVisible = false;
        Vector2 indexTipNorm = Vector2.zero;
        bool pinching = false;

        if (result.handLandmarks != null && result.handLandmarks.Count > 0)
        {
            var landmarks = result.handLandmarks[0];

            if (landmarks.landmarks != null && landmarks.landmarks.Count > 8)
            {
                var indexTip = landmarks.landmarks[8];
                var thumbTip = landmarks.landmarks[4];

                handVisible = true;
                indexTipNorm = new Vector2(indexTip.x, indexTip.y);

                float pinchDist = Vector2.Distance(
                    new Vector2(indexTip.x, indexTip.y),
                    new Vector2(thumbTip.x, thumbTip.y)
                );

                pinching = pinchDist < pinchThreshold;
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

    void UpdatePinchState(bool pinching)
    {
        GrabDown = pinching && !_prevPinch;
        GrabUp = !pinching && _prevPinch;
        GrabHeld = pinching;
        _prevPinch = pinching;
    }
}
