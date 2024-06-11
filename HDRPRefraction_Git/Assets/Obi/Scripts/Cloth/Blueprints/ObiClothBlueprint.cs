using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{

    [CreateAssetMenu(fileName = "cloth blueprint", menuName = "Obi/Cloth Blueprint", order = 120)]
    public class ObiClothBlueprint : ObiClothBlueprintBase
    {

        public override bool usesTethers
        {
            get { return true; }
        }
        
        protected override IEnumerator Initialize(){

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

            positions = new Vector3[topology.vertices.Count];
            restPositions = new Vector4[topology.vertices.Count];
            velocities = new Vector3[topology.vertices.Count];
            invMasses = new float[topology.vertices.Count];
            principalRadii = new Vector3[topology.vertices.Count];
            filters = new int[topology.vertices.Count];
            colors = new Color[topology.vertices.Count];

            areaContribution = new float[topology.vertices.Count];

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
                    Vector3 v1 = Vector3.Scale(scale,topology.vertices[topology.GetHalfEdgeStartVertex(edge)].position);
                    Vector3 v2 = Vector3.Scale(scale,topology.vertices[edge.endVertex].position);

                    minEdgeLength = Mathf.Min(minEdgeLength, Vector3.Distance(v1, v2));
                }

                invMasses[i] = (/*skinnedMeshRenderer == null &&*/ areaContribution[i] > 0) ? (1.0f / (DEFAULT_PARTICLE_MASS * areaContribution[i])) : 0;
                positions[i] = Vector3.Scale(scale,vertex.position);
                restPositions[i] = positions[i];
                restPositions[i][3] = 1; // activate rest position.
                principalRadii[i] = Vector3.one * minEdgeLength * 0.5f;
                filters[i] = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 1);
                colors[i] = Color.white;

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating particles...", i / (float)topology.vertices.Count);
            }

            // Deformable triangles: TODO: replaced by simplices.
            IEnumerator dt = GenerateDeformableTriangles(); 

            while (dt.MoveNext())
                yield return dt.Current;

            // Create triangle simplices:
            IEnumerator t = CreateSimplices();

            while (t.MoveNext())
                yield return t.Current;

            // Create distance constraints:
            IEnumerator dc = CreateDistanceConstraints();

            while (dc.MoveNext())
                yield return dc.Current;

            // Create aerodynamic constraints:
            IEnumerator ac = CreateAerodynamicConstraints();

            while (ac.MoveNext())
                yield return ac.Current;
            
            // Create bending constraints:
            IEnumerator bc = CreateBendingConstraints();

            while (bc.MoveNext())
                yield return bc.Current;

            // Create volume constraints:
            IEnumerator vc = CreateVolumeConstraints();

            while (vc.MoveNext())
                yield return vc.Current;

        }

        protected virtual IEnumerator CreateDistanceConstraints()
        {
            //Create distance constraints:
            List<int> edges = topology.GetEdgeList();

            distanceConstraintsData = new ObiDistanceConstraintsData();

            List<int> particleIndices = new List<int>();
            List<int> constraintIndices = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                HalfEdgeMesh.HalfEdge hedge = topology.halfEdges[edges[i]];

                particleIndices.Add(topology.GetHalfEdgeStartVertex(hedge));
                particleIndices.Add(hedge.endVertex);
                constraintIndices.Add(constraintIndices.Count * 2);

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating structural constraints...", i / (float)edges.Count);
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
                                                                         Vector3.Distance(Vector3.Scale(scale,startVertex.position), Vector3.Scale(scale,endVertex.position)));
            }

            // Set initial amount of active constraints:
            for (int i = 0; i < distanceConstraintsData.batches.Count; ++i)
            {
                distanceConstraintsData.batches[i].activeConstraintCount = distanceConstraintsData.batches[i].constraintCount;
            }
        }

        protected virtual IEnumerator CreateAerodynamicConstraints()
        {
            aerodynamicConstraintsData = new ObiAerodynamicConstraintsData();
            var aeroBatch = new ObiAerodynamicConstraintsBatch();
            aerodynamicConstraintsData.AddBatch(aeroBatch);

            for (int i = 0; i < topology.vertices.Count; i++)
            {
                aeroBatch.AddConstraint(i, areaContribution[i], 1, 1);

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating aerodynamic constraints...", i / (float)topology.vertices.Count);
            }

            // Set initial amount of active constraints:
            for (int i = 0; i < aerodynamicConstraintsData.batches.Count; ++i)
            {
                aerodynamicConstraintsData.batches[i].activeConstraintCount = aerodynamicConstraintsData.batches[i].constraintCount;
            }
        }

        protected virtual IEnumerator CreateBendingConstraints()
        {
            bendConstraintsData = new ObiBendConstraintsData();

            List<int> particleIndices = new List<int>();
            List<int> constraintIndices = new List<int>();

            Dictionary<int, int> cons = new Dictionary<int, int>();
            for (int i = 0; i < topology.vertices.Count; i++)
            {

                HalfEdgeMesh.Vertex vertex = topology.vertices[i];

                foreach (HalfEdgeMesh.Vertex n1 in topology.GetNeighbourVerticesEnumerator(vertex))
                {

                    float cosBest = 0;
                    HalfEdgeMesh.Vertex vBest = n1;

                    foreach (HalfEdgeMesh.Vertex n2 in topology.GetNeighbourVerticesEnumerator(vertex))
                    {
                        float cos = Vector3.Dot((n1.position - vertex.position).normalized,
                                                (n2.position - vertex.position).normalized);
                        if (cos < cosBest)
                        {
                            cosBest = cos;
                            vBest = n2;
                        }
                    }

                    if (!cons.ContainsKey(vBest.index) || cons[vBest.index] != n1.index)
                    {

                        cons[n1.index] = vBest.index;

                        particleIndices.Add(n1.index);
                        particleIndices.Add(vBest.index);
                        particleIndices.Add(vertex.index);
                        constraintIndices.Add(constraintIndices.Count * 3);
                    }

                }

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: adding bend constraints...", i / (float)topology.vertices.Count);
            }
            constraintIndices.Add(constraintIndices.Count * 3);

            int[] constraintColors = GraphColoring.Colorize(particleIndices.ToArray(), constraintIndices.ToArray());

            for (int i = 0; i < constraintColors.Length; ++i)
            {
                int color = constraintColors[i];
                int cIndex = constraintIndices[i];

                // Add a new batch if needed:
                if (color >= bendConstraintsData.GetBatchCount())
                    bendConstraintsData.AddBatch(new ObiBendConstraintsBatch());

                HalfEdgeMesh.Vertex n1 = topology.vertices[particleIndices[cIndex]];
                HalfEdgeMesh.Vertex vBest = topology.vertices[particleIndices[cIndex + 1]];
                HalfEdgeMesh.Vertex vertex = topology.vertices[particleIndices[cIndex + 2]];

                Vector3 n1Pos = Vector3.Scale(scale,n1.position);
                Vector3 bestPos = Vector3.Scale(scale,vBest.position);
                Vector3 vertexPos = Vector3.Scale(scale,vertex.position);

                float restBend = ObiUtils.RestBendingConstraint(n1Pos,bestPos,vertexPos);
                bendConstraintsData.batches[color].AddConstraint(new Vector3Int(particleIndices[cIndex], particleIndices[cIndex + 1], particleIndices[cIndex + 2]), restBend);

            }

            // Set initial amount of active constraints:
            for (int i = 0; i < bendConstraintsData.batches.Count; ++i)
            {
                bendConstraintsData.batches[i].activeConstraintCount = bendConstraintsData.batches[i].constraintCount;
            }
        }

        protected virtual IEnumerator CreateVolumeConstraints()
        {
            //Create pressure constraints if the mesh is closed:
            if (topology.closed)
            {
                volumeConstraintsData = new ObiVolumeConstraintsData();

                ObiVolumeConstraintsBatch volumeBatch = new ObiVolumeConstraintsBatch();
                volumeConstraintsData.AddBatch(volumeBatch);

                float avgInitialScale = (scale.x + scale.y + scale.z) * 0.33f;

                int[] triangleIndices = new int[topology.faces.Count * 3];
                for (int i = 0; i < topology.faces.Count; i++)
                {
                    HalfEdgeMesh.Face face = topology.faces[i];

                    HalfEdgeMesh.HalfEdge e1 = topology.halfEdges[face.halfEdge];
                    HalfEdgeMesh.HalfEdge e2 = topology.halfEdges[e1.nextHalfEdge];
                    HalfEdgeMesh.HalfEdge e3 = topology.halfEdges[e2.nextHalfEdge];

                    triangleIndices[i * 3] = e1.endVertex;
                    triangleIndices[i * 3 + 1] = e2.endVertex;
                    triangleIndices[i * 3 + 2] = e3.endVertex;

                    if (i % 500 == 0)
                        yield return new CoroutineJob.ProgressInfo("ObiCloth: generating volume constraints...", i / (float)topology.faces.Count);
                }

                volumeBatch.AddConstraint(triangleIndices, topology.volume * avgInitialScale);

                // Set initial amount of active constraints:
                for (int i = 0; i < volumeConstraintsData.batches.Count; ++i)
                {
                    volumeConstraintsData.batches[i].activeConstraintCount = volumeConstraintsData.batches[i].constraintCount;
                }
            }
        }

        public override void ClearTethers()
        {
            tetherConstraintsData.Clear();
        }

        private List<HashSet<int>> GenerateIslands(IEnumerable<int> particles, Func<int, bool> condition)
        {

            List<HashSet<int>> islands = new List<HashSet<int>>();

            // Partition fixed particles into islands:
            foreach (int i in particles)
            {

                HalfEdgeMesh.Vertex vertex = topology.vertices[i];

                if (condition != null && !condition(i)) continue;

                int assignedIsland = -1;

                // keep a list of islands to merge with ours:
                List<int> mergeableIslands = new List<int>();

                // See if any of our neighbors is part of an island:
                foreach (HalfEdgeMesh.Vertex n in topology.GetNeighbourVerticesEnumerator(vertex))
                {

                    for (int k = 0; k < islands.Count; ++k)
                    {

                        if (islands[k].Contains(n.index))
                        {

                            // if we are not in an island yet, pick this one:
                            if (assignedIsland < 0)
                            {
                                assignedIsland = k;
                                islands[k].Add(i);
                            }
                            // if we already are in an island, we will merge this newfound island with ours:
                            else if (assignedIsland != k && !mergeableIslands.Contains(k))
                            {
                                mergeableIslands.Add(k);
                            }
                        }
                    }
                }

                // merge islands with the assigned one:
                foreach (int merge in mergeableIslands)
                {
                    islands[assignedIsland].UnionWith(islands[merge]);
                }

                // remove merged islands:
                mergeableIslands.Sort();
                mergeableIslands.Reverse();
                foreach (int merge in mergeableIslands)
                {
                    islands.RemoveAt(merge);
                }

                // If no adjacent particle is in an island, create a new one:
                if (assignedIsland < 0)
                {
                    islands.Add(new HashSet<int>() { i });
                }
            }

            return islands;
        }

        /**
         * Automatically generates tether constraints for the cloth.
         * Partitions fixed particles into "islands", then generates up to maxTethers constraints for each 
         * particle, linking it to the closest point in each island.
         */
        public override void GenerateTethers(bool[] selected)
        {

            tetherConstraintsData = new ObiTetherConstraintsData();

            // generate disjoint groups of particles (islands)
            List<HashSet<int>> islands = GenerateIslands(System.Linq.Enumerable.Range(0, topology.vertices.Count), null);

            // generate tethers for each one:
            List<int> particleIndices = new List<int>();
            foreach (HashSet<int> island in islands)
                GenerateTethersForIsland(island,particleIndices,selected,4);

            // for tethers, it's easy to use the optimal amount of colors analytically.
            if (particleIndices.Count > 0)
            {
                int color = 0;
                int lastParticle = particleIndices[0];
                for (int i = 0; i < particleIndices.Count; i += 2)
                {

                    if (particleIndices[i] != lastParticle)
                    {
                        lastParticle = particleIndices[i];
                        color = 0;
                    }

                    // Add a new batch if needed:
                    if (color >= tetherConstraintsData.GetBatchCount())
                        tetherConstraintsData.AddBatch(new ObiTetherConstraintsBatch());

                    HalfEdgeMesh.Vertex startVertex = topology.vertices[particleIndices[i]];
                    HalfEdgeMesh.Vertex endVertex = topology.vertices[particleIndices[i + 1]];

                    tetherConstraintsData.batches[color].AddConstraint(new Vector2Int(particleIndices[i], particleIndices[i + 1]),
                                                                       Vector3.Distance(Vector3.Scale(scale, startVertex.position), Vector3.Scale(scale, endVertex.position)),
                                                                       1);
                    color++;
                }
            }

            // Set initial amount of active constraints:
            for (int i = 0; i < tetherConstraintsData.batches.Count; ++i)
            {
                tetherConstraintsData.batches[i].activeConstraintCount = tetherConstraintsData.batches[i].constraintCount;
            }
        }

        /**
         * This function generates tethers for a given set of particles, all belonging a connected graph. 
         * This is use ful when the cloth mesh is composed of several
         * disjoint islands, and we dont want tethers in one island to anchor particles to fixed particles in a different island.
         * 
         * Inside each island, fixed particles are partitioned again into "islands", then generates up to maxTethers constraints for each 
         * particle linking it to the closest point in each fixed island.
         */
        private void GenerateTethersForIsland(HashSet<int> particles, List<int> particleIndices, bool[] selected, int maxTethers)
        {

            if (maxTethers > 0)
            {

                List<HashSet<int>> fixedIslands = GenerateIslands(particles,(x => selected[x]));

                // Generate tether constraints:
                foreach (int i in particles)
                {
                    // Skip inactive particles.
                    if (!IsParticleActive(i) || selected[i]) 
                        continue;

                    List<KeyValuePair<float,int>> tethers = new List<KeyValuePair<float,int>>(fixedIslands.Count*maxTethers);

                    // Find the closest particle in each island, and add it to tethers.
                    foreach(HashSet<int> island in fixedIslands)
                    {
                        int closest = -1;
                        float minDistance = Mathf.Infinity;
                        foreach (int j in island)
                        {
                            float distance = (topology.vertices[i].position - topology.vertices[j].position).sqrMagnitude;
                            if (distance < minDistance && i != j)
                            {
                                minDistance = distance;
                                closest = j;
                            }
                        }
                        if (closest >= 0)
                            tethers.Add(new KeyValuePair<float,int>(minDistance, closest));
                    }

                    // Sort tether indices by distance:
                    tethers.Sort(
                    delegate(KeyValuePair<float,int> x, KeyValuePair<float,int> y)
                    {
                        return x.Key.CompareTo(y.Key);
                    }
                    );

                    // Create constraints for "maxTethers" closest anchor particles:
                    for (int k = 0; k < Mathf.Min(maxTethers,tethers.Count); ++k){
                        particleIndices.Add(i);
                        particleIndices.Add(tethers[k].Value); // the second particle is the anchor (assumed to be fixed)
                    }
                }
            }
        }
    }
}