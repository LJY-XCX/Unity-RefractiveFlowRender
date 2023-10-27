using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace Obi
{
    public static class FluidRenderingUtils
    {
        [System.Serializable]
        public class FluidRendererSettings
        {
            public BlendMode blendSource = BlendMode.SrcAlpha;
            public BlendMode blendDestination = BlendMode.OneMinusSrcAlpha;

            public BlendMode particleBlendSource = BlendMode.DstColor;
            public BlendMode particleBlendDestination = BlendMode.Zero;
            public bool particleZWrite = false;

            [Range(0.01f, 5)]
            public float thicknessCutoff = 1.2f;
            [Range(1, 4)]
            public int thicknessDownsample = 2;

            // surface reconstruction (depth and normals)
            public bool generateSurface = true;
            [Range(0, 0.1f)]
            public float blurRadius = 0.02f;
            [Range(1, 4)]
            public int surfaceDownsample = 1;

            // lighting
            public bool lighting = true;
            [Range(0, 1)]
            public float smoothness = 0.8f;
            [Range(0, 1)]
            public float metalness = 0;
            [Range(0, 6)]
            public float ambientMultiplier = 1;

            // reflection
            public bool generateReflection = true;
            [Range(0, 1)]
            public float reflection = 0.2f;

            // refraction
            public bool generateRefraction = true;
            [Range(0, 1)]
            public float transparency = 1;
            [Range(0, 30)]
            public float absorption = 5;
            [Range(-0.1f, 0.1f)]
            public float refraction = 0.01f;
            [Range(1, 4)]
            public int refractionDownsample = 1;

            // foam
            public bool generateFoam = true;
            [Range(1, 4)]
            public int foamDownsample = 1;
        }

        public struct FluidRenderTargets
        {
            public int refraction;
            public int foam;
            public int depth;
            public int thickness1;
            public int thickness2;
            public int smoothDepth;
            public int normals;
        }

        public static Color thicknessBufferClear = new Color(1, 1, 1, 0); /**< clears alpha to black (0 thickness) and color to white.*/

        public static Material CreateMaterial(Shader shader)
        {
            if (!shader || !shader.isSupported)
                return null;
            Material m = new Material(shader);
            m.hideFlags = HideFlags.HideAndDontSave;
            return m;
        }

        public static float SetupFluidCamera(Camera cam)
        {
            Shader.SetGlobalMatrix("_Camera_to_World", cam.cameraToWorldMatrix);
            Shader.SetGlobalMatrix("_World_to_Camera", cam.worldToCameraMatrix);
            Shader.SetGlobalMatrix("_InvProj", cam.projectionMatrix.inverse);

            float fovY = cam.fieldOfView;
            float far = cam.farClipPlane;
            float y = cam.orthographic ? 2 * cam.orthographicSize : 2 * Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f) * far;
            float x = y * cam.aspect;
            Shader.SetGlobalVector("_FarCorner", new Vector3(x, y, far));

            return cam.orthographic ? 1 : cam.pixelWidth / cam.aspect * (1.0f / Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f));
        }

        public static void SurfaceReconstruction(CommandBuffer cmd,
                                                 FluidRenderTargets renderTargets,
                                                 Material depth_BlurMaterial,
                                                 Material normal_ReconstructMaterial,
                                                 ObiParticleRenderer[] renderers)
        {
            // draw fluid depth texture:
            foreach (ObiParticleRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    foreach (Mesh mesh in renderer.ParticleMeshes)
                    {
                        if (renderer.ParticleMaterial != null)
                            cmd.DrawMesh(mesh, Matrix4x4.identity, renderer.ParticleMaterial, 0, 0);
                    }
                }
            }

            cmd.SetGlobalTexture("_FluidSurface", renderTargets.smoothDepth);
            cmd.SetGlobalTexture("_Normals", renderTargets.normals);
        }

        public static void VolumeReconstruction(CommandBuffer cmd,
                                                FluidRenderTargets renderTargets,
                                                Material thickness_Material,
                                                Material colorMaterial,
                                                ObiParticleRenderer[] renderers)
        {
            // Draw fluid thickness and color:
            foreach (ObiParticleRenderer renderer in renderers)
            {
                if (renderer != null)
                {

                    cmd.SetGlobalColor("_ParticleColor", renderer.particleColor);
                    cmd.SetGlobalFloat("_RadiusScale", renderer.radiusScale);

                    foreach (Mesh mesh in renderer.ParticleMeshes)
                    {
                        cmd.DrawMesh(mesh, Matrix4x4.identity, thickness_Material, 0, 0);
                        cmd.DrawMesh(mesh, Matrix4x4.identity, colorMaterial, 0, 0);
                    }
                }
            }
        }

        public static void Foam(CommandBuffer cmd, FluidRenderTargets renderTargets, ObiParticleRenderer[] renderers)
        {

            foreach (ObiParticleRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    ObiFoamGenerator foamGenerator = renderer.GetComponent<ObiFoamGenerator>();
                    if (foamGenerator != null && foamGenerator.advector != null && foamGenerator.advector.Particles != null)
                    {
                        ParticleSystemRenderer psRenderer = foamGenerator.advector.Particles.GetComponent<ParticleSystemRenderer>();
                        if (psRenderer != null)
                            cmd.DrawRenderer(psRenderer, psRenderer.material);
                    }
                }
            }

            cmd.SetGlobalTexture("_Foam", renderTargets.foam);
        }

        // Only used in built-in pipeline:
        public static void Refraction(CommandBuffer cmd, RenderTargetIdentifier cameraTarget, FluidRenderTargets renderTargets)
        {
            cmd.Blit(cameraTarget, renderTargets.refraction);
            cmd.SetGlobalTexture("_CameraOpaqueTexture", renderTargets.refraction);
        }

    }

}