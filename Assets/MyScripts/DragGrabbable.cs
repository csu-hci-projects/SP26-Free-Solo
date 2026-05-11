using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DragGrabbable : MonoBehaviour
{
    public MouseInteractor    mouseInteractor;
    public HandInteractor     handInteractor;
    public InteractionManager interactionManager;

    public float followSpeed    = 20f;
    public float handGrabRadius = 0.35f;

    [Header("Floor Constraint")]
    [Tooltip("World Y of the floor surface. Match this to your Floor object's top surface Y.")]
    public float floorY = 0f;

    [Tooltip("Small gap so the object doesn't clip the floor while sliding.")]
    public float surfaceLift = 0.02f;

    Rigidbody _rb;
    Collider  _col;
    bool      _grabbed;
    bool      _grabbedByHand;
    Vector2   _grabOffsetXZ;
    bool      _recordedThisGrab;

    // Resolved each frame separately for mouse and hand
    Vector3 _mouseFloorPoint;
    bool    _hasMouseFloorPoint;

    Vector3 _handFloorPoint;
    bool    _hasHandFloorPoint;

    void Awake()
    {
        _rb  = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints   = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // --- Resolve mouse floor point (always, via math plane raycast) ---
        _hasMouseFloorPoint = false;
        if (mouseInteractor != null && mouseInteractor.mainCamera != null)
        {
            Ray ray = mouseInteractor.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (RaycastFloorPlane(ray, out Vector3 hit))
            {
                _mouseFloorPoint    = hit;
                _hasMouseFloorPoint = true;
            }
        }

        // --- Resolve hand floor point (always when hand has a hit) ---
        _hasHandFloorPoint = false;
        if (handInteractor != null && handInteractor.HasHit)
        {
            _handFloorPoint    = ProjectToFloor(handInteractor.PointerWorld);
            _hasHandFloorPoint = true;
        }

        // --- Process input ---
        UpdateMouseGrab();

        if (interactionManager != null && interactionManager.handMode)
            UpdateHandGrab();
    }

    void UpdateMouseGrab()
    {
        if (mouseInteractor == null) return;
        if (_grabbedByHand) return; // hand owns this grab

        if (!_grabbed && mouseInteractor.GrabDown && _hasMouseFloorPoint)
        {
            Ray ray = mouseInteractor.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, mouseInteractor.raycastMask))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                    BeginGrab(byHand: false);
            }
        }

        if (_grabbed && !_grabbedByHand && mouseInteractor.GrabUp)
            EndGrab();
    }

    void UpdateHandGrab()
    {
        if (handInteractor == null) return;

        if (!_grabbed && handInteractor.GrabDown && _hasHandFloorPoint)
        {
            // Check proximity on the floor plane — ignore Y difference
            Vector3 toObject = transform.position - _handFloorPoint;
            toObject.y = 0f;
            if (toObject.magnitude <= handGrabRadius)
                BeginGrab(byHand: true);
        }

        if (_grabbed && _grabbedByHand && handInteractor.GrabUp)
            EndGrab();
    }

    void BeginGrab(bool byHand)
    {
        _grabbed          = true;
        _grabbedByHand    = byHand;
        _recordedThisGrab = false;

        Vector3 floorPoint = byHand ? _handFloorPoint : _mouseFloorPoint;
        _grabOffsetXZ = new Vector2(
            transform.position.x - floorPoint.x,
            transform.position.z - floorPoint.z);

        SelectionManager.Instance?.Select(this);
    }

    void EndGrab()
    {
        _grabbed       = false;
        _grabbedByHand = false;
        SelectionManager.Instance?.NotifyReleased(this);
    }


    void FixedUpdate()
    {
        if (!_grabbed) return;

        Vector3 floorPoint;
        bool    hasPoint;

        if (_grabbedByHand)
        {
            floorPoint = _handFloorPoint;
            hasPoint   = _hasHandFloorPoint;
        }
        else
        {
            floorPoint = _mouseFloorPoint;
            hasPoint   = _hasMouseFloorPoint;
        }

        if (!hasPoint) return;

        if (!_recordedThisGrab)
        {
            SelectionManager.Instance?.RecordPreMove(this);
            _recordedThisGrab = true;
        }

        float halfH  = _col.bounds.extents.y;
        float targetY = floorY + halfH + surfaceLift;

        Vector3 target = new Vector3(
            floorPoint.x + _grabOffsetXZ.x,
            targetY,
            floorPoint.z + _grabOffsetXZ.y);

        Vector3 next = Vector3.Lerp(_rb.position, target,
                           1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime));
        _rb.MovePosition(next);
    }


    bool RaycastFloorPlane(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        float denom = ray.direction.y;
        if (Mathf.Abs(denom) < 0.0001f) return false;
        float t = (floorY - ray.origin.y) / denom;
        if (t < 0) return false;
        hitPoint = ray.origin + ray.direction * t;
        return true;
    }

    Vector3 ProjectToFloor(Vector3 worldPoint)
    {
        return new Vector3(worldPoint.x, floorY, worldPoint.z);
    }
}
