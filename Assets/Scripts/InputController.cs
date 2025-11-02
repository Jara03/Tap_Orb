using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class InputController : MonoBehaviour
{
    // Start is called before the first frame update
    bool isPressed = false;
    
    public Level levelItem;

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.Space) || Input.touchCount > 0)
        {
            isPressed = true;
           // Debug.Log("Pressed");
        }
        else
        {
            isPressed = false;
           // Debug.Log("Released");
        }

        updateState();

    }

    public bool IsPressed()
    {
        return isPressed;
    }
    
    void updateState()
    {
        
       // Debug.Log("is Pressed : " + isPressed);
        
        if (isPressed)
        {
            levelItem.SetTransformState(true);
        }
        else
        {
            levelItem.SetTransformState(false);
        }
    }
}
