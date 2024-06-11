using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{

    [CreateAssetMenu(fileName = "granular blueprint", menuName = "Obi/Granular Blueprint", order = 101)]
    public class ObiGranularEmitterBlueprint : ObiEmitterBlueprintBase
    {
        public float randomness = 0;

        public void OnValidate()
        {
            resolution = Mathf.Max(0.001f, resolution);
            restDensity = Mathf.Max(0.001f, restDensity);
            randomness = Mathf.Max(0, randomness);
        }
    }
}