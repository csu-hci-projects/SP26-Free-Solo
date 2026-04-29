using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives the structured task sequence for the user study.
///
/// Tasks (from the paper §3.3):
///   1. Select a specified object
///   2. Move it to a target location
///   3. Rotate it
///   4. Undo an action
///   5. Delete an object
///
/// The sequencer:
///  - displays the current task prompt via HUDController
///  - logs task-start and task-end timestamps via GestureEventLogger
///  - waits for the participant (or experimenter) to press Space to advance
///
/// To integrate auto-detection of task completion, replace the
/// WaitForSpaceBar coroutine with your own completion logic
/// (e.g., check object position against a target zone).
/// </summary>
public class TaskSequencer : MonoBehaviour
{
    [Header("Refs")]
    public HUDController hud;
    public GestureEventLogger logger;
    public InteractionManager interactionManager;

    [Header("Study Config")]
    [Tooltip("Participant ID forwarded to the logger.")]
    public string participantID = "P00";
    public bool gestureCondition = true; // false = MouseOnly

    // Task definitions
    static readonly string[] Tasks =
    {
        "Task 1 / 5 – Select the RED cube",
        "Task 2 / 5 – Move the RED cube to the target circle",
        "Task 3 / 5 – Rotate the RED cube (Index Point gesture or R key)",
        "Task 4 / 5 – Undo your last action (Thumbs Up gesture or Ctrl+Z)",
        "Task 5 / 5 – Delete the GREEN sphere (Closed Fist gesture or Delete key)"
    };

    void Start()
    {
        // Sync participant ID to the logger
        if (logger != null)
        {
            logger.participantID = participantID;
            logger.condition     = gestureCondition ? "GestureAugmented" : "MouseOnly";
        }

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // Brief intro pause
        hud?.SetTaskPrompt("Get ready… press SPACE to begin.");
        yield return WaitForSpaceBar();

        for (int i = 0; i < Tasks.Length; i++)
        {
            string task = Tasks[i];
            string taskID = $"Task{i + 1}";

            hud?.SetTaskPrompt(task);
            logger?.LogRawEvent("TaskStart", taskID);

            float startTime = Time.time;

            // Wait until experimenter / participant presses Space to signal completion
            yield return WaitForSpaceBar();

            float elapsed = Time.time - startTime;
            logger?.LogRawEvent("TaskEnd", $"{taskID}_elapsed_{elapsed:F2}s");
        }

        hud?.SetTaskPrompt("All tasks complete! Thank you.");
        logger?.LogRawEvent("SessionEnd", "AllTasksDone");
        Debug.Log("[TaskSequencer] Session complete.");
    }

    IEnumerator WaitForSpaceBar()
    {
        // Consume any lingering Space press from before this yield
        yield return null;
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;
    }
}
