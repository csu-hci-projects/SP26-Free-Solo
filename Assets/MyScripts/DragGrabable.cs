using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DragGrabbableRB : MonoBehaviour
{
    public MouseInteractor interactor;

    public float followSpeed = 20f;   // higher = snappier
    public float surfaceLift = 0.0f;  // extra lift above table

    Rigidbody _rb;
    Collider _col;
    bool _grabbed;
    Vector3 _grabOffset;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (interactor == null) return;

        // Begin grab only when clicking this object
        if (!_grabbed && interactor.GrabDown)
        {
            Ray ray = interactor.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactor.raycastMask))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    if (interactor.HasHit)
                    {
                        _grabbed = true;

                        // Offset from pointer hit to object position so it doesn't snap
                        _grabOffset = transform.position - interactor.PointerWorld;
                    }
                }
            }
        }

        if (_grabbed && interactor.GrabUp)
            _grabbed = false;
    }

    void FixedUpdate()
    {
        if (!_grabbed || interactor == null || !interactor.HasHit) return;

        // target point from pointer + offset
        Vector3 target = interactor.PointerWorld + _grabOffset;

        // keep bottom of object above surface
        float halfHeight = _col.bounds.extents.y;
        target.y = interactor.PointerWorld.y + halfHeight + surfaceLift;

        // Smooth motion
        Vector3 next = Vector3.Lerp(_rb.position, target, 1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime));
        _rb.MovePosition(next);
    }
}
