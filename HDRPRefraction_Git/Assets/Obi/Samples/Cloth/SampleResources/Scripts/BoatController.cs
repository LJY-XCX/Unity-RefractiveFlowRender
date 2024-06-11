using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class BoatController : MonoBehaviour
{
    public Transform rudder;
    public Transform mast;

    public float rudderRotationSpeed = 70;
    public float mastRotationSpeed = 5;

    // Update is called once per frame
    void Update()
    {
        if (rudder != null)
        {
            Vector3 currentRotation = rudder.localRotation.eulerAngles;

            if (Input.GetKey(KeyCode.A))
            {
                currentRotation.y += rudderRotationSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.D))
            {
                currentRotation.y -= rudderRotationSpeed * Time.deltaTime;
            }

            currentRotation.y = Mathf.Clamp(currentRotation.y, 45, 135);

            rudder.localRotation = Quaternion.Euler(currentRotation);
        }

        if (mast != null)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                mast.Rotate(Vector3.up, mastRotationSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                mast.Rotate(Vector3.up,-mastRotationSpeed * Time.deltaTime);
            }
        }
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        rudder.localRotation = Quaternion.AngleAxis(90, Vector3.up);
        mast.localRotation = Quaternion.AngleAxis(45, Vector3.up);
        GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezePositionY;

        // Reload cloth blueprint, to undo any tearing:
        var cloth = GetComponentInChildren<ObiTearableCloth>();
        cloth.RemoveFromSolver();
        cloth.ClearState();
        cloth.AddToSolver();
    }
}
