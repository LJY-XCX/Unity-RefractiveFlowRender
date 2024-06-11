using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

    /**
     * This class allows a mesh (skinned or not) to follow the simulation performed by a different cloth object. The most
     * common use case is having a high-poly mesh mimic the movement of a low-poly cloth simulation. 
     */

    [DisallowMultipleComponent]
    [AddComponentMenu("Physics/Obi/Obi Cloth Proxy", 906)]
    public class ObiClothProxy : MonoBehaviour
    {
        [SerializeField] [HideInInspector] private ObiClothRendererBase m_Master;
        public ObiTriangleSkinMap skinMap;
        private Mesh slaveMesh;

        private Vector3[] slavePositions;
        private Vector3[] slaveNormals;
        private Vector4[] slaveTangents;

        private MeshFilter meshFilter;

        public ObiClothRendererBase master
        {
            set
            {
                if (m_Master != null)
                    m_Master.OnRendererUpdated -= UpdateSkinning;

                m_Master = value;

                if (m_Master != null && isActiveAndEnabled)
                    m_Master.OnRendererUpdated += UpdateSkinning;

            }
            get { return m_Master; }
        }

        public void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (m_Master != null)
                m_Master.OnRendererUpdated += UpdateSkinning;
        }

        public void OnDisable()
        {
            if (m_Master != null)
                m_Master.OnRendererUpdated -= UpdateSkinning;
        }


        private void GetSlaveMeshIfNeeded()
        {
            if (slaveMesh == null)
            {
                // If the proxy is used on the cloth itself, pick the mesh from the master renderer.
                if (gameObject == m_Master.gameObject)
                {
                    slaveMesh = m_Master.clothMesh;
                }
                else if (meshFilter != null)// Try to pick the mesh from a mesh filter.
                {
                    slaveMesh = meshFilter.mesh;
                }
            }
        }

        public void UpdateSkinning(ObiActor actor)
        {
            GetSlaveMeshIfNeeded();
            ObiClothBase masterCloth = m_Master.GetComponent<ObiClothBase>();

            if (skinMap.bound && slaveMesh != null && masterCloth.isLoaded)
            {
                var solver = masterCloth.solver;

                Matrix4x4 s2l;
                if (gameObject == m_Master.gameObject)
                    s2l = m_Master.renderMatrix * solver.transform.localToWorldMatrix;
                else
                    s2l = transform.worldToLocalMatrix * solver.transform.localToWorldMatrix;

                slavePositions = slaveMesh.vertices;
                slaveNormals = slaveMesh.normals;
                slaveTangents = slaveMesh.tangents;

                Vector3 skinnedPos = Vector3.zero;
                Vector3 skinnedNormal = Vector3.zero;
                Vector3 skinnedTangent = Vector3.zero;

                for (int i = 0; i < skinMap.skinnedVertices.Count; ++i)
                {
                    var data = skinMap.skinnedVertices[i];
                    int firstVertex = data.masterTriangleIndex;

                    int t1 = masterCloth.clothBlueprintBase.deformableTriangles[firstVertex];
                    int t2 = masterCloth.clothBlueprintBase.deformableTriangles[firstVertex + 1];
                    int t3 = masterCloth.clothBlueprintBase.deformableTriangles[firstVertex + 2];

                    // get solver indices for each particle:
                    int s1 = masterCloth.solverIndices[t1];
                    int s2 = masterCloth.solverIndices[t2];
                    int s3 = masterCloth.solverIndices[t3];

                    // get master particle positions/normals:
                    Vector3 p1 = solver.renderablePositions[s1];
                    Vector3 p2 = solver.renderablePositions[s2];
                    Vector3 p3 = solver.renderablePositions[s3];

                    Vector3 n1 = solver.normals[s1];
                    Vector3 n2 = solver.normals[s2];
                    Vector3 n3 = solver.normals[s3];

                    ObiVectorMath.BarycentricInterpolation(p1, p2, p3, n1, n2, n3, data.position.barycentricCoords, data.position.height, ref skinnedPos);

                    ObiVectorMath.BarycentricInterpolation(p1, p2, p3, n1, n2, n3, data.normal.barycentricCoords, data.normal.height, ref skinnedNormal);

                    ObiVectorMath.BarycentricInterpolation(p1, p2, p3, n1, n2, n3, data.tangent.barycentricCoords, data.tangent.height, ref skinnedTangent);

                    // update slave data arrays:
                    slavePositions[data.slaveIndex] = s2l.MultiplyPoint3x4(skinnedPos);
                    slaveNormals[data.slaveIndex] = s2l.MultiplyVector(skinnedNormal - skinnedPos);

                    Vector3 tangent = s2l.MultiplyVector(skinnedTangent - skinnedPos);
                    slaveTangents[data.slaveIndex] = new Vector4(tangent.x, tangent.y, tangent.z, slaveTangents[data.slaveIndex].w);
                }

                slaveMesh.vertices = slavePositions;
                slaveMesh.normals = slaveNormals;
                slaveMesh.tangents = slaveTangents;
                slaveMesh.RecalculateBounds();
            }
        }

    }
}
