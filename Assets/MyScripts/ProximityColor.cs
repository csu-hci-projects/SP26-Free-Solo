using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProximityColor : MonoBehaviour
{
    public MouseInteractor interactor;

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
        if (interactor == null || !interactor.HasHit)
        {
            _mat.color = farColor;
            return;
        }

        Vector3 pointerFlat = interactor.PointerWorld;
        pointerFlat.y = transform.position.y;

        float d = Vector3.Distance(pointerFlat, transform.position);        
        _mat.color = (d <= radius) ? nearColor : farColor;
    }
}