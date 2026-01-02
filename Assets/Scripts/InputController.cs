using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    // Start is called before the first frame update
    bool isPressed = false;
    private bool previousPressed = false;

    
    public Level levelItem;

    // Update is called once per frame
    void Update()
    {
        if(levelItem.isPaused) return;
        // Détecte si une touche ou un touch est actif
        bool currentPressed = Input.GetKey(KeyCode.Space) || Input.touchCount > 0;
   
        // Vérifie si l’état a changé depuis la dernière frame
        if (currentPressed != previousPressed )
        {
            isPressed = currentPressed;
            updateState();
        }

        // Sauvegarde l’état pour la prochaine frame
        previousPressed = currentPressed;
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
                levelItem.togglesUsedThisRun++;
                ObjectiveManager.Instance?.RegisterToggleUsed();

        }
        else
        {
                levelItem.SetTransformState(false);
        }
    }
}
