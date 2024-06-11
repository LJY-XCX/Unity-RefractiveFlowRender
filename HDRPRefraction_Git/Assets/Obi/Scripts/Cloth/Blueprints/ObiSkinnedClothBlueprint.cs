using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{

    [CreateAssetMenu(fileName = "skinned cloth blueprint", menuName = "Obi/Skinned Cloth Blueprint", order = 122)]
    public class ObiSkinnedClothBlueprint : ObiClothBlueprint
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
                    Vector3 v1 = Vector3.Scale(scale, topology.vertices[topology.GetHalfEdgeStartVertex(edge)].position);
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

           
            // Create skin constraints:
            IEnumerator sc = CreateSkinConstraints();

            while (sc.MoveNext())
                yield return sc.Current;

        }

        protected IEnumerator CreateSkinConstraints()
        {
            skinConstraintsData = new ObiSkinConstraintsData();
            ObiSkinConstraintsBatch skinBatch = new ObiSkinConstraintsBatch();
            skinConstraintsData.AddBatch(skinBatch);

            for (int i = 0; i < topology.vertices.Count; ++i)
            {

                skinBatch.AddConstraint(i, Vector3.Scale(scale,topology.vertices[i].position), Vector3.up, 0.05f, 0.1f, 0, 0);
                skinBatch.activeConstraintCount++;

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating skin constraints...", i / (float)topology.vertices.Count);
            }

            Vector3[] normals = topology.inputMesh.normals;
            for (int i = 0; i < normals.Length; ++i)
            {
                skinBatch.skinNormals[topology.rawToWelded[i]] = normals[i];
            }
        }

    }
}