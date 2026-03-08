using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class HandGestureLogger : MonoBehaviour
{
    public HandLandmarkerRunner runner;

    private void OnEnable()
    {
        if (runner != null) runner.OnResult += HandleResult;
    }

    private void OnDisable()
    {
        if (runner != null) runner.OnResult -= HandleResult;
    }

    private void HandleResult(HandLandmarkerResult result)
    {
        // HandLandmarkerResult is a struct, so no "result == null"
        if (result.handLandmarks == null || result.handLandmarks.Count == 0) return;

        // First hand
        var landmarks = result.handLandmarks[0];

        // NormalizedLandmarks is not indexable; use .landmarks
        if (landmarks.landmarks == null || landmarks.landmarks.Count <= 8) return;

        var indexTip = landmarks.landmarks[8]; // index fingertip

        Debug.Log($"IndexTip x={indexTip.x:F3} y={indexTip.y:F3} z={indexTip.z:F3}");
    }
}
