using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiEmitter))]
public class EmitOnKeyPress : MonoBehaviour
{
    ObiEmitter emitter;
    public float emitSpeed = 4;
    public KeyCode key;

    void Start()
    {
        emitter = GetComponent<ObiEmitter>();
    }

    void Update()
    {
        if (Input.GetKey(key))
            emitter.speed = emitSpeed;
        else
            emitter.speed = 0;
    }
}
