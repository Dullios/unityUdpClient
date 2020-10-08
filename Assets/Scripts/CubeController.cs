using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    private Vector3 currentPos;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        currentPos = gameObject.transform.position;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentPos.x--;
            gameObject.transform.position = currentPos;
        }
        else if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentPos.x++;
            gameObject.transform.position = currentPos;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentPos.y--;
            gameObject.transform.position = currentPos;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentPos.y++;
            gameObject.transform.position = currentPos;
        }
    }
}
