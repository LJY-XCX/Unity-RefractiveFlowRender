using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Obi
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public abstract class ObiClothRendererMeshFilter : ObiClothRendererBase
    {
        MeshFilter filter;
        protected override void Awake()
        {
            filter = GetComponent<MeshFilter>();
            base.Awake();
        }

        protected override void OnBlueprintLoaded(ObiActor actor, ObiActorBlueprint blueprint)
		{
            // destroy old cloth mesh instance:
            if (clothMesh != null)
                DestroyImmediate(clothMesh);

            if (cloth.clothBlueprintBase != null && cloth.clothBlueprintBase.inputMesh != null)
            {

                // create a new instance of the shared mesh:
                clothMesh = Instantiate(cloth.clothBlueprintBase.inputMesh);
                clothMesh.MarkDynamic();
                GetClothMeshData();

                // assign it to the mesh filter:
                filter.sharedMesh = clothMesh;

                cloth.clothBlueprintBase.inputMesh.GetNormals(restNormals);
                cloth.clothBlueprintBase.inputMesh.GetTangents(restTangents);
            }
            else // if there's no blueprint present, or no mesh in the blueprint, set our mesh instance and the filter's mesh to null.
            {
                clothMesh = null;
                filter.sharedMesh = null;
            }
		}

        protected override void OnBlueprintUnloaded(ObiActor actor, ObiActorBlueprint blueprint)
        {
            if (!Application.isPlaying)
            {
                clothMesh = null;
                filter.sharedMesh = null;
            }
        }

    }
}