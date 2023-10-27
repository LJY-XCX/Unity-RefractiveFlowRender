using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mine : MonoBehaviour
{
    public float floatFrequency = 2;
    public float floatAmplitude = 0.2f;
    public GameObject explosionPrefab;
    float random;
   

    private void Awake()
    {
        random = UnityEngine.Random.value * 2;
    }

    public void Update()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Sin(random + Time.time * floatFrequency) * floatAmplitude;
        transform.position = pos;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            var gameController = collision.gameObject.GetComponent<BoatGameController>();
            if (gameController != null)
                gameController.Die();

            collision.rigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
