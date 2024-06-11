using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rudder : MonoBehaviour
{
    public float intensity = 2;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    void FixedUpdate()
    {

        // Rudimentary rudder, assuming zero current velocity:
        Vector3 velocityAtRudder = rb.GetPointVelocity(transform.position);

        Vector3 drag = -transform.right * Vector3.Dot(velocityAtRudder, transform.right) * intensity;

        rb.AddForceAtPosition(drag, transform.position);
    }
}
