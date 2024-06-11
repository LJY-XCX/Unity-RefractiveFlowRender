using UnityEngine;
using System.Collections.Generic;

namespace Obi
{

    [AddComponentMenu("Physics/Obi/Obi Emitter", 850)]
    [ExecuteInEditMode]
    public class ObiEmitter : ObiActor
    {

        public delegate void EmitterParticleCallback(ObiEmitter emitter, int particleIndex);

        public event EmitterParticleCallback OnEmitParticle;
        public event EmitterParticleCallback OnKillParticle;

        public enum EmissionMethod
        {
            /// <summary>  
            /// Continously emits particles until there are no particles left to emit.
            /// </summary>
            STREAM,

            /// <summary>  
            /// Emits a single burst of particles from the emitter, and does not emit any more until
            /// all alive particles have died.
            /// </summary>
            BURST       
        }

        public ObiEmitterBlueprintBase emitterBlueprint;

        /// <summary>  
        /// The base actor blueprint used by this actor.
        /// </summary>
        /// This is the same as <see cref="emitterBlueprint"/>.
        public override ObiActorBlueprint sourceBlueprint
        {
            get { return emitterBlueprint; }
        }

        [Tooltip("Filter used for collision detection.")]
        private int filter = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 1);

        /// <summary>  
        /// Emission method used by this emitter.
        /// </summary>
        /// Can be either STREAM or BURST. 
        [Tooltip("Changes how the emitter behaves. Available modes are Stream and Burst.")]
        public EmissionMethod emissionMethod = EmissionMethod.STREAM;

        /// <summary>  
        /// Minimum amount of inactive particles available before the emitter is allowed to resume emission.
        /// </summary>
        [Range(0, 1)]
        public float minPoolSize = 0.5f;

        /// <summary>  
        /// Speed (in meters/second) at which fluid is emitter.
        /// </summary>
        /// Note this affects both the speed and the amount of particles emitted per second, to ensure flow is as smooth as possible.
        /// Set it to zero to deactivate emission.
        [Tooltip("Speed (in meters/second) of emitted particles. Setting it to zero will stop emission. Large values will cause more particles to be emitted.")]
        public float speed = 0.25f;

        /// <summary>  
        /// Particle lifespan in seconds.
        /// </summary>
        /// Particles older than this value will become inactive and go back to the solver's emission pool, making them available for reuse.
        [Tooltip("Lifespan of each particle.")]
        public float lifespan = 4;

        /// <summary>  
        /// Amount of random velocity added to particles when emitted.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("Amount of randomization applied to particles.")]
        public float randomVelocity = 0;

        /// <summary>  
        /// Use the emitter shape color to tint particles upon emission.
        /// </summary>
        [Tooltip("Spawned particles are tinted by the corresponding emitter shape's color.")]
        public bool useShapeColor = true;

        [HideInInspector] [SerializeField] private List<ObiEmitterShape> emitterShapes = new List<ObiEmitterShape>();
        private IEnumerator<ObiEmitterShape.DistributionPoint> distEnumerator;


        /// <summary>  
        /// Per particle remaining life (in seconds).
        /// </summary>
        [HideInInspector] public float[] life;     

        private float unemittedBursts = 0;
        private bool m_IsEmitting = false;

        /// <summary>  
        /// Collision filter value used by fluid particles.
        /// </summary>
        public int Filter
        {
            set
            {
                if (filter != value)
                {
                    filter = value;
                    UpdateFilter();
                }
            }
            get { return filter; }
        }


        /// <summary>  
        /// Whether the emitter is currently emitting particles.
        /// </summary>
        public bool isEmitting
        {
            get { return m_IsEmitting; }
        }

        /// <summary>
        /// Whether to use simplices (triangles, edges) for contact generation.
        /// </summary>
        public override bool surfaceCollisions
        {
            get
            {
                return false;
            }
            set
            {
                if (m_SurfaceCollisions != value)
                    m_SurfaceCollisions = false;
            }
        }

        /// <summary>  
        /// Whether this actor applies external forces in a custom way. 
        /// </summary>
        /// In case of fluid, this is true as forces are interpreted as wind and affected by athmospheric drag.
        public override bool usesCustomExternalForces
        {
            get { return true; }
        }

        /// <summary>  
        /// Whether this actor makes use of particle anisotropy
        /// </summary>
        /// In case of fluid, this is true as particles adapt their shape to fit the fluid's surface.
        public override bool usesAnisotropicParticles
        {
            get { return true; }
        }

        public override void LoadBlueprint(ObiSolver solver)
        {
            base.LoadBlueprint(solver);

            //Copy local arrays:
            life = new float[particleCount];
            for (int i = 0; i < life.Length; ++i)
                life[i] = lifespan;

            UpdateParticleMaterial();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateEmitterDistribution();
        }

        /// <summary>  
        /// Adds a shape trough which to emit particles. This is called automatically by <see cref="ObiEmitterShape"/>.
        /// </summary>
        public void AddShape(ObiEmitterShape shape)
        {
            if (!emitterShapes.Contains(shape))
            {
                emitterShapes.Add(shape);

                if (solver != null)
                {
                    shape.particleSize = (emitterBlueprint != null) ? emitterBlueprint.GetParticleSize(m_Solver.parameters.mode) : 0.1f;
                    shape.GenerateDistribution();
                    distEnumerator = GetDistributionEnumerator();
                }
            }
        }

        /// <summary>  
        /// Removes a shape trough which to emit particles. This is called automatically by <see cref="ObiEmitterShape"/>.
        /// </summary>
        public void RemoveShape(ObiEmitterShape shape)
        {
            emitterShapes.Remove(shape);
            if (solver != null)
            {
                distEnumerator = GetDistributionEnumerator();
            }
        }

        /// <summary>  
        /// Updates the spawn point distribution of all shapes used by this emitter.
        /// </summary>
        public void UpdateEmitterDistribution()
        {
            if (solver != null)
            {
                for (int i = 0; i < emitterShapes.Count; ++i)
                {
                    emitterShapes[i].particleSize = (emitterBlueprint != null) ? emitterBlueprint.GetParticleSize(m_Solver.parameters.mode) : 0.1f;
                    emitterShapes[i].GenerateDistribution();
                }
                distEnumerator = GetDistributionEnumerator();
            }
        }

        private IEnumerator<ObiEmitterShape.DistributionPoint> GetDistributionEnumerator()
        {

            // In case there are no shapes, emit using the emitter itself as a single-point shape.
            if (emitterShapes.Count == 0)
            {
                while (true)
                {
                    Matrix4x4 l2sTransform = actorLocalToSolverMatrix;
                    yield return new ObiEmitterShape.DistributionPoint(l2sTransform.GetColumn(3), l2sTransform.GetColumn(2), Color.white);
                }
            }

            // Emit distributing emission among all shapes:
            while (true)
            {
                for (int j = 0; j < emitterShapes.Count; ++j)
                {
                    ObiEmitterShape shape = emitterShapes[j];

                    if (shape.distribution.Count == 0)
                        yield return new ObiEmitterShape.DistributionPoint(shape.ShapeLocalToSolverMatrix.GetColumn(3), shape.ShapeLocalToSolverMatrix.GetColumn(2), Color.white);

                    for (int i = 0; i < shape.distribution.Count; ++i)
                        yield return shape.distribution[i].GetTransformed(shape.ShapeLocalToSolverMatrix, shape.color);

                }
            }

        }

        public void UpdateParticleMaterial()
        {
            for (int i = 0; i < activeParticleCount; ++i)
                UpdateParticleMaterial(i);

            UpdateEmitterDistribution();
        }

        public override void SetSelfCollisions(bool selfCollisions)
        {
            if (solver != null && isLoaded)
            {
                ObiUtils.ParticleFlags particleFlags = ObiUtils.ParticleFlags.Fluid;
                if (emitterBlueprint != null && !(emitterBlueprint is ObiFluidEmitterBlueprint))
                    particleFlags = 0;

                for (int i = 0; i < solverIndices.Length; i++)
                {
                    int group = ObiUtils.GetGroupFromPhase(m_Solver.phases[solverIndices[i]]);
                    m_Solver.phases[solverIndices[i]] = ObiUtils.MakePhase(group, (selfCollisions ? ObiUtils.ParticleFlags.SelfCollide : 0) | particleFlags);
                }
            }
        }

        private void UpdateFilter()
        {
            if (solver != null && isLoaded)
            {
                for (int i = 0; i < solverIndices.Length; i++)
                    m_Solver.filters[solverIndices[i]] = filter;
            }
        }

        private void UpdateParticleResolution(int index)
        {

            if (m_Solver == null) return;

            ObiFluidEmitterBlueprint fluidMaterial = emitterBlueprint as ObiFluidEmitterBlueprint;

            int solverIndex = solverIndices[index];

            float restDistance = (emitterBlueprint != null) ? emitterBlueprint.GetParticleSize(m_Solver.parameters.mode) : 0.1f;
            float pmass = (emitterBlueprint != null) ? emitterBlueprint.GetParticleMass(m_Solver.parameters.mode) : 0.1f;
            float radius;

            if (emitterBlueprint != null && fluidMaterial == null)
            {
                float randomRadius = UnityEngine.Random.Range(0, restDistance / 100.0f * (emitterBlueprint as ObiGranularEmitterBlueprint).randomness);
                radius = Mathf.Max(0.001f + restDistance * 0.5f - randomRadius);
            }
            else
                radius = restDistance * 0.5f;

            m_Solver.principalRadii[solverIndex] = Vector3.one * radius;
            if (emitterBlueprint != null)
                m_Solver.smoothingRadii[solverIndex] = fluidMaterial != null ? fluidMaterial.GetSmoothingRadius(m_Solver.parameters.mode) : 0;
            else
                m_Solver.smoothingRadii[solverIndex] = 1f / (10 * Mathf.Pow(1, 1 / (m_Solver.parameters.mode == Oni.SolverParameters.Mode.Mode3D ? 3.0f : 2.0f)));

            m_Solver.invMasses[solverIndex] = 1 / pmass;
            m_Solver.invRotationalMasses[solverIndex] = m_Solver.invMasses[solverIndex];

        }

        private void UpdateParticleMaterial(int index)
        {

            if (m_Solver == null) return;

            UpdateParticleResolution(index);

            ObiFluidEmitterBlueprint fluidMaterial = emitterBlueprint as ObiFluidEmitterBlueprint;

            int solverIndex = solverIndices[index];

            m_Solver.restDensities[solverIndex] = fluidMaterial != null ? fluidMaterial.restDensity : 0;
            m_Solver.viscosities[solverIndex] = fluidMaterial != null ? fluidMaterial.viscosity : 0;
            m_Solver.vortConfinement[solverIndex] = fluidMaterial != null ? fluidMaterial.vorticity : 0;
            m_Solver.surfaceTension[solverIndex] = fluidMaterial != null ? fluidMaterial.surfaceTension : 0;
            m_Solver.buoyancies[solverIndex] = fluidMaterial != null ? fluidMaterial.buoyancy : -1;
            m_Solver.atmosphericDrag[solverIndex] = fluidMaterial != null ? fluidMaterial.atmosphericDrag : 0;
            m_Solver.atmosphericPressure[solverIndex] = fluidMaterial != null ? fluidMaterial.atmosphericPressure : 0;
            m_Solver.diffusion[solverIndex] = fluidMaterial != null ? fluidMaterial.diffusion : 0;
            m_Solver.userData[solverIndex] = fluidMaterial != null ? fluidMaterial.diffusionData : Vector4.zero;
            m_Solver.filters[solverIndex] = filter;

            ObiUtils.ParticleFlags particleFlags = ObiUtils.ParticleFlags.Fluid;
            if (emitterBlueprint != null && fluidMaterial == null)
                particleFlags = 0;

            var group = ObiUtils.GetGroupFromPhase(m_Solver.phases[solverIndex]);
            m_Solver.phases[solverIndex] = ObiUtils.MakePhase(group, ObiUtils.ParticleFlags.SelfCollide | particleFlags);
        }

        protected override void SwapWithFirstInactiveParticle(int actorIndex)
        {
            base.SwapWithFirstInactiveParticle(actorIndex);
            life.Swap(actorIndex, activeParticleCount);
        }

        private void ResetParticle(int index, float offset, float deltaTime)
        {

            distEnumerator.MoveNext();
            ObiEmitterShape.DistributionPoint distributionPoint = distEnumerator.Current;

            Vector3 spawnVelocity = Vector3.Lerp(distributionPoint.velocity, UnityEngine.Random.onUnitSphere, randomVelocity);
            Vector3 positionOffset = spawnVelocity * (speed * deltaTime) * offset;

            int solverIndex = solverIndices[index];

            m_Solver.startPositions[solverIndex] = m_Solver.positions[solverIndex] = distributionPoint.position + positionOffset;
            m_Solver.velocities[solverIndex] = spawnVelocity * speed;

            UpdateParticleMaterial(index);

            if (useShapeColor)
                m_Solver.colors[solverIndex] = distributionPoint.color;
        }

        /// <summary>  
        /// Asks the emitter to emit a new particle. Returns whether the emission was succesful.
        /// </summary>
        /// <param name="offset"> Distance from the emitter surface at which the particle should be emitted.</param>
        /// <param name="deltaTime"> Duration of the last step in seconds.</param>
        /// <returns>
        /// If at least one particle was in the emission pool and it could be emitted, will return true. False otherwise.
        /// </returns> 
        public bool EmitParticle(float offset, float deltaTime)
        {

            if (activeParticleCount == particleCount) return false;

            life[activeParticleCount] = lifespan;

            // move particle to its spawn position:
            ResetParticle(activeParticleCount, offset, deltaTime);

            // now there's one active particle more:
            if (!ActivateParticle(activeParticleCount))
                return false;

            if (OnEmitParticle != null)
                OnEmitParticle(this, activeParticleCount - 1);

            m_IsEmitting = true;

            return true;
        }

        /// <summary>  
        /// Asks the emiter to kill a particle. Returns whether it was succesful.
        /// </summary>
        /// <returns>
        /// True if the particle could be killed. False if it was already inactive.
        /// </returns> 
        public bool KillParticle(int index)
        {
            // reduce amount of active particles:
            if (!DeactivateParticle(index))
                return false;

            if (OnKillParticle != null)
                OnKillParticle(this, activeParticleCount);

            return true;

        }

        /// <summary>  
        /// Kills all particles in the emitter, and returns them to the emission pool.
        /// </summary>
        public void KillAll()
        {
            for (int i = activeParticleCount - 1; i >= 0; --i)
            {
                KillParticle(i);
            }
        }

        private int GetDistributionPointsCount()
        {
            int size = 0;
            for (int i = 0; i < emitterShapes.Count; ++i)
                size += emitterShapes[i].distribution.Count;
            return Mathf.Max(1, size);
        }

        public override void BeginStep(float stepTime)
        {
            base.BeginStep(stepTime);

            // cache a per-shape matrix that transforms from shape local space to solver space.
            for (int j = 0; j < emitterShapes.Count; ++j)
            {
                emitterShapes[j].UpdateLocalToSolverMatrix();
            }

            // Update lifetime and kill dead particles:
            for (int i = activeParticleCount - 1; i >= 0; --i)
            {
                life[i] -= stepTime;

                if (life[i] <= 0)
                {
                    KillParticle(i);
                }
            }

            int emissionPoints = GetDistributionPointsCount();

            int pooledParticles = particleCount - activeParticleCount;

            if (pooledParticles == 0)
                m_IsEmitting = false;

            if (m_IsEmitting || pooledParticles > Mathf.FloorToInt(minPoolSize * particleCount))
            {

                // stream emission:
                if (emissionMethod == EmissionMethod.STREAM)
                {
                    // number of bursts per simulation step:
                    float burstCount = (speed * stepTime) / ((emitterBlueprint != null) ? emitterBlueprint.GetParticleSize(m_Solver.parameters.mode) : 0.1f);

                    // Emit new particles:
                    unemittedBursts += burstCount;
                    int burst = 0;
                    while (unemittedBursts > 0)
                    {
                        for (int i = 0; i < emissionPoints; ++i)
                        {
                            EmitParticle(burst / burstCount, stepTime);
                        }
                        unemittedBursts -= 1;
                        burst++;
                    }
                }
                else
                { // burst emission:

                    if (activeParticleCount == 0)
                    {
                        for (int i = 0; i < emissionPoints; ++i)
                        {
                            EmitParticle(0, stepTime);
                        }
                    }
                }
            }

        }
    }
}
