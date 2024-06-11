using UnityEngine;
using Unity.Profiling;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi SkinnedCloth", 902)]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class ObiSkinnedCloth : ObiClothBase, ITetherConstraintsUser
    {
        static ProfilerMarker m_SetTargetSkinPerfMarker = new ProfilerMarker("SetTargetSkin");
        static ProfilerMarker m_SortSkinInputsPerfMarker = new ProfilerMarker("SortSkinInputs");
        static ProfilerMarker m_SetSkinInputsPerfMarker = new ProfilerMarker("SetSkinInputs");
        static ProfilerMarker m_BakeMeshPerfMarker = new ProfilerMarker("BakeMesh");

        [SerializeField] protected ObiSkinnedClothBlueprint m_SkinnedClothBlueprint;

        // tethers
        [SerializeField] protected bool _tetherConstraintsEnabled = true;
        [SerializeField] protected float _tetherCompliance = 0;
        [SerializeField] [Range(0.1f, 2)] protected float _tetherScale = 1;

        public override ObiActorBlueprint sourceBlueprint
        {
            get { return m_SkinnedClothBlueprint; }
        }

        public override ObiClothBlueprintBase clothBlueprintBase
        {
            get { return m_SkinnedClothBlueprint; }
        }

        /// <summary>  
        /// Whether this actor's tether constraints are enabled.
        /// </summary>
        public bool tetherConstraintsEnabled
        {
            get { return _tetherConstraintsEnabled; }
            set { if (value != _tetherConstraintsEnabled) { _tetherConstraintsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.Tether); } }
        }

        /// <summary>  
        /// Compliance of this actor's tether constraints.
        /// </summary>
        public float tetherCompliance
        {
            get { return _tetherCompliance; }
            set { _tetherCompliance = value; SetConstraintsDirty(Oni.ConstraintType.Tether); }
        }

        /// <summary>  
        /// Rest length scaling for this actor's tether constraints.
        /// </summary>
        public float tetherScale
        {
            get { return _tetherScale; }
            set { _tetherScale = value; SetConstraintsDirty(Oni.ConstraintType.Tether); }
        }

        public ObiSkinnedClothBlueprint skinnedClothBlueprint
        {
            get { return m_SkinnedClothBlueprint; }
            set
            {
                if (m_SkinnedClothBlueprint != value)
                {
                    RemoveFromSolver();
                    ClearState();
                    m_SkinnedClothBlueprint = value;
                    AddToSolver();
                }
            }
        }

        private SkinnedMeshRenderer skin;
        [HideInInspector] public List<Vector3> bakedVertices = new List<Vector3>();
        [HideInInspector] public List<Vector3> bakedNormals = new List<Vector3>();
        [HideInInspector] public List<Vector4> bakedTangents = new List<Vector4>();
        [HideInInspector] public Mesh bakedMesh;   /**< Unique instance of the shared mesh, gets modified by the simulation*/

        private Vector3[] sortedPoints;
        private Vector3[] sortedNormals;

        public override void LoadBlueprint(ObiSolver solver)
        {
            base.LoadBlueprint(solver);

            var skinConstraints = GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
            if (skinConstraints != null && skinConstraints.GetBatchCount() > 0)
            {
                var batch = skinConstraints.batches[0] as ObiSkinConstraintsBatch;
                sortedPoints = new Vector3[batch.constraintCount];
                sortedNormals = new Vector3[batch.constraintCount];
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetupRuntimeConstraints();
        }

        private void SetupRuntimeConstraints()
        {
            SetConstraintsDirty(Oni.ConstraintType.Distance);
            SetConstraintsDirty(Oni.ConstraintType.Bending);
            SetConstraintsDirty(Oni.ConstraintType.Aerodynamics);
            SetConstraintsDirty(Oni.ConstraintType.Tether);
            SetSelfCollisions(m_SelfCollisions);
            SetSimplicesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            skin = GetComponent<SkinnedMeshRenderer>();
            bakedMesh = null;
            CreateBakedMeshIfNeeded();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_SkinnedClothBlueprint != null)
                skin.sharedMesh = m_SkinnedClothBlueprint.inputMesh;

            DestroyImmediate(bakedMesh);
        }

        private void CreateBakedMeshIfNeeded()
        {
            if (bakedMesh == null && m_SkinnedClothBlueprint != null)
                bakedMesh = Instantiate(m_SkinnedClothBlueprint.inputMesh);
        }

        private void SetTargetSkin()
        {
            using (m_SetTargetSkinPerfMarker.Auto())
            {
                var skinConstraints = GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
                var batch = skinConstraints.batches[0] as ObiSkinConstraintsBatch;

                var solverSkinConstraints = solver.GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
                var solverBatch = solverSkinConstraints.batches[0] as ObiSkinConstraintsBatch;

                using (m_SortSkinInputsPerfMarker.Auto())
                {
                    int pointCount = bakedVertices.Count;
                    for (int i = 0; i < pointCount; ++i)
                    {
                        int welded = m_SkinnedClothBlueprint.topology.rawToWelded[i];
                        sortedPoints[welded] = bakedVertices[i];
                        sortedNormals[welded] = bakedNormals[i];
                    }
                }

                using (m_SetSkinInputsPerfMarker.Auto())
                {
                    Matrix4x4 skinToSolver = actorLocalToSolverMatrix;
                    int offset = solverBatchOffsets[(int)Oni.ConstraintType.Skin][0];

                    for (int i = 0; i < batch.activeConstraintCount; ++i)
                    {
                        int constraintIndex = offset + i;
                        int solverIndex = solverBatch.particleIndices[constraintIndex];
                        int actorIndex = solver.particleToActor[solverIndex].indexInActor;

                        solverBatch.skinPoints[constraintIndex] = skinToSolver.MultiplyPoint3x4(sortedPoints[actorIndex]);
                        solverBatch.skinNormals[constraintIndex] = skinToSolver.MultiplyVector(sortedNormals[actorIndex]);

                        // Rigidly transform particles with zero skin radius and zero compliance:
                        if (Mathf.Approximately(solverBatch.skinRadiiBackstop[constraintIndex * 3], 0) & Mathf.Approximately(solverBatch.skinCompliance[constraintIndex], 0))
                        {
                            solver.invMasses[solverIndex] = 0;
                            solver.positions[solverIndex] = solverBatch.skinPoints[constraintIndex];
                        }
                    }
                }

            }
        }

        public override void BeginStep(float stepTime)
        {
            base.BeginStep(stepTime);
            CreateBakedMeshIfNeeded();

            if (m_SkinnedClothBlueprint != null && bakedMesh != null && isLoaded)
            {
                using (m_BakeMeshPerfMarker.Auto())
                {
                    // bake skinned vertices/normals:
                    skin.sharedMesh = m_SkinnedClothBlueprint.inputMesh;
                    skin.BakeMesh(bakedMesh);
                    bakedMesh.GetVertices(bakedVertices);
                    bakedMesh.GetNormals(bakedNormals);
                    bakedMesh.GetTangents(bakedTangents);
                }

                // update skin constraints / particles:
                SetTargetSkin();
            }
        }

    }

}