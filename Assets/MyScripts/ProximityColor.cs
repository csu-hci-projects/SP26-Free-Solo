//using System.Collections;
//using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;

public class ProximityColor : MonoBehaviour
{
    public MouseInteractor mouseInteractor;
    public HandInteractor handInteractor;
    public InteractionManager interactionManager;

    [Header("Proximity")]
    public float radius = 0.20f;

    [Header("Colors")]
    public Color farColor = Color.blue;
    public Color nearColor = Color.white;

    Renderer _renderer;
    Material _mat;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mat = _renderer.material; // instance
        _mat.color = farColor;
    }

    void Update()
    {
        bool useHand = interactionManager != null && interactionManager.handMode;

        Vector3 pointerWorld = Vector3.zero;
        bool hasHit = false;
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
        if (!hasHit)
        {
            _mat.color = farColor;
            return;
        }

        Vector3 pointerFlat = pointerWorld;
        pointerFlat.y = transform.position.y;

        float d = Vector3.Distance(pointerFlat, transform.position);        
        _mat.color = (d <= radius) ? nearColor : farColor;
    }
}