using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("Rotation")]
    [Tooltip("Degrees per rotate trigger.")]
    public float rotateStep = 45f;
    public Vector3 rotateAxis = Vector3.up;

    [Header("Selection Highlight")]
    [Tooltip("Color applied to the selected object's material.")]
    public Color selectedColor  = Color.yellow;
    [Tooltip("Emission intensity of the highlight (0 = no glow).")]
    public float emissionIntensity = 0.4f;

    [Header("Refs")]
    public GestureEventLogger gestureLogger;
    public InteractionManager interactionManager;
    public MouseInteractor    mouseInteractor; // used to detect click-on-empty deselect

    DragGrabbable _selected;
    Color         _originalColor;
    bool          _hadEmission;
    Material      _selectedMat; // cached so we can restore it

    readonly Stack<UndoRecord> _undoStack = new Stack<UndoRecord>();

    struct UndoRecord
    {
        public GameObject obj;
        public Vector3    position;
        public Quaternion rotation;
        public bool       wasDeleted;
    }

    // ---------------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        ClosedFistDetector.OnDeleteGesture += HandleDelete;
        IndexPointDetector.OnRotateGesture += HandleRotate;
        ThumbsUpDetector.OnUndoGesture     += HandleUndo;
    }

    void OnDisable()
    {
        ClosedFistDetector.OnDeleteGesture -= HandleDelete;
        IndexPointDetector.OnRotateGesture -= HandleRotate;
        ThumbsUpDetector.OnUndoGesture     -= HandleUndo;
    }

    void Update()
    {
        HandleKeyboard();
        HandleMouseDeselect();
    }

    void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
        {
            gestureLogger?.LogEvent("Keyboard", "Delete");
            HandleDelete();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            gestureLogger?.LogEvent("Keyboard", "Rotate");
            HandleRotate();
        }
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrl && Input.GetKeyDown(KeyCode.Z))
        {
            gestureLogger?.LogEvent("Keyboard", "Undo");
            HandleUndo();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelection();
    }

    /// <summary>
    /// If the player clicks and the raycast misses all objects, clear selection.
    /// This gives mouse-mode users a natural way to deselect.
    /// </summary>
    void HandleMouseDeselect()
    {
        if (mouseInteractor == null) return;
        if (mouseInteractor.GrabDown && !mouseInteractor.HasHit)
            ClearSelection();
    }

    // ---------------------------------------------------------------
    // Selection registration — called by DragGrabbable
    // ---------------------------------------------------------------
    public void Select(DragGrabbable obj)
    {
        if (_selected == obj) return; // already selected

        ClearSelection();             // remove highlight from previous

        _selected = obj;

        // Apply highlight
        var rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            _selectedMat   = rend.material; // instance material
            _originalColor = _selectedMat.color;
            _hadEmission   = _selectedMat.IsKeywordEnabled("_EMISSION");

            _selectedMat.color = selectedColor;
            _selectedMat.EnableKeyword("_EMISSION");
            _selectedMat.SetColor("_EmissionColor", selectedColor * emissionIntensity);
        }

        Debug.Log($"[SelectionManager] Selected: {obj.gameObject.name}");
        HUDController.Instance?.ShowGesture($"Selected: {obj.gameObject.name}");
    }

    /// <summary>
    /// Called by DragGrabbable on release — selection is intentionally KEPT.
    /// The object stays selected so gesture commands can act on it.
    /// </summary>
    public void NotifyReleased(DragGrabbable obj)
    {
        // Selection persists — nothing to do here.
        // Kept as a hook in case you need release-specific logic later.
    }

    public void ClearSelection()
    {
        if (_selected == null) return;

        // Restore original material
        if (_selectedMat != null)
        {
            _selectedMat.color = _originalColor;
            if (!_hadEmission) _selectedMat.DisableKeyword("_EMISSION");
            _selectedMat.SetColor("_EmissionColor", Color.black);
        }

        _selected    = null;
        _selectedMat = null;
    }

    /// <summary>
    /// Called by DragGrabbable just before it begins moving so we can record
    /// a pre-move snapshot for undo.
    /// </summary>
    public void RecordPreMove(DragGrabbable obj)
    {
        _undoStack.Push(new UndoRecord
        {
            obj        = obj.gameObject,
            position   = obj.transform.position,
            rotation   = obj.transform.rotation,
            wasDeleted = false
        });
    }

    public DragGrabbable CurrentSelection => _selected;

    // ---------------------------------------------------------------
    // Commands — called by both gesture detectors and keyboard
    // ---------------------------------------------------------------
    public void HandleDelete()
    {
        if (_selected == null)
        {
            Debug.Log("[SelectionManager] Delete — nothing selected.");
            HUDController.Instance?.ShowGesture("Pinch an object first, then fist to delete");
            return;
        }

        _undoStack.Push(new UndoRecord
        {
            obj        = _selected.gameObject,
            position   = _selected.transform.position,
            rotation   = _selected.transform.rotation,
            wasDeleted = true
        });

        Debug.Log($"[SelectionManager] Deleted: {_selected.gameObject.name}");
        var toHide = _selected.gameObject;
        ClearSelection(); // removes highlight before hiding
        toHide.SetActive(false);

        HUDController.Instance?.ShowGesture("✊ Fist  →  Deleted");
    }

    public void HandleRotate()
    {
        if (_selected == null)
        {
            Debug.Log("[SelectionManager] Rotate — nothing selected.");
            HUDController.Instance?.ShowGesture("Pinch an object first, then point to rotate");
            return;
        }

        _undoStack.Push(new UndoRecord
        {
            obj        = _selected.gameObject,
            position   = _selected.transform.position,
            rotation   = _selected.transform.rotation,
            wasDeleted = false
        });

        _selected.transform.Rotate(rotateAxis, rotateStep, Space.World);
        Debug.Log($"[SelectionManager] Rotated: {_selected.gameObject.name}");

        HUDController.Instance?.ShowGesture("☝ Point  →  Rotated");
    }

    public void HandleUndo()
    {
        if (_undoStack.Count == 0)
        {
            Debug.Log("[SelectionManager] Undo — stack empty.");
            HUDController.Instance?.ShowGesture("👍 Thumbs Up  →  Nothing to undo");
            return;
        }

        UndoRecord r = _undoStack.Pop();
        if (r.obj == null) { Debug.Log("[SelectionManager] Undo — object gone."); return; }

        r.obj.SetActive(true);
        r.obj.transform.position = r.position;
        r.obj.transform.rotation = r.rotation;
        Debug.Log($"[SelectionManager] Undone: {r.obj.name}");

        HUDController.Instance?.ShowGesture("👍 Thumbs Up  →  Undone");
    }
}
