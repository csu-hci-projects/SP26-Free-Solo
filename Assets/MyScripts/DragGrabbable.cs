using UnityEngine;

[RequireComponent(typeof(Collider))] 
[RequireComponent(typeof(Rigidbody))]
public class DragGrabbable : MonoBehaviour
{
    public MouseInteractor mouseInteractor;
    public HandInteractor handInteractor;
    public InteractionManager interactionManager; // for mode switching


    public float followSpeed = 20f;   // higher = snappier
    public float surfaceLift = 0.0f;  // extra lift above table
    public float handGrabRadius = 0.35f; // max distance from hand pointer to grab

    Rigidbody _rb; 
    Collider _col;
    bool _grabbed;
    Vector3 _grabOffset; // from pointer to object center, in world space

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth movement
    }

    void Update()
    {
        bool useHand = interactionManager != null && interactionManager.handMode; // check mode

        //check for grab depending on mode
        if (useHand)
            UpdateHandGrab();
        else
            UpdateMouseGrab(); 
    }

    void UpdateMouseGrab()
    {
        if (mouseInteractor == null) return;

        if (!_grabbed && mouseInteractor.GrabDown)
        {
            Ray ray = mouseInteractor.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, mouseInteractor.raycastMask)) // raycast to find what we hit
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject && mouseInteractor.HasHit) // ensure we hit this object
                {
                    _grabbed = true;
                    _grabOffset = transform.position - mouseInteractor.PointerWorld; // calculate offset from pointer to object center
                }
            }
        }

        if(_grabbed && mouseInteractor.GrabUp)
            _grabbed = false;
    }

    void UpdateHandGrab()
    {
        if (handInteractor == null) return;

        if ( !_grabbed && handInteractor.GrabDown && handInteractor.HasHit) // check for grab input and valid hit
        {
            Vector3 pointerFlat = handInteractor.PointerWorld; // flatten to horizontal plane for distance check
            pointerFlat.y = transform.position.y;

            float d = Vector3.Distance(pointerFlat, transform.position); // check distance from hand pointer to object
            if (d <= handGrabRadius)
            {
                _grabbed = true;
                _grabOffset = transform.position - handInteractor.PointerWorld;
            }
        }

        if (_grabbed && handInteractor.GrabUp)
            _grabbed = false;
    }


    void FixedUpdate() // physics update for smooth movement
    {
        if (!_grabbed) return;

        bool useHand = interactionManager != null && interactionManager.handMode;

        Vector3 pointerWorld;
        bool hasHit;

        if (useHand && handInteractor != null)
        {
            pointerWorld = handInteractor.PointerWorld;
            hasHit = handInteractor.HasHit;
        }
        else if (mouseInteractor != null)
        {
            pointerWorld = mouseInteractor.PointerWorld;
            hasHit = mouseInteractor.HasHit;
        }
        else
        {
            return;
        }

        if (!hasHit) return;

        Vector3 target = pointerWorld + _grabOffset;
        float halfHeight = _col.bounds.extents.y;
        target.y = pointerWorld.y + halfHeight + surfaceLift;

        Vector3 next = Vector3.Lerp(_rb.position, target, 1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime));
        _rb.position = next;
    }
}
