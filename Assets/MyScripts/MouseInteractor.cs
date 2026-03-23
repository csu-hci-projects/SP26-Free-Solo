using UnityEngine;

public class MouseInteractor : MonoBehaviour
{
    [Header("Refs")]
    public Camera mainCamera;
    public LayerMask raycastMask = ~0; // everything by default

    [Header("Output (read-only)")]
    private Vector3 _pointerWorld; // world position of mouse raycast hit
    private bool _hasHit;
    private bool _grabDown;
    private bool _grabHeld;
    private bool _grabUp;

    // Public read-only properties
    public Vector3 PointerWorld { get { return _pointerWorld; } private set { _pointerWorld = value; } } 
    public bool HasHit { get { return _hasHit; } private set { _hasHit = value; } }
    public bool GrabDown { get { return _grabDown; } private set { _grabDown = value; } }
    public bool GrabHeld { get { return _grabHeld; } private set { _grabHeld = value; } }
    public bool GrabUp { get { return _grabUp; } private set { _grabUp = value; } }

    
    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // Check Mouse button state
        GrabDown = Input.GetMouseButtonDown(0);
        GrabHeld = Input.GetMouseButton(0);
        GrabUp   = Input.GetMouseButtonUp(0);

        // Raycast from mouse to world
        HasHit = false;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, raycastMask, QueryTriggerInteraction.Ignore))
        {
            HasHit = true;
            PointerWorld = hit.point;
        }
    }
}