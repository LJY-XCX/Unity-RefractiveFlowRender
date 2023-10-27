using UnityEngine;
using Unity.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi Tearable Cloth", 901)]
    [RequireComponent(typeof(MeshFilter))]
    public class ObiTearableCloth : ObiClothBase
    {
        static ProfilerMarker m_TearingPerfMarker = new ProfilerMarker("ClothTearing");

        public ObiTearableClothBlueprint m_TearableClothBlueprint;
        private ObiTearableClothBlueprint m_TearableBlueprintInstance;

        public bool tearingEnabled = true;
        public float tearResistanceMultiplier = 1000;                   /**< Factor that controls how much a structural cloth spring can stretch before breaking.*/
        public int tearRate = 1;
        [Range(0, 1)] public float tearDebilitation = 0.5f;

        public override ObiActorBlueprint sourceBlueprint
        {
            get { return m_TearableClothBlueprint; }
        }

        public override ObiClothBlueprintBase clothBlueprintBase
        {
            get { return m_TearableClothBlueprint; }
        }

        public ObiTearableClothBlueprint clothBlueprint
        {
            get { return m_TearableClothBlueprint; }
            set
            {
                if (m_TearableClothBlueprint != value)
                {
                    RemoveFromSolver();
                    ClearState();
                    m_TearableClothBlueprint = value;
                    AddToSolver();
                }
            }
        }

        public delegate void ClothTornCallback(ObiTearableCloth cloth, ObiClothTornEventArgs tearInfo);
        public event ClothTornCallback OnClothTorn;  /**< Called when a constraint is torn.*/

        public class ObiClothTornEventArgs
        {
            public StructuralConstraint edge;       /**< info about the edge being torn.*/
            public int particleIndex;   /**< index of the particle being torn*/
            public List<HalfEdgeMesh.Face> updatedFaces;

            public ObiClothTornEventArgs(StructuralConstraint edge, int particle, List<HalfEdgeMesh.Face> updatedFaces)
            {
                this.edge = edge;
                this.particleIndex = particle;
                this.updatedFaces = updatedFaces;
            }
        }

        public override void LoadBlueprint(ObiSolver solver)
        {
            // create a copy of the blueprint for this cloth:
            m_TearableBlueprintInstance = this.blueprint as ObiTearableClothBlueprint;

            base.LoadBlueprint(solver);
        }

        public override void UnloadBlueprint(ObiSolver solver)
        {
            base.UnloadBlueprint(solver);

            // delete the blueprint instance:
            if (m_TearableBlueprintInstance != null)
                DestroyImmediate(m_TearableBlueprintInstance);
        }

        private void SetupRuntimeConstraints()
        {
            SetConstraintsDirty(Oni.ConstraintType.Distance);
            SetConstraintsDirty(Oni.ConstraintType.Bending);
            SetConstraintsDirty(Oni.ConstraintType.Aerodynamics);
            SetSelfCollisions(selfCollisions);
            SetSimplicesDirty();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetupRuntimeConstraints();
        }

        public override void Substep(float substepTime)
        {
            base.Substep(substepTime);

            if (isActiveAndEnabled && tearingEnabled)
                ApplyTearing(substepTime);
        }

        private void ApplyTearing(float substepTime)
        {
            using (m_TearingPerfMarker.Auto())
            {
                float sqrTime = substepTime * substepTime;
                List<StructuralConstraint> tornEdges = new List<StructuralConstraint>();

                var dc = GetConstraintsByType(Oni.ConstraintType.Distance) as ObiConstraints<ObiDistanceConstraintsBatch>;
                var sc = this.solver.GetConstraintsByType(Oni.ConstraintType.Distance) as ObiConstraints<ObiDistanceConstraintsBatch>;

                if (dc != null && sc != null)
                for (int j = 0; j < dc.batches.Count; ++j)
                {
                    var batch       = dc.batches[j] as ObiDistanceConstraintsBatch;
                    var solverBatch = sc.batches[j] as ObiDistanceConstraintsBatch;

                    for (int i = 0; i < batch.activeConstraintCount; i++)
                    {
                        float p1Resistance = m_TearableBlueprintInstance.tearResistance[batch.particleIndices[i * 2]];
                        float p2Resistance = m_TearableBlueprintInstance.tearResistance[batch.particleIndices[i * 2 + 1]];

                        // average particle resistances:
                        float resistance = (p1Resistance + p2Resistance) * 0.5f * tearResistanceMultiplier;

                        // divide lambda by squared delta time to get force in newtons:
                        int offset = solverBatchOffsets[(int)Oni.ConstraintType.Distance][j];
                        float force = solverBatch.lambdas[offset + i] / sqrTime;

                        if (-force > resistance)
                        { // units are newtons.
                            tornEdges.Add(new StructuralConstraint(batch, i, force));
                        }
                    }
                }

                if (tornEdges.Count > 0)
                {

                    // sort edges by tear force:
                    tornEdges.Sort(delegate (StructuralConstraint x, StructuralConstraint y)
                    {
                        return x.force.CompareTo(y.force);
                    });

                    int tornCount = 0;
                    for (int i = 0; i < tornEdges.Count; i++)
                    {
                        if (Tear(tornEdges[i]))
                            tornCount++;
                        if (tornCount >= tearRate)
                            break;
                    }

                    // update solver deformable triangle indices:
                    if (tornCount > 0)
                        UpdateDeformableTriangles();

                }

            }

        }

        /**
         * Tears a cloth distance constraint, affecting both the physical representation of the cloth and its mesh.
         */
        public bool Tear(StructuralConstraint edge)
        {
            // don't allow splitting if there are no free particles left in the pool.
            if (activeParticleCount >= m_TearableClothBlueprint.particleCount)
                return false;

            // get actor particle indices at both ends of the constraint:
            ParticlePair indices = edge.batchIndex.GetParticleIndices(edge.constraintIndex);

            // Try to perform a split operation on the topology. If we cannot perform it, bail out.
            Vector3 point, normal;
            HashSet<int> updatedHalfEdges = new HashSet<int>();
            List<HalfEdgeMesh.Face> updatedFaces = new List<HalfEdgeMesh.Face>();
            if (!TopologySplitAttempt(ref indices.first, ref indices.second, out point, out normal, updatedFaces, updatedHalfEdges))
                return false;

            // Weaken edges around the cut:
            WeakenCutPoint(indices.first, point, normal);

            // split the particle in two, adding a new active particle:
            SplitParticle(indices.first);

            // update constraints:
            UpdateTornDistanceConstraints(updatedHalfEdges);
            UpdateTornBendConstraints(indices.first);

            if (OnClothTorn != null)
                OnClothTorn(this, new ObiClothTornEventArgs(edge, indices.first, updatedFaces));

            return true;
        }


        private bool TopologySplitAttempt(ref int splitActorIndex,
                                          ref int intactActorIndex,
                                          out Vector3 point,
                                          out Vector3 normal,
                                          List<HalfEdgeMesh.Face> updatedFaces,
                                          HashSet<int> updatedHalfEdges)
        {
            int splitSolverIndex  = solverIndices[splitActorIndex];
            int intactSolverIndex = solverIndices[intactActorIndex];

            // we will first try to split the particle with higher mass, so swap them if needed.
            if (m_Solver.invMasses[splitSolverIndex] > m_Solver.invMasses[intactSolverIndex])
                ObiUtils.Swap(ref splitSolverIndex, ref intactSolverIndex);

            // Calculate the splitting plane:
            point = m_Solver.positions[splitSolverIndex];
            Vector3 v2 = m_Solver.positions[intactSolverIndex];
            normal = (v2 - point).normalized;

            // Try to split the vertex at that particle. 
            // If we cannot not split the higher mass particle, try the other one. If that fails too, we cannot tear this edge.
            if (m_Solver.invMasses[splitSolverIndex] == 0 ||
                !SplitTopologyAtVertex(splitActorIndex, new Plane(normal, point), updatedFaces, updatedHalfEdges))
            {
                // Try to split the other particle:
                ObiUtils.Swap(ref splitActorIndex,  ref intactActorIndex);
                ObiUtils.Swap(ref splitSolverIndex, ref intactSolverIndex);

                point = m_Solver.positions[splitSolverIndex];
                v2 = m_Solver.positions[intactSolverIndex];
                normal = (v2 - point).normalized;

                if (m_Solver.invMasses[splitSolverIndex] == 0 ||
                    !SplitTopologyAtVertex(splitActorIndex, new Plane(normal, point), updatedFaces, updatedHalfEdges))
                    return false;
            }
            return true;
        }

        private void SplitParticle(int splitActorIndex)
        {
            int splitSolverIndex = solverIndices[splitActorIndex];

            // halve the original particle's mass and radius:
            m_Solver.invMasses[splitSolverIndex] *= 2;
            m_Solver.principalRadii[splitSolverIndex] *= 0.5f;

            // create a copy of the original particle:
            m_TearableBlueprintInstance.tearResistance[activeParticleCount] = m_TearableBlueprintInstance.tearResistance[splitActorIndex];

            CopyParticle(splitActorIndex, activeParticleCount);
            ActivateParticle(activeParticleCount);
        }

        private void WeakenCutPoint(int splitActorIndex, Vector3 point, Vector3 normal)
        {

            int weakPt1 = -1;
            int weakPt2 = -1;
            float weakestValue = float.MaxValue;
            float secondWeakestValue = float.MaxValue;

            foreach (HalfEdgeMesh.Vertex v in m_TearableBlueprintInstance.topology.GetNeighbourVerticesEnumerator(m_TearableBlueprintInstance.topology.vertices[splitActorIndex]))
            {
                Vector3 neighbour = m_Solver.positions[solverIndices[v.index]];
                float weakness = Mathf.Abs(Vector3.Dot(normal, (neighbour - point).normalized));

                if (weakness < weakestValue)
                {
                    secondWeakestValue = weakestValue;
                    weakestValue = weakness;
                    weakPt2 = weakPt1;
                    weakPt1 = v.index;
                }
                else if (weakness < secondWeakestValue)
                {
                    secondWeakestValue = weakness;
                    weakPt2 = v.index;
                }
            }

            // reduce tear resistance at the weak spots of the cut, to encourage coherent tear formation.
            if (weakPt1 >= 0) m_TearableBlueprintInstance.tearResistance[weakPt1] *= 1 - tearDebilitation;
            if (weakPt2 >= 0) m_TearableBlueprintInstance.tearResistance[weakPt2] *= 1 - tearDebilitation;
        }

        private void ClassifyFaces(HalfEdgeMesh.Vertex vertex,
                                   Plane plane,
                                   List<HalfEdgeMesh.Face> side1,
                                   List<HalfEdgeMesh.Face> side2)
        {
            foreach (HalfEdgeMesh.Face face in m_TearableBlueprintInstance.topology.GetNeighbourFacesEnumerator(vertex))
            {
                HalfEdgeMesh.HalfEdge e1 = m_TearableBlueprintInstance.topology.halfEdges[face.halfEdge];
                HalfEdgeMesh.HalfEdge e2 = m_TearableBlueprintInstance.topology.halfEdges[e1.nextHalfEdge];
                HalfEdgeMesh.HalfEdge e3 = m_TearableBlueprintInstance.topology.halfEdges[e2.nextHalfEdge];

                // Skip this face if it doesn't contain the vertex being split.
                // This can happen because edge pair links are not updated in a vertex split operation,
                // so split vertices still "see" faces at the other side of the cut as adjacent.
                if (e1.endVertex != vertex.index &&
                    e2.endVertex != vertex.index &&
                    e3.endVertex != vertex.index)
                    continue;

                // calculate actual face center from deformed vertex positions:
                Vector3 faceCenter = (m_Solver.positions[solverIndices[e1.endVertex]] +
                                      m_Solver.positions[solverIndices[e2.endVertex]] +
                                      m_Solver.positions[solverIndices[e3.endVertex]]) * 0.33f;

                if (plane.GetSide(faceCenter))
                    side1.Add(face);
                else
                    side2.Add(face);
            }
        }

        private bool SplitTopologyAtVertex(int vertexIndex,
                                           Plane plane,
                                           List<HalfEdgeMesh.Face> updatedFaces,
                                           HashSet<int> updatedEdgeIndices)
        {
            if (vertexIndex < 0 || vertexIndex >= m_TearableBlueprintInstance.topology.vertices.Count)
                return false;

            updatedFaces.Clear();
            updatedEdgeIndices.Clear();
            HalfEdgeMesh.Vertex vertex = m_TearableBlueprintInstance.topology.vertices[vertexIndex];

            // classify adjacent faces depending on which side of the plane they're at:
            var otherSide = new List<HalfEdgeMesh.Face>();
            ClassifyFaces(vertex, plane, updatedFaces, otherSide);

            // guard against pathological case in which all particles are in one side of the plane:
            if (otherSide.Count == 0 || updatedFaces.Count == 0)
                return false;

            // create a new vertex:
            var newVertex = new HalfEdgeMesh.Vertex();
            newVertex.position = vertex.position;
            newVertex.index = m_TearableBlueprintInstance.topology.vertices.Count;
            newVertex.halfEdge = vertex.halfEdge;

            // rearrange edges at the updated side:
            foreach (HalfEdgeMesh.Face face in updatedFaces)
            {
                // find half edges that start and end at the split vertex:
                HalfEdgeMesh.HalfEdge e1 = m_TearableBlueprintInstance.topology.halfEdges[face.halfEdge];
                HalfEdgeMesh.HalfEdge e2 = m_TearableBlueprintInstance.topology.halfEdges[e1.nextHalfEdge];
                HalfEdgeMesh.HalfEdge e3 = m_TearableBlueprintInstance.topology.halfEdges[e2.nextHalfEdge];

                var in_ = e1;
                var out_ = e2;

                if (e1.endVertex == vertex.index)
                    in_ = e1;
                else if (m_TearableBlueprintInstance.topology.GetHalfEdgeStartVertex(e1) == vertex.index)
                    out_ = e1;

                if (e2.endVertex == vertex.index)
                    in_ = e2;
                else if (m_TearableBlueprintInstance.topology.GetHalfEdgeStartVertex(e2) == vertex.index)
                    out_ = e2;

                if (e3.endVertex == vertex.index)
                    in_ = e3;
                else if (m_TearableBlueprintInstance.topology.GetHalfEdgeStartVertex(e3) == vertex.index)
                    out_ = e3;

                // stitch edges to new vertex:
                in_.endVertex = newVertex.index;
                m_TearableBlueprintInstance.topology.halfEdges[in_.index] = in_;
                newVertex.halfEdge = out_.index;

                // store edges to be updated:
                updatedEdgeIndices.UnionWith(new int[]
                {
                    in_.index, in_.pair, out_.index, out_.pair
                });
            }

            // add new vertex:
            m_TearableBlueprintInstance.topology.vertices.Add(newVertex);
            m_TearableBlueprintInstance.topology.restNormals.Add(m_TearableBlueprintInstance.topology.restNormals[vertexIndex]);
            m_TearableBlueprintInstance.topology.restOrientations.Add(m_TearableBlueprintInstance.topology.restOrientations[vertexIndex]);

            //TODO: update mesh info. (mesh cannot be closed now)

            return true;
        }


        private void UpdateTornDistanceConstraints(HashSet<int> updatedHalfEdges)
        {
            var distanceConstraints = GetConstraintsByType(Oni.ConstraintType.Distance) as ObiConstraints<ObiDistanceConstraintsBatch>;

            foreach (int halfEdgeIndex in updatedHalfEdges)
            {
                HalfEdgeMesh.HalfEdge e = m_TearableBlueprintInstance.topology.halfEdges[halfEdgeIndex];
                Vector2Int constraintDescriptor = m_TearableClothBlueprint.distanceConstraintMap[halfEdgeIndex];

                // skip edges with no associated constraint (border half-edges)
                if (constraintDescriptor.x > -1)
                {
                    // get batch and index of the constraint:
                    var batch = distanceConstraints.batches[constraintDescriptor.x] as ObiDistanceConstraintsBatch;
                    int index = batch.GetConstraintIndex(constraintDescriptor.y);

                    // update constraint particle indices:
                    batch.particleIndices[index * 2] = m_TearableBlueprintInstance.topology.GetHalfEdgeStartVertex(e);
                    batch.particleIndices[index * 2 + 1] = e.endVertex;

                    // make sure the constraint is active, in case it is a newly added one.
                    batch.ActivateConstraint(index);
                }

                // update deformable triangles:
                if (e.indexInFace > -1)
                {
                    m_TearableBlueprintInstance.deformableTriangles[e.face * 3 + e.indexInFace] = e.endVertex;
                }
            }
        }

        private void UpdateTornBendConstraints(int splitActorIndex)
        {
            var bendConstraints = GetConstraintsByType(Oni.ConstraintType.Bending) as ObiConstraints<ObiBendConstraintsBatch>;

            foreach (ObiBendConstraintsBatch batch in bendConstraints.batches)
            {
                // iterate in reverse order so that swapping due to deactivation does not cause us to skip constraints.
                for (int i = batch.activeConstraintCount - 1; i >= 0; --i)
                {
                    if (batch.particleIndices[i * 3] == splitActorIndex ||
                        batch.particleIndices[i * 3 + 1] == splitActorIndex ||
                        batch.particleIndices[i * 3 + 2] == splitActorIndex)
                    {
                        batch.DeactivateConstraint(i);
                    }
                }
            }
        }

        public override void UpdateDeformableTriangles()
        {
            if (m_TearableBlueprintInstance != null && m_TearableBlueprintInstance.deformableTriangles != null)
            {
                // Send deformable triangle indices to the solver:
                int[] solverTriangles = new int[m_TearableBlueprintInstance.deformableTriangles.Length];
                for (int i = 0; i < m_TearableBlueprintInstance.deformableTriangles.Length; ++i)
                {
                    solverTriangles[i] = solverIndices[m_TearableBlueprintInstance.deformableTriangles[i]];
                }
                m_Solver.implementation.SetDeformableTriangles(solverTriangles, solverTriangles.Length / 3, trianglesOffset);
            }

            solver.dirtyConstraints |= (1 << (int)Oni.ConstraintType.Distance) | (1 << (int)Oni.ConstraintType.Bending);
        }

        public void OnDrawGizmosSelected()
        {

            /*if (solver == null || !isLoaded) return;

            Color[] co = new Color[12]{
                Color.red,
                Color.yellow,
                Color.blue,
                Color.white,
                Color.black,
                Color.green,
                Color.cyan,
                Color.magenta,
                Color.gray,
                new Color(1,0.7f,0.1f),
                new Color(0.1f,0.6f,0.5f),
                new Color(0.8f,0.1f,0.6f)
            };

            var constraints = GetConstraintsByType(Oni.ConstraintType.Distance) as ObiConstraints<ObiDistanceConstraintsBatch>;


            int j = 0;
            foreach (ObiDistanceConstraintsBatch batch in constraints.batches){

                //Gizmos.color = Color.green;//co[j%12];



                for (int i = 0; i < batch.activeConstraintCount; ++i)
                {

                    Gizmos.color = new Color(0, 0, 1, 0.75f);//co[j % 12];
                    if (j == btch && i == ctr)
                        Gizmos.color = Color.green;

                    Gizmos.DrawLine(solver.positions[batch.particleIndices[i*2]],
                                    solver.positions[batch.particleIndices[i*2+1]]);
                }
                j++;
            }




            /*if (!InSolver) return;

            var constraints = GetConstraints(Oni.ConstraintType.Bending) as ObiRuntimeConstraints<ObiBendConstraintsBatch>;

            int j = 0;
            foreach (ObiBendConstraintsBatch batch in constraints.GetBatches())
            {

                for (int i = 0; i < batch.activeConstraintCount; ++i)
                {
                    Gizmos.color = new Color(1,0,0,0.2f);//co[j % 12];
                    if (j == btch && i == ctr)
                        Gizmos.color = Color.green;
                    
                    Gizmos.DrawLine(GetParticlePosition(batch.springIndices[i * 2]),
                                    GetParticlePosition(batch.springIndices[i * 2 + 1]));
                }
                j++;
            }*/


        }

        int btch = 0;
        int ctr = 0;
        public void Update()
        {

            /*var constraints = GetConstraintsByType(Oni.ConstraintType.Distance) as ObiRuntimeConstraints<ObiDistanceConstraintsBatch>;

            if (Input.GetKeyDown(KeyCode.UpArrow)){
                ctr++;
                if (ctr >= constraints.GetBatches()[btch].activeConstraintCount)
                {
                    btch++;
                    ctr = 0;
                }
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ctr--;
                if (ctr < 0)
                {
                    btch--;
                    ctr = constraints.GetBatches()[btch].activeConstraintCount-1;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space)) {

                Tear(new StructuralConstraint(constraints.GetBatches()[btch] as IStructuralConstraintBatch,ctr,0));
                solver.UpdateActiveParticles();

                UpdateDeformableTriangles();
            }*/

        }

    }

}