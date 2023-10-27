using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{
    public abstract class ObiClothBlueprintBase : ObiMeshBasedActorBlueprint
    {
        [SerializeField] [HideInInspector] public HalfEdgeMesh topology;                    /**< Topology generated from the input mesh.*/

        [HideInInspector] public int[] deformableTriangles = null;    /**< Indices of deformable triangles (3 per triangle)*/
        [HideInInspector] public Vector3[] restNormals = null;
        [HideInInspector] public float[] areaContribution = null;           /**< How much mesh surface area each particle represents.*/

        public const float DEFAULT_PARTICLE_MASS = 0.1f;

        public HalfEdgeMesh Topology
        {
            get { return topology; }
        }

        protected override void SwapWithFirstInactiveParticle(int index)
        {
            base.SwapWithFirstInactiveParticle(index);

            areaContribution.Swap(index, m_ActiveParticleCount);
            restNormals.Swap(index, m_ActiveParticleCount);

            // Keep topology in sync:
            if (topology != null && topology.containsData)
            {
                topology.SwapVertices(index, m_ActiveParticleCount);
            }

            // Keep deformable triangles in sync:
            for (int i = 0; i < deformableTriangles.Length; ++i)
            {
                if (deformableTriangles[i] == index)
                    deformableTriangles[i] = m_ActiveParticleCount;
                else if (deformableTriangles[i] == m_ActiveParticleCount)
                    deformableTriangles[i] = index;
            }
        }

        protected virtual IEnumerator GenerateDeformableTriangles() 
        {
            deformableTriangles = new int[topology.faces.Count * 3];
            restNormals = new Vector3[topology.vertices.Count];

            // Generate deformable triangles:
            for (int i = 0; i < topology.faces.Count; i++)
            {
                HalfEdgeMesh.Face face = topology.faces[i];

                HalfEdgeMesh.HalfEdge e1 = topology.halfEdges[face.halfEdge];
                HalfEdgeMesh.HalfEdge e2 = topology.halfEdges[e1.nextHalfEdge];
                HalfEdgeMesh.HalfEdge e3 = topology.halfEdges[e2.nextHalfEdge];

                deformableTriangles[i * 3] = e1.endVertex;
                deformableTriangles[i * 3 + 1] = e2.endVertex;
                deformableTriangles[i * 3 + 2] = e3.endVertex;

                Vector3 v1 = positions[e1.endVertex];
                Vector3 v2 = positions[e2.endVertex];
                Vector3 v3 = positions[e3.endVertex];

                Vector3 n = Vector3.Cross(v2 - v1, v3 - v1);

                restNormals[e1.endVertex] += n;
                restNormals[e2.endVertex] += n;
                restNormals[e3.endVertex] += n;

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating deformable geometry...", i / (float)topology.faces.Count);
            }

            for (int i = 0; i < restNormals.Length; ++i)
                restNormals[i].Normalize();
        }

        protected virtual IEnumerator CreateSimplices()
        {
            triangles = new int[topology.faces.Count * 3];
            restNormals = new Vector3[topology.vertices.Count];

            // Generate deformable triangles:
            for (int i = 0; i < topology.faces.Count; i++)
            {
                HalfEdgeMesh.Face face = topology.faces[i];

                HalfEdgeMesh.HalfEdge e1 = topology.halfEdges[face.halfEdge];
                HalfEdgeMesh.HalfEdge e2 = topology.halfEdges[e1.nextHalfEdge];
                HalfEdgeMesh.HalfEdge e3 = topology.halfEdges[e2.nextHalfEdge];

                triangles[i * 3] = e1.endVertex;
                triangles[i * 3 + 1] = e2.endVertex;
                triangles[i * 3 + 2] = e3.endVertex;

                Vector3 v1 = positions[e1.endVertex];
                Vector3 v2 = positions[e2.endVertex];
                Vector3 v3 = positions[e3.endVertex];

                Vector3 n = Vector3.Cross(v2 - v1, v3 - v1);

                restNormals[e1.endVertex] += n;
                restNormals[e2.endVertex] += n;
                restNormals[e3.endVertex] += n;

                if (i % 500 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiCloth: generating deformable geometry...", i / (float)topology.faces.Count);
            }

            for (int i = 0; i < restNormals.Length; ++i)
                restNormals[i].Normalize();
        }
    }
}