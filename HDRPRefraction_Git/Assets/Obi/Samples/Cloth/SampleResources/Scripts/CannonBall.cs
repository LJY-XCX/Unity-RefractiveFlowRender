using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBall : MonoBehaviour
{

    public GameObject explosionPrefab;
    int waterMask;

    private void Awake()
    {
        waterMask = LayerMask.NameToLayer("Water");
    }

    private void OnTriggerEnter(Collider c)
    {
        if ((c.gameObject.layer & waterMask) != 0)
        {
            Instantiate(explosionPrefab, new Vector3(transform.position.x,0, transform.position.z), Quaternion.identity);
            Destroy(gameObject,1);
        }
    }
}
