using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Obi;

public class BoatGameController : MonoBehaviour
{
    public BoatController boat;
    public Transform boatSpawnPoint;
    public Transform cameraSpawnPoint;

    public GameObject mineFieldInstance;
    public GameObject minefieldPrefab;

    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent onFinish = new UnityEvent();
    public UnityEvent onRestart = new UnityEvent();

    public void Die()
    {
        onDeath.Invoke();
    }

    public void Finish()
    {
        onFinish.Invoke();
    }

    void Update()
    {
        if (boat != null && boatSpawnPoint != null && cameraSpawnPoint != null)
        {
            if (Input.GetKey(KeyCode.R))
            {
                if (mineFieldInstance != null && minefieldPrefab != null)
                {
                    Destroy(mineFieldInstance);
                    mineFieldInstance = Instantiate(minefieldPrefab);
                }

                boat.Respawn(boatSpawnPoint.position, boatSpawnPoint.rotation);
                Camera.main.GetComponent<ExtrapolationCamera>().Teleport(cameraSpawnPoint.position, cameraSpawnPoint.rotation);

                onRestart.Invoke();
            }
        }
    }
}
