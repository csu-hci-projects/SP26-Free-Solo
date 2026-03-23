using UnityEngine;

public class ProximityColor : MonoBehaviour
{
    public MouseInteractor mouseInteractor;
    public HandInteractor handInteractor;
    public InteractionManager interactionManager;

    [Header("Proximity")]
    public float radius = 0.20f; // distance threshold for "near" color, in world units (adjust based on your scene scale)

    [Header("Colors")]
    public Color farColor = Color.blue;
    public Color nearColor = Color.white;

    Renderer _renderer;
    Material _mat;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mat = _renderer.material; 
        _mat.color = farColor;
    }

    void Update()
    {
        bool useHand = interactionManager != null && interactionManager.handMode; // check if we're in hand mode

        Vector3 pointerWorld = Vector3.zero; // world position of pointer (hand or mouse)
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
            _mat.color = farColor; // if we don't have a valid pointer position, treat it as far
            return;
        }

        Vector3 pointerFlat = pointerWorld; 
        pointerFlat.y = transform.position.y; // ignore vertical distance for proximity check, so we can be "near" even if we're above the object

        float d = Vector3.Distance(pointerFlat, transform.position);    // distance from pointer to object center     
        _mat.color = (d <= radius) ? nearColor : farColor; // set color based on proximity threshold
    }
}