using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoCannon : MonoBehaviour
{
    public GameObject cannonBall;
    public Transform target;
    public Rigidbody targetRigidbody;
    public float launchAngle = 45;
    public float shotFrequency = 10;
    public float shotVariability = 2;

    Vector3 projectileXZPos;
    Vector3 targetXZPos;
    Vector3 targetXZVel;

    private void Awake()
    {
        StartCoroutine(Shoot());
    }

    void Update()
    {
        projectileXZPos = new Vector3(transform.position.x, 0.0f, transform.position.z);
        targetXZPos = new Vector3(target.position.x, 0.0f, target.position.z);
        targetXZVel = new Vector3(targetRigidbody.velocity.x, 0.0f, targetRigidbody.velocity.z);
        transform.LookAt(targetXZPos);
    }

    IEnumerator Shoot()
    {
        while (true)
        {
            yield return new WaitForSeconds(shotFrequency - shotVariability + UnityEngine.Random.value * shotVariability);
            var ball = Instantiate(cannonBall, transform.position, Quaternion.identity);
            Launch(ball.GetComponent<Rigidbody>());
        }
    }

    void Launch(Rigidbody rb)
    {
        float G = Physics.gravity.y;
        float tanAlpha = Mathf.Tan(launchAngle * Mathf.Deg2Rad);
        float H = target.position.y - transform.position.y;

        float R = Vector3.Distance(projectileXZPos, targetXZPos);
        float Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));

        if (Vz > 0.001f)
        {
            float projectileT = R / Vz;
            Vector3 extrapolatedPos = targetXZPos + targetXZVel * projectileT;

            transform.LookAt(extrapolatedPos);

            R = Vector3.Distance(projectileXZPos, extrapolatedPos);
            Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));

            rb.velocity = transform.TransformDirection(new Vector3(0f, tanAlpha * Vz, Vz));
        }
    }
}
