using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ModifySkybox : MonoBehaviour
{
    [SerializeField] Volume volume;
    HDRISky sky;
    Material material;
    float initialIOR;
    // Start is called before the first frame update
    void Start()
    {
        volume.profile.TryGet(out sky);
        Cubemap cubemap = Resources.Load("HDRIImages/abandoned_church", typeof(Cubemap)) as Cubemap;
        if (cubemap == null)
        {
            print("no!");
        }
        sky.hdriSky.value = cubemap;

        material = Resources.Load("Glass", typeof(Material)) as Material;
        initialIOR = material.GetFloat("_Ior");
    }

    // Update is called once per frame
    void Update()
    {
        if (initialIOR <= 1.01f || initialIOR >=2.49f)
        {
            return;
        }
        if (Input.GetKey("right"))
        {
            material.SetFloat("_Ior", initialIOR + 0.01f);
            initialIOR += 0.01f;
        }
        else if (Input.GetKey("left"))
        {
            material.SetFloat("_Ior", initialIOR - 0.01f);
            initialIOR -= 0.01f;
        }
    }
}
