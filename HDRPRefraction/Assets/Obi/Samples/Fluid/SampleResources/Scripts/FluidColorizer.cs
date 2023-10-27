using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiCollider))]
public class FluidColorizer : MonoBehaviour
{
    public Color color;
	public float tintSpeed = 5;
	public ObiCollider collider;

    void Awake()
    {
		collider = GetComponent<ObiCollider>();
    }
}
