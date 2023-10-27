using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi Skinned Cloth Renderer", 905)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(ObiSkinnedCloth))]
    public class ObiSkinnedClothRenderer : ObiClothRendererBase
    {
        private SkinnedMeshRenderer skin;
        private Matrix4x4 local2Root;
        private List<Material> rendererMaterials = new List<Material>();

        public override Matrix4x4 renderMatrix
        {
            get
            {
                if (skin.rootBone != null)
                    return skin.rootBone.worldToLocalMatrix;
                else
                    return cloth.transform.worldToLocalMatrix;
            }
        }

        protected override void Awake()
        {
            skin = GetComponent<SkinnedMeshRenderer>();
            base.Awake();
        }

        protected override void OnBlueprintLoaded(ObiActor actor, ObiActorBlueprint blueprint)
        {
            if (cloth.clothBlueprintBase != null && cloth.clothBlueprintBase.inputMesh != null)
            {
                cloth.clothBlueprintBase.inputMesh.GetNormals(restNormals);
                cloth.clothBlueprintBase.inputMesh.GetTangents(restTangents);
            }
        }

        protected override void SetupUpdate()
        {

            // reuse the same mesh used for baking the skinned pose. 
            // we do this every frame, to make sure non-simulated vertices are fully skinned.
            clothMesh = ((ObiSkinnedCloth)cloth).bakedMesh;
            GetClothMeshData();

            // update local space to root bone space matrix:
            local2Root = skin.rootBone.worldToLocalMatrix * skin.transform.localToWorldMatrix;

        }

        protected override void UpdateInactiveVertex(ObiSolver solver, int actorIndex, int meshVertexIndex)
        {
            var skinnedCloth = (ObiSkinnedCloth)cloth;

            clothVertices[meshVertexIndex] = local2Root.MultiplyPoint3x4(skinnedCloth.bakedVertices[meshVertexIndex]);
            clothNormals[meshVertexIndex] = local2Root.MultiplyVector(skinnedCloth.bakedNormals[meshVertexIndex]);
            Vector3 tangent = local2Root.MultiplyVector(skinnedCloth.bakedTangents[meshVertexIndex]);
            clothTangents[meshVertexIndex] = new Vector4(tangent.x, tangent.y, tangent.z, clothTangents[meshVertexIndex].w);
        }

        public override void UpdateRenderer(ObiActor actor)
        {

            base.UpdateRenderer(actor);

            if (Application.isPlaying && clothMesh != null && cloth.isLoaded)
            {
                // since the skinned mesh renderer won't accept a mesh with no bone weights, we need to render the mesh ourselves:
                skin.sharedMesh = null;

                skin.GetSharedMaterials(rendererMaterials);

                // Render all submeshes and materials:
                Matrix4x4 matrix = skin.rootBone.transform.localToWorldMatrix;
                int subMeshCount = clothMesh.subMeshCount;
                int drawcalls = Mathf.Max(subMeshCount, rendererMaterials.Count);

                for (int j = 0; j < drawcalls; ++j)
                {
                    if (j < rendererMaterials.Count)
                        Graphics.DrawMesh(clothMesh, matrix, rendererMaterials[j], gameObject.layer, null, Mathf.Min(j, subMeshCount - 1), null, skin.shadowCastingMode, skin.receiveShadows);
                }
            }
        }
    }
}