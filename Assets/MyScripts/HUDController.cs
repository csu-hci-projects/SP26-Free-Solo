using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("TMP Text References")]
    public TMP_Text modeLabel;
    public TMP_Text gestureLabel;
    public TMP_Text taskPromptLabel;

    [Header("Settings")]
    [Tooltip("Seconds before the gesture label auto-clears.")]
    public float gestureLabelDuration = 2.5f;

    [Header("Refs")]
    public InteractionManager interactionManager;

    float _gestureLabelTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Update mode label every frame
        if (modeLabel != null && interactionManager != null)
        {
            modeLabel.text = interactionManager.handMode
                ? "Hand Mode  (Tab to switch)"
                : "Mouse Mode  (Tab to switch)";
        }

        // Auto-clear gesture label
        if (_gestureLabelTimer > 0f)
        {
            _gestureLabelTimer -= Time.deltaTime;
            if (_gestureLabelTimer <= 0f && gestureLabel != null)
                gestureLabel.text = "";
        }
    }


    public void ShowGesture(string text)
    {
        if (gestureLabel == null) return;
        gestureLabel.text = text;
        _gestureLabelTimer = gestureLabelDuration;
    }

    public void SetTaskPrompt(string prompt)
    {
        if (taskPromptLabel != null) taskPromptLabel.text = prompt;
    }

    public void ClearTaskPrompt()
    {
        if (taskPromptLabel != null) taskPromptLabel.text = "";
    }
}
