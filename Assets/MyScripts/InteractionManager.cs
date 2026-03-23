using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public MouseInteractor mouseInteractor;
    public HandInteractor handInteractor;

    [Header("Mode")]
    public bool handMode = false; // Starts in mouse mode

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))  
        {
            handMode = !handMode;
            Debug.Log(handMode ? "Hand Mode" : "Mouse Mode"); // Log mode switch
        }
    }
}
