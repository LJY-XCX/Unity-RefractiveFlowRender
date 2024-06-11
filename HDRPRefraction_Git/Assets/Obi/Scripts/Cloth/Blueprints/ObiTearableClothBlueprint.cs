using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{

    [CreateAssetMenu(fileName = "tearable cloth blueprint", menuName = "Obi/Tearable Cloth Blueprint", order = 121)]
    public class ObiTearableClothBlueprint : ObiClothBlueprint
    {
        [Tooltip("Amount of memory preallocated to create extra particles and mesh data when tearing the cloth. 0 means no extra memory will be allocated, and the cloth will not be tearable. 1 means all cloth triangles will be fully tearable.")]
        [Range(0, 1)]
        public float tearCapacity = 0.5f;

        [HideInInspector][SerializeField] private int pooledParticles = 0;

        [HideInInspector] public float[] tearResistance;                                 /**< Per-particle tear resistance.*/
        [HideInInspector][SerializeField] public Vector2Int[] distanceConstraintMap;     /** constraintHalfEdgeMap[half-edge index] = distance constraint index, or -1 if there's no constraint. 
                                                                                               Each initial constraint is the lower-index of each pair of half-edges. When a half-edge is split during
                                                                                               tearing, one of the two half-edges gets its constraint updated and the other gets a new constraint.*/

        public int PooledParticles{
            get { return pooledParticles; }
        }

        public override bool usesTethers
        {
            get { return false; }
        }

        protected override IEnumerator Initialize()
        {
            /**
             * Have a map for half-edge->constraint.
             * Initially create all constraints and pre-cook them.
             * Constraints at each side of the same edge, are in different batches.
             */

            if (inputMesh == null || !inputMesh.isReadable)
            {
                // TODO: return an error in the coroutine.
                Debug.LogError("The input mesh is null, or not readable.");
                yield break;
            }

            ClearParticleGroups();

            topology = new HalfEdgeMesh();
            topology.inputMesh = inputMesh;
            topology.Generate();


            pooledParticles = (int)((topology.faces.Count * 3 - topology.vertices.Count) * tearCapacity);
            int totalParticles = topology.vertices.Count + pooledParticles;

            positions = new Vector3[totalParticles];
            restPositions = new Vector4[totalParticles];
            velocities = new Vector3[totalParticles];
            invMasses = new float[totalParticles];
            principalRadii = new Vector3[totalParticles];
            filters = new int[totalParticles];
            colors = new Color[totalParticles];

            areaContribution = new float[totalParticles];
            tearResistance = new float[totalParticles];

            // Create a particle for each vertex:
            m_ActiveParticleCount = topology.vertices.Count;
            for (int i = 0; i < topology.vertices.Count; i++)
            {
                HalfEdgeMesh.Vertex vertex = topology.vertices[i];

                // Get the particle's area contribution.
                areaContribution[i] = 0;
                foreach (HalfEdgeMesh.Face face in topology.GetNeighbourFacesEnumerator(vertex))
                {
                    areaContribution[i] += topology.GetFaceArea(face) / 3;
                }

                // Get the shortest neighbour edge, particle radius will be half of its length.
                float minEdgeLength = Single.MaxValue;
                foreach (HalfEdgeMesh.HalfEdge edge in topology.GetNeighbourEdgesEnumerator(vertex))
                {

                    // vertices at each end of the edge:
                    Vector3 v1 = Vector3.Scale(scale, topology.vertices[topology.GetHalfEdgeStartVertex(edge)].position);
                    Vector3 v2 = Vector3.Scale(scale, topology.vertices[edge.endVertex].position);

                    minEdgeLength = Mathf.Min(minEdgeLength, Vector3.Distance(v1, v2));
                }

                tearResistance[i] = 1;
                invMasses[i] = 1;//(/*skinnedMeshRenderer == null &&*/ areaContribution[i] > 0) ? (1.0f / (DEFAULT_PARTICLE_MASS * areaContribution[i])) : 0;
                positions[i] = Vector3.Scale(scale,vertex.position);
                restPositions[i] = positions[i];
                restPositions[i][3] = 1; // activate rest position.
                principalRadii[i] = Vector3.one * minEdgeLength * 0.5f;
                filters[i] = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 1);
                colors[i] = Color.white;

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating particles...", i / (float)topology.vertices.Count);
            }

            IEnumerator dt = GenerateDeformableTriangles();

            while (dt.MoveNext())
                yield return dt.Current;

            //Create distance constraints:
            IEnumerator dc = CreateDistanceConstraints();

            while (dc.MoveNext())
                yield return dc.Current;

            // Create aerodynamic constraints:
            IEnumerator ac = CreateAerodynamicConstraints();

            while (ac.MoveNext())
                yield return ac.Current;
            
            //Create bending constraints:
            IEnumerator bc = CreateBendingConstraints();

            while (bc.MoveNext())
                yield return bc.Current;

        }

        protected override IEnumerator CreateDistanceConstraints()
        {
            // prepare an array that maps from half edge index to <batch, constraintId>
            distanceConstraintMap = new Vector2Int[topology.halfEdges.Count];
            for (int i = 0; i < distanceConstraintMap.Length; i++) 
                distanceConstraintMap[i] = new Vector2Int(-1,-1);

            //Create distance constraints, one for each half-edge.
            distanceConstraintsData = new ObiDistanceConstraintsData();

            List<int> edges = topology.GetEdgeList();

            IEnumerator dc = CreateInitialDistanceConstraints(edges);
            while (dc.MoveNext())
                yield return dc.Current;

            dc = CreatePooledDistanceConstraints(edges);
            while (dc.MoveNext())
                yield return dc.Current;
        }

        private IEnumerator CreateInitialDistanceConstraints(List<int> edges)
        {
            List<int> particleIndices = new List<int>();
            List<int> constraintIndices = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                HalfEdgeMesh.HalfEdge hedge = topology.halfEdges[edges[i]];

                // ignore borders:
                if (hedge.face < 0)
                    continue;

                particleIndices.Add(topology.GetHalfEdgeStartVertex(hedge));
                particleIndices.Add(hedge.endVertex);
                constraintIndices.Add(constraintIndices.Count * 2);

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating structural constraints...", i / (float)topology.halfEdges.Count);
            }
            constraintIndices.Add(constraintIndices.Count * 2);

            int[] constraintColors = GraphColoring.Colorize(particleIndices.ToArray(), constraintIndices.ToArray());

            for (int i = 0; i < constraintColors.Length; ++i)
            {
                int color = constraintColors[i];
                int cIndex = constraintIndices[i];

                // Add a new batch if needed:
                if (color >= distanceConstraintsData.GetBatchCount())
                    distanceConstraintsData.AddBatch(new ObiDistanceConstraintsBatch());

                HalfEdgeMesh.HalfEdge hedge = topology.halfEdges[edges[i]];
                HalfEdgeMesh.Vertex startVertex = topology.vertices[topology.GetHalfEdgeStartVertex(hedge)];
                HalfEdgeMesh.Vertex endVertex = topology.vertices[hedge.endVertex];

                distanceConstraintsData.batches[color].AddConstraint(new Vector2Int(particleIndices[cIndex], particleIndices[cIndex + 1]),
                                                                     Vector3.Distance(Vector3.Scale(scale,startVertex.position), Vector3.Scale(scale, endVertex.position)));


                distanceConstraintMap[hedge.index] = new Vector2Int(color, distanceConstraintsData.batches[color].constraintCount - 1);
            }

            // Set initial amount of active constraints:
            for (int i = 0; i < distanceConstraintsData.batches.Count; ++i)
            {
                distanceConstraintsData.batches[i].activeConstraintCount = distanceConstraintsData.batches[i].constraintCount;
            }
        }

        private IEnumerator CreatePooledDistanceConstraints(List<int> edges)
        {
            List<int> particleIndices = new List<int>();
            List<int> constraintIndices = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                HalfEdgeMesh.HalfEdge hedge = topology.halfEdges[topology.halfEdges[edges[i]].pair];

                // ignore borders:
                if (hedge.face < 0)
                    continue;

                particleIndices.Add(topology.GetHalfEdgeStartVertex(hedge));
                particleIndices.Add(hedge.endVertex);
                constraintIndices.Add(constraintIndices.Count * 2);

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating structural constraints...", i / (float)topology.halfEdges.Count);
            }
            constraintIndices.Add(constraintIndices.Count * 2);

            int[] constraintColors = GraphColoring.Colorize(particleIndices.ToArray(), constraintIndices.ToArray());
            int batchCount = distanceConstraintsData.batches.Count;

            int j = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                HalfEdgeMesh.HalfEdge hedge = topology.halfEdges[topology.halfEdges[edges[i]].pair];

                if (hedge.face < 0)
                    continue;

                int color = batchCount + constraintColors[j];
                int cIndex = constraintIndices[j];

                // Add a new batch if needed:
                if (color >= distanceConstraintsData.GetBatchCount())
                    distanceConstraintsData.AddBatch(new ObiDistanceConstraintsBatch());

                HalfEdgeMesh.Vertex startVertex = topology.vertices[topology.GetHalfEdgeStartVertex(hedge)];
                HalfEdgeMesh.Vertex endVertex = topology.vertices[hedge.endVertex];

                distanceConstraintsData.batches[color].AddConstraint(new Vector2Int(particleIndices[cIndex], particleIndices[cIndex + 1]),
                                                                     Vector3.Distance(Vector3.Scale(scale, startVertex.position), Vector3.Scale(scale,endVertex.position)));


                distanceConstraintMap[hedge.index] = new Vector2Int(color, distanceConstraintsData.batches[color].constraintCount - 1);

                ++j;
            }
        }
    }
}