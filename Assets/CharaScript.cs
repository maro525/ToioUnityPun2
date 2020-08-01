using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class CharaScript : MonoBehaviour
{
    GameObject gameObject;

    void Start()
    {
        gameObject = GameObject.Find("scene");
        Cube cube = gameObject.GetComponent<SimplePun>().cube;
        Debug.Log(cube);
        gameObject.GetComponent<SimplePun>().ReturnAccess();
    }

    void Update()
    {
        if(Application.isEditor) {

            if (Input.GetKey(KeyCode.Return)){
                gameObject.GetComponent<SimplePun>().moveToio();
            }
            if (Input.GetKey("up"))
            {
                transform.position += transform.forward * 0.05f;
            }
            if (Input.GetKey("down"))
            {
                transform.position -= transform.forward * 0.05f;
            }
            if (Input.GetKey("right"))
            {
                transform.Rotate(0, 100 * Time.deltaTime, 0) ;
            }
            if (Input.GetKey("left"))
            {
                transform.Rotate(0, -100 * Time.deltaTime, 0);
            }

        }
        else {

            if (Input.touchCount > 0) {
                
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended) {

                }
            }
        }
    }
}