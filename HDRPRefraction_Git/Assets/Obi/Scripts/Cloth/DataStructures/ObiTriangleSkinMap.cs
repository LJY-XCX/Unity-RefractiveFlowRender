using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

    [CreateAssetMenu(fileName = "triangle skinmap", menuName = "Obi/Triangle Skinmap", order = 123)]
    public class ObiTriangleSkinMap : ScriptableObject
    {

        private class MasterFace
        {
            public Vector3 p1;
            public Vector3 p2;
            public Vector3 p3;

            public Vector3 n1;
            public Vector3 n2;
            public Vector3 n3;

            private Vector3 v0;
            private Vector3 v1;
            private float dot00;
            private float dot01;
            private float dot11;

            public Vector3 faceNormal;
            public float size;

            public int index;
            public uint master;

            public void CacheBarycentricData()
            {
                v0 = p3 - p1;
                v1 = p2 - p1;
                dot00 = Vector3.Dot(v0, v0);
                dot01 = Vector3.Dot(v0, v1);
                dot11 = Vector3.Dot(v1, v1);
            }

            public bool BarycentricCoords(Vector3 point, ref Vector3 coords)
            {
                // Compute vectors
                Vector3 v2 = point - p1;

                // Compute dot products
                float dot02 = Vector3.Dot(v0, v2);
                float dot12 = Vector3.Dot(v1, v2);

                // Compute barycentric coordinates
                float det = dot00 * dot11 - dot01 * dot01;
                if (!Mathf.Approximately(det, 0))
                {
                    float u = (dot11 * dot02 - dot01 * dot12) / det;
                    float v = (dot00 * dot12 - dot01 * dot02) / det;
                    coords = new Vector3(1 - u - v, v, u);
                    return true;
                }
                return false;
            }
        }

        [Serializable]
        public struct SkinTransform
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public SkinTransform(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }

            public SkinTransform(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
            }

            public void Apply(Transform transform)
            {
                transform.position = position;
                transform.rotation = rotation;
                transform.localScale = scale;
            }

            public Matrix4x4 GetMatrix4X4()
            {
                return Matrix4x4.TRS(position, rotation, scale);
            }

            public void Reset()
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                scale = Vector3.one;
            }
        }

        [Serializable]
        public struct BarycentricPoint
        {
            public Vector3 barycentricCoords;
            public float height;

            public static BarycentricPoint zero
            {
                get
                {
                    return new BarycentricPoint(Vector3.zero, 0);
                }
            }

            public BarycentricPoint(Vector3 position, float height)
            {
                this.barycentricCoords = position;
                this.height = height;
            }
        }

        [Serializable]
        public class SlaveVertex
        {
            public int slaveIndex;
            public int masterTriangleIndex;
            public BarycentricPoint position;
            public BarycentricPoint normal;
            public BarycentricPoint tangent;

            public static SlaveVertex empty
            {
                get
                {
                    return new SlaveVertex(-1, -1, BarycentricPoint.zero, BarycentricPoint.zero, BarycentricPoint.zero);
                }
            }

            public bool isEmpty
            {
                get { return slaveIndex < 0 || masterTriangleIndex < 0; }
            }

            public SlaveVertex(int slaveIndex, int masterTriangleIndex, BarycentricPoint position, BarycentricPoint normal, BarycentricPoint tangent)
            {
                this.slaveIndex = slaveIndex;
                this.masterTriangleIndex = masterTriangleIndex;
                this.position = position;
                this.normal = normal;
                this.tangent = tangent;
            }
        }

        [HideInInspector] public bool bound = false;

        [Range(0, 1)]
        [HideInInspector] public float barycentricWeight = 1;

        [Range(0, 1)]
        [HideInInspector] public float normalAlignmentWeight = 1;

        [Range(0, 1)]
        [HideInInspector] public float elevationWeight = 1;

        // channels:
        [HideInInspector] public uint[] m_MasterChannels;
        [HideInInspector] public uint[] m_SlaveChannels;

        // slave transform:
        [HideInInspector] public SkinTransform m_SlaveTransform = new SkinTransform(Vector3.zero, Quaternion.identity, Vector3.one);

        // master blueprint and slave mesh:
        [HideInInspector] public ObiClothBlueprintBase m_Master;
        [HideInInspector] public Mesh m_Slave;

        // skinmap data (list of slave mesh vertices)
        [HideInInspector] public List<SlaveVertex> skinnedVertices = new List<SlaveVertex>();                     /**< skin info for all skinned vertices. */

        public ObiClothBlueprintBase master
        {
            set
            {
                if (value != m_Master)
                {
                    m_Master = value;
                    ValidateMasterChannels(true);
                }
            }
            get { return m_Master; }
        }

        public Mesh slave
        {
            set
            {
                if (value != m_Slave)
                {
                    m_Slave = value;
                    m_SlaveTransform = new SkinTransform(Vector3.zero, Quaternion.identity, Vector3.one);
                    ValidateSlaveChannels(true);
                }
            }
            get { return m_Slave; }
        }

        public void OnEnable()
        {
            ValidateMasterChannels(false);
            ValidateSlaveChannels(false);
        }

        public void Clear()
        {
            skinnedVertices.Clear();
            bound = false;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void ValidateMasterChannels(bool clearChannels)
        {
            if (m_Master != null)
            {
                if (m_MasterChannels == null || m_MasterChannels.Length != m_Master.activeParticleCount)
                {
                    Array.Resize(ref m_MasterChannels, m_Master.activeParticleCount);

                    if (clearChannels)
                    {
                        for (int i = 0; i < m_MasterChannels.Length; ++i)
                            m_MasterChannels[i] = 0x00000001;
                    }
                }
            }
            else
                m_MasterChannels = null;
        }

        public void ValidateSlaveChannels(bool clearChannels)
        {
            if (m_Slave != null)
            {
                if (m_SlaveChannels == null || m_SlaveChannels.Length != m_Slave.vertexCount)
                {
                    Array.Resize(ref m_SlaveChannels, m_Slave.vertexCount);

                    if (clearChannels)
                    {
                        for (int i = 0; i < m_SlaveChannels.Length; ++i)
                            m_SlaveChannels[i] = 0x00000001;
                    }
                }
            }
            else
                m_SlaveChannels = null;
        }

        public void CopyChannel(uint[] channels, int source, int dest)
        {
            int shift = source - dest;
            uint destMask = (uint)(1 << dest);

            for (int i = 0; i < channels.Length; ++i)
            {
                // move bit from source to destination:
                uint copy = (shift > 0) ? (uint)(channels[i] >> shift) : (uint)(channels[i] << Mathf.Abs(shift));

                // clear destination bit and or with displaced source bit:
                channels[i] = (channels[i] & ~destMask) | (copy & destMask);
            }
        }

        public void FillChannel(uint[] channels, int channel)
        {
            for (int i = 0; i < channels.Length; ++i)
                channels[i] |= (uint)(1 << channel);
        }

        public void ClearChannel(uint[] channels, int channel)
        {
            for (int i = 0; i < channels.Length; ++i)
                channels[i] &= ~(uint)(1 << channel);
        }

        private bool BindToFace(int slaveIndex,
                                MasterFace triangle,
                                Vector3 position,
                                Vector3 normalPoint,
                                Vector3 tangentPoint,
                                out SlaveVertex skinning)
        {
            skinning = SlaveVertex.empty;

            BarycentricPoint posBary;
            if (FindSkinBarycentricCoords(triangle, position, 24, 0.001f, out posBary))
            {
                BarycentricPoint normBary;
                BarycentricPoint tangentBary;
                FindSkinBarycentricCoords(triangle, normalPoint, 16, 0.005f, out normBary);
                FindSkinBarycentricCoords(triangle, tangentPoint, 16, 0.005f, out tangentBary);

                skinning = new SlaveVertex(slaveIndex, triangle.index, posBary, normBary, tangentBary);

                return true;
            }
            return false;
        }

        private float GetBarycentricError(Vector3 bary)
        {
            Vector3 error = bary - Vector3.one * 0.5f;
            error[0] = Mathf.Max(Mathf.Abs(error[0]) - 0.5f, 0);
            error[1] = Mathf.Max(Mathf.Abs(error[1]) - 0.5f, 0);
            error[2] = Mathf.Max(Mathf.Abs(error[2]) - 0.5f, 0);
            return error.sqrMagnitude;
        }

        private float GetFaceMappingError(MasterFace triangle,
                                          SlaveVertex vertex,
                                          Vector3 normal)
        {
            // initialize the error with barycentric coords error (larger the further away we are from the triangle's limits):
            float bary_error = GetBarycentricError(vertex.position.barycentricCoords) * barycentricWeight;
            float error = bary_error;

            // calculate deviation of point normal from face normal, use it to weight normal barycentric error:
            float normal_deviation = Mathf.Clamp01(0.5f * (1.0f - Mathf.Abs(Vector3.Dot(triangle.faceNormal, normal))));
            error += normal_deviation * GetBarycentricError(vertex.normal.barycentricCoords) * normalAlignmentWeight;

            // make height relative to triangle size, and calculate error weight based on barycentric error:
            float height_val = vertex.position.height / Mathf.Max(0.0001f, triangle.size);
            float height_w = 0.3f + 2.5f * bary_error;
            error += height_w * Mathf.Abs(height_val) * elevationWeight;

            return error;
        }

        /**  
         * We need to find the barycentric coordinates of point such that the interpolated normal at that point passes trough our target position.
         *
         *            X
         *  \        /  /
         *   \------/--/
         *
         * This is necessary to ensure curvature changes in the surface affect skinned points away from the face plane.
         * To do so, we use an iterative method similar to Newton´s method for root finding:
         *
         * - Project the point on the triangle using an initial normal.
         * - Get interpolated normal at projection.
         * - Intersect line from point and interpolated normal with triangle, to find a new projection.
         * - Repeat.
         */
        bool FindSkinBarycentricCoords(MasterFace triangle,
                                       Vector3 position,
                                       int max_iterations,
                                       float min_convergence,
                                       out BarycentricPoint barycentricPoint)
        {
            barycentricPoint = BarycentricPoint.zero;

            // start at center of triangle:
            Vector3 trusted_bary = Vector3.one / 3.0f;
            Vector3 temp_normal;
            ObiUtils.BarycentricInterpolation(in triangle.n1,
                                              in triangle.n2,
                                              in triangle.n3,
                                              in trusted_bary,
                                              out temp_normal);

            int it = 0;
            float trust = 1.0f;
            float convergence = float.MaxValue;
            while (it++ < max_iterations)
            {
                Vector3 point;
                if (!Obi.ObiUtils.LinePlaneIntersection(triangle.p1, triangle.faceNormal, position, temp_normal, out point))
                    return false;

                // get bary coords at intersection:
                Vector3 bary = Vector3.zero;
                if (!triangle.BarycentricCoords(point, ref bary))
                    break;

                // calculate error:
                Vector3 error = bary - trusted_bary;     // distance from current estimation to last trusted estimation.
                convergence = Vector3.Dot(error, error); // get a single convergence value.

                // weighted sum of bary coords:
                trusted_bary = (1.0f - trust) * trusted_bary + trust * bary;

                // do we still maintain the barycentric invariant?
                if (Mathf.Abs(1.0f - (trusted_bary.x + trusted_bary.y + trusted_bary.z)) > 0.001f)
                    return false;

                // update normal
                ObiUtils.BarycentricInterpolation(in triangle.n1,
                                                  in triangle.n2,
                                                  in triangle.n3,
                                                  in trusted_bary,
                                                  out temp_normal);

                if (convergence < min_convergence)
                    break;

                trust *= 0.8f;
            }

            Vector3 pos_on_tri = trusted_bary[0] * triangle.p1 +
                                 trusted_bary[1] * triangle.p2 +
                                 trusted_bary[2] * triangle.p3;

            float height = Vector3.Dot(position - pos_on_tri, temp_normal);

            barycentricPoint.barycentricCoords = trusted_bary;
            barycentricPoint.height = height;

            return convergence < min_convergence;

        }

        public IEnumerator Bind()
        {
            Clear();

            if (master == null || slave == null)
                yield break;

            Vector3[] slavePositions = slave.vertices;
            Vector3[] slaveNormals = slave.normals;
            Vector4[] slaveTangents = slave.tangents;

            Matrix4x4 s2world = m_SlaveTransform.GetMatrix4X4();
            Matrix4x4 s2worldNormal = s2world.inverse.transpose;

            // count active triangles normals:
            int activeDeformableTriangles = 0;
            for (int i = 0; i < master.deformableTriangles.Length; i += 3)
            {
                int t1 = master.deformableTriangles[i];
                int t2 = master.deformableTriangles[i + 1];
                int t3 = master.deformableTriangles[i + 2];

                if (t1 >= master.activeParticleCount || t2 >= master.activeParticleCount || t3 >= master.activeParticleCount)
                    continue;

                activeDeformableTriangles++;
            }


            // generate master triangle info:
            MasterFace[] masterFaces = new MasterFace[activeDeformableTriangles];
            int count = 0;
            for (int i = 0; i < master.deformableTriangles.Length; i += 3)
            {
                MasterFace face = new MasterFace();

                int t1 = master.deformableTriangles[i];
                int t2 = master.deformableTriangles[i + 1];
                int t3 = master.deformableTriangles[i + 2];

                if (t1 >= master.activeParticleCount || t2 >= master.activeParticleCount || t3 >= master.activeParticleCount)
                    continue;

                face.p1 = master.positions[t1];
                face.p2 = master.positions[t2];
                face.p3 = master.positions[t3];

                face.n1 = master.restNormals[t1];
                face.n2 = master.restNormals[t2];
                face.n3 = master.restNormals[t3];

                face.master = m_MasterChannels[t1] |
                              m_MasterChannels[t2] |
                              m_MasterChannels[t3];

                face.size = ((face.p1 - face.p2).magnitude +
                             (face.p1 - face.p3).magnitude +
                             (face.p2 - face.p3).magnitude) / 3.0f;

                face.faceNormal = Vector3.Cross(face.p2 - face.p1, face.p3 - face.p1).normalized;

                face.index = i;
                face.CacheBarycentricData();

                masterFaces[count++] = face;

                if (i % 10 == 0)
                    yield return new CoroutineJob.ProgressInfo("Generating master faces...", count / (float)masterFaces.Length);
            }

            // for each slave vertex, find the best fitting master triangle:
            for (int i = 0; i < slavePositions.Length; ++i)
            {
                // if vertex slave channel is deactivated, don´t skin it.
                if (m_SlaveChannels[i] == 0) continue;

                // initialize best triangle error and index:
                float bestError = float.MaxValue;
                SlaveVertex bestSkinning = SlaveVertex.empty;

                Vector3 worldPos = s2world.MultiplyPoint3x4(slavePositions[i]);
                Vector3 worldNormalDir = s2worldNormal.MultiplyVector(slaveNormals[i]).normalized;
                Vector3 worldNormalPoint = worldPos + worldNormalDir * 0.05f;
                Vector3 worldTangentPoint = worldPos + s2worldNormal.MultiplyVector(new Vector3(slaveTangents[i].x, slaveTangents[i].y, slaveTangents[i].z).normalized) * 0.05f;

                // find the best fitting master face:
                for (int j = 0; j < masterFaces.Length; ++j)
                {
                    MasterFace face = masterFaces[j];

                    // if the triangle master channel and the target slave channel do not overlap, skip it.
                    if ((face.master & m_SlaveChannels[i]) == 0)
                        continue;

                    // calculate skinning data for this face:
                    SlaveVertex skinning;
                    if (BindToFace(i, face, worldPos, worldNormalPoint, worldTangentPoint, out skinning))
                    {
                        // calculate mapping error for this triangle:
                        float error = GetFaceMappingError(face, skinning, worldNormalDir);

                        // if the error is less than the best, update it.
                        if (error < bestError)
                        {
                            bestError = error;
                            bestSkinning = skinning;
                        }
                    }
                }

                // skin target vertex to the best source triangle found, if any:
                if (!bestSkinning.isEmpty)
                    skinnedVertices.Add(bestSkinning);

                if (i % 5 == 0)
                    yield return new CoroutineJob.ProgressInfo("Skinning slave vertices (" + i + "/" + slavePositions.Length + ")...", i / (float)slavePositions.Length);
            }

            bound = true;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

    }
}


