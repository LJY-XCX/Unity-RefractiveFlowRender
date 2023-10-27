using UnityEngine;
using System.Collections;


namespace Obi
{

    public abstract class ObiEmitterBlueprintBase : ObiActorBlueprint
    {

        public uint capacity = 1000;
        public float resolution = 1;
        public float restDensity = 1000;        /**< rest density of the material.*/

        /** 
         * Returns the diameter (2 * radius) of a single particle of this material.
         */
        public float GetParticleSize(Oni.SolverParameters.Mode mode)
        {
            return 1f / (10 * Mathf.Pow(resolution, 1 / (mode == Oni.SolverParameters.Mode.Mode3D ? 3.0f : 2.0f)));
        }

        /** 
         * Returns the mass (in kilograms) of a single particle of this material.
         */
        public float GetParticleMass(Oni.SolverParameters.Mode mode)
        {
            return restDensity * Mathf.Pow(GetParticleSize(mode), mode == Oni.SolverParameters.Mode.Mode3D ? 3 : 2);
        }

        protected override IEnumerator Initialize()
        {
            ClearParticleGroups();
            m_ActiveParticleCount = 0;

            positions = new Vector3[capacity];
            orientations = new Quaternion[capacity];
            restPositions = new Vector4[capacity];
            restOrientations = new Quaternion[capacity];
            velocities = new Vector3[capacity];
            angularVelocities = new Vector3[capacity];
            invMasses = new float[capacity];
            invRotationalMasses = new float[capacity];
            principalRadii = new Vector3[capacity];
            filters = new int[capacity];
            colors = new Color[capacity];

            for (int i = 0; i < capacity; i++)
            {
                invRotationalMasses[i] = invMasses[i] = 1.0f;
                positions[i] = Vector3.zero;
                orientations[i] = restOrientations[i] = Quaternion.identity;
                principalRadii[i] = Vector3.one;

                colors[i] = Color.white;
                filters[i] = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 1);

            }

            yield return new CoroutineJob.ProgressInfo("ObiEmitter: done", 1);
        }
    }
}