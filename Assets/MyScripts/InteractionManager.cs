using UnityEngine;

/// <summary>
/// Manages input mode state.
///
/// MOUSE MODE (handMode = false):
///   Mouse + keyboard only. Hand gestures are inactive.
///
/// HAND MODE (handMode = true):
///   Mouse and keyboard remain fully available (click-drag still works,
///   keyboard shortcuts still work). Hand gestures are ADDED on top.
///   Tab toggles between the two.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public MouseInteractor mouseInteractor;
    public HandInteractor  handInteractor;

    [Header("Mode")]
    public bool handMode = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            handMode = !handMode;
            string modeLabel = handMode ? "Hand Mode (gestures + mouse/keyboard)" : "Mouse Mode";
            Debug.Log($"[InteractionManager] Switched to: {modeLabel}");
            HUDController.Instance?.ShowGesture(handMode
                ? "✋ Hand Mode ON — gestures, mouse & keyboard active"
                : "🖱 Mouse Mode — mouse & keyboard only");
        }
    }
}
