using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi Tearable Cloth Renderer", 904)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(ObiTearableCloth))]
    public class ObiTearableClothRenderer : ObiClothRendererMeshFilter
    {
        private List<int> clothTriangles = new List<int>();
        private List<Vector2> clothUV1 = new List<Vector2>();
        private List<Vector2> clothUV2 = new List<Vector2>();
        private List<Vector2> clothUV3 = new List<Vector2>();
        private List<Vector2> clothUV4 = new List<Vector2>();

        public override HalfEdgeMesh topology
        {
            get { return cloth != null ? ((ObiTearableClothBlueprint)((ObiTearableCloth)cloth).blueprint).topology : null; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ((ObiTearableCloth)cloth).OnClothTorn += UpdateMesh;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ((ObiTearableCloth)cloth).OnClothTorn -= UpdateMesh;
        }

        protected override void GetClothMeshData()
        {
            base.GetClothMeshData();
            if (clothMesh != null)
            {
                clothMesh.GetUVs(0, clothUV1);
                clothMesh.GetUVs(1, clothUV2);
                clothMesh.GetUVs(2, clothUV2);
                clothMesh.GetUVs(3, clothUV2);
                clothMesh.GetTriangles(clothTriangles, 0);
            }
        }

        protected override void SetClothMeshData()
        {
            base.SetClothMeshData();
            if (clothMesh != null)
            {
                clothMesh.SetUVs(0, clothUV1);
                clothMesh.SetUVs(1, clothUV2);
                clothMesh.SetUVs(2, clothUV3);
                clothMesh.SetUVs(3, clothUV4);
                clothMesh.SetTriangles(clothTriangles, 0);
            }
        }

        public void UpdateMesh(object sender, ObiTearableCloth.ObiClothTornEventArgs args)
        {
            HashSet<int> tornVertices = GetTornMeshVertices(args.particleIndex, args.updatedFaces);
            UpdateTornMeshVertices(tornVertices, args.updatedFaces);
            SetClothMeshData();
        }

        private HashSet<int> GetTornMeshVertices(int vertexIndex, List<HalfEdgeMesh.Face> updatedFaces)
        {
            HashSet<int> meshVertices = new HashSet<int>();

            foreach (HalfEdgeMesh.Face face in updatedFaces)
            {
                int triIndex = face.index * 3;
                int v1 = clothTriangles[triIndex];
                int v2 = clothTriangles[triIndex + 1];
                int v3 = clothTriangles[triIndex + 2];

                if (topology.rawToWelded[v1] == vertexIndex)
                    meshVertices.Add(v1);
                else if (topology.rawToWelded[v2] == vertexIndex)
                    meshVertices.Add(v2);
                else if (topology.rawToWelded[v3] == vertexIndex)
                    meshVertices.Add(v3);
            }
            return meshVertices;
        }

        private void UpdateTornMeshVertices(HashSet<int> meshVertices, List<HalfEdgeMesh.Face> updatedFaces)
        {
            foreach (int j in meshVertices)
            {
                if (j < clothVertices.Count) clothVertices.Add(clothVertices[j]);
                if (j < clothNormals.Count) clothNormals.Add(clothNormals[j]);
                if (j < clothTangents.Count) clothTangents.Add(clothTangents[j]);
                if (j < clothColors.Count) clothColors.Add(clothColors[j]);
                if (j < clothUV1.Count) clothUV1.Add(clothUV1[j]);
                if (j < clothUV2.Count) clothUV2.Add(clothUV2[j]);
                if (j < clothUV3.Count) clothUV3.Add(clothUV3[j]);
                if (j < clothUV4.Count) clothUV4.Add(clothUV4[j]);

                if (j < restNormals.Count) restNormals.Add(restNormals[j]);
                if (j < restTangents.Count) restTangents.Add(restTangents[j]);

                // map the new mesh vertex to the last topology vertex (the one we just created):
                topology.rawToWelded.Add(topology.vertices.Count - 1);

                // re-wire mesh triangles, so that they reference the new mesh vertices:
                foreach (HalfEdgeMesh.Face face in updatedFaces)
                {
                    int triIndex = face.index * 3;
                    if (clothTriangles[triIndex] == j) clothTriangles[triIndex] = clothVertices.Count - 1;
                    if (clothTriangles[triIndex + 1] == j) clothTriangles[triIndex + 1] = clothVertices.Count - 1;
                    if (clothTriangles[triIndex + 2] == j) clothTriangles[triIndex + 2] = clothVertices.Count - 1;
                }
            }
        }
    }
}