using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public MouseInteractor mouseInteractor;
    public HandInteractor handInteractor;

    [Header("Mode")]
    public bool handMode = false; // later

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            handMode = !handMode;
            Debug.Log(handMode ? "Hand Mode" : "Mouse Mode");
        }

        // Later: switch which interactor objects read from.
        // For now you're always using mouseInteractor.
    }
}
