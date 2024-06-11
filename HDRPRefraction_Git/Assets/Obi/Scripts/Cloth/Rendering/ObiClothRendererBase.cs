using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Obi
{
    [ExecuteInEditMode]
    public abstract class ObiClothRendererBase : MonoBehaviour
    {

        static ProfilerMarker m_UpdateClothRendererPerfMarker = new ProfilerMarker("UpdateClothRenderer");

        public enum TangentSpaceUpdateMode
        {
            None,
            CopyNormalsFromSimulation,
            RecalculateNormalsFromMesh,
            RecalculateNormalsAndTangentsFromMesh,
            TransformNormalsAndTangents
        }

        protected ObiClothBase cloth;

        protected List<Vector3> restNormals = new List<Vector3>();
        protected List<Vector4> restTangents = new List<Vector4>();

        protected List<Vector3> clothVertices = new List<Vector3>();
        protected List<Vector3> clothNormals = new List<Vector3>();
        protected List<Vector4> clothTangents = new List<Vector4>();
        protected List<Color> clothColors = new List<Color>();

        public TangentSpaceUpdateMode tangentSpaceUpdate = TangentSpaceUpdateMode.TransformNormalsAndTangents;
        public bool transferParticleColors = false;

        public event ObiActor.ActorCallback OnRendererUpdated;

        private Matrix4x4 space;

        [HideInInspector] [NonSerialized] public Mesh clothMesh;

        public virtual HalfEdgeMesh topology
        {
            get { return cloth.clothBlueprintBase.topology; }
        }

        public virtual Matrix4x4 renderMatrix
        {
            get { return cloth.transform.worldToLocalMatrix; }
        }

        protected virtual void Awake()
        {
            cloth = GetComponent<ObiClothBase>();
            cloth.OnBlueprintLoaded += OnBlueprintLoaded;
            cloth.OnBlueprintUnloaded += OnBlueprintUnloaded;
            if (cloth.isLoaded)
                OnBlueprintLoaded(cloth, cloth.sourceBlueprint);
        }
        protected virtual void OnDestroy()
        {
            if (cloth != null)
            {
                cloth.OnBlueprintLoaded -= OnBlueprintLoaded;
                cloth.OnBlueprintUnloaded -= OnBlueprintUnloaded;
                DestroyImmediate(clothMesh);
            }
        }

        protected virtual void OnEnable()
        {
            cloth.OnInterpolate += UpdateRenderer;
        }

        protected virtual void OnDisable()
        {
            cloth.OnInterpolate -= UpdateRenderer;
        }

        protected virtual void GetClothMeshData()
        {
            if (clothMesh != null)
            {
                clothMesh.GetVertices(clothVertices);
                clothMesh.GetNormals(clothNormals);
                clothMesh.GetTangents(clothTangents);
                clothMesh.GetColors(clothColors);

                if (clothColors.Count == 0)
                {
                    clothColors.Clear();
                    clothColors.AddRange(Enumerable.Repeat(Color.white, clothVertices.Count));
                }
            }
        }

        protected virtual void SetClothMeshData()
        {
            if (clothMesh != null)
            {
                clothMesh.SetVertices(clothVertices);
                clothMesh.SetNormals(clothNormals);
                clothMesh.SetTangents(clothTangents);

                if (transferParticleColors)
                    clothMesh.SetColors(clothColors);
            }
        }

        protected virtual void OnBlueprintLoaded(ObiActor actor, ObiActorBlueprint blueprint) { }
        protected virtual void OnBlueprintUnloaded(ObiActor actor, ObiActorBlueprint blueprint) { }
        protected virtual void SetupUpdate() { }

        protected virtual void UpdateActiveVertex(ObiSolver solver, int actorIndex, int meshVertexIndex)
        {
            int solverIndex = cloth.solverIndices[actorIndex];

            // update vertex position:
            clothVertices[meshVertexIndex] = space.MultiplyPoint3x4(solver.renderablePositions.GetVector3(solverIndex));

            if (transferParticleColors)
                clothColors[meshVertexIndex] = solver.colors[solverIndex];

            if (tangentSpaceUpdate == TangentSpaceUpdateMode.CopyNormalsFromSimulation)
            {
                // no skinning, copy normal from simulation as-is.
                if (meshVertexIndex < clothNormals.Count)
                    clothNormals[meshVertexIndex] = space.MultiplyVector(solver.normals.GetVector3(solverIndex));
            }
            else if (tangentSpaceUpdate == TangentSpaceUpdateMode.TransformNormalsAndTangents)
            {
                // get first neighbour vertex index, use vector to it to build a skin basis.
                int neighbour = topology.halfEdges[topology.vertices[actorIndex].halfEdge].endVertex;

                // calculate orientation delta from current orientation and rest orientation:
                Vector3 surface = solver.renderablePositions.GetVector3(cloth.solverIndices[neighbour]) - solver.renderablePositions.GetVector3(solverIndex);
                Quaternion delta = Quaternion.LookRotation(solver.normals.GetVector3(solverIndex), surface) * topology.restOrientations[actorIndex];

                // skin normal and tangent:
                if (meshVertexIndex < restNormals.Count)
                    clothNormals[meshVertexIndex] = space.MultiplyVector(delta * restNormals[meshVertexIndex]);

                if (meshVertexIndex < restTangents.Count)
                {
                    Vector3 tangent = space.MultiplyVector(delta * restTangents[meshVertexIndex]);
                    clothTangents[meshVertexIndex] = new Vector4(tangent.x, tangent.y, tangent.z, clothTangents[meshVertexIndex].w);
                }
            }
        }

        protected virtual void UpdateInactiveVertex(ObiSolver solver, int actorIndex, int meshVertexIndex) { }

        public virtual void UpdateRenderer(ObiActor actor)
        {
            using (m_UpdateClothRendererPerfMarker.Auto())
            {
                SetupUpdate();

                // Only update the mesh if the blueprint is loaded
                if (Application.isPlaying && cloth.isLoaded && clothMesh != null)
                {
                    var solver = cloth.solver;
                    space = renderMatrix * solver.transform.localToWorldMatrix;

                    //Update mesh data:
                    for (int i = 0; i < clothVertices.Count; ++i)
                    {
                        int actorIndex = topology.rawToWelded[i];

                        if (cloth.IsParticleActive(actorIndex))
                        {
                            UpdateActiveVertex(solver, actorIndex, i);
                        }
                        else
                        {
                            UpdateInactiveVertex(solver, actorIndex, i);
                        }
                    }

                    SetClothMeshData();
                    clothMesh.RecalculateBounds();

                    if (tangentSpaceUpdate == TangentSpaceUpdateMode.RecalculateNormalsFromMesh ||
                        tangentSpaceUpdate == TangentSpaceUpdateMode.RecalculateNormalsAndTangentsFromMesh)
                        clothMesh.RecalculateNormals();
                    if (tangentSpaceUpdate == TangentSpaceUpdateMode.RecalculateNormalsAndTangentsFromMesh)
                        clothMesh.RecalculateTangents();

                    if (OnRendererUpdated != null)
                        OnRendererUpdated(actor);
                }
            }
        }
    }
}