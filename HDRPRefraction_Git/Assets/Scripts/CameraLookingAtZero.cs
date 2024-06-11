using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookingAtZero : MonoBehaviour
{
    public Camera camera;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        camera.transform.LookAt(new Vector3(0, 0, 0));
    }
}
