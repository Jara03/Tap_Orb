using DefaultNamespace;
using UnityEngine;

public class resetBallPosition : MonoBehaviour
{
    public Level level;

    private void Start()
    {
        //s'abonner au onRestore de Level
        level.onRestore += resetPosition;
    }

    private void resetPosition()
    {
        //mettre le gameobject a la position du 1er ItemToDestroy de Level 
        gameObject.transform.position = level.itemToDestroy[0].transform.position+ new Vector3(0,2f,0);
    }

}
