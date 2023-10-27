using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{

    [CreateAssetMenu(fileName = "fluid blueprint", menuName = "Obi/Fluid Blueprint", order = 100)]
    public class ObiFluidEmitterBlueprint : ObiEmitterBlueprintBase
    {
        // fluid parameters:
        public float smoothing = 2f;
        public float viscosity = 0.05f;         /**< viscosity of the fluid particles.*/
        public float surfaceTension = 1f; /**< surface tension of the fluid particles.*/

        // gas parameters:
        public float buoyancy = -1.0f;                      /**< how dense is this material with respect to air?*/
        public float atmosphericDrag = 0.0f;                /**< amount of drag applied by the surrounding air to particles near the surface of the material.*/
        public float atmosphericPressure = 0.0f;            /**< amount of pressure applied by the surrounding air particles.*/
        public float vorticity = 0.0f;                      /**< amount of vorticity confinement.*/

        public float diffusion = 0.0f;
        public Vector4 diffusionData;                       /**< values affected by diffusion.*/

        public void OnValidate()
        {
            resolution = Mathf.Max(0.001f, resolution);
            restDensity = Mathf.Max(0.001f, restDensity);
            smoothing = Mathf.Max(1, smoothing);
            viscosity = Mathf.Max(0, viscosity);
            atmosphericDrag = Mathf.Max(0, atmosphericDrag);
        }

        public float GetSmoothingRadius(Oni.SolverParameters.Mode mode)
        {
            return GetParticleSize(mode) * smoothing;
        }
    }
}